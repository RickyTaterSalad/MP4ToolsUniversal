using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MP4ToolsLib;

/// <summary>
/// Detects half-inning starts from kids baseball video. Prefers BaseballCV pitcher/hitter/catcher
/// ONNX roles when available; otherwise falls back to COCO person + ROI heuristics (coach near
/// the plate allowed during empty-box warmup).
/// </summary>
public static class HalfInningDetector
{
	/// <summary>One analysis JPEG with its game-content timestamp (seconds).</summary>
	public readonly record struct TimedSampleFrame(string Path, double ElapsedSeconds);

	/// <summary>Maps a game-content time to a media file for refine-window extracts.</summary>
	public interface IRefineMediaSource
	{
		bool TryResolve(double gameSeconds, out string mediaPath, out double localSeconds);
	}

	public sealed class Options
	{
		/// <summary>
		/// Seconds between analyzed frames. Warmups last minutes, so coarse sampling is enough;
		/// walk-up bookmarks are then refined inside that interval (default 5s).
		/// </summary>
		public double SampleIntervalSeconds { get; set; } = 5.0;

		/// <summary>
		/// After a coarse half-inning hit, densely sample before the hit to pull the bookmark
		/// earlier (prefer early over late when a half has already started).
		/// </summary>
		public bool RefineHalfInningHits { get; set; } = true;

		/// <summary>Dense sample interval inside each refine window (seconds).</summary>
		public double RefineIntervalSeconds { get; set; } = 0.5;

		/// <summary>
		/// How far before the coarse hit to search for an earlier batter/hitter.
		/// Default is intentionally wide so late coarse hits can still snap early.
		/// Null uses <see cref="DefaultRefineLookbackSeconds"/>.
		/// </summary>
		public double? RefineLookbackSeconds { get; set; } = DefaultRefineLookbackSeconds;

		/// <summary>Default refine lookback when <see cref="RefineLookbackSeconds"/> is null.</summary>
		public const double DefaultRefineLookbackSeconds = 45;

		/// <summary>How far after the coarse hit to keep searching (usually 0 — prefer early).</summary>
		public double RefineLookaheadSeconds { get; set; } = 0;

		/// <summary>Max analysis width (keeps aspect ratio).</summary>
		public int AnalysisWidth { get; set; } = 960;

		public float ConfidenceThreshold { get; set; } = 0.35f;

		/// <summary>
		/// Floor used when loading the BaseballCV PHC model so class-specific thresholds below
		/// <see cref="ConfidenceThreshold"/> can still surface (hitter/catcher filters apply later).
		/// </summary>
		public float RoleModelConfidenceFloor { get; set; } = 0.20f;

		/// <summary>
		/// PHC hitter score gate. Kept low so dusk/LRF/backstop footage bookmarks as soon as a
		/// hitter is plausible (prefer early half starts over late ones).
		/// </summary>
		public float RoleHitterConfidence { get; set; } = 0.22f;

		/// <summary>PHC catcher score gate (primary defense proxy when pitcher is never detected).</summary>
		public float RoleCatcherConfidence { get; set; } = 0.25f;

		/// <summary>PHC pitcher score gate (rarely fires on wide backstop kids footage).</summary>
		public float RolePitcherConfidence { get; set; } = 0.20f;

		/// <summary>
		/// Normalized batter-box ROI (x, y, w, h) in 0..1 of the frame.
		/// Half-inning bookmarks fire when someone first enters this ROI after warmup.
		/// </summary>
		public Rect2d BatterBoxRoi { get; set; } = new(0.34, 0.52, 0.32, 0.38);

		/// <summary>
		/// Larger plate-area ROI (approach / catcher side). Occupancy here during warmup is
		/// treated as a coach warming up the pitcher, not as a batter — only
		/// <see cref="BatterBoxRoi"/> starts a half-inning.
		/// </summary>
		public Rect2d BatterApproachRoi { get; set; } = new(0.26, 0.40, 0.48, 0.52);

		/// <summary>
		/// Left on-deck circle ROI (normalized). A hitter here while another is in
		/// <see cref="BatterBoxRoi"/> means the half is already in progress (not a side change).
		/// </summary>
		public Rect2d OnDeckLeftRoi { get; set; } = new(0.05, 0.48, 0.26, 0.45);

		/// <summary>
		/// Right on-deck circle ROI (normalized). Mirror of <see cref="OnDeckLeftRoi"/>.
		/// </summary>
		public Rect2d OnDeckRightRoi { get; set; } = new(0.69, 0.48, 0.26, 0.45);

		/// <summary>
		/// When true, batter-in-box + on-deck hitter (or a second hitter outside the box)
		/// clears empty/side-change tracking so mid-inning batter changes are not bookmarks.
		/// </summary>
		public bool UseOnDeckInProgressSignal { get; set; } = true;

		/// <summary>Normalized field ROI where fielders are counted.</summary>
		public Rect2d FieldRoi { get; set; } = new(0.08, 0.12, 0.84, 0.70);

		public int MinFieldersPlaying { get; set; } = 5;
		public int MaxFieldersEmpty { get; set; } = 2;
		public double EmptyFieldHoldSeconds { get; set; } = 15;

		/// <summary>
		/// Roles mode: how long PHC must see no hitter, no pitcher, and no plate-area catcher
		/// before counting as an empty (side-change) stretch. Dugout false catchers are ignored
		/// (see <see cref="BuildFrameSignals"/>).
		/// </summary>
		public double RoleEmptyHoldSeconds { get; set; } = 10;

		/// <summary>
		/// Roles mode: a new half-inning hitter must occur within this many seconds after an
		/// empty stretch ends. Kept tight so mid-inning YOLO dropouts that arm empty cannot
		/// bookmark a batter returning minutes later.
		/// </summary>
		public double RoleEmptyBeforeHitterSeconds { get; set; } = 100;

		/// <summary>
		/// After an empty arm, if defense is visible without a batter but this is <em>not</em>
		/// an empty-box plate-catcher throw-down (see <see cref="RoleThrowDownHoldSeconds"/>),
		/// drop the arm after this many seconds. Mid-inning YOLO blanks often resume as
		/// catcher-only for minutes before the next batter.
		/// </summary>
		public double RoleDefenseOnlyClearSeconds { get; set; } = 100;

		/// <summary>
		/// Roles mode: accumulated plate-catcher + empty-box time needed to count as the
		/// pre-half throw-down (catcher throws to second, umpire not set). PHC often flickers
		/// catcher/hitter labels, so time accumulates across brief gaps (see
		/// <see cref="RoleThrowDownGapSeconds"/>). Bookmark at the start of that stretch.
		/// </summary>
		public double RoleThrowDownHoldSeconds { get; set; } = 15;

		/// <summary>
		/// Max gap without an empty-box-defense sample before the throw-down accumulator resets.
		/// Default covers one missed 5s sample when YOLO mis-labels the catcher as a hitter.
		/// Effective limit is at least 2× <see cref="SampleIntervalSeconds"/> (see throw-down gap helper).
		/// </summary>
		public double RoleThrowDownGapSeconds { get; set; } = 10;

		/// <summary>
		/// How long after a pending throw-down a batter may appear to confirm the half start.
		/// Longer than <see cref="RoleEmptyBeforeHitterSeconds"/> so slow kids walk-ups still
		/// confirm; mid-inning false pendings rarely get a clean batter-in-box that late.
		/// </summary>
		public double RoleThrowDownConfirmSeconds { get; set; } = 120;

		/// <summary>
		/// After the last empty-box-defense sample, wait this long before a batter can confirm.
		/// Prevents false hitters during warmup from locking the bookmark before the real
		/// throw-down (often ~1 min after the catcher first sets with an empty box).
		/// </summary>
		public double RoleThrowDownPauseSeconds { get; set; } = 20;

		/// <summary>
		/// Longer empty-box + catcher/pitcher stretch that can arm a throw-down even when the
		/// prior empty arm was wiped (e.g. false in-progress). Covers knee throw-downs and
		/// mound huddles that only happen immediately before a half starts.
		/// </summary>
		public double RoleThrowDownStrongSeconds { get; set; } = 25;

		/// <summary>
		/// Empty stretches shorter than this only count as a side-change if the next batter
		/// arrives within <see cref="RoleShortEmptyWalkupSeconds"/>. Longer clears may wait up
		/// to <see cref="RoleEmptyBeforeHitterSeconds"/>.
		/// </summary>
		public double RoleSolidEmptySeconds { get; set; } = 20;

		/// <summary>
		/// Max delay from a short empty arm to the walk-up hitter (rejects mid-inning 10–15s
		/// YOLO blanks where the batter returns half a minute later). Also used as the
		/// "recent batter in box" window for throw-down arming: within this gap a normal
		/// throw-down also needs a solid LooksEmpty clear (see <see cref="RoleSolidEmptySeconds"/>).
		/// </summary>
		public double RoleShortEmptyWalkupSeconds { get; set; } = 25;

		/// <summary>
		/// After batter+on-deck (in-progress), suppress <em>strong</em> throw-downs (no field
		/// clear) and empty→hitter bookmarks for this many seconds. Mid-inning between-batter
		/// pauses often look like empty-box catcher warmups; real half changes usually have a
		/// LooksEmpty stretch first and still arm via the normal empty-arm throw-down path.
		/// </summary>
		public double RoleMidInningRiskSeconds { get; set; } = 360;

		/// <summary>
		/// Roles mode: minimum time without a hitter detection before the next hitter can open
		/// a new half-inning.
		/// </summary>
		public double RoleNoHitterGapSeconds { get; set; } = 90;

		/// <summary>
		/// How long the pitcher/defense must be out with an empty batter's box before a
		/// batter entering the box can bookmark (coach near plate is OK during this window).
		/// </summary>
		public double WarmupHoldSeconds { get; set; } = 20;

		/// <summary>
		/// Shorter warmup hold used only for the first half-inning (Top 1st often starts
		/// within the first few samples once defense is visible).
		/// </summary>
		public double FirstHalfWarmupHoldSeconds { get; set; } = 5;

		public double MinSecondsBetweenHalfInnings { get; set; } = 45;

		/// <summary>Roles mode minimum gap between half-inning bookmarks.</summary>
		public double RoleMinSecondsBetweenHalfInnings { get; set; } = 240;

		/// <summary>
		/// After at least <see cref="RoleMinHalfInningsBeforeGameOverStop"/> bookmarks, stop if
		/// the field stays empty this long (end of kids games / packing up).
		/// </summary>
		public double RoleGameOverEmptySeconds { get; set; } = 90;

		public int RoleMinHalfInningsBeforeGameOverStop { get; set; } = 6;

		/// <summary>
		/// Ignore Combine / Replace-Segment intro cards (solid background + text, ~3–10s).
		/// Most appear at the start; mid-video cards are rare but also skipped.
		/// </summary>
		public bool SkipIntroTitleCards { get; set; } = true;

		/// <summary>Require this many consecutive title-card samples before skipping (reduces false positives).</summary>
		public int IntroConfirmSamples { get; set; } = 2;

		/// <summary>
		/// Bookmark Top 1st at the first defense-ready sample (roles: catcher/pitcher) after any
		/// leading intro, or at t=0 / first non-intro frame for the person model.
		/// </summary>
		public bool AssumeTopFirstAtGameStart { get; set; } = true;
	}

	private enum Phase
	{
		Playing,
		FieldClearing,
		Warmup,
	}

	public static async Task<InningDetectionResult> DetectAsync(
		string videoPath,
		CancellationToken ct = default,
		Action<string> log = null,
		Action<double> progress01 = null,
		Options options = null,
		string modelPath = null,
		string outputJsonPath = null,
		bool writeOutputFiles = true,
		Action<string> status = null,
		string existingFramesDirectory = null)
	{
		if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
			throw new FileNotFoundException("Video not found.", videoPath);

		options ??= new Options();
		if (!string.IsNullOrWhiteSpace(existingFramesDirectory))
		{
			return await DetectFromExistingFramesAsync(
					videoPath,
					existingFramesDirectory,
					ct,
					log,
					progress01,
					options,
					modelPath,
					outputJsonPath,
					writeOutputFiles,
					status)
				.ConfigureAwait(false);
		}

		InningDetectionArtifactSession artifactSession = null;
		if (InningDetectionArtifactRuntime.Enabled)
		{
			artifactSession = InningDetectionArtifactSession.Create(videoPath, "combined-mp4");
			status = artifactSession.WrapStatus(status);
		}

		void Status(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
				return;
			status?.Invoke(message);
			log?.Invoke(message);
		}

		Status("Reading video duration…");
		var durationText = await FFMpegUtils.Instance.GetFileDurationAsync(videoPath, ct, log).ConfigureAwait(false);
		var durationSeconds = ParseDurationSeconds(durationText);
		if (durationSeconds <= 0)
			durationSeconds = ProbeDurationWithOpenCv(videoPath);

		Status(
			$"Preparing detection ({FormatElapsed(durationSeconds)} video, sample every {options.SampleIntervalSeconds:0.###}s)…");
		progress01?.Invoke(0);

		const double extractProgressEnd = 0.55;
		var frameDir = artifactSession != null
			? artifactSession.FramesDirectory
			: Path.Combine(TempPathHelper.GetTempPath(), $"innings_frames_{Guid.NewGuid():N}");
		if (artifactSession != null)
			Status($"Saving inning-detect debug package under {artifactSession.SessionDirectory}");
		else
			Directory.CreateDirectory(frameDir);

		try
		{
			await ExtractSampleFramesAsync(
					videoPath,
					frameDir,
					options,
					durationSeconds,
					ct,
					log,
					Status,
					p => progress01?.Invoke(extractProgressEnd * Math.Clamp(p, 0, 1)))
				.ConfigureAwait(false);
			progress01?.Invoke(extractProgressEnd);

			var frames = Directory.GetFiles(frameDir, "*.jpg").OrderBy(f => f, StringComparer.Ordinal).ToArray();
			if (frames.Length == 0)
				throw new InvalidOperationException("No sample frames were extracted from the video.");

			var interval = Math.Max(0.25, options.SampleIntervalSeconds);
			var samples = new List<TimedSampleFrame>(frames.Length);
			for (var i = 0; i < frames.Length; i++)
				samples.Add(new TimedSampleFrame(frames[i], i * interval));

			return await DetectFromSamplesAsync(
					videoPath,
					samples,
					durationSeconds,
					ct,
					log,
					p => progress01?.Invoke(extractProgressEnd + (1.0 - extractProgressEnd) * Math.Clamp(p, 0, 1)),
					options,
					modelPath,
					outputJsonPath,
					writeOutputFiles,
					status,
					refineMedia: null,
					ownSampleFiles: false,
					artifactSession: artifactSession)
				.ConfigureAwait(false);
		}
		finally
		{
			if (artifactSession == null)
				TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(frameDir, recursive: true);
		}
	}

	/// <summary>
	/// Re-runs detection using JPEGs already on disk (session folder or a <c>frames/</c> dir).
	/// Only the JPEGs are read — sidecar JSON/CSV/logs in the folder are ignored. Refine windows
	/// still extract from <paramref name="videoPath"/>.
	/// </summary>
	public static async Task<InningDetectionResult> DetectFromExistingFramesAsync(
		string videoPath,
		string framesSourcePath,
		CancellationToken ct = default,
		Action<string> log = null,
		Action<double> progress01 = null,
		Options options = null,
		string modelPath = null,
		string outputJsonPath = null,
		bool writeOutputFiles = true,
		Action<string> status = null)
	{
		if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
			throw new FileNotFoundException("Video not found.", videoPath);
		if (!TryResolveExistingFramesDirectory(framesSourcePath, out var framesDir))
		{
			throw new DirectoryNotFoundException(
				"No sample JPEGs found. Pass an inning-detect session folder (with a frames/ subfolder) " +
				$"or a directory of .jpg snapshots: {framesSourcePath}");
		}

		options ??= new Options();
		InningDetectionArtifactSession artifactSession = null;
		if (InningDetectionArtifactRuntime.Enabled)
		{
			artifactSession = InningDetectionArtifactSession.Create(videoPath, "reuse-frames");
			status = artifactSession.WrapStatus(status);
		}

		void Status(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
				return;
			status?.Invoke(message);
			log?.Invoke(message);
		}

		Status("Reading video duration…");
		var durationText = await FFMpegUtils.Instance.GetFileDurationAsync(videoPath, ct, log).ConfigureAwait(false);
		var durationSeconds = ParseDurationSeconds(durationText);
		if (durationSeconds <= 0)
			durationSeconds = ProbeDurationWithOpenCv(videoPath);

		var sourceFrames = Directory.GetFiles(framesDir, "*.jpg").OrderBy(f => f, StringComparer.Ordinal).ToArray();
		if (sourceFrames.Length == 0)
			throw new InvalidOperationException($"No .jpg sample frames in {framesDir}.");

		// Infer spacing from duration + count so reused packs match the original extract
		// (UI defaults differ: combined-MP4 5s vs LRF 3s).
		var interval = InferSampleIntervalSeconds(
			sourceFrames.Length,
			durationSeconds,
			options.SampleIntervalSeconds);
		options.SampleIntervalSeconds = interval;

		Status(
			$"Reusing {sourceFrames.Length} sample frame(s) from {framesDir} " +
			$"(interval {interval:0.###}s, video {FormatElapsed(durationSeconds)}) — skipping extract…");
		progress01?.Invoke(0.05);

		IReadOnlyList<TimedSampleFrame> samples;
		if (artifactSession != null)
		{
			Status($"Saving inning-detect debug package under {artifactSession.SessionDirectory}");
			LinkOrCopyFramesIntoDirectory(sourceFrames, artifactSession.FramesDirectory, Status);
			samples = LoadSamplesFromFramesDirectory(artifactSession.FramesDirectory, interval);
		}
		else
		{
			samples = LoadSamplesFromFramesDirectory(framesDir, interval);
		}

		progress01?.Invoke(0.55);
		return await DetectFromSamplesAsync(
				videoPath,
				samples,
				durationSeconds,
				ct,
				log,
				p => progress01?.Invoke(0.55 + 0.45 * Math.Clamp(p, 0, 1)),
				options,
				modelPath,
				outputJsonPath,
				writeOutputFiles,
				status,
				refineMedia: null,
				ownSampleFiles: false,
				artifactSession: artifactSession)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Resolves a session folder (<c>…/frames/*.jpg</c>) or a directory of JPEGs.
	/// </summary>
	public static bool TryResolveExistingFramesDirectory(string path, out string framesDirectory)
	{
		framesDirectory = null;
		if (string.IsNullOrWhiteSpace(path))
			return false;

		var trimmed = path.Trim();
		if (!Directory.Exists(trimmed))
			return false;

		var nested = Path.Combine(trimmed, "frames");
		if (Directory.Exists(nested) && Directory.EnumerateFiles(nested, "*.jpg").Any())
		{
			framesDirectory = nested;
			return true;
		}

		if (Directory.EnumerateFiles(trimmed, "*.jpg").Any())
		{
			framesDirectory = trimmed;
			return true;
		}

		return false;
	}

	public static IReadOnlyList<TimedSampleFrame> LoadSamplesFromFramesDirectory(
		string framesDirectory,
		double sampleIntervalSeconds)
	{
		if (string.IsNullOrWhiteSpace(framesDirectory) || !Directory.Exists(framesDirectory))
			throw new DirectoryNotFoundException($"Frames directory not found: {framesDirectory}");

		var frames = Directory.GetFiles(framesDirectory, "*.jpg").OrderBy(f => f, StringComparer.Ordinal).ToArray();
		if (frames.Length == 0)
			throw new InvalidOperationException($"No .jpg sample frames in {framesDirectory}.");

		var interval = Math.Max(0.25, sampleIntervalSeconds);
		var samples = new List<TimedSampleFrame>(frames.Length);
		for (var i = 0; i < frames.Length; i++)
			samples.Add(new TimedSampleFrame(frames[i], i * interval));
		return samples;
	}

	/// <summary>
	/// Spacing used when frames were extracted as <c>fps=1/interval</c> over <paramref name="durationSeconds"/>.
	/// Prefers the configured interval when the frame count matches the extract (±1), so a
	/// short pack (e.g. 1375 vs expected 1376) does not become 5.001s and break throw-down
	/// gap math that assumes exact 2×sample spacing for one missed frame.
	/// </summary>
	public static double InferSampleIntervalSeconds(
		int frameCount,
		double durationSeconds,
		double fallbackIntervalSeconds)
	{
		var fallback = Math.Max(0.25, fallbackIntervalSeconds);
		if (frameCount <= 0 || durationSeconds <= 0.5)
			return fallback;

		// Extract expects ~ceil(duration/interval) frames at t = 0, i, 2i, …
		var expectedCount = (int)Math.Ceiling(durationSeconds / fallback);
		if (Math.Abs(expectedCount - frameCount) <= 1)
			return fallback;

		var inferred = durationSeconds / Math.Max(1, frameCount - 1);
		if (inferred < 0.25 || inferred > 60)
			return fallback;
		// Snap to configured interval when within 2% (avoids 5.001-style drift).
		if (Math.Abs(inferred - fallback) <= fallback * 0.02)
			return fallback;
		return inferred;
	}

	/// <summary>
	/// Max gap without an empty-box-defense sample before the throw-down streak resets.
	/// Always allows at least one missed coarse sample (2× sample interval) plus a tiny
	/// epsilon so floating-point spacing cannot break the "one miss OK" contract.
	/// </summary>
	private static double ThrowDownGapLimitSeconds(Options options)
	{
		var sample = Math.Max(0.25, options.SampleIntervalSeconds);
		var configured = Math.Max(0, options.RoleThrowDownGapSeconds);
		return Math.Max(configured, sample * 2) + 0.05;
	}

	private static void LinkOrCopyFramesIntoDirectory(
		IReadOnlyList<string> sourceFrames,
		string destDirectory,
		Action<string> status)
	{
		var sourceDir = sourceFrames.Count > 0 ? Path.GetDirectoryName(sourceFrames[0]) : null;
		if (!string.IsNullOrWhiteSpace(sourceDir) && Directory.Exists(sourceDir))
		{
			try
			{
				// Replace the empty frames/ directory with a symlink to the source pack.
				if (Directory.Exists(destDirectory))
					Directory.Delete(destDirectory, recursive: false);
				Directory.CreateSymbolicLink(destDirectory, sourceDir);
				status?.Invoke($"Linked sample frames directory → {sourceDir}");
				return;
			}
			catch
			{
				// Fall through to copy.
			}
		}

		Directory.CreateDirectory(destDirectory);
		foreach (var leftover in Directory.EnumerateFiles(destDirectory, "*.jpg"))
			FileUtils.TryDeleteFile(leftover);

		foreach (var src in sourceFrames)
		{
			var dest = Path.Combine(destDirectory, Path.GetFileName(src));
			File.Copy(src, dest, overwrite: true);
		}

		status?.Invoke($"Copied {sourceFrames.Count} sample frame(s) into debug package.");
	}

	/// <summary>
	/// Runs detection on pre-extracted timed samples (e.g. parallel per-clip extract).
	/// Progress is 0..1 for analyze/refine/write only (extract already finished).
	/// </summary>
	public static async Task<InningDetectionResult> DetectFromSamplesAsync(
		string resultVideoPath,
		IReadOnlyList<TimedSampleFrame> samples,
		double durationSeconds,
		CancellationToken ct = default,
		Action<string> log = null,
		Action<double> progress01 = null,
		Options options = null,
		string modelPath = null,
		string outputJsonPath = null,
		bool writeOutputFiles = true,
		Action<string> status = null,
		IRefineMediaSource refineMedia = null,
		bool ownSampleFiles = false,
		InningDetectionArtifactSession artifactSession = null,
		bool finalizeArtifacts = true)
	{
		if (samples == null || samples.Count == 0)
			throw new ArgumentException("At least one sample frame is required.", nameof(samples));
		if (string.IsNullOrWhiteSpace(resultVideoPath))
			throw new ArgumentException("Result video path is required.", nameof(resultVideoPath));

		options ??= new Options();
		durationSeconds = Math.Max(durationSeconds, samples[^1].ElapsedSeconds);

		if (artifactSession == null && InningDetectionArtifactRuntime.Enabled)
		{
			artifactSession = InningDetectionArtifactSession.Create(resultVideoPath, "samples");
			status = artifactSession.WrapStatus(status);
		}

		void Status(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
				return;
			status?.Invoke(message);
			log?.Invoke(message);
		}

		if (artifactSession != null)
		{
			await artifactSession.WriteSamplesCsvAsync(samples, ct).ConfigureAwait(false);
			await artifactSession.WriteDetectorOptionsAsync(options, ct).ConfigureAwait(false);
		}

		Status("Loading YOLO detection model…");
		var resolvedModel = await YoloModelStore.EnsureDetectorModelAsync(modelPath, ct, log).ConfigureAwait(false);
		var useRoles = resolvedModel.Kind == YoloModelKind.BaseballPhc;

		const double analyzeProgressEnd = 0.85;
		try
		{
			Status(
				useRoles
					? $"Analyzing gameplay with BaseballCV roles ({samples.Count} samples)…"
					: $"Analyzing gameplay with YOLO person model ({samples.Count} samples)…");

			var detectConfidence = useRoles
				? Math.Min(options.ConfidenceThreshold, options.RoleModelConfidenceFloor)
				: options.ConfidenceThreshold;
			using var detector = new YoloOnnxDetector(resolvedModel.Path, resolvedModel.Kind, detectConfidence);
			var halfStarts = new List<double>();
			var phase = Phase.Playing;
			var emptySince = -1.0;
			var warmupSince = -1.0;
			var warmupConfirmed = false;
			var lastHalfSeconds = -1_000.0;
			var lastHitterSeconds = -1_000.0;
			var lastEmptyEndSeconds = -1_000.0;
			var lastEmptyArmStartSeconds = -1_000.0;
			// LooksEmpty-only clear (EmptyBoxDefense must not inflate this). Used to reject
			// mid-at-bat catcher crouches that extend a short blank into a fake side-change.
			var lastLooksEmptyArmStartSeconds = -1_000.0;
			var lastLooksEmptyEndSeconds = -1_000.0;
			var bestLooksEmptyClearSeconds = 0.0;
			var bestLooksEmptyEndSeconds = -1_000.0;
			var lastInProgressSeconds = -1_000.0;
			var defenseOnlySince = -1.0;
			var throwDownSince = -1.0;
			var throwDownLastSeen = -1.0;
			var throwDownAccumSeconds = 0.0;
			var throwDownSawPitcher = false;
			var pendingThrowDownSeconds = -1.0;
			// First arm time (does not slide). Used so LooksEmpty freeze can track the full
			// between-half clear even after the bookmark advances to the clear's end.
			var pendingThrowDownArmedAt = -1.0;
			// After a solid LooksEmpty following throw-down arm, freeze the bookmark at the
			// clear (real half boundary) and stop sliding on later empty-box catcher frames.
			var pendingThrowDownFrozen = false;
			var longEmptySince = -1.0;
			var introStreak = 0;
			var skippingIntro = false;
			var introSkipCount = 0;
			double? introSegmentStart = null;
			var topFirstSeeded = false;
			var sawLeadingIntro = false;
			var stoppedForGameOver = false;

			if (options.SkipIntroTitleCards)
				Status("Intro title-card skip enabled (solid background + text).");
			if (options.AssumeTopFirstAtGameStart)
			{
				Status(
					useRoles
						? "Top 1st will be marked at first catcher/pitcher after intro."
						: "Top 1st will be marked at game start (t=0 or first frame after intro).");
			}

			Status(
				useRoles
					? "Scanning for half-innings (empty→hitter; catcher throw-down + empty box; on-deck+box = in progress)…"
					: "Scanning for later half-innings (field clear → warmup → first batter in box)…");

			for (var i = 0; i < samples.Count; i++)
			{
				ct.ThrowIfCancellationRequested();
				if (stoppedForGameOver)
					break;

				var elapsed = samples[i].ElapsedSeconds;
				var analyzeFrac = samples.Count <= 1 ? 1.0 : (double)i / (samples.Count - 1);
				progress01?.Invoke(analyzeProgressEnd * analyzeFrac);

				using var mat = Cv2.ImRead(samples[i].Path, ImreadModes.Color);
				if (mat.Empty())
					continue;

				if (options.SkipIntroTitleCards)
				{
					var looksLikeIntro = IntroTitleCardClassifier.IsSolidTextCard(mat);
					if (looksLikeIntro)
					{
						introStreak++;
						if (!skippingIntro && introStreak >= Math.Max(1, options.IntroConfirmSamples))
						{
							skippingIntro = true;
							if (!topFirstSeeded)
								sawLeadingIntro = true;
							introSegmentStart = elapsed - (introStreak - 1) * options.SampleIntervalSeconds;
							// Intro has no fielders; do not treat it as a half-inning break.
							emptySince = -1;
							longEmptySince = -1;
							warmupSince = -1;
							warmupConfirmed = false;
							if (phase != Phase.Playing)
								phase = Phase.Playing;
							Status($"[{FormatElapsed(introSegmentStart.Value)}] Skipping intro / title card…");
						}
					}
					else
					{
						if (skippingIntro)
						{
							var start = introSegmentStart ?? elapsed;
							Status(
								$"[{FormatElapsed(elapsed)}] Resumed game footage after title card ({FormatElapsed(start)}–{FormatElapsed(elapsed)}).");
						}

						skippingIntro = false;
						introStreak = 0;
						introSegmentStart = null;
					}

					if (skippingIntro)
					{
						introSkipCount++;
						if (i % 10 == 0 || i + 1 == samples.Count)
						{
							Status(
								$"Skipping title card… sample {i + 1}/{samples.Count} ({FormatElapsed(elapsed)})");
						}

						continue;
					}
				}

				var detections = detector.Detect(mat);
				var signals = BuildFrameSignals(detections, mat.Width, mat.Height, options, useRoles);

				if (useRoles)
				{
					// Tuned kids/backstop PHC path: bookmark uses either
					// (A) empty end-of-half → hitter return, or
					// (B) catcher throw-down with empty batter's box (last ritual before a half).
					// Batter-in-box + on-deck hitter means play is ongoing, so wipe empty /
					// throw-down tracking (mid-inning, not a new half).
					if (signals.InningInProgress)
					{
						// Only advance the mid-inning risk clock when in-progress aborts an
						// active side-change cue. Updating on every batter+on-deck frame during
						// live play keeps the 360s gate from ever expiring before a real half
						// change (last batter of a half often still has an on-deck hitter).
						var abortedSideChange = lastEmptyEndSeconds >= 0
							|| emptySince >= 0
							|| throwDownSince >= 0
							|| pendingThrowDownSeconds >= 0;
						if (abortedSideChange)
						{
							Status(
								$"[{FormatElapsed(elapsed)}] Inning in progress (batter in box + on-deck) — not end of half");
							lastInProgressSeconds = elapsed;
						}

						emptySince = -1;
						longEmptySince = -1;
						lastEmptyEndSeconds = -1;
						lastEmptyArmStartSeconds = -1;
						lastLooksEmptyArmStartSeconds = -1;
						lastLooksEmptyEndSeconds = -1;
						bestLooksEmptyClearSeconds = 0;
						bestLooksEmptyEndSeconds = -1;
						defenseOnlySince = -1;
						throwDownSince = -1;
						throwDownLastSeen = -1;
						throwDownAccumSeconds = 0;
						throwDownSawPitcher = false;
						pendingThrowDownSeconds = -1;
						pendingThrowDownArmedAt = -1;
						pendingThrowDownFrozen = false;
					}
					else if (signals.LooksEmpty)
					{
						if (emptySince < 0)
							emptySince = elapsed;
						else if (elapsed - emptySince >= options.RoleEmptyHoldSeconds)
						{
							// New clear stretch if prior arm ended before this emptySince —
							// unless this is a brief resume after EmptyBoxDefense / YOLO flicker
							// (same side-change). Resetting the arm start here was dropping Top 3rd
							// on long between-half empties interrupted by one catcher frame.
							if (lastEmptyEndSeconds < emptySince)
							{
								var resumeGap = ThrowDownGapLimitSeconds(options);
								var continuesPriorArm = lastEmptyArmStartSeconds >= 0
									&& lastEmptyEndSeconds >= 0
									&& emptySince - lastEmptyEndSeconds <= resumeGap;
								if (!continuesPriorArm)
									lastEmptyArmStartSeconds = emptySince;
							}

							lastEmptyEndSeconds = elapsed;

							// Parallel LooksEmpty-only arm (not extended by EmptyBoxDefense).
							if (lastLooksEmptyEndSeconds < emptySince)
							{
								var resumeGap = ThrowDownGapLimitSeconds(options);
								var continuesLooksEmpty = lastLooksEmptyArmStartSeconds >= 0
									&& lastLooksEmptyEndSeconds >= 0
									&& emptySince - lastLooksEmptyEndSeconds <= resumeGap;
								if (!continuesLooksEmpty)
									lastLooksEmptyArmStartSeconds = emptySince;
							}

							lastLooksEmptyEndSeconds = elapsed;
							// Best clear since last half — only stretches that began after the
							// last batter left the box (true between-half empties).
							if (lastLooksEmptyArmStartSeconds >= 0
								&& lastHitterSeconds < lastLooksEmptyArmStartSeconds)
							{
								var looksClear = lastLooksEmptyEndSeconds - lastLooksEmptyArmStartSeconds;
								if (looksClear >= bestLooksEmptyClearSeconds)
								{
									bestLooksEmptyClearSeconds = looksClear;
									bestLooksEmptyEndSeconds = lastLooksEmptyEndSeconds;
								}
							}

							// Solid LooksEmpty after throw-down = between-half clear. Freeze the
							// bookmark at the clear's end so later new-half catcher crouches cannot
							// slide it past the first batter (Coyotes Bottom 1st slid 12:30→14:05).
							if (pendingThrowDownSeconds >= 0
								&& pendingThrowDownArmedAt >= 0
								&& lastLooksEmptyArmStartSeconds >= pendingThrowDownArmedAt - 0.05
								&& lastLooksEmptyEndSeconds - lastLooksEmptyArmStartSeconds
								>= options.RoleSolidEmptySeconds)
							{
								var freezeAt = lastLooksEmptyEndSeconds;
								if (!pendingThrowDownFrozen)
								{
									pendingThrowDownFrozen = true;
									throwDownLastSeen = -1;
									throwDownSince = -1;
									throwDownAccumSeconds = 0;
									throwDownSawPitcher = false;
									pendingThrowDownSeconds = freezeAt;
									Status(
										$"[{FormatElapsed(pendingThrowDownSeconds)}] Throw-down pending frozen at LooksEmpty clear — waiting for batter");
								}
								else if (freezeAt > pendingThrowDownSeconds)
								{
									pendingThrowDownSeconds = freezeAt;
								}
							}
						}

						defenseOnlySince = -1;
						// Empty field is compatible with an in-progress throw-down streak (camera
						// briefly loses the catcher). Only reset after the throw-down gap limit.
						var emptyThrowDownGap = ThrowDownGapLimitSeconds(options);
						if (throwDownSince >= 0
							&& throwDownLastSeen >= 0
							&& elapsed - throwDownLastSeen > emptyThrowDownGap)
						{
							throwDownSince = -1;
							throwDownLastSeen = -1;
							throwDownAccumSeconds = 0;
							throwDownSawPitcher = false;
						}

						if (longEmptySince < 0)
							longEmptySince = elapsed;
						else if (halfStarts.Count >= Math.Max(1, options.RoleMinHalfInningsBeforeGameOverStop)
							&& elapsed - longEmptySince >= options.RoleGameOverEmptySeconds)
						{
							Status(
								$"[{FormatElapsed(elapsed)}] Game appears over (empty ≥ {options.RoleGameOverEmptySeconds:0}s) — stopping.");
							stoppedForGameOver = true;
							break;
						}
					}
					else
					{
						emptySince = -1;
						longEmptySince = -1;

						// Catcher at plate + empty box = throw-down / pre-half defense warmup.
						// Do not clear the empty arm for this — it is the real side-change cue.
						// Accumulate across brief YOLO flickers (catcher↔hitter mislabels).
						if (signals.EmptyBoxDefense)
						{
							// Keep the side-change empty arm alive through the throw-down ritual
							// so empty→hitter fallback still sees a solid clear if throw-down
							// never reaches hold (quick walk-up after a long empty).
							if (lastEmptyEndSeconds > lastHalfSeconds)
								lastEmptyEndSeconds = elapsed;

							var gap = ThrowDownGapLimitSeconds(options);
							if (throwDownSince < 0
								|| (throwDownLastSeen >= 0 && elapsed - throwDownLastSeen > gap))
							{
								throwDownSince = elapsed;
								throwDownAccumSeconds = 0;
								throwDownSawPitcher = false;
							}

							var step = options.SampleIntervalSeconds > 0
								? options.SampleIntervalSeconds
								: 5;
							if (throwDownLastSeen < 0 || elapsed > throwDownLastSeen + 0.05)
								throwDownAccumSeconds += step;
							throwDownLastSeen = elapsed;
							if (signals.HasPitcher)
								throwDownSawPitcher = true;
							// Slide pending bookmark to the latest throw-down frame — early
							// empty-box catcher crouch is before the actual throw to second.
							// Stop sliding once a solid LooksEmpty froze the half-boundary mark.
							if (pendingThrowDownSeconds >= 0 && !pendingThrowDownFrozen)
								pendingThrowDownSeconds = elapsed;
							defenseOnlySince = -1;
						}
						else
						{
							var gap = ThrowDownGapLimitSeconds(options);
							if (throwDownSince >= 0
								&& throwDownLastSeen >= 0
								&& elapsed - throwDownLastSeen > gap)
							{
								throwDownSince = -1;
								throwDownLastSeen = -1;
								throwDownAccumSeconds = 0;
								throwDownSawPitcher = false;
							}

							// Defense visible again after an empty arm, still no batter, but not
							// a plate-catcher throw-down — if this lasts too long the empty was a
							// mid-inning dropout, not a side change.
							if (lastEmptyEndSeconds > lastHalfSeconds
								&& signals.DefenseReady
								&& !signals.BatterPresent)
							{
								if (defenseOnlySince < 0)
									defenseOnlySince = elapsed;
								else if (options.RoleDefenseOnlyClearSeconds > 0
									&& elapsed - defenseOnlySince >= options.RoleDefenseOnlyClearSeconds)
								{
									Status(
										$"[{FormatElapsed(elapsed)}] Clearing stale empty arm (defense without batter ≥ {options.RoleDefenseOnlyClearSeconds:0}s)");
									lastEmptyEndSeconds = -1;
									lastEmptyArmStartSeconds = -1;
									lastLooksEmptyArmStartSeconds = -1;
									lastLooksEmptyEndSeconds = -1;
									bestLooksEmptyClearSeconds = 0;
									bestLooksEmptyEndSeconds = -1;
									defenseOnlySince = -1;
								}
							}
							else
							{
								defenseOnlySince = -1;
							}
						}
					}

					// Drop stale side-change arms: a mid-inning YOLO blank can arm empty, then a
					// batter minutes later must not bookmark a new half. Keep the arm while a
					// throw-down is in progress (empty-box catcher).
					if (lastEmptyEndSeconds >= 0
						&& throwDownSince < 0
						&& pendingThrowDownSeconds < 0
						&& elapsed - lastEmptyEndSeconds > options.RoleEmptyBeforeHitterSeconds)
					{
						lastEmptyEndSeconds = -1;
						lastEmptyArmStartSeconds = -1;
						lastLooksEmptyArmStartSeconds = -1;
						lastLooksEmptyEndSeconds = -1;
						bestLooksEmptyClearSeconds = 0;
						bestLooksEmptyEndSeconds = -1;
						defenseOnlySince = -1;
					}

					// Pending throw-down expires if no batter walk-up follows (mid-inning false arm).
					if (pendingThrowDownSeconds >= 0
						&& elapsed - pendingThrowDownSeconds > options.RoleThrowDownConfirmSeconds)
					{
						Status(
							$"[{FormatElapsed(elapsed)}] Clearing stale throw-down pending ({FormatElapsed(pendingThrowDownSeconds)}) — no batter walk-up");
						pendingThrowDownSeconds = -1;
						pendingThrowDownArmedAt = -1;
						pendingThrowDownFrozen = false;
					}

					if (options.AssumeTopFirstAtGameStart && !topFirstSeeded)
					{
						if (signals.DefenseReady)
						{
							halfStarts.Add(elapsed);
							lastHalfSeconds = elapsed;
							topFirstSeeded = true;
							throwDownSince = -1;
							throwDownLastSeen = -1;
							throwDownAccumSeconds = 0;
							throwDownSawPitcher = false;
							pendingThrowDownSeconds = -1;
							pendingThrowDownArmedAt = -1;
							pendingThrowDownFrozen = false;
							lastLooksEmptyArmStartSeconds = -1;
							lastLooksEmptyEndSeconds = -1;
							bestLooksEmptyClearSeconds = 0;
							bestLooksEmptyEndSeconds = -1;
							Status(
								sawLeadingIntro
									? $"[{FormatElapsed(elapsed)}] Top 1st (first defense after intro)"
									: $"[{FormatElapsed(elapsed)}] Top 1st (first defense)");
						}

						continue;
					}

					// Arm a pending throw-down (do not bookmark yet). Mid-inning empty→catcher
					// flickers look the same; confirming with a later batter walk-up rejects those.
					// Two paths:
					//  (A) normal: empty arm + short throw-down hold
					//  (B) strong: longer catcher/pitcher empty-box stretch without empty arm
					//      (covers knee throw-downs / mound huddles after a false in-progress wipe)
					// Recent batter+on-deck (RoleMidInningRiskSeconds) + strong path (no field
					// clear) ≈ between-batter pause — discard. Normal path with a LooksEmpty arm
					// is allowed even inside the window (real half changes can follow quickly).
					// Recent batter-in-box (RoleShortEmptyWalkupSeconds) + empty arm inflated only
					// by EmptyBoxDefense (no solid LooksEmpty) ≈ still mid at-bat — discard.
					if (topFirstSeeded
						&& pendingThrowDownSeconds < 0
						&& throwDownSince >= 0
						&& options.RoleThrowDownHoldSeconds > 0
						&& throwDownAccumSeconds >= options.RoleThrowDownHoldSeconds)
					{
						var minGapOk = throwDownSince - lastHalfSeconds
							>= options.RoleMinSecondsBetweenHalfInnings;
						var totalClear = lastEmptyArmStartSeconds >= 0
							&& lastEmptyEndSeconds >= lastEmptyArmStartSeconds
							? lastEmptyEndSeconds - lastEmptyArmStartSeconds
							: 0;
						var classicEmptyArm = lastEmptyEndSeconds > lastHalfSeconds
							&& lastEmptyArmStartSeconds >= 0
							&& lastEmptyArmStartSeconds <= throwDownSince
							&& totalClear >= options.RoleEmptyHoldSeconds;
						var recentBatterGap = options.RoleShortEmptyWalkupSeconds
							+ Math.Max(0, options.SampleIntervalSeconds);
						var recentBatter = lastHitterSeconds >= 0
							&& elapsed - lastHitterSeconds < recentBatterGap;
						var looksEmptyNear = bestLooksEmptyEndSeconds >= 0
							&& bestLooksEmptyEndSeconds <= throwDownSince + 0.05
							&& throwDownSince - bestLooksEmptyEndSeconds
							<= options.RoleEmptyBeforeHitterSeconds;
						var looksEmptySolid = looksEmptyNear
							&& bestLooksEmptyClearSeconds >= options.RoleSolidEmptySeconds;
						// Mid-AB catcher crouches keep EmptyBoxDefense extending a short blank;
						// require a real LooksEmpty clear when a batter was just in the box.
						var hadEmptyArm = classicEmptyArm && (!recentBatter || looksEmptySolid);
						var strongHold = Math.Max(
							options.RoleThrowDownStrongSeconds,
							options.RoleThrowDownHoldSeconds);
						var strongEnough = throwDownAccumSeconds >= strongHold;
						var classicNormal = classicEmptyArm
							&& throwDownAccumSeconds >= options.RoleThrowDownHoldSeconds;
						var wall = elapsed - throwDownSince;
						var throwDownGap = ThrowDownGapLimitSeconds(options);
						var denseLimit = (hadEmptyArm || classicEmptyArm
								? options.RoleThrowDownHoldSeconds
								: strongHold)
							+ throwDownGap;
						var denseOk = wall <= denseLimit;
						var midInningRisk = options.RoleMidInningRiskSeconds > 0
							&& lastInProgressSeconds >= 0
							&& elapsed - lastInProgressSeconds < options.RoleMidInningRiskSeconds;
						// Pitcher on the mound during throw-down — real pre-half warmups /
						// mound huddles; mid-inning catcher-only flickers often lack it.
						if (minGapOk && denseOk && throwDownSawPitcher
							&& (classicNormal || strongEnough))
						{
							// Mid-at-bat empty-box catcher: batter was just in the box and there
							// was no solid LooksEmpty field clear (Coyotes false Top 2nd @28:05).
							if (recentBatter && !looksEmptySolid)
							{
								Status(
									$"[{FormatElapsed(elapsed)}] Ignoring throw-down (batter in box within {recentBatterGap:0}s, no solid LooksEmpty clear) — not end of half");
								throwDownSince = -1;
								throwDownLastSeen = -1;
								throwDownAccumSeconds = 0;
								throwDownSawPitcher = false;
							}
							// Only suppress strong (no empty clear) throw-downs under mid-inning
							// risk. Coyotes Top 3rd is a normal empty-arm throw-down ~2 min after
							// the last in-progress signal — must not be ignored.
							else if (midInningRisk && !classicEmptyArm)
							{
								Status(
									$"[{FormatElapsed(elapsed)}] Ignoring strong throw-down (in progress within {options.RoleMidInningRiskSeconds:0}s, no field clear) — not end of half");
								throwDownSince = -1;
								throwDownLastSeen = -1;
								throwDownAccumSeconds = 0;
								throwDownSawPitcher = false;
							}
							else
							{
								// Bookmark near the latest throw-down sample, not the first empty-box
								// catcher crouch (warmup often starts ~1 min before the throw).
								pendingThrowDownSeconds = throwDownLastSeen >= 0
									? throwDownLastSeen
									: throwDownSince;
								pendingThrowDownArmedAt = pendingThrowDownSeconds;
								pendingThrowDownFrozen = false;
								lastEmptyEndSeconds = -1;
								lastEmptyArmStartSeconds = -1;
								lastLooksEmptyArmStartSeconds = -1;
								lastLooksEmptyEndSeconds = -1;
								defenseOnlySince = -1;
								Status(
									strongEnough && !hadEmptyArm
										? $"[{FormatElapsed(pendingThrowDownSeconds)}] Throw-down pending (strong empty-box defense) — waiting for batter"
										: $"[{FormatElapsed(pendingThrowDownSeconds)}] Throw-down pending (empty box + catcher/pitcher) — waiting for batter");
								// Keep throwDownLastSeen so we can refuse early batter confirms while
								// the ritual is still ongoing, and slide pending on later TD frames.
								throwDownSince = -1;
								throwDownAccumSeconds = 0;
								throwDownSawPitcher = false;
							}
						}
						else if (!minGapOk || wall > strongHold + throwDownGap)
						{
							throwDownSince = -1;
							throwDownLastSeen = -1;
							throwDownAccumSeconds = 0;
							throwDownSawPitcher = false;
						}
					}

					if (signals.BatterPresent)
					{
						var minGapOk = elapsed - lastHalfSeconds >= options.RoleMinSecondsBetweenHalfInnings;
						// Do not confirm on a false hitter mid-warmup — wait until empty-box
						// defense has paused long enough that the throw-down ritual finished.
						var pauseNeed = Math.Max(
							options.RoleThrowDownPauseSeconds,
							ThrowDownGapLimitSeconds(options));
						// Frozen pending already saw a solid LooksEmpty — ritual is over even if
						// a new-half catcher briefly reappears in EmptyBoxDefense.
						var throwDownPaused = pendingThrowDownFrozen
							|| throwDownLastSeen < 0
							|| elapsed - throwDownLastSeen >= pauseNeed;

						// Confirm pending throw-down with the first batter walk-up after ritual.
						// Mid-inning risk is applied only at arm time (strong path); normal
						// empty-arm pendings may sit inside the window and must still confirm.
						if (topFirstSeeded
							&& pendingThrowDownSeconds >= 0
							&& throwDownPaused
							&& pendingThrowDownSeconds - lastHalfSeconds
							>= options.RoleMinSecondsBetweenHalfInnings
							&& elapsed - pendingThrowDownSeconds <= options.RoleThrowDownConfirmSeconds)
						{
							var hitAt = pendingThrowDownSeconds;
							halfStarts.Add(hitAt);
							lastHalfSeconds = hitAt;
							pendingThrowDownSeconds = -1;
							pendingThrowDownArmedAt = -1;
							pendingThrowDownFrozen = false;
							throwDownSince = -1;
							throwDownLastSeen = -1;
							throwDownAccumSeconds = 0;
							throwDownSawPitcher = false;
							defenseOnlySince = -1;
							lastEmptyArmStartSeconds = -1;
							lastEmptyEndSeconds = -1;
							lastLooksEmptyArmStartSeconds = -1;
							lastLooksEmptyEndSeconds = -1;
							bestLooksEmptyClearSeconds = 0;
							bestLooksEmptyEndSeconds = -1;
							Status(
								$"[{FormatElapsed(hitAt)}] Half-inning start (catcher throw-down, confirmed by batter at {FormatElapsed(elapsed)}) → {InningHalfLabels.FormatHalfInning(halfStarts.Count - 1)}");
						}
						else
						{
							if (pendingThrowDownSeconds >= 0
								&& elapsed - pendingThrowDownSeconds > options.RoleThrowDownConfirmSeconds)
							{
								pendingThrowDownSeconds = -1;
								pendingThrowDownArmedAt = -1;
								pendingThrowDownFrozen = false;
							}

							var noHitterGapOk = lastHitterSeconds < 0
								|| elapsed - lastHitterSeconds >= options.RoleNoHitterGapSeconds;
							var emptyAgeOk = lastEmptyEndSeconds > lastHalfSeconds
								&& elapsed - lastEmptyEndSeconds <= options.RoleEmptyBeforeHitterSeconds;
							var totalClear = lastEmptyArmStartSeconds >= 0
								&& lastEmptyEndSeconds >= lastEmptyArmStartSeconds
								? lastEmptyEndSeconds - lastEmptyArmStartSeconds
								: 0;
							// Fallback only after a long clear — throw-down is the primary cue.
							var emptyQualityOk = totalClear >= Math.Max(options.RoleSolidEmptySeconds * 2, 40);
							// Same mid-inning gate as throw-down: between-batter blanks after
							// batter+on-deck must not bookmark via empty→hitter either.
							var midInningRisk = options.RoleMidInningRiskSeconds > 0
								&& lastInProgressSeconds >= 0
								&& elapsed - lastInProgressSeconds < options.RoleMidInningRiskSeconds;
							var emptyBeforeOk = emptyAgeOk && emptyQualityOk && !midInningRisk;

							if (topFirstSeeded && noHitterGapOk && emptyBeforeOk && minGapOk
								&& pendingThrowDownSeconds < 0)
							{
								var earliest = FindEarliestBatterStartInSamples(
									samples,
									i,
									detector,
									options,
									useRoles,
									notBeforeSeconds: lastHalfSeconds,
									ct);
								halfStarts.Add(earliest);
								lastHalfSeconds = earliest;
								defenseOnlySince = -1;
								throwDownSince = -1;
								throwDownLastSeen = -1;
								throwDownAccumSeconds = 0;
								throwDownSawPitcher = false;
								lastEmptyArmStartSeconds = -1;
								lastLooksEmptyArmStartSeconds = -1;
								lastLooksEmptyEndSeconds = -1;
								bestLooksEmptyClearSeconds = 0;
								bestLooksEmptyEndSeconds = -1;
								if (earliest < elapsed - 0.05)
								{
									Status(
										$"[{FormatElapsed(earliest)}] Half-inning start (hitter after empty, walked back from {FormatElapsed(elapsed)}) → {InningHalfLabels.FormatHalfInning(halfStarts.Count - 1)}");
								}
								else
								{
									Status(
										$"[{FormatElapsed(earliest)}] Half-inning start (hitter after empty) → {InningHalfLabels.FormatHalfInning(halfStarts.Count - 1)}");
								}
							}
							else if (lastEmptyEndSeconds > lastHalfSeconds
								&& throwDownSince < 0
								&& pendingThrowDownSeconds < 0
								&& signals.DefenseReady)
							{
								lastEmptyEndSeconds = -1;
								lastEmptyArmStartSeconds = -1;
								lastLooksEmptyArmStartSeconds = -1;
								lastLooksEmptyEndSeconds = -1;
								defenseOnlySince = -1;
							}
						}

						lastHitterSeconds = elapsed;
					}
				}
				else
				{
					// Person-model path: seed Top 1st at first non-intro sample, then classic SM.
					if (options.AssumeTopFirstAtGameStart && !topFirstSeeded)
					{
						halfStarts.Add(elapsed);
						lastHalfSeconds = elapsed;
						topFirstSeeded = true;
						phase = Phase.Playing;
						emptySince = -1;
						warmupSince = -1;
						warmupConfirmed = false;
						Status(
							sawLeadingIntro
								? $"[{FormatElapsed(elapsed)}] Top 1st (game start after intro)"
								: $"[{FormatElapsed(elapsed)}] Top 1st (game start)");
					}

					switch (phase)
					{
						case Phase.Playing:
							if (signals.InningInProgress)
							{
								emptySince = -1;
							}
							else if (signals.LooksEmpty)
							{
								if (emptySince < 0)
									emptySince = elapsed;
								else if (elapsed - emptySince >= options.EmptyFieldHoldSeconds)
								{
									phase = Phase.FieldClearing;
									warmupConfirmed = false;
									warmupSince = -1;
									Status($"[{FormatElapsed(elapsed)}] Field clearing / side change");
								}
							}
							else
							{
								emptySince = -1;
							}
							break;

						case Phase.FieldClearing:
							if (signals.DefenseReady && !signals.BatterPresent)
							{
								phase = Phase.Warmup;
								warmupSince = elapsed;
								warmupConfirmed = false;
								Status(
									signals.CoachNearPlate
										? $"[{FormatElapsed(elapsed)}] Defense out / pitcher warmup (coach near plate, empty batter's box)"
										: $"[{FormatElapsed(elapsed)}] Defense out / pitcher warmup (empty batter's box)");
							}
							break;

						case Phase.Warmup:
							if (signals.LooksEmpty)
							{
								if (emptySince < 0)
									emptySince = elapsed;
								else if (elapsed - emptySince >= options.EmptyFieldHoldSeconds)
								{
									phase = Phase.FieldClearing;
									warmupSince = -1;
									warmupConfirmed = false;
									emptySince = -1;
									Status($"[{FormatElapsed(elapsed)}] Warmup abandoned — field empty again");
								}

								break;
							}

							emptySince = -1;

							if (!signals.BatterPresent)
							{
								if (!signals.DefenseReady)
									break;

								if (warmupSince < 0)
									warmupSince = elapsed;
								else if (!warmupConfirmed
									&& elapsed - warmupSince >= options.WarmupHoldSeconds)
								{
									warmupConfirmed = true;
									Status(
										signals.CoachNearPlate
											? $"[{FormatElapsed(elapsed)}] Warmup confirmed (coach OK) — waiting for first batter in the box"
											: $"[{FormatElapsed(elapsed)}] Warmup confirmed — waiting for first batter in the box");
								}

								break;
							}

							if (warmupConfirmed
								&& elapsed - lastHalfSeconds >= options.MinSecondsBetweenHalfInnings)
							{
								var earliest = FindEarliestBatterStartInSamples(
									samples,
									i,
									detector,
									options,
									useRoles,
									notBeforeSeconds: lastHalfSeconds,
									ct);
								halfStarts.Add(earliest);
								lastHalfSeconds = earliest;
								phase = Phase.Playing;
								emptySince = -1;
								warmupSince = -1;
								warmupConfirmed = false;
								if (earliest < elapsed - 0.05)
								{
									Status(
										$"[{FormatElapsed(earliest)}] Half-inning start (first batter in box, walked back from {FormatElapsed(elapsed)}) → {InningHalfLabels.FormatHalfInning(halfStarts.Count - 1)}");
								}
								else
								{
									Status(
										$"[{FormatElapsed(earliest)}] Half-inning start (first batter in box) → {InningHalfLabels.FormatHalfInning(halfStarts.Count - 1)}");
								}
							}
							else if (!warmupConfirmed)
							{
								warmupSince = -1;
							}
							break;
					}
				}

				if (i % 10 == 0 || i + 1 == samples.Count)
				{
					var phaseLabel = useRoles
						? (topFirstSeeded ? "watching for empty→hitter half-innings" : "waiting for first defense (Top 1st)")
						: phase switch
						{
							Phase.Playing => "watching play",
							Phase.FieldClearing => "waiting for defense/warmup",
							Phase.Warmup => warmupConfirmed
								? "waiting for first batter in box"
								: signals.CoachNearPlate
									? "confirming warmup (coach near plate)"
									: "confirming empty-box warmup",
							_ => phase.ToString(),
						};
					var roleHint = useRoles
						? $"; P={signals.HasPitcher} H={signals.HasHitter} C={signals.HasCatcher}"
						: "";
					Status(
						$"Analyzing gameplay… {i + 1}/{samples.Count} ({FormatElapsed(elapsed)}) — {phaseLabel}{roleHint}; found {halfStarts.Count} half-inning(s)");
				}
			}

			progress01?.Invoke(analyzeProgressEnd);

			// Fallback only when Top 1st seeding is disabled.
			if (halfStarts.Count == 0 && !options.AssumeTopFirstAtGameStart)
			{
				Status(
					useRoles
						? "No half-inning transitions found — scanning for Top 1st hitter-after-warmup fallback…"
						: "No half-inning transitions found — scanning for Top 1st batter-in-box fallback…");
				var first = FindFirstBatterAfterWarmup(samples, detector, options, useRoles, ct);
				if (first.HasValue)
				{
					halfStarts.Add(first.Value);
					Status(
						useRoles
							? $"[{FormatElapsed(first.Value)}] Assumed Top 1st (first hitter after pitcher warmup)"
							: $"[{FormatElapsed(first.Value)}] Assumed Top 1st (first batter in box after warmup)");
				}
			}
			else if (halfStarts.Count == 0)
			{
				halfStarts.Add(0);
				Status("[00:00:00] Top 1st (game start fallback)");
			}

			if (options.RefineHalfInningHits && halfStarts.Count > 0)
			{
				Status(
					$"Refining {halfStarts.Count} half-inning hit(s) " +
					$"(±lookback, sample every {Math.Max(0.1, options.RefineIntervalSeconds):0.###}s)…");
				for (var hi = 0; hi < halfStarts.Count; hi++)
				{
					ct.ThrowIfCancellationRequested();
					var coarse = halfStarts[hi];
					// Keep seeded Top 1st at game start / post-intro — don't hunt earlier.
					if (hi == 0 && options.AssumeTopFirstAtGameStart)
					{
						Status(
							$"[{FormatElapsed(coarse)}] {InningHalfLabels.FormatHalfInning(hi)} " +
							"kept at game start (no refine)");
						var seedFrac = halfStarts.Count <= 1 ? 1.0 : (double)(hi + 1) / halfStarts.Count;
						progress01?.Invoke(analyzeProgressEnd + (0.98 - analyzeProgressEnd) * seedFrac);
						continue;
					}

					var previousHalf = hi > 0 ? halfStarts[hi - 1] : 0;
					var refined = await RefineHalfInningStartAsync(
							resultVideoPath,
							coarse,
							durationSeconds,
							detector,
							options,
							useRoles,
							ct,
							Status,
							refineMedia,
							artifactSession,
							halfIndex: hi,
							notBeforeSeconds: previousHalf)
						.ConfigureAwait(false);
					halfStarts[hi] = refined;
					var refineFrac = halfStarts.Count <= 1 ? 1.0 : (double)(hi + 1) / halfStarts.Count;
					progress01?.Invoke(analyzeProgressEnd + (0.98 - analyzeProgressEnd) * refineFrac);
					if (Math.Abs(refined - coarse) >= 0.05)
					{
						Status(
							$"[{FormatElapsed(refined)}] Refined {InningHalfLabels.FormatHalfInning(hi)} " +
							$"start {FormatElapsed(coarse)} → {FormatElapsed(refined)}");
					}
					else
					{
						Status(
							$"[{FormatElapsed(refined)}] {InningHalfLabels.FormatHalfInning(hi)} " +
							"start already at best sample in window");
					}
				}
			}

			var events = BuildEvents(halfStarts, durationSeconds);
			string outputPath = null;
			string youtubePath = null;
			if (writeOutputFiles)
			{
				Status("Writing recording JSON and YouTube bookmarks…");
				progress01?.Invoke(0.99);
				outputPath = string.IsNullOrWhiteSpace(outputJsonPath)
					? InningDetectionRecordingWriter.BuildUniqueOutputPath(resultVideoPath)
					: outputJsonPath.Trim();
				var root = InningDetectionRecordingWriter.BuildSessionJson(resultVideoPath, durationSeconds, events);
				(_, youtubePath) = await InningDetectionRecordingWriter
					.WriteSessionAndYoutubeAsync(root, outputPath, ct, log)
					.ConfigureAwait(false);
			}

			progress01?.Invoke(1);

			if (options.SkipIntroTitleCards && introSkipCount > 0)
			{
				Status(
					$"Skipped {introSkipCount} title-card sample(s) (~{introSkipCount * options.SampleIntervalSeconds:0.#}s).");
			}

			Status($"Detection complete — {halfStarts.Count} half-inning start(s).");

			if (artifactSession != null)
			{
				await artifactSession.WriteEventsAsync(artifactSession.EventsGamePath, events, ct)
					.ConfigureAwait(false);
				if (finalizeArtifacts)
				{
					var manifest = new InningDetectionArtifactManifest
					{
						SampleIntervalSeconds = options.SampleIntervalSeconds,
						GameDurationSeconds = durationSeconds,
						OutputDurationSeconds = durationSeconds,
						IntroOffsetSeconds = 0,
						ModelPath = resolvedModel.Path,
						ModelKind = resolvedModel.Kind.ToString(),
						RecordingJsonPath = outputPath,
						YoutubeDescriptionPath = youtubePath,
						HalfInningEvents = events
							.Where(e => string.Equals(e.Kind, "half_inning", StringComparison.OrdinalIgnoreCase))
							.ToList(),
					};
					await artifactSession.FinalizeAsync(manifest, ct, log).ConfigureAwait(false);
					Status($"Cursor handoff: {artifactSession.HandoffMarkdownPath}");
				}
			}

			return new InningDetectionResult
			{
				VideoPath = resultVideoPath,
				OutputJsonPath = outputPath,
				OutputYoutubeDescriptionPath = youtubePath,
				DurationSeconds = durationSeconds,
				Events = events,
				ArtifactHandoffPath = artifactSession?.HandoffMarkdownPath,
				ArtifactSessionDirectory = artifactSession?.SessionDirectory,
			};
		}
		finally
		{
			if (ownSampleFiles && artifactSession == null)
			{
				foreach (var sample in samples)
					TempPathHelper.DeleteTemporaryFileUnlessRetained(sample.Path);
			}
		}
	}

	private static List<InningDetectionEvent> BuildEvents(IReadOnlyList<double> halfStarts, double durationSeconds)
	{
		var events = new List<InningDetectionEvent>
		{
			new()
			{
				ElapsedSeconds = 0,
				Label = InningHalfLabels.RecordingStart,
				Kind = "recording",
			},
		};

		for (var i = 0; i < halfStarts.Count; i++)
		{
			events.Add(new InningDetectionEvent
			{
				ElapsedSeconds = halfStarts[i],
				Label = InningHalfLabels.FormatHalfInning(i),
				Kind = "half_inning",
			});
		}

		events.Add(new InningDetectionEvent
		{
			ElapsedSeconds = Math.Max(0, durationSeconds),
			Label = InningHalfLabels.RecordingEnd,
			Kind = "recording",
		});

		return events;
	}

	private readonly record struct FrameSignals(
		bool HasHitter,
		bool HasPitcher,
		bool HasCatcher,
		bool BatterPresent,
		bool DefenseReady,
		bool LooksEmpty,
		bool CoachNearPlate,
		bool OnDeckPresent,
		bool InningInProgress,
		bool EmptyBoxDefense);

	private static FrameSignals BuildFrameSignals(
		IReadOnlyList<DetectedObject> detections,
		int frameW,
		int frameH,
		Options options,
		bool useRoles)
	{
		if (useRoles)
		{
			var hasHitter = HasClassAbove(detections, "hitter", options.RoleHitterConfidence);
			var hasPitcher = HasClassAbove(detections, "pitcher", options.RolePitcherConfidence);
			var hasCatcher = HasClassAbove(detections, "catcher", options.RoleCatcherConfidence);
			// Only a hitter whose center is in the batter's box counts as "batter present".
			// Sideline/on-deck "hitter" labels are common during throw-downs and must not block
			// empty / empty-box-defense tracking.
			var hitterInBox = CountClassInRoiAbove(
				detections, "hitter", options.RoleHitterConfidence, frameW, frameH, options.BatterBoxRoi) > 0;
			var onDeckInRoi = CountClassInRoiAbove(
					detections, "hitter", options.RoleHitterConfidence, frameW, frameH, options.OnDeckLeftRoi)
				+ CountClassInRoiAbove(
					detections, "hitter", options.RoleHitterConfidence, frameW, frameH, options.OnDeckRightRoi);
			var hittersOutsideBox = CountClassOutsideRoiAbove(
				detections, "hitter", options.RoleHitterConfidence, frameW, frameH, options.BatterBoxRoi);
			var onDeckPresent = onDeckInRoi > 0
				|| (hitterInBox && hittersOutsideBox > 0);
			var batterPresent = hitterInBox;
			// Plate-area catcher only — dugout/sideline "catcher" FPs are common on this angle
			// and were blocking real side-change empties while also not being real defense.
			var catcherAtPlate = CountClassInRoiAbove(
					detections, "catcher", options.RoleCatcherConfidence, frameW, frameH, options.BatterApproachRoi)
				+ CountClassInRoiAbove(
					detections, "catcher", options.RoleCatcherConfidence, frameW, frameH, options.BatterBoxRoi)
				> 0;
			// BaseballCV PHC often misses the distant mound pitcher on kids/backstop wide shots;
			// catcher gear is larger and more reliable as a "defense is out" proxy.
			var defenseReady = hasPitcher || catcherAtPlate;
			// Pre-half ritual: catcher at plate, empty box (throw to second; umpire usually unset).
			var emptyBoxDefense = catcherAtPlate && !hitterInBox;
			// Side-change empty: no hitter label at all and no plate catcher. Sideline "hitter"
			// FPs still block empty (reduces mid-inning false arms); throw-down uses empty-box
			// defense and does not require LooksEmpty on the same frames.
			var looksEmpty = !hasHitter && !catcherAtPlate;
			var inningInProgress = options.UseOnDeckInProgressSignal && hitterInBox && onDeckPresent;
			return new FrameSignals(
				hasHitter, hasPitcher, hasCatcher, batterPresent, defenseReady, looksEmpty,
				CoachNearPlate: false, OnDeckPresent: onDeckPresent, InningInProgress: inningInProgress,
				EmptyBoxDefense: emptyBoxDefense);
		}

		var fieldCount = CountInRoi(detections, frameW, frameH, options.FieldRoi);
		var batterInBox = CountInRoi(detections, frameW, frameH, options.BatterBoxRoi) > 0;
		var personOnDeck = CountInRoi(detections, frameW, frameH, options.OnDeckLeftRoi)
			+ CountInRoi(detections, frameW, frameH, options.OnDeckRightRoi) > 0;
		var coachNearPlate = !batterInBox
			&& CountInRoi(detections, frameW, frameH, options.BatterApproachRoi) > 0;
		var personInProgress = options.UseOnDeckInProgressSignal && batterInBox && personOnDeck;
		var fieldDefenseReady = fieldCount >= options.MinFieldersPlaying;
		return new FrameSignals(
			HasHitter: batterInBox,
			HasPitcher: fieldDefenseReady,
			HasCatcher: false,
			BatterPresent: batterInBox,
			DefenseReady: fieldDefenseReady,
			LooksEmpty: fieldCount <= options.MaxFieldersEmpty,
			CoachNearPlate: coachNearPlate,
			OnDeckPresent: personOnDeck,
			InningInProgress: personInProgress,
			EmptyBoxDefense: fieldDefenseReady && !batterInBox);
	}

	/// <summary>
	/// Fallback for Top 1st: warmup without a batter/hitter, then first batter/hitter.
	/// </summary>
	private static double? FindFirstBatterAfterWarmup(
		IReadOnlyList<TimedSampleFrame> samples,
		YoloOnnxDetector detector,
		Options options,
		bool useRoles,
		CancellationToken ct)
	{
		var warmupSince = -1.0;
		var warmupConfirmed = false;
		var introStreak = 0;
		var skippingIntro = false;
		var warmupNeed = Math.Min(options.WarmupHoldSeconds, Math.Max(0, options.FirstHalfWarmupHoldSeconds));
		for (var i = 0; i < samples.Count; i++)
		{
			ct.ThrowIfCancellationRequested();
			var elapsed = samples[i].ElapsedSeconds;
			using var mat = Cv2.ImRead(samples[i].Path, ImreadModes.Color);
			if (mat.Empty())
				continue;

			if (options.SkipIntroTitleCards)
			{
				if (IntroTitleCardClassifier.IsSolidTextCard(mat))
				{
					introStreak++;
					if (introStreak >= Math.Max(1, options.IntroConfirmSamples))
						skippingIntro = true;
				}
				else
				{
					skippingIntro = false;
					introStreak = 0;
				}

				if (skippingIntro)
				{
					warmupSince = -1;
					warmupConfirmed = false;
					continue;
				}
			}

			var signals = BuildFrameSignals(detector.Detect(mat), mat.Width, mat.Height, options, useRoles);
			if (!signals.DefenseReady)
			{
				warmupSince = -1;
				warmupConfirmed = false;
				continue;
			}

			if (!signals.BatterPresent)
			{
				if (warmupSince < 0)
					warmupSince = elapsed;
				else if (!warmupConfirmed && elapsed - warmupSince >= warmupNeed)
					warmupConfirmed = true;
				continue;
			}

			if (warmupConfirmed)
				return elapsed;

			warmupSince = -1;
		}

		return null;
	}

	private static bool HasClass(IReadOnlyList<DetectedObject> detections, string className) =>
		detections.Any(d => string.Equals(d.ClassName, className, StringComparison.OrdinalIgnoreCase));

	private static bool HasClassAbove(IReadOnlyList<DetectedObject> detections, string className, float minConfidence) =>
		detections.Any(d =>
			string.Equals(d.ClassName, className, StringComparison.OrdinalIgnoreCase)
			&& d.Confidence >= minConfidence);

	private static int CountClassInRoi(
		IReadOnlyList<DetectedObject> detections,
		string className,
		int frameW,
		int frameH,
		Rect2d roiNorm) =>
		CountClassInRoiAbove(detections, className, 0f, frameW, frameH, roiNorm);

	private static int CountClassInRoiAbove(
		IReadOnlyList<DetectedObject> detections,
		string className,
		float minConfidence,
		int frameW,
		int frameH,
		Rect2d roiNorm)
	{
		var roi = ToPixelRoi(roiNorm, frameW, frameH);
		var count = 0;
		foreach (var det in detections)
		{
			if (!string.Equals(det.ClassName, className, StringComparison.OrdinalIgnoreCase))
				continue;
			if (det.Confidence < minConfidence)
				continue;
			var c = new Point(det.Box.X + det.Box.Width / 2, det.Box.Y + det.Box.Height / 2);
			if (roi.Contains(c))
				count++;
		}

		return count;
	}

	private static int CountClassOutsideRoiAbove(
		IReadOnlyList<DetectedObject> detections,
		string className,
		float minConfidence,
		int frameW,
		int frameH,
		Rect2d roiNorm)
	{
		var roi = ToPixelRoi(roiNorm, frameW, frameH);
		var count = 0;
		foreach (var det in detections)
		{
			if (!string.Equals(det.ClassName, className, StringComparison.OrdinalIgnoreCase))
				continue;
			if (det.Confidence < minConfidence)
				continue;
			var c = new Point(det.Box.X + det.Box.Width / 2, det.Box.Y + det.Box.Height / 2);
			if (!roi.Contains(c))
				count++;
		}

		return count;
	}

	private static int CountInRoi(IReadOnlyList<DetectedObject> detections, int frameW, int frameH, Rect2d roiNorm)
	{
		var roi = ToPixelRoi(roiNorm, frameW, frameH);
		var count = 0;
		foreach (var det in detections)
		{
			var c = new Point(det.Box.X + det.Box.Width / 2, det.Box.Y + det.Box.Height / 2);
			if (roi.Contains(c))
				count++;
		}

		return count;
	}

	private static Rect ToPixelRoi(Rect2d roiNorm, int frameW, int frameH) =>
		new(
			(int)(roiNorm.X * frameW),
			(int)(roiNorm.Y * frameH),
			Math.Max(1, (int)(roiNorm.Width * frameW)),
			Math.Max(1, (int)(roiNorm.Height * frameH)));

	/// <summary>
	/// Coarse hits fire on the first 5s sample that shows a batter/hitter. The true start lies
	/// in the preceding coarse interval — densely sample that window and pick the earliest
	/// high-confidence batter/hitter frame (preferring hitter-in-box / higher confidence).
	/// </summary>
	private static async Task<double> RefineHalfInningStartAsync(
		string fallbackVideoPath,
		double coarseHitSeconds,
		double durationSeconds,
		YoloOnnxDetector detector,
		Options options,
		bool useRoles,
		CancellationToken ct,
		Action<string> status,
		IRefineMediaSource refineMedia = null,
		InningDetectionArtifactSession artifactSession = null,
		int halfIndex = 0,
		double notBeforeSeconds = 0)
	{
		var lookback = Math.Max(
			0.25,
			options.RefineLookbackSeconds ?? Options.DefaultRefineLookbackSeconds);
		var lookahead = Math.Max(0, options.RefineLookaheadSeconds);
		var refineInterval = Math.Clamp(options.RefineIntervalSeconds, 0.1, Math.Max(0.1, options.SampleIntervalSeconds));

		// Prefer early: search a wide window before the coarse hit, but never into the prior half.
		var windowStart = Math.Max(Math.Max(0, notBeforeSeconds), coarseHitSeconds - lookback);
		var windowEnd = coarseHitSeconds + lookahead;
		if (durationSeconds > 0)
			windowEnd = Math.Min(durationSeconds, windowEnd);
		if (windowEnd < coarseHitSeconds)
			windowEnd = coarseHitSeconds;

		var windowDuration = Math.Max(refineInterval, windowEnd - windowStart + refineInterval);
		status?.Invoke(
			$"Refine window {FormatElapsed(windowStart)}–{FormatElapsed(Math.Min(windowStart + windowDuration, windowEnd + refineInterval))} " +
			$"(coarse hit {FormatElapsed(coarseHitSeconds)})");

		if (!TryResolveRefineExtract(
			    refineMedia,
			    fallbackVideoPath,
			    windowStart,
			    out var extractPath,
			    out var extractLocalStart))
		{
			status?.Invoke("Refine skipped — could not resolve media for refine window.");
			return coarseHitSeconds;
		}

		var keepRefine = artifactSession != null;
		var refineDir = keepRefine
			? artifactSession.CreateRefineWindowDirectory(halfIndex, coarseHitSeconds)
			: Path.Combine(TempPathHelper.GetTempPath(), $"innings_refine_{Guid.NewGuid():N}");
		if (!keepRefine)
			Directory.CreateDirectory(refineDir);
		try
		{
			await ExtractRangeSampleFramesAsync(
					extractPath,
					refineDir,
					extractLocalStart,
					windowDuration,
					refineInterval,
					options.AnalysisWidth,
					ct,
					log: null,
					status)
				.ConfigureAwait(false);

			var frames = Directory.GetFiles(refineDir, "*.jpg").OrderBy(f => f, StringComparer.Ordinal).ToArray();
			if (frames.Length == 0)
				return coarseHitSeconds;

			// Collect batter-present samples, then bookmark the start of the contiguous run that
			// contains (or immediately precedes) the coarse hit. Taking the earliest after-empty
			// flicker in the lookback window was snapping to false mid-warmup hitters ~45s early.
			var batterSamples = new List<(double Time, float Confidence, bool InBox)>();
			var localToGame = windowStart - extractLocalStart;

			for (var i = 0; i < frames.Length; i++)
			{
				ct.ThrowIfCancellationRequested();
				var t = extractLocalStart + i * refineInterval + localToGame;
				if (t > coarseHitSeconds + lookahead + 0.001)
					break;

				using var mat = Cv2.ImRead(frames[i], ImreadModes.Color);
				if (mat.Empty())
					continue;

				if (options.SkipIntroTitleCards && IntroTitleCardClassifier.IsSolidTextCard(mat))
					continue;

				var detections = detector.Detect(mat);
				var signals = BuildFrameSignals(detections, mat.Width, mat.Height, options, useRoles);

				if (!signals.BatterPresent)
					continue;

				var confidence = BestBatterConfidence(detections, useRoles, mat.Width, mat.Height, options);
				var inBox = useRoles
					? CountClassInRoi(detections, "hitter", mat.Width, mat.Height, options.BatterBoxRoi) > 0
					: CountInRoi(detections, mat.Width, mat.Height, options.BatterBoxRoi) > 0;
				batterSamples.Add((t, confidence, inBox));
			}

			if (batterSamples.Count == 0)
				return coarseHitSeconds;

			var minConfidence = useRoles
				? options.RoleHitterConfidence
				: options.ConfidenceThreshold;

			return PickRefineBatterRunStart(batterSamples, coarseHitSeconds, refineInterval, minConfidence);
		}
		finally
		{
			if (!keepRefine)
				TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(refineDir, recursive: true);
		}
	}

	/// <summary>
	/// Picks the start of the batter-present run that contains the coarse hit, or the last run
	/// that ends at/before the coarse hit. Ignores earlier isolated flickers in the lookback.
	/// </summary>
	private static double PickRefineBatterRunStart(
		IReadOnlyList<(double Time, float Confidence, bool InBox)> batterSamples,
		double coarseHitSeconds,
		double refineInterval,
		float minConfidence)
	{
		if (batterSamples == null || batterSamples.Count == 0)
			return coarseHitSeconds;

		var gapTol = Math.Max(refineInterval * 1.5, 0.75);
		var runs = new List<(double Start, double End, float BestConf, bool AnyInBox)>();
		double runStart = batterSamples[0].Time;
		double runEnd = batterSamples[0].Time;
		float bestConf = batterSamples[0].Confidence;
		var anyInBox = batterSamples[0].InBox;

		for (var i = 1; i < batterSamples.Count; i++)
		{
			var s = batterSamples[i];
			if (s.Time - runEnd <= gapTol)
			{
				runEnd = s.Time;
				if (s.Confidence > bestConf)
					bestConf = s.Confidence;
				anyInBox |= s.InBox;
			}
			else
			{
				runs.Add((runStart, runEnd, bestConf, anyInBox));
				runStart = s.Time;
				runEnd = s.Time;
				bestConf = s.Confidence;
				anyInBox = s.InBox;
			}
		}

		runs.Add((runStart, runEnd, bestConf, anyInBox));

		// Prefer a confident in-box run that covers the coarse hit.
		foreach (var run in runs)
		{
			if (run.Start <= coarseHitSeconds + 0.001
				&& run.End >= coarseHitSeconds - gapTol
				&& run.BestConf >= minConfidence
				&& run.AnyInBox)
			{
				return run.Start;
			}
		}

		foreach (var run in runs)
		{
			if (run.Start <= coarseHitSeconds + 0.001
				&& run.End >= coarseHitSeconds - gapTol
				&& run.BestConf >= minConfidence)
			{
				return run.Start;
			}
		}

		// Otherwise the last qualifying run at or before the coarse hit (skip early flickers).
		for (var i = runs.Count - 1; i >= 0; i--)
		{
			var run = runs[i];
			if (run.Start <= coarseHitSeconds + 0.001
				&& run.BestConf >= minConfidence
				&& run.AnyInBox)
			{
				return run.Start;
			}
		}

		for (var i = runs.Count - 1; i >= 0; i--)
		{
			var run = runs[i];
			if (run.Start <= coarseHitSeconds + 0.001 && run.BestConf >= minConfidence)
				return run.Start;
		}

		return coarseHitSeconds;
	}

	/// <summary>
	/// Walks backward from a coarse hit through already-extracted samples to the earliest
	/// continuous batter/hitter presence (stops at empty / non-batter). Prefers early bookmarks.
	/// </summary>
	private static double FindEarliestBatterStartInSamples(
		IReadOnlyList<TimedSampleFrame> samples,
		int hitIndex,
		YoloOnnxDetector detector,
		Options options,
		bool useRoles,
		double notBeforeSeconds,
		CancellationToken ct)
	{
		if (samples == null || samples.Count == 0 || hitIndex < 0 || hitIndex >= samples.Count)
			return 0;
		if (hitIndex == 0)
			return samples[0].ElapsedSeconds;

		var coarse = samples[hitIndex].ElapsedSeconds;
		var lookback = Math.Max(0.25, options.RefineLookbackSeconds ?? Options.DefaultRefineLookbackSeconds);
		var floor = Math.Max(Math.Max(0, notBeforeSeconds), coarse - lookback);
		var earliest = coarse;

		for (var j = hitIndex - 1; j >= 0; j--)
		{
			ct.ThrowIfCancellationRequested();
			var t = samples[j].ElapsedSeconds;
			if (t < floor - 0.001)
				break;

			using var mat = Cv2.ImRead(samples[j].Path, ImreadModes.Color);
			if (mat.Empty())
				continue;

			if (options.SkipIntroTitleCards && IntroTitleCardClassifier.IsSolidTextCard(mat))
				break;

			var detections = detector.Detect(mat);
			var signals = BuildFrameSignals(detections, mat.Width, mat.Height, options, useRoles);
			if (signals.BatterPresent)
			{
				earliest = t;
				continue;
			}

			// Empty or non-batter lead-in — earliest batter after this gap is the start.
			break;
		}

		return earliest;
	}

	private static float BestBatterConfidence(
		IReadOnlyList<DetectedObject> detections,
		bool useRoles,
		int frameW,
		int frameH,
		Options options)
	{
		if (detections == null || detections.Count == 0)
			return 0;

		if (useRoles)
		{
			float best = 0;
			foreach (var d in detections)
			{
				if (!string.Equals(d.ClassName, "hitter", StringComparison.OrdinalIgnoreCase))
					continue;
				if (d.Confidence > best)
					best = d.Confidence;
			}

			return best;
		}

		var roi = ToPixelRoi(options.BatterBoxRoi, frameW, frameH);
		float bestPerson = 0;
		foreach (var d in detections)
		{
			var c = new Point(d.Box.X + d.Box.Width / 2, d.Box.Y + d.Box.Height / 2);
			if (roi.Contains(c) && d.Confidence > bestPerson)
				bestPerson = d.Confidence;
		}

		return bestPerson;
	}

	/// <summary>Public entry for timed range extracts used by parallel per-clip sampling.</summary>
	public static Task ExtractTimedRangeAsync(
		string videoPath,
		string frameDir,
		double startSeconds,
		double durationSeconds,
		double intervalSeconds,
		int analysisWidth,
		CancellationToken ct,
		Action<string> log,
		Action<string> status = null) =>
		ExtractRangeSampleFramesAsync(
			videoPath,
			frameDir,
			startSeconds,
			durationSeconds,
			intervalSeconds,
			analysisWidth,
			ct,
			log,
			status);

	private static bool TryResolveRefineExtract(
		IRefineMediaSource refineMedia,
		string fallbackVideoPath,
		double gameWindowStart,
		out string mediaPath,
		out double localStartSeconds)
	{
		if (refineMedia != null
			&& refineMedia.TryResolve(gameWindowStart, out mediaPath, out localStartSeconds)
			&& !string.IsNullOrWhiteSpace(mediaPath)
			&& File.Exists(mediaPath))
		{
			return true;
		}

		mediaPath = fallbackVideoPath;
		localStartSeconds = gameWindowStart;
		return !string.IsNullOrWhiteSpace(mediaPath) && File.Exists(mediaPath);
	}

	private static async Task ExtractRangeSampleFramesAsync(
		string videoPath,
		string frameDir,
		double startSeconds,
		double durationSeconds,
		double intervalSeconds,
		int analysisWidth,
		CancellationToken ct,
		Action<string> log,
		Action<string> status = null)
	{
		var interval = Math.Max(0.1, intervalSeconds);
		var fps = (1.0 / interval).ToString("0.###", CultureInfo.InvariantCulture);
		var width = Math.Clamp(analysisWidth, 320, 1920);
		var ss = Math.Max(0, startSeconds).ToString("0.###", CultureInfo.InvariantCulture);
		var dur = Math.Max(interval, durationSeconds).ToString("0.###", CultureInfo.InvariantCulture);
		var pattern = Path.Combine(frameDir, "%06d.jpg");
		var vf = $"fps={fps},scale={width}:-1";

		try
		{
			var hwArgs =
				$"-hide_banner -nostdin -y -hwaccel vaapi -hwaccel_device /dev/dri/renderD128 " +
				$"-ss {ss} -i {Quote(videoPath)} -t {dur} -vf {Quote(vf)} -q:v 3 {Quote(pattern)}";
			status?.Invoke("Extracting refine frames (VAAPI)…");
			await FFMpegUtils.Instance.RunCaptureFFMpegAsync(hwArgs, ct, log).ConfigureAwait(false);
			if (Directory.EnumerateFiles(frameDir, "*.jpg").Any())
				return;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (TimeoutException)
		{
			throw;
		}
		catch (Exception ex)
		{
			status?.Invoke($"VAAPI refine extract unavailable ({ex.Message}); using software decode…");
		}

		foreach (var leftover in Directory.EnumerateFiles(frameDir, "*.jpg"))
			FileUtils.TryDeleteFile(leftover);

		var swArgs =
			$"-hide_banner -nostdin -y -ss {ss} -i {Quote(videoPath)} -t {dur} " +
			$"-vf {Quote(vf)} -q:v 3 {Quote(pattern)}";
		status?.Invoke("Extracting refine frames (software)…");
		await FFMpegUtils.Instance.RunCaptureFFMpegAsync(swArgs, ct, log).ConfigureAwait(false);
	}

	/// <summary>
	/// Wall-clock budget for full-video sample extract. Long games at ~2x decode need
	/// well over 30 minutes; scale with duration and clamp to a generous range.
	/// </summary>
	private static TimeSpan ComputeSampleExtractTimeout(double durationSeconds)
	{
		// Assume worst-case ~0.75x realtime + 45 min cushion; min 2h, max 6h.
		var seconds = Math.Max(0, durationSeconds) / 0.75 + 45 * 60;
		var clamped = Math.Clamp(seconds, 2 * 3600, 6 * 3600);
		return TimeSpan.FromSeconds(clamped);
	}

	private static async Task ExtractSampleFramesAsync(
		string videoPath,
		string frameDir,
		Options options,
		double durationSeconds,
		CancellationToken ct,
		Action<string> log,
		Action<string> status = null,
		Action<double> progress01 = null)
	{
		var interval = Math.Max(0.25, options.SampleIntervalSeconds);
		var fps = (1.0 / interval).ToString("0.###", CultureInfo.InvariantCulture);
		var width = Math.Clamp(options.AnalysisWidth, 320, 1920);
		var timeout = ComputeSampleExtractTimeout(durationSeconds);
		var expectedFrames = durationSeconds > 0
			? Math.Max(1, (int)Math.Ceiling(durationSeconds / interval))
			: 0;

		// Prefer VAAPI decode on AMD when available; fall back to software.
		var pattern = Path.Combine(frameDir, "%06d.jpg");
		var vf = $"fps={fps},scale={width}:-1";

		try
		{
			var hwArgs =
				$"-hide_banner -nostdin -y -hwaccel vaapi -hwaccel_device /dev/dri/renderD128 -i {Quote(videoPath)} " +
				$"-vf {Quote(vf)} -q:v 3 {Quote(pattern)}";
			status?.Invoke(
				$"Extracting sample frames (VAAPI decode)… ~{expectedFrames} snapshots " +
				$"(timeout {timeout.TotalMinutes:0} min)");
			await RunExtractWithProgressAsync(
					hwArgs,
					frameDir,
					durationSeconds,
					expectedFrames,
					timeout,
					ct,
					log,
					status,
					progress01)
				.ConfigureAwait(false);
			if (Directory.EnumerateFiles(frameDir, "*.jpg").Any())
			{
				progress01?.Invoke(1);
				status?.Invoke("Sample frame extraction complete.");
				return;
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (TimeoutException)
		{
			throw;
		}
		catch (Exception ex)
		{
			status?.Invoke($"VAAPI frame extract unavailable ({ex.Message}); switching to software decode…");
		}

		foreach (var leftover in Directory.EnumerateFiles(frameDir, "*.jpg"))
			FileUtils.TryDeleteFile(leftover);

		var swArgs =
			$"-hide_banner -nostdin -y -i {Quote(videoPath)} -vf {Quote(vf)} -q:v 3 {Quote(pattern)}";
		status?.Invoke(
			$"Extracting sample frames (software decode)… ~{expectedFrames} snapshots " +
			$"(timeout {timeout.TotalMinutes:0} min)");
		await RunExtractWithProgressAsync(
				swArgs,
				frameDir,
				durationSeconds,
				expectedFrames,
				timeout,
				ct,
				log,
				status,
				progress01)
			.ConfigureAwait(false);
		progress01?.Invoke(1);
		status?.Invoke("Sample frame extraction complete.");
	}

	private static async Task RunExtractWithProgressAsync(
		string ffmpegArgs,
		string frameDir,
		double durationSeconds,
		int expectedFrames,
		TimeSpan timeout,
		CancellationToken ct,
		Action<string> log,
		Action<string> status,
		Action<double> progress01)
	{
		var lastProgress = -1.0;
		var lastStatusAt = DateTime.UtcNow - TimeSpan.FromSeconds(5);
		var lastMediaSeconds = 0.0;

		void Report(double mediaSeconds, int writtenFrames)
		{
			lastMediaSeconds = Math.Max(lastMediaSeconds, mediaSeconds);
			double byTime = 0;
			if (durationSeconds > 0.5)
				byTime = lastMediaSeconds / durationSeconds;
			double byFrames = 0;
			if (expectedFrames > 0 && writtenFrames > 0)
				byFrames = (double)writtenFrames / expectedFrames;

			var p = Math.Clamp(Math.Max(byTime, byFrames), 0, 0.995);
			if (p < lastProgress + 0.002 && p < 0.99)
				return;
			lastProgress = p;
			progress01?.Invoke(p);

			var now = DateTime.UtcNow;
			if ((now - lastStatusAt).TotalSeconds < 2.5 && p < 0.99)
				return;
			lastStatusAt = now;
			var pct = (int)Math.Round(p * 100);
			if (durationSeconds > 0.5)
			{
				status?.Invoke(
					$"Extracting snapshots… {pct}% " +
					$"({FormatElapsed(lastMediaSeconds)} / {FormatElapsed(durationSeconds)}" +
					(writtenFrames > 0 ? $", {writtenFrames}/{expectedFrames} frames" : "") +
					")");
			}
			else if (writtenFrames > 0 && expectedFrames > 0)
			{
				status?.Invoke($"Extracting snapshots… {writtenFrames}/{expectedFrames} frames");
			}
		}

		void OnLog(string line)
		{
			log?.Invoke(line);
			if (string.IsNullOrWhiteSpace(line))
				return;
			if (!FfmpegProgressParser.TryParseTimeSeconds(line, out var mediaSeconds))
				return;

			var written = 0;
			try
			{
				written = Directory.EnumerateFiles(frameDir, "*.jpg").Count();
			}
			catch
			{
				// ignore transient FS errors while ffmpeg is writing
			}

			Report(mediaSeconds, written);
		}

		// Poll frame counts as a backup when ffmpeg progress lines are sparse.
		using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		var pollTask = Task.Run(async () =>
		{
			while (!pollCts.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(1000, pollCts.Token).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					return;
				}

				try
				{
					var written = Directory.EnumerateFiles(frameDir, "*.jpg").Count();
					if (written <= 0)
						continue;
					var inferred = written * Math.Max(0.25, durationSeconds > 0 && expectedFrames > 0
						? durationSeconds / expectedFrames
						: 5.0);
					Report(inferred, written);
				}
				catch
				{
					// ignore
				}
			}
		}, pollCts.Token);

		try
		{
			await FFMpegUtils.Instance.RunCaptureFFMpegAsync(ffmpegArgs, ct, OnLog, timeout)
				.ConfigureAwait(false);
		}
		finally
		{
			pollCts.Cancel();
			try
			{
				await pollTask.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// expected
			}
		}
	}

	private static double ProbeDurationWithOpenCv(string videoPath)
	{
		try
		{
			using var capture = new VideoCapture(videoPath);
			var fps = capture.Fps;
			var frames = capture.Get(VideoCaptureProperties.FrameCount);
			if (fps > 1 && frames > 0)
				return frames / fps;
		}
		catch
		{
			// ignored
		}

		return 0;
	}

	private static double ParseDurationSeconds(string sexagesimalOrSeconds)
	{
		if (string.IsNullOrWhiteSpace(sexagesimalOrSeconds))
			return 0;

		var text = sexagesimalOrSeconds.Trim();
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
			&& !text.Contains(':'))
		{
			return seconds;
		}

		var range = TimeRange.FromString(text.Split('.')[0]);
		return range?.TotalSeconds ?? 0;
	}

	private static string FormatElapsed(double totalSeconds)
	{
		var ts = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(totalSeconds)));
		return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
	}

	private static string Quote(string value)
	{
		if (value == null)
			return "\"\"";
		return "\"" + value.Replace("\"", "\\\"") + "\"";
	}
}
