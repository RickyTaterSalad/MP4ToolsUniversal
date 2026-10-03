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
/// Detects half-inning starts from kids baseball video: after a fielding change and pitcher
/// warmup with an empty batter's box, bookmarks when the first batter walks up to the box.
/// </summary>
public static class HalfInningDetector
{
	public sealed class Options
	{
		/// <summary>
		/// Seconds between analyzed frames. Warmups last minutes, so coarse sampling is enough;
		/// walk-up bookmarks are accurate to about this interval (default 5s).
		/// </summary>
		public double SampleIntervalSeconds { get; set; } = 5.0;

		/// <summary>Max analysis width (keeps aspect ratio).</summary>
		public int AnalysisWidth { get; set; } = 960;

		public float ConfidenceThreshold { get; set; } = 0.35f;

		/// <summary>Normalized batter-box ROI (x, y, w, h) in 0..1 of the frame.</summary>
		public Rect2d BatterBoxRoi { get; set; } = new(0.34, 0.52, 0.32, 0.38);

		/// <summary>
		/// Larger ROI used to catch the batter walking up to the box (includes approach path).
		/// Bookmark fires on first entry here after a confirmed empty-box warmup.
		/// </summary>
		public Rect2d BatterApproachRoi { get; set; } = new(0.26, 0.40, 0.48, 0.52);

		/// <summary>Normalized field ROI where fielders are counted.</summary>
		public Rect2d FieldRoi { get; set; } = new(0.08, 0.12, 0.84, 0.70);

		public int MinFieldersPlaying { get; set; } = 5;
		public int MaxFieldersEmpty { get; set; } = 2;
		public double EmptyFieldHoldSeconds { get; set; } = 15;

		/// <summary>How long the pitcher/defense must be out with an empty batter's box before a walk-up can bookmark.</summary>
		public double WarmupHoldSeconds { get; set; } = 20;

		public double MinSecondsBetweenHalfInnings { get; set; } = 45;

		/// <summary>
		/// Ignore Combine / Replace-Segment intro cards (solid background + text, ~3–10s).
		/// Most appear at the start; mid-video cards are rare but also skipped.
		/// </summary>
		public bool SkipIntroTitleCards { get; set; } = true;

		/// <summary>Require this many consecutive title-card samples before skipping (reduces false positives).</summary>
		public int IntroConfirmSamples { get; set; } = 2;
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
		string outputJsonPath = null)
	{
		if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
			throw new FileNotFoundException("Video not found.", videoPath);

		options ??= new Options();
		var resolvedModel = await YoloModelStore.EnsureModelAsync(modelPath, ct, log).ConfigureAwait(false);

		var durationText = await FFMpegUtils.Instance.GetFileDurationAsync(videoPath, ct, log).ConfigureAwait(false);
		var durationSeconds = ParseDurationSeconds(durationText);
		if (durationSeconds <= 0)
			durationSeconds = ProbeDurationWithOpenCv(videoPath);

		log?.Invoke($"Inning detector: duration {FormatElapsed(durationSeconds)}, sample every {options.SampleIntervalSeconds:0.###}s");

		var frameDir = Path.Combine(TempPathHelper.GetTempPath(), $"innings_frames_{Guid.NewGuid():N}");
		Directory.CreateDirectory(frameDir);

		try
		{
			await ExtractSampleFramesAsync(videoPath, frameDir, options, ct, log).ConfigureAwait(false);
			var frames = Directory.GetFiles(frameDir, "*.jpg").OrderBy(f => f, StringComparer.Ordinal).ToArray();
			if (frames.Length == 0)
				throw new InvalidOperationException("No sample frames were extracted from the video.");

			log?.Invoke($"Analyzing {frames.Length} frames with YOLO person detection…");

			using var detector = new YoloPersonDetector(resolvedModel, options.ConfidenceThreshold);
			var halfStarts = new List<double>();
			var phase = Phase.Playing;
			var emptySince = -1.0;
			var warmupSince = -1.0;
			var warmupConfirmed = false;
			var lastHalfSeconds = -1_000.0;
			var introStreak = 0;
			var skippingIntro = false;
			var introSkipCount = 0;
			double? introSegmentStart = null;

			if (options.SkipIntroTitleCards)
				log?.Invoke("Intro title-card skip enabled (solid background + text).");
			log?.Invoke("Half-inning bookmark: after empty-box pitcher warmup, when first batter walks up.");

			for (var i = 0; i < frames.Length; i++)
			{
				ct.ThrowIfCancellationRequested();
				var elapsed = i * options.SampleIntervalSeconds;
				progress01?.Invoke(frames.Length <= 1 ? 1 : (double)i / (frames.Length - 1));

				using var mat = Cv2.ImRead(frames[i], ImreadModes.Color);
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
							introSegmentStart = elapsed - (introStreak - 1) * options.SampleIntervalSeconds;
							// Intro has no fielders; do not treat it as a half-inning break.
							emptySince = -1;
							warmupSince = -1;
							warmupConfirmed = false;
							if (phase != Phase.Playing)
								phase = Phase.Playing;
							log?.Invoke($"[{FormatElapsed(introSegmentStart.Value)}] Skipping intro / title card…");
						}
					}
					else
					{
						if (skippingIntro)
						{
							var start = introSegmentStart ?? elapsed;
							log?.Invoke(
								$"[{FormatElapsed(elapsed)}] Resumed game footage after title card ({FormatElapsed(start)}–{FormatElapsed(elapsed)}).");
						}

						skippingIntro = false;
						introStreak = 0;
						introSegmentStart = null;
					}

					if (skippingIntro)
					{
						introSkipCount++;
						if (i % 30 == 0)
							log?.Invoke($"Progress {i + 1}/{frames.Length} ({FormatElapsed(elapsed)}) phase=IntroSkip");
						continue;
					}
				}

				var people = detector.Detect(mat);
				var fieldCount = CountInRoi(people, mat.Width, mat.Height, options.FieldRoi);
				var batterInBox = CountInRoi(people, mat.Width, mat.Height, options.BatterBoxRoi) > 0;
				var batterWalkingUp = CountInRoi(people, mat.Width, mat.Height, options.BatterApproachRoi) > 0;
				var plateAreaEmpty = !batterInBox && !batterWalkingUp;

				switch (phase)
				{
					case Phase.Playing:
						if (fieldCount <= options.MaxFieldersEmpty)
						{
							if (emptySince < 0)
								emptySince = elapsed;
							else if (elapsed - emptySince >= options.EmptyFieldHoldSeconds)
							{
								phase = Phase.FieldClearing;
								warmupConfirmed = false;
								warmupSince = -1;
								log?.Invoke($"[{FormatElapsed(elapsed)}] Field clearing / side change");
							}
						}
						else
						{
							emptySince = -1;
						}
						break;

					case Phase.FieldClearing:
						// Must see defense out with an empty batter's box (pitcher warmup) before bookmarking.
						if (fieldCount >= options.MinFieldersPlaying && plateAreaEmpty)
						{
							phase = Phase.Warmup;
							warmupSince = elapsed;
							warmupConfirmed = false;
							log?.Invoke($"[{FormatElapsed(elapsed)}] Defense out / pitcher warmup (empty batter's box)");
						}
						break;

					case Phase.Warmup:
						if (fieldCount <= options.MaxFieldersEmpty)
						{
							// Still transitioning; restart warmup clock when defense settles again.
							warmupSince = -1;
							warmupConfirmed = false;
							break;
						}

						if (plateAreaEmpty)
						{
							if (warmupSince < 0)
								warmupSince = elapsed;
							else if (!warmupConfirmed
								&& elapsed - warmupSince >= options.WarmupHoldSeconds)
							{
								warmupConfirmed = true;
								log?.Invoke(
									$"[{FormatElapsed(elapsed)}] Warmup confirmed (empty box ≥ {options.WarmupHoldSeconds:0.#}s) — waiting for first batter walk-up");
							}

							break;
						}

						// Someone entered the approach / box area.
						if (warmupConfirmed
							&& batterWalkingUp
							&& elapsed - lastHalfSeconds >= options.MinSecondsBetweenHalfInnings)
						{
							halfStarts.Add(elapsed);
							lastHalfSeconds = elapsed;
							phase = Phase.Playing;
							emptySince = -1;
							warmupSince = -1;
							warmupConfirmed = false;
							log?.Invoke(
								$"[{FormatElapsed(elapsed)}] Half-inning start (batter walking up) → {InningHalfLabels.FormatHalfInning(halfStarts.Count - 1)}");
						}
						else if (!warmupConfirmed && batterInBox)
						{
							// Batter appeared before warmup was confirmed; keep waiting for a clean empty-box warmup.
							warmupSince = -1;
						}
						break;
				}

				if (i % 30 == 0)
				{
					log?.Invoke(
						$"Progress {i + 1}/{frames.Length} ({FormatElapsed(elapsed)}) phase={phase} fielders={fieldCount} box={(batterInBox ? "yes" : "no")} walkup={(batterWalkingUp ? "yes" : "no")} warmupOk={warmupConfirmed}");
				}
			}

			progress01?.Invoke(1);

			// First half-inning fallback: Top 1st walk-up after an observed empty-box warmup at game start.
			if (halfStarts.Count == 0)
			{
				var first = FindFirstWalkUpAfterWarmup(frames, detector, options, ct);
				if (first.HasValue)
				{
					halfStarts.Add(first.Value);
					log?.Invoke($"[{FormatElapsed(first.Value)}] Assumed Top 1st (first batter walking up after warmup)");
				}
			}

			var events = BuildEvents(halfStarts, durationSeconds);
			var outputPath = string.IsNullOrWhiteSpace(outputJsonPath)
				? InningDetectionRecordingWriter.BuildUniqueOutputPath(videoPath)
				: outputJsonPath.Trim();
			var root = InningDetectionRecordingWriter.BuildSessionJson(videoPath, durationSeconds, events);
			var (_, youtubePath) = await InningDetectionRecordingWriter
				.WriteSessionAndYoutubeAsync(root, outputPath, ct, log)
				.ConfigureAwait(false);

			if (options.SkipIntroTitleCards && introSkipCount > 0)
			{
				log?.Invoke(
					$"Skipped {introSkipCount} title-card sample(s) (~{introSkipCount * options.SampleIntervalSeconds:0.#}s).");
			}

			log?.Invoke($"Detected {halfStarts.Count} half-inning start(s).");
			return new InningDetectionResult
			{
				VideoPath = videoPath,
				OutputJsonPath = outputPath,
				OutputYoutubeDescriptionPath = youtubePath,
				DurationSeconds = durationSeconds,
				Events = events,
			};
		}
		finally
		{
			TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(frameDir, recursive: true);
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

	/// <summary>
	/// Fallback for Top 1st: require empty-box warmup with defense present, then first walk-up.
	/// </summary>
	private static double? FindFirstWalkUpAfterWarmup(
		string[] frames,
		YoloPersonDetector detector,
		Options options,
		CancellationToken ct)
	{
		var warmupSince = -1.0;
		var warmupConfirmed = false;
		var introStreak = 0;
		var skippingIntro = false;
		for (var i = 0; i < frames.Length; i++)
		{
			ct.ThrowIfCancellationRequested();
			var elapsed = i * options.SampleIntervalSeconds;
			using var mat = Cv2.ImRead(frames[i], ImreadModes.Color);
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

			var people = detector.Detect(mat);
			var fieldCount = CountInRoi(people, mat.Width, mat.Height, options.FieldRoi);
			var batterInBox = CountInRoi(people, mat.Width, mat.Height, options.BatterBoxRoi) > 0;
			var batterWalkingUp = CountInRoi(people, mat.Width, mat.Height, options.BatterApproachRoi) > 0;
			var plateAreaEmpty = !batterInBox && !batterWalkingUp;

			if (fieldCount < options.MinFieldersPlaying)
			{
				warmupSince = -1;
				warmupConfirmed = false;
				continue;
			}

			if (plateAreaEmpty)
			{
				if (warmupSince < 0)
					warmupSince = elapsed;
				else if (!warmupConfirmed && elapsed - warmupSince >= options.WarmupHoldSeconds)
					warmupConfirmed = true;
				continue;
			}

			if (warmupConfirmed && batterWalkingUp)
				return elapsed;

			if (!warmupConfirmed && batterInBox)
				warmupSince = -1;
		}

		return null;
	}

	private static int CountInRoi(IReadOnlyList<DetectedPerson> people, int frameW, int frameH, Rect2d roiNorm)
	{
		var roi = new Rect(
			(int)(roiNorm.X * frameW),
			(int)(roiNorm.Y * frameH),
			Math.Max(1, (int)(roiNorm.Width * frameW)),
			Math.Max(1, (int)(roiNorm.Height * frameH)));

		var count = 0;
		foreach (var person in people)
		{
			var c = new Point(
				person.Box.X + person.Box.Width / 2,
				person.Box.Y + person.Box.Height / 2);
			if (roi.Contains(c))
				count++;
		}

		return count;
	}

	private static async Task ExtractSampleFramesAsync(
		string videoPath,
		string frameDir,
		Options options,
		CancellationToken ct,
		Action<string> log)
	{
		var interval = Math.Max(0.25, options.SampleIntervalSeconds);
		var fps = (1.0 / interval).ToString("0.###", CultureInfo.InvariantCulture);
		var width = Math.Clamp(options.AnalysisWidth, 320, 1920);

		// Prefer VAAPI decode on AMD when available; fall back to software.
		var pattern = Path.Combine(frameDir, "%06d.jpg");
		var vf = $"fps={fps},scale={width}:-1";

		try
		{
			var hwArgs =
				$"-hide_banner -nostdin -y -hwaccel vaapi -hwaccel_device /dev/dri/renderD128 -i {Quote(videoPath)} " +
				$"-vf {Quote(vf)} -q:v 3 {Quote(pattern)}";
			log?.Invoke("Extracting sample frames (VAAPI)…");
			await FFMpegUtils.Instance.RunCaptureFFMpegAsync(hwArgs, ct, log).ConfigureAwait(false);
			if (Directory.EnumerateFiles(frameDir, "*.jpg").Any())
				return;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			log?.Invoke($"VAAPI frame extract unavailable ({ex.Message}); using software decode.");
		}

		foreach (var leftover in Directory.EnumerateFiles(frameDir, "*.jpg"))
			FileUtils.TryDeleteFile(leftover);

		var swArgs =
			$"-hide_banner -nostdin -y -i {Quote(videoPath)} -vf {Quote(vf)} -q:v 3 {Quote(pattern)}";
		log?.Invoke("Extracting sample frames (software)…");
		await FFMpegUtils.Instance.RunCaptureFFMpegAsync(swArgs, ct, log).ConfigureAwait(false);
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
