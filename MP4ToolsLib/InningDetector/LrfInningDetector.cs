using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>
/// Runs half-inning detection on DJI <c>.LRF</c> proxies using a Combine edit map, then remaps
/// timestamps onto the combined output timeline (intro + start/end trims).
/// </summary>
public static class LrfInningDetector
{
	public sealed class Options
	{
		public HalfInningDetector.Options DetectorOptions { get; set; } = new()
		{
			// LRF proxies do not contain Combine intros / replace-segment cards.
			SkipIntroTitleCards = false,
			SampleIntervalSeconds = 5,
		};
	}

	public static async Task<InningDetectionResult> DetectAsync(
		string combinedVideoPath,
		string lrfFolderPath,
		CancellationToken ct = default,
		Action<string> log = null,
		Action<double> progress01 = null,
		Options options = null,
		string outputJsonPath = null,
		Action<string> status = null)
	{
		if (string.IsNullOrWhiteSpace(combinedVideoPath) || !File.Exists(combinedVideoPath))
			throw new FileNotFoundException("Combined video not found.", combinedVideoPath);
		if (string.IsNullOrWhiteSpace(lrfFolderPath) || !Directory.Exists(lrfFolderPath))
			throw new DirectoryNotFoundException($"LRF folder not found: {lrfFolderPath}");

		void Status(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
				return;
			status?.Invoke(message);
			log?.Invoke(message);
		}

		Status("Loading combine edit map…");
		var map = await CombineEditMapIO.LoadAsync(combinedVideoPath, ct).ConfigureAwait(false);
		Status($"Loaded combine edit map ({map.Clips?.Count ?? 0} clip(s)).");

		Status("Matching .LRF proxies to combine clips…");
		var lrfPaths = ResolveLrfPaths(map, lrfFolderPath, log);
		Status($"Matched {lrfPaths.Count} LRF proxy file(s).");

		return await DetectWithResolvedLrfAsync(
				combinedVideoPath,
				map,
				lrfPaths,
				ct,
				log,
				progress01,
				options,
				outputJsonPath,
				status)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Runs LRF detection using proxies already resolved (e.g. beside Combine source clips).
	/// </summary>
	public static Task<InningDetectionResult> DetectWithResolvedLrfAsync(
		string combinedVideoPath,
		CombineEditMap map,
		IReadOnlyList<string> lrfPaths,
		CancellationToken ct = default,
		Action<string> log = null,
		Action<double> progress01 = null,
		Options options = null,
		string outputJsonPath = null,
		Action<string> status = null)
	{
		if (string.IsNullOrWhiteSpace(combinedVideoPath) || !File.Exists(combinedVideoPath))
			throw new FileNotFoundException("Combined video not found.", combinedVideoPath);
		if (map?.Clips == null || map.Clips.Count == 0)
			throw new InvalidOperationException("Combine edit map has no clips.");
		if (lrfPaths == null || lrfPaths.Count == 0)
			throw new ArgumentException("At least one LRF path is required.", nameof(lrfPaths));

		return DetectCoreAsync(
			combinedVideoPath,
			map,
			lrfPaths,
			ct,
			log,
			progress01,
			options,
			outputJsonPath,
			status);
	}

	private static async Task<InningDetectionResult> DetectCoreAsync(
		string combinedVideoPath,
		CombineEditMap map,
		IReadOnlyList<string> lrfPaths,
		CancellationToken ct,
		Action<string> log,
		Action<double> progress01,
		Options options,
		string outputJsonPath,
		Action<string> status)
	{
		void Status(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
				return;
			status?.Invoke(message);
			log?.Invoke(message);
		}

		options ??= new Options();
		options.DetectorOptions ??= new HalfInningDetector.Options { SkipIntroTitleCards = false };

		var workDir = Path.Combine(TempPathHelper.GetTempPath(), $"lrf_innings_{Guid.NewGuid():N}");
		Directory.CreateDirectory(workDir);

		try
		{
			progress01?.Invoke(0.02);
			var proxyPath = Path.Combine(workDir, "lrf_game_proxy.mp4");
			await BuildLrfGameProxyAsync(map, lrfPaths, proxyPath, ct, log, Status).ConfigureAwait(false);
			progress01?.Invoke(0.15);

			Status("Running half-inning detection on LRF game proxy…");
			var proxyResult = await HalfInningDetector.DetectAsync(
				proxyPath,
				ct,
				log,
				p => progress01?.Invoke(0.15 + 0.75 * Math.Clamp(p, 0, 1)),
				options.DetectorOptions,
				writeOutputFiles: false,
				status: status).ConfigureAwait(false);

			var intro = map.IntroApplied ? Math.Max(0, map.IntroDurationSeconds) : 0;
			var gameDuration = proxyResult.DurationSeconds > 0
				? proxyResult.DurationSeconds
				: map.ComputeGameContentDurationSeconds();
			var outputDuration = intro + gameDuration;

			Status(
				intro > 0
					? $"Remapping bookmarks onto combined timeline (+{intro:0.###}s intro)…"
					: "Remapping bookmarks onto combined timeline…");
			var remapped = RemapEventsToCombinedTimeline(proxyResult.Events, intro, outputDuration);
			var jsonPath = string.IsNullOrWhiteSpace(outputJsonPath)
				? InningDetectionRecordingWriter.BuildUniqueOutputPath(combinedVideoPath)
				: outputJsonPath.Trim();

			Status("Writing recording JSON and YouTube bookmarks…");
			var root = InningDetectionRecordingWriter.BuildSessionJson(
				combinedVideoPath,
				outputDuration,
				remapped);
			var (_, youtubePath) = await InningDetectionRecordingWriter
				.WriteSessionAndYoutubeAsync(root, jsonPath, ct, log)
				.ConfigureAwait(false);

			progress01?.Invoke(1);
			Status(
				$"LRF inning detect complete. Intro offset {intro:0.###}s; wrote {jsonPath}");

			return new InningDetectionResult
			{
				VideoPath = combinedVideoPath,
				OutputJsonPath = jsonPath,
				OutputYoutubeDescriptionPath = youtubePath,
				DurationSeconds = outputDuration,
				Events = remapped,
			};
		}
		finally
		{
			TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(workDir, recursive: true);
		}
	}

	/// <summary>
	/// Looks for <c>{sourceStem}.LRF</c> in the same folder as each combine source clip.
	/// Returns true only when every clip has a matching proxy.
	/// </summary>
	public static bool TryResolveLrfPathsAlongsideSources(
		CombineEditMap map,
		out IReadOnlyList<string> lrfPaths,
		Action<string> log = null)
	{
		lrfPaths = Array.Empty<string>();
		if (map?.Clips == null || map.Clips.Count == 0)
			return false;

		var resolved = new List<string>(map.Clips.Count);
		foreach (var clip in map.Clips.OrderBy(c => c.Index))
		{
			if (string.IsNullOrWhiteSpace(clip.SourceStem))
				return false;

			var dir = !string.IsNullOrWhiteSpace(clip.SourcePath)
				? Path.GetDirectoryName(clip.SourcePath)
				: null;
			if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
			{
				log?.Invoke($"No source folder for clip {clip.Index} ({clip.SourceFileName}); cannot find sibling LRF.");
				return false;
			}

			var lrf = FindLrfBesideSource(dir, clip.SourceStem);
			if (lrf == null)
			{
				log?.Invoke(
					$"No sibling .LRF for clip {clip.Index}: expected {Path.Combine(dir, clip.SourceStem + ".LRF")}");
				return false;
			}

			log?.Invoke($"Matched clip {clip.Index}: {clip.SourceFileName} → {lrf}");
			resolved.Add(lrf);
		}

		lrfPaths = resolved;
		return true;
	}

	public static IReadOnlyList<string> ResolveLrfPaths(
		CombineEditMap map,
		string lrfFolderPath,
		Action<string> log = null)
	{
		if (map?.Clips == null || map.Clips.Count == 0)
			throw new InvalidOperationException("Combine edit map has no clips.");

		// Prefer proxies sitting next to each source clip, then fall back to folder scan.
		if (TryResolveLrfPathsAlongsideSources(map, out var alongside, log: null))
		{
			log?.Invoke($"Using {alongside.Count} .LRF proxy file(s) beside combine source clips.");
			foreach (var (clip, lrf) in map.Clips.OrderBy(c => c.Index).Zip(alongside))
				log?.Invoke($"Matched clip {clip.Index}: {clip.SourceFileName} → {lrf}");
			return alongside;
		}

		var folder = Path.GetFullPath(lrfFolderPath);
		var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
			.Where(p => p.EndsWith(".lrf", StringComparison.OrdinalIgnoreCase))
			.ToList();

		var byStem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in files)
		{
			var stem = Path.GetFileNameWithoutExtension(file);
			if (string.IsNullOrWhiteSpace(stem))
				continue;
			// Prefer shorter/shallower paths if duplicates exist.
			if (!byStem.TryGetValue(stem, out var existing)
				|| file.Length < existing.Length)
			{
				byStem[stem] = file;
			}
		}

		var resolved = new List<string>(map.Clips.Count);
		foreach (var clip in map.Clips.OrderBy(c => c.Index))
		{
			if (string.IsNullOrWhiteSpace(clip.SourceStem))
				throw new InvalidOperationException($"Clip {clip.Index} has no source stem.");

			if (!byStem.TryGetValue(clip.SourceStem, out var lrf))
			{
				throw new FileNotFoundException(
					$"No .LRF proxy found for '{clip.SourceStem}' under {folder}. " +
					$"Expected something like {clip.SourceStem}.LRF");
			}

			log?.Invoke($"Matched clip {clip.Index}: {clip.SourceFileName} → {lrf}");
			resolved.Add(lrf);
		}

		return resolved;
	}

	private static string FindLrfBesideSource(string directory, string sourceStem)
	{
		foreach (var ext in new[] { ".LRF", ".lrf", ".Lrf" })
		{
			var candidate = Path.Combine(directory, sourceStem + ext);
			if (File.Exists(candidate))
				return candidate;
		}

		try
		{
			return Directory.EnumerateFiles(directory, sourceStem + ".*")
				.FirstOrDefault(p => p.EndsWith(".lrf", StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static async Task BuildLrfGameProxyAsync(
		CombineEditMap map,
		IReadOnlyList<string> lrfPaths,
		string proxyPath,
		CancellationToken ct,
		Action<string> log,
		Action<string> status = null)
	{
		if (lrfPaths.Count != map.Clips.Count)
			throw new InvalidOperationException("LRF path count does not match combine map clips.");

		var listPath = Path.Combine(Path.GetDirectoryName(proxyPath) ?? TempPathHelper.GetTempPath(), "lrf_concat.txt");
		static string Escape(string p) => (p ?? string.Empty).Replace("'", "'\\''");
		await File.WriteAllLinesAsync(
			listPath,
			lrfPaths.Select(p => $"file '{Escape(Path.GetFullPath(p))}'"),
			ct).ConfigureAwait(false);

		var startSkip = map.TrimFirstVideo ? Math.Max(0, map.StartSkipSeconds) : 0;
		var gameDuration = map.ComputeGameContentDurationSeconds();
		if (gameDuration <= 0.5)
			throw new InvalidOperationException("Combine edit map implies empty game content duration.");

		var opts = new List<FfmpegOption?>();
		if (startSkip > 0)
		{
			opts.Add(FfmpegOption.Pair(
				FfmpegArguments.SeekInputTimestamp,
				startSkip.ToString("0.###", CultureInfo.InvariantCulture)));
		}

		opts.Add(FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatConcatDemuxer));
		opts.Add(FfmpegOption.Pair(FfmpegArguments.ConcatDemuxerSafeFlag, FfmpegArguments.ConcatDemuxerAllowAnyPath));
		opts.Add(FfmpegCommandLine.DefaultInputThreadQueue());
		opts.Add(FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(listPath)));
		opts.Add(FfmpegOption.Pair(
			FfmpegArguments.LimitOutputDuration,
			gameDuration.ToString("0.###", CultureInfo.InvariantCulture)));
		LegacyFastEncoding.AppendStreamCopyTail(opts);
		opts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(proxyPath)));

		status?.Invoke(
			$"Building LRF game proxy (start skip {startSkip:0.###}s, duration {gameDuration:0.###}s)…");
		await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(opts), ct, log)
			.ConfigureAwait(false);

		if (!File.Exists(proxyPath) || new FileInfo(proxyPath).Length <= 0)
			throw new InvalidOperationException("Failed to build LRF game proxy.");

		status?.Invoke("LRF game proxy ready.");
	}

	private static List<InningDetectionEvent> RemapEventsToCombinedTimeline(
		IReadOnlyList<InningDetectionEvent> proxyEvents,
		double introSeconds,
		double outputDurationSeconds)
	{
		var intro = Math.Max(0, introSeconds);
		var outputDuration = Math.Max(0, outputDurationSeconds);
		var list = new List<InningDetectionEvent>();

		foreach (var ev in proxyEvents.OrderBy(e => e.ElapsedSeconds))
		{
			var label = ev.Label ?? string.Empty;
			if (label.Equals(InningHalfLabels.RecordingStart, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(new InningDetectionEvent
				{
					ElapsedSeconds = 0,
					Label = InningHalfLabels.RecordingStart,
					Kind = "recording",
				});
				continue;
			}

			if (label.Equals(InningHalfLabels.RecordingEnd, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(new InningDetectionEvent
				{
					ElapsedSeconds = outputDuration,
					Label = InningHalfLabels.RecordingEnd,
					Kind = "recording",
				});
				continue;
			}

			list.Add(new InningDetectionEvent
			{
				ElapsedSeconds = intro + Math.Max(0, ev.ElapsedSeconds),
				Label = label,
				Kind = ev.Kind ?? "half_inning",
			});
		}

		if (list.All(e => e.Label != InningHalfLabels.RecordingStart))
		{
			list.Insert(0, new InningDetectionEvent
			{
				ElapsedSeconds = 0,
				Label = InningHalfLabels.RecordingStart,
				Kind = "recording",
			});
		}

		if (list.All(e => e.Label != InningHalfLabels.RecordingEnd))
		{
			list.Add(new InningDetectionEvent
			{
				ElapsedSeconds = outputDuration,
				Label = InningHalfLabels.RecordingEnd,
				Kind = "recording",
			});
		}

		return list
			.OrderBy(e => e.ElapsedSeconds)
			.ThenBy(e => e.Label, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}
}
