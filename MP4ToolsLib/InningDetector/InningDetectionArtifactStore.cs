using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>Runtime toggle for keeping inning-detection debug packages on disk.</summary>
public static class InningDetectionArtifactRuntime
{
	/// <summary>When true (default), detection writes a named session folder + Cursor handoff file.</summary>
	public static bool Enabled { get; private set; } = true;

	public static void Apply(bool? enabled) =>
		Enabled = enabled ?? true;
}

/// <summary>
/// One on-disk package of frames, maps, options, and events for refining half-inning detection.
/// Folder name includes the video stem so sessions are easy to match to a game.
/// </summary>
public sealed class InningDetectionArtifactSession
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public string SessionDirectory { get; }
	public string VideoPath { get; }
	public string Mode { get; }
	public string FramesDirectory => Path.Combine(SessionDirectory, "frames");
	public string RefineDirectory => Path.Combine(SessionDirectory, "refine");
	public string HandoffMarkdownPath => Path.Combine(SessionDirectory, "CURSOR_REFINE.md");
	public string SessionJsonPath => Path.Combine(SessionDirectory, "session.json");
	public string SamplesCsvPath => Path.Combine(SessionDirectory, "samples.csv");
	public string DetectorOptionsPath => Path.Combine(SessionDirectory, "detector-options.json");
	public string CombineMapCopyPath => Path.Combine(SessionDirectory, "combine-map.json");
	public string EventsGamePath => Path.Combine(SessionDirectory, "events-game.json");
	public string EventsCombinedPath => Path.Combine(SessionDirectory, "events-combined.json");
	public string StatusLogPath => Path.Combine(SessionDirectory, "status.log");

	private readonly List<string> _statusLines = new();
	private readonly object _statusLock = new();

	private InningDetectionArtifactSession(string sessionDirectory, string videoPath, string mode)
	{
		SessionDirectory = sessionDirectory;
		VideoPath = videoPath;
		Mode = mode;
	}

	public static InningDetectionArtifactSession Create(string videoPath, string mode)
	{
		if (string.IsNullOrWhiteSpace(videoPath))
			throw new ArgumentException("Video path is required.", nameof(videoPath));

		var stem = SanitizeStem(Path.GetFileNameWithoutExtension(videoPath));
		if (string.IsNullOrWhiteSpace(stem))
			stem = "video";

		var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
		var root = Path.Combine(TempPathHelper.GetTempPath(), "inning_detect");
		Directory.CreateDirectory(root);

		var dir = Path.Combine(root, $"{stem}_{stamp}");
		var suffix = 0;
		while (Directory.Exists(dir))
		{
			suffix++;
			dir = Path.Combine(root, $"{stem}_{stamp}_{suffix}");
		}

		Directory.CreateDirectory(dir);
		Directory.CreateDirectory(Path.Combine(dir, "frames"));
		Directory.CreateDirectory(Path.Combine(dir, "refine"));

		return new InningDetectionArtifactSession(dir, Path.GetFullPath(videoPath), mode ?? "detect");
	}

	public void AppendStatus(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
			return;
		var line = $"[{DateTime.Now:HH:mm:ss}] {message.Trim()}";
		lock (_statusLock)
			_statusLines.Add(line);
		try
		{
			File.AppendAllText(StatusLogPath, line + Environment.NewLine);
		}
		catch
		{
			// ignored
		}
	}

	public Action<string> WrapStatus(Action<string> inner)
	{
		return message =>
		{
			AppendStatus(message);
			inner?.Invoke(message);
		};
	}

	public string CreateRefineWindowDirectory(int halfIndex, double coarseHitSeconds)
	{
		var name =
			$"half_{halfIndex:00}_{FormatFileTime(coarseHitSeconds)}";
		var dir = Path.Combine(RefineDirectory, name);
		Directory.CreateDirectory(dir);
		return dir;
	}

	public async Task WriteSamplesCsvAsync(
		IReadOnlyList<HalfInningDetector.TimedSampleFrame> samples,
		CancellationToken ct = default)
	{
		var sb = new StringBuilder();
		sb.AppendLine("index,elapsedSeconds,elapsed,path");
		for (var i = 0; i < samples.Count; i++)
		{
			ct.ThrowIfCancellationRequested();
			var s = samples[i];
			sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(s.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
				.Append(FormatElapsed(s.ElapsedSeconds)).Append(',')
				.Append(EscapeCsv(s.Path))
				.AppendLine();
		}

		await File.WriteAllTextAsync(SamplesCsvPath, sb.ToString(), ct).ConfigureAwait(false);
	}

	public async Task WriteDetectorOptionsAsync(HalfInningDetector.Options options, CancellationToken ct = default)
	{
		options ??= new HalfInningDetector.Options();
		var payload = new
		{
			options.SampleIntervalSeconds,
			options.RefineHalfInningHits,
			options.RefineIntervalSeconds,
			options.RefineLookbackSeconds,
			options.RefineLookaheadSeconds,
			options.AnalysisWidth,
			options.ConfidenceThreshold,
			options.RoleModelConfidenceFloor,
			options.RoleHitterConfidence,
			options.RoleCatcherConfidence,
			options.RolePitcherConfidence,
			options.SkipIntroTitleCards,
			options.AssumeTopFirstAtGameStart,
			options.WarmupHoldSeconds,
			options.FirstHalfWarmupHoldSeconds,
			options.EmptyFieldHoldSeconds,
			options.MinSecondsBetweenHalfInnings,
			options.RoleEmptyHoldSeconds,
			options.RoleEmptyBeforeHitterSeconds,
			options.RoleNoHitterGapSeconds,
			options.RoleMinSecondsBetweenHalfInnings,
			options.RoleMinHalfInningsBeforeGameOverStop,
			options.RoleGameOverEmptySeconds,
			options.UseOnDeckInProgressSignal,
			options.MinFieldersPlaying,
			options.MaxFieldersEmpty,
			options.IntroConfirmSamples,
			BatterBoxRoi = RectToAnon(options.BatterBoxRoi),
			BatterApproachRoi = RectToAnon(options.BatterApproachRoi),
			OnDeckLeftRoi = RectToAnon(options.OnDeckLeftRoi),
			OnDeckRightRoi = RectToAnon(options.OnDeckRightRoi),
			FieldRoi = RectToAnon(options.FieldRoi),
		};
		var json = JsonSerializer.Serialize(payload, JsonOptions);
		await File.WriteAllTextAsync(DetectorOptionsPath, json, ct).ConfigureAwait(false);
	}

	public async Task WriteCombineMapCopyAsync(CombineEditMap map, CancellationToken ct = default)
	{
		if (map == null)
			return;
		var json = JsonSerializer.Serialize(map, JsonOptions);
		await File.WriteAllTextAsync(CombineMapCopyPath, json, ct).ConfigureAwait(false);
	}

	public async Task WriteEventsAsync(
		string path,
		IReadOnlyList<InningDetectionEvent> events,
		CancellationToken ct = default)
	{
		var payload = (events ?? Array.Empty<InningDetectionEvent>()).Select(e => new
		{
			e.ElapsedSeconds,
			Elapsed = FormatElapsed(e.ElapsedSeconds),
			e.Label,
			e.Kind,
		});
		var json = JsonSerializer.Serialize(payload, JsonOptions);
		await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
	}

	public async Task FinalizeAsync(
		InningDetectionArtifactManifest manifest,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		manifest ??= new InningDetectionArtifactManifest();
		manifest.SessionDirectory = SessionDirectory;
		manifest.HandoffMarkdownPath = HandoffMarkdownPath;
		manifest.SessionJsonPath = SessionJsonPath;
		manifest.VideoPath = VideoPath;
		manifest.Mode = Mode;
		manifest.FramesDirectory = FramesDirectory;
		manifest.RefineDirectory = RefineDirectory;
		manifest.SamplesCsvPath = File.Exists(SamplesCsvPath) ? SamplesCsvPath : null;
		manifest.DetectorOptionsPath = File.Exists(DetectorOptionsPath) ? DetectorOptionsPath : null;
		manifest.CombineMapCopyPath = File.Exists(CombineMapCopyPath) ? CombineMapCopyPath : null;
		manifest.EventsGamePath = File.Exists(EventsGamePath) ? EventsGamePath : null;
		manifest.EventsCombinedPath = File.Exists(EventsCombinedPath) ? EventsCombinedPath : null;
		manifest.StatusLogPath = File.Exists(StatusLogPath) ? StatusLogPath : null;
		manifest.CreatedLocal = DateTimeOffset.Now;

		var json = JsonSerializer.Serialize(manifest, JsonOptions);
		await File.WriteAllTextAsync(SessionJsonPath, json, ct).ConfigureAwait(false);
		await File.WriteAllTextAsync(HandoffMarkdownPath, BuildHandoffMarkdown(manifest), ct).ConfigureAwait(false);

		log?.Invoke($"Inning-detect debug package kept: {HandoffMarkdownPath}");
		log?.Invoke($"Point Cursor at: {HandoffMarkdownPath}");
	}

	private static string BuildHandoffMarkdown(InningDetectionArtifactManifest m)
	{
		var sb = new StringBuilder();
		sb.AppendLine("# Inning detection refine handoff");
		sb.AppendLine();
		sb.AppendLine("Point Cursor at **this file** (or its folder) to refine half-inning detection modeling.");
		sb.AppendLine();
		sb.AppendLine("## Quick start for Cursor");
		sb.AppendLine();
		sb.AppendLine("```");
		sb.AppendLine($"Read {m.HandoffMarkdownPath} and {m.SessionJsonPath}, then help refine HalfInningDetector thresholds / state machine using the saved frames and events.");
		sb.AppendLine("```");
		sb.AppendLine();
		sb.AppendLine("## Video / mode");
		sb.AppendLine();
		sb.AppendLine($"- **Mode:** `{m.Mode}`");
		sb.AppendLine($"- **Video:** `{m.VideoPath}`");
		if (!string.IsNullOrWhiteSpace(m.CombineMapPath))
			sb.AppendLine($"- **Combine map (source):** `{m.CombineMapPath}`");
		sb.AppendLine($"- **Game-content duration:** {m.GameDurationSeconds:0.###}s ({FormatElapsed(m.GameDurationSeconds)})");
		sb.AppendLine($"- **Output duration (with intro):** {m.OutputDurationSeconds:0.###}s ({FormatElapsed(m.OutputDurationSeconds)})");
		sb.AppendLine($"- **Intro offset:** {m.IntroOffsetSeconds:0.###}s");
		sb.AppendLine($"- **Sample interval:** {m.SampleIntervalSeconds:0.###}s");
		if (!string.IsNullOrWhiteSpace(m.ModelPath))
			sb.AppendLine($"- **ONNX model:** `{m.ModelPath}` ({m.ModelKind ?? "?"})");
		sb.AppendLine();
		sb.AppendLine("## Artifact paths");
		sb.AppendLine();
		sb.AppendLine($"| What | Path |");
		sb.AppendLine($"| --- | --- |");
		sb.AppendLine($"| Session folder | `{m.SessionDirectory}` |");
		sb.AppendLine($"| This handoff | `{m.HandoffMarkdownPath}` |");
		sb.AppendLine($"| Machine manifest | `{m.SessionJsonPath}` |");
		if (!string.IsNullOrWhiteSpace(m.FramesDirectory))
			sb.AppendLine($"| Sample frames | `{m.FramesDirectory}` |");
		if (!string.IsNullOrWhiteSpace(m.SamplesCsvPath))
			sb.AppendLine($"| Sample index (time→file) | `{m.SamplesCsvPath}` |");
		if (!string.IsNullOrWhiteSpace(m.RefineDirectory))
			sb.AppendLine($"| Refine windows | `{m.RefineDirectory}` |");
		if (!string.IsNullOrWhiteSpace(m.DetectorOptionsPath))
			sb.AppendLine($"| Detector options | `{m.DetectorOptionsPath}` |");
		if (!string.IsNullOrWhiteSpace(m.CombineMapCopyPath))
			sb.AppendLine($"| Combine map copy | `{m.CombineMapCopyPath}` |");
		if (!string.IsNullOrWhiteSpace(m.EventsGamePath))
			sb.AppendLine($"| Events (game timeline) | `{m.EventsGamePath}` |");
		if (!string.IsNullOrWhiteSpace(m.EventsCombinedPath))
			sb.AppendLine($"| Events (combined + intro) | `{m.EventsCombinedPath}` |");
		if (!string.IsNullOrWhiteSpace(m.RecordingJsonPath))
			sb.AppendLine($"| Recording JSON | `{m.RecordingJsonPath}` |");
		if (!string.IsNullOrWhiteSpace(m.YoutubeDescriptionPath))
			sb.AppendLine($"| YouTube description | `{m.YoutubeDescriptionPath}` |");
		if (!string.IsNullOrWhiteSpace(m.StatusLogPath))
			sb.AppendLine($"| Status log | `{m.StatusLogPath}` |");
		sb.AppendLine();

		if (m.LrfPaths is { Count: > 0 })
		{
			sb.AppendLine("## LRF proxies");
			sb.AppendLine();
			for (var i = 0; i < m.LrfPaths.Count; i++)
				sb.AppendLine($"- clip {i}: `{m.LrfPaths[i]}`");
			sb.AppendLine();
		}

		if (m.Segments is { Count: > 0 })
		{
			sb.AppendLine("## Game-content segments");
			sb.AppendLine();
			sb.AppendLine("| Clip | Local start | Contribution | Game start | Media |");
			sb.AppendLine("| --- | ---: | ---: | ---: | --- |");
			foreach (var seg in m.Segments)
			{
				sb.Append("| ").Append(seg.ClipIndex)
					.Append(" | ").Append(seg.LocalStartSeconds.ToString("0.###", CultureInfo.InvariantCulture))
					.Append(" | ").Append(seg.ContributionSeconds.ToString("0.###", CultureInfo.InvariantCulture))
					.Append(" | ").Append(seg.GameStartSeconds.ToString("0.###", CultureInfo.InvariantCulture))
					.Append(" | `").Append(seg.MediaPath).Append("` |")
					.AppendLine();
			}

			sb.AppendLine();
		}

		if (m.HalfInningEvents is { Count: > 0 })
		{
			sb.AppendLine("## Detected half-innings (combined timeline)");
			sb.AppendLine();
			foreach (var ev in m.HalfInningEvents)
			{
				sb.Append("- ")
					.Append(FormatElapsed(ev.ElapsedSeconds))
					.Append(" (")
					.Append(ev.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture))
					.Append("s) — ")
					.Append(ev.Label)
					.AppendLine();
			}

			sb.AppendLine();
		}

		sb.AppendLine("## Suggested refinement workflow");
		sb.AppendLine();
		sb.AppendLine("1. Open `samples.csv` and jump to JPEGs near suspected half-inning transitions.");
		sb.AppendLine("2. Compare `events-combined.json` (or recording JSON) to ground-truth times.");
		sb.AppendLine("3. Inspect `refine/half_XX_*` folders for dense samples around coarse hits.");
		sb.AppendLine("4. Tune `HalfInningDetector.Options` / role thresholds using `detector-options.json` as the baseline.");
		sb.AppendLine("5. Re-run LRF inning detect and diff the new session folder against this one.");
		sb.AppendLine();
		sb.AppendLine($"Created: {m.CreatedLocal:yyyy-MM-dd HH:mm:ss zzz}");
		return sb.ToString();
	}

	private static object RectToAnon(OpenCvSharp.Rect2d r) => new { r.X, r.Y, r.Width, r.Height };

	private static string SanitizeStem(string stem)
	{
		if (string.IsNullOrWhiteSpace(stem))
			return "video";
		var invalid = Path.GetInvalidFileNameChars();
		var chars = stem.Trim().Select(ch => invalid.Contains(ch) || ch is ' ' or '.' ? '_' : ch).ToArray();
		var cleaned = new string(chars).Trim('_');
		while (cleaned.Contains("__", StringComparison.Ordinal))
			cleaned = cleaned.Replace("__", "_", StringComparison.Ordinal);
		return cleaned.Length > 80 ? cleaned[..80].Trim('_') : cleaned;
	}

	private static string FormatFileTime(double totalSeconds)
	{
		var ts = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(totalSeconds)));
		return $"{(int)ts.TotalHours:00}h{ts.Minutes:00}m{ts.Seconds:00}s";
	}

	private static string FormatElapsed(double totalSeconds)
	{
		var ts = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(totalSeconds)));
		return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
	}

	private static string EscapeCsv(string value)
	{
		value ??= "";
		if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
			return "\"" + value.Replace("\"", "\"\"") + "\"";
		return value;
	}
}

public sealed class InningDetectionArtifactManifest
{
	public string Mode { get; set; }
	public string VideoPath { get; set; }
	public string SessionDirectory { get; set; }
	public string HandoffMarkdownPath { get; set; }
	public string SessionJsonPath { get; set; }
	public string FramesDirectory { get; set; }
	public string RefineDirectory { get; set; }
	public string SamplesCsvPath { get; set; }
	public string DetectorOptionsPath { get; set; }
	public string CombineMapPath { get; set; }
	public string CombineMapCopyPath { get; set; }
	public string EventsGamePath { get; set; }
	public string EventsCombinedPath { get; set; }
	public string RecordingJsonPath { get; set; }
	public string YoutubeDescriptionPath { get; set; }
	public string StatusLogPath { get; set; }
	public string ModelPath { get; set; }
	public string ModelKind { get; set; }
	public double SampleIntervalSeconds { get; set; }
	public double GameDurationSeconds { get; set; }
	public double OutputDurationSeconds { get; set; }
	public double IntroOffsetSeconds { get; set; }
	public DateTimeOffset CreatedLocal { get; set; }
	public List<string> LrfPaths { get; set; }
	public List<ArtifactSegmentInfo> Segments { get; set; }
	public List<InningDetectionEvent> HalfInningEvents { get; set; }
}

public sealed class ArtifactSegmentInfo
{
	public int ClipIndex { get; set; }
	public string MediaPath { get; set; }
	public double LocalStartSeconds { get; set; }
	public double ContributionSeconds { get; set; }
	public double GameStartSeconds { get; set; }
}
