using System;
using System.Collections.Generic;
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
		public HalfInningDetector.Options DetectorOptions { get; set; } = CreateDefaultDetectorOptions();

		/// <summary>Default LRF sampling: every 3s (finer than the combined-MP4 5s default).</summary>
		public static HalfInningDetector.Options CreateDefaultDetectorOptions(double sampleIntervalSeconds = 3) =>
			new()
			{
				// LRF proxies do not contain Combine intros / replace-segment cards.
				SkipIntroTitleCards = false,
				SampleIntervalSeconds = Math.Clamp(sampleIntervalSeconds, 0.5, 30),
				// Prefer early half bookmarks on dusk/backstop LRF (see HalfInningDetector.Options).
				RefineLookbackSeconds = HalfInningDetector.Options.DefaultRefineLookbackSeconds,
				RoleHitterConfidence = 0.22f,
				RoleCatcherConfidence = 0.25f,
				RoleEmptyHoldSeconds = 20,
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
		options.DetectorOptions ??= Options.CreateDefaultDetectorOptions();

		InningDetectionArtifactSession artifactSession = null;
		string workDir;
		if (InningDetectionArtifactRuntime.Enabled)
		{
			artifactSession = InningDetectionArtifactSession.Create(combinedVideoPath, "lrf-parallel");
			workDir = artifactSession.SessionDirectory;
			status = artifactSession.WrapStatus(status);
		}
		else
		{
			workDir = Path.Combine(TempPathHelper.GetTempPath(), $"lrf_innings_{Guid.NewGuid():N}");
			Directory.CreateDirectory(workDir);
		}

		try
		{
			progress01?.Invoke(0.02);
			var orderedLrf = lrfPaths;
			if (orderedLrf.Count != map.Clips.Count)
				throw new InvalidOperationException("LRF path count does not match combine map clips.");

			var segments = GameContentTimeline.BuildSegments(map, orderedLrf);
			var gameDuration = GameContentTimeline.TotalDurationSeconds(segments);
			var mapDuration = map.ComputeGameContentDurationSeconds();
			if (Math.Abs(gameDuration - mapDuration) > 1.0)
			{
				log?.Invoke(
					$"Game-content duration from segments {gameDuration:0.###}s vs map {mapDuration:0.###}s " +
					"(using segment total).");
			}

			if (gameDuration <= 0.5)
				throw new InvalidOperationException("Combine edit map implies empty game content duration.");

			if (artifactSession != null)
			{
				Status($"Saving inning-detect debug package under {artifactSession.SessionDirectory}");
				await artifactSession.WriteCombineMapCopyAsync(map, ct).ConfigureAwait(false);
				await artifactSession.WriteDetectorOptionsAsync(options.DetectorOptions, ct).ConfigureAwait(false);
			}

			Status(
				$"Parallel snapshot extract across {segments.Count} LRF clip(s) " +
				$"({gameDuration:0.###}s game content)…");
			var frameDir = artifactSession != null
				? artifactSession.FramesDirectory
				: Path.Combine(workDir, "frames");
			var samples = await ParallelClipSampleExtractor.ExtractAsync(
					segments,
					options.DetectorOptions,
					frameDir,
					ct,
					log,
					Status,
					p => progress01?.Invoke(0.02 + 0.53 * Math.Clamp(p, 0, 1)))
				.ConfigureAwait(false);
			progress01?.Invoke(0.55);

			Status("Running half-inning detection on parallel LRF samples…");
			var refineMedia = new GameContentRefineMediaSource(segments);
			var proxyResult = await HalfInningDetector.DetectFromSamplesAsync(
					combinedVideoPath,
					samples,
					gameDuration,
					ct,
					log,
					p => progress01?.Invoke(0.55 + 0.40 * Math.Clamp(p, 0, 1)),
					options.DetectorOptions,
					writeOutputFiles: false,
					status: status,
					refineMedia: refineMedia,
					artifactSession: artifactSession,
					finalizeArtifacts: false)
				.ConfigureAwait(false);

			var intro = map.IntroApplied ? Math.Max(0, map.IntroDurationSeconds) : 0;
			var outputDuration = intro + (proxyResult.DurationSeconds > 0
				? proxyResult.DurationSeconds
				: gameDuration);

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

			if (artifactSession != null)
			{
				await artifactSession.WriteEventsAsync(artifactSession.EventsCombinedPath, remapped, ct)
					.ConfigureAwait(false);
				var manifest = new InningDetectionArtifactManifest
				{
					SampleIntervalSeconds = options.DetectorOptions.SampleIntervalSeconds,
					GameDurationSeconds = gameDuration,
					OutputDurationSeconds = outputDuration,
					IntroOffsetSeconds = intro,
					CombineMapPath = map.CombineMapPath,
					RecordingJsonPath = jsonPath,
					YoutubeDescriptionPath = youtubePath,
					LrfPaths = orderedLrf.ToList(),
					Segments = segments.Select(s => new ArtifactSegmentInfo
					{
						ClipIndex = s.ClipIndex,
						MediaPath = s.MediaPath,
						LocalStartSeconds = s.LocalStartSeconds,
						ContributionSeconds = s.ContributionSeconds,
						GameStartSeconds = s.GameStartSeconds,
					}).ToList(),
					HalfInningEvents = remapped
						.Where(e => string.Equals(e.Kind, "half_inning", StringComparison.OrdinalIgnoreCase))
						.ToList(),
				};
				await artifactSession.FinalizeAsync(manifest, ct, log).ConfigureAwait(false);
				Status($"Cursor handoff: {artifactSession.HandoffMarkdownPath}");
			}

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
				ArtifactHandoffPath = artifactSession?.HandoffMarkdownPath,
				ArtifactSessionDirectory = artifactSession?.SessionDirectory,
			};
		}
		finally
		{
			if (artifactSession == null)
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
