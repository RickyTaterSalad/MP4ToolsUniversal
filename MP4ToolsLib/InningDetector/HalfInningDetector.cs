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
	public sealed class Options
	{
		/// <summary>
		/// Seconds between analyzed frames. Warmups last minutes, so coarse sampling is enough;
		/// walk-up bookmarks are then refined inside that interval (default 5s).
		/// </summary>
		public double SampleIntervalSeconds { get; set; } = 5.0;

		/// <summary>
		/// After a coarse half-inning hit, densely sample the hit's time window to find an
		/// earlier, higher-confidence start (first batter/hitter appearance in that range).
		/// </summary>
		public bool RefineHalfInningHits { get; set; } = true;

		/// <summary>Dense sample interval inside each refine window (seconds).</summary>
		public double RefineIntervalSeconds { get; set; } = 0.5;

		/// <summary>
		/// How far before the coarse hit to search. Defaults to one coarse sample interval
		/// (the transition falls between the previous empty sample and the hit).
		/// </summary>
		public double? RefineLookbackSeconds { get; set; }

		/// <summary>How far after the coarse hit to keep searching (usually 0).</summary>
		public double RefineLookaheadSeconds { get; set; } = 0;

		/// <summary>Max analysis width (keeps aspect ratio).</summary>
		public int AnalysisWidth { get; set; } = 960;

		public float ConfidenceThreshold { get; set; } = 0.35f;

		/// <summary>
		/// Floor used when loading the BaseballCV PHC model so class-specific thresholds below
		/// <see cref="ConfidenceThreshold"/> can still surface (hitter/catcher filters apply later).
		/// </summary>
		public float RoleModelConfidenceFloor { get; set; } = 0.20f;

		/// <summary>PHC hitter score gate (kids/backstop cameras need a lower bar than broadcast).</summary>
		public float RoleHitterConfidence { get; set; } = 0.28f;

		/// <summary>PHC catcher score gate (primary defense proxy when pitcher is never detected).</summary>
		public float RoleCatcherConfidence { get; set; } = 0.28f;

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
		/// Roles mode: how long PHC must see no hitter/pitcher/catcher before counting as an
		/// empty (side-change) stretch.
		/// </summary>
		public double RoleEmptyHoldSeconds { get; set; } = 30;

		/// <summary>
		/// Roles mode: a new half-inning hitter must occur within this many seconds after an
		/// empty stretch ends.
		/// </summary>
		public double RoleEmptyBeforeHitterSeconds { get; set; } = 150;

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
		Action<string> status = null)
	{
		if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
			throw new FileNotFoundException("Video not found.", videoPath);

		void Status(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
				return;
			status?.Invoke(message);
			log?.Invoke(message);
		}

		options ??= new Options();
		Status("Loading YOLO detection model…");
		var resolvedModel = await YoloModelStore.EnsureDetectorModelAsync(modelPath, ct, log).ConfigureAwait(false);
		var useRoles = resolvedModel.Kind == YoloModelKind.BaseballPhc;

		Status("Reading video duration…");
		var durationText = await FFMpegUtils.Instance.GetFileDurationAsync(videoPath, ct, log).ConfigureAwait(false);
		var durationSeconds = ParseDurationSeconds(durationText);
		if (durationSeconds <= 0)
			durationSeconds = ProbeDurationWithOpenCv(videoPath);

		Status(
			$"Preparing detection ({FormatElapsed(durationSeconds)} video, sample every {options.SampleIntervalSeconds:0.###}s)…");
		progress01?.Invoke(0);

		// Extract dominates wall time on long games; reserve most of the bar for it.
		const double extractProgressEnd = 0.55;
		const double analyzeProgressEnd = 0.92;

		var frameDir = Path.Combine(TempPathHelper.GetTempPath(), $"innings_frames_{Guid.NewGuid():N}");
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

			Status(
				useRoles
					? $"Analyzing gameplay with BaseballCV roles ({frames.Length} samples)…"
					: $"Analyzing gameplay with YOLO person model ({frames.Length} samples)…");

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
					? "Scanning for half-innings (empty end-of-half → hitter start; on-deck+box = in progress; catcher = defense)…"
					: "Scanning for later half-innings (field clear → warmup → first batter in box)…");

			for (var i = 0; i < frames.Length; i++)
			{
				ct.ThrowIfCancellationRequested();
				if (stoppedForGameOver)
					break;

				var elapsed = i * options.SampleIntervalSeconds;
				var analyzeFrac = frames.Length <= 1 ? 1.0 : (double)i / (frames.Length - 1);
				progress01?.Invoke(extractProgressEnd + (analyzeProgressEnd - extractProgressEnd) * analyzeFrac);

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
						if (i % 10 == 0 || i + 1 == frames.Length)
						{
							Status(
								$"Skipping title card… sample {i + 1}/{frames.Length} ({FormatElapsed(elapsed)})");
						}

						continue;
					}
				}

				var detections = detector.Detect(mat);
				var signals = BuildFrameSignals(detections, mat.Width, mat.Height, options, useRoles);

				if (useRoles)
				{
					// Tuned kids/backstop PHC path: bookmark uses BOTH sides of the transition —
					// (1) end-of-half = sustained empty (no H/P/C), then (2) start-of-half =
					// hitter return. Batter-in-box + on-deck hitter means play is ongoing, so
					// wipe empty/side-change tracking (mid-inning, not a new half).
					if (signals.InningInProgress)
					{
						if (lastEmptyEndSeconds >= 0 || emptySince >= 0)
						{
							Status(
								$"[{FormatElapsed(elapsed)}] Inning in progress (batter in box + on-deck) — not end of half");
						}

						emptySince = -1;
						longEmptySince = -1;
						lastEmptyEndSeconds = -1;
					}
					else if (signals.LooksEmpty)
					{
						if (emptySince < 0)
							emptySince = elapsed;
						else if (elapsed - emptySince >= options.RoleEmptyHoldSeconds)
							lastEmptyEndSeconds = elapsed;

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
					}

					if (options.AssumeTopFirstAtGameStart && !topFirstSeeded)
					{
						if (signals.DefenseReady)
						{
							halfStarts.Add(elapsed);
							lastHalfSeconds = elapsed;
							topFirstSeeded = true;
							Status(
								sawLeadingIntro
									? $"[{FormatElapsed(elapsed)}] Top 1st (first defense after intro)"
									: $"[{FormatElapsed(elapsed)}] Top 1st (first defense)");
						}

						continue;
					}

					if (signals.BatterPresent)
					{
						var noHitterGapOk = lastHitterSeconds < 0
							|| elapsed - lastHitterSeconds >= options.RoleNoHitterGapSeconds;
						var emptyBeforeOk = lastEmptyEndSeconds > lastHalfSeconds
							&& elapsed - lastEmptyEndSeconds <= options.RoleEmptyBeforeHitterSeconds;
						var minGapOk = elapsed - lastHalfSeconds >= options.RoleMinSecondsBetweenHalfInnings;

						if (topFirstSeeded && noHitterGapOk && emptyBeforeOk && minGapOk)
						{
							halfStarts.Add(elapsed);
							lastHalfSeconds = elapsed;
							Status(
								$"[{FormatElapsed(elapsed)}] Half-inning start (hitter after empty) → {InningHalfLabels.FormatHalfInning(halfStarts.Count - 1)}");
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
								halfStarts.Add(elapsed);
								lastHalfSeconds = elapsed;
								phase = Phase.Playing;
								emptySince = -1;
								warmupSince = -1;
								warmupConfirmed = false;
								Status(
									$"[{FormatElapsed(elapsed)}] Half-inning start (first batter in box) → {InningHalfLabels.FormatHalfInning(halfStarts.Count - 1)}");
							}
							else if (!warmupConfirmed)
							{
								warmupSince = -1;
							}
							break;
					}
				}

				if (i % 10 == 0 || i + 1 == frames.Length)
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
						$"Analyzing gameplay… {i + 1}/{frames.Length} ({FormatElapsed(elapsed)}) — {phaseLabel}{roleHint}; found {halfStarts.Count} half-inning(s)");
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
				var first = FindFirstBatterAfterWarmup(frames, detector, options, useRoles, ct);
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

					var refined = await RefineHalfInningStartAsync(
							videoPath,
							coarse,
							durationSeconds,
							detector,
							options,
							useRoles,
							ct,
							Status)
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
					? InningDetectionRecordingWriter.BuildUniqueOutputPath(videoPath)
					: outputJsonPath.Trim();
				var root = InningDetectionRecordingWriter.BuildSessionJson(videoPath, durationSeconds, events);
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

	private readonly record struct FrameSignals(
		bool HasHitter,
		bool HasPitcher,
		bool HasCatcher,
		bool BatterPresent,
		bool DefenseReady,
		bool LooksEmpty,
		bool CoachNearPlate,
		bool OnDeckPresent,
		bool InningInProgress);

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
			// Prefer a hitter whose center is in the batter's box; accept any hitter as fallback
			// unless that hitter is only in an on-deck ROI (next batter, not at plate).
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
			// Lone on-deck hitter is not "batter present" for half-start bookmarks.
			var batterPresent = hitterInBox || (hasHitter && !onDeckPresent);
			// BaseballCV PHC often misses the distant mound pitcher on kids/backstop wide shots;
			// catcher gear is larger and more reliable as a "defense is out" proxy.
			var defenseReady = hasPitcher || hasCatcher;
			var looksEmpty = !hasPitcher && !hasHitter && !hasCatcher;
			var inningInProgress = options.UseOnDeckInProgressSignal && hitterInBox && onDeckPresent;
			return new FrameSignals(
				hasHitter, hasPitcher, hasCatcher, batterPresent, defenseReady, looksEmpty,
				CoachNearPlate: false, OnDeckPresent: onDeckPresent, InningInProgress: inningInProgress);
		}

		var fieldCount = CountInRoi(detections, frameW, frameH, options.FieldRoi);
		var batterInBox = CountInRoi(detections, frameW, frameH, options.BatterBoxRoi) > 0;
		var personOnDeck = CountInRoi(detections, frameW, frameH, options.OnDeckLeftRoi)
			+ CountInRoi(detections, frameW, frameH, options.OnDeckRightRoi) > 0;
		var coachNearPlate = !batterInBox
			&& CountInRoi(detections, frameW, frameH, options.BatterApproachRoi) > 0;
		var personInProgress = options.UseOnDeckInProgressSignal && batterInBox && personOnDeck;
		return new FrameSignals(
			HasHitter: batterInBox,
			HasPitcher: fieldCount >= options.MinFieldersPlaying,
			HasCatcher: false,
			BatterPresent: batterInBox,
			DefenseReady: fieldCount >= options.MinFieldersPlaying,
			LooksEmpty: fieldCount <= options.MaxFieldersEmpty,
			CoachNearPlate: coachNearPlate,
			OnDeckPresent: personOnDeck,
			InningInProgress: personInProgress);
	}

	/// <summary>
	/// Fallback for Top 1st: warmup without a batter/hitter, then first batter/hitter.
	/// </summary>
	private static double? FindFirstBatterAfterWarmup(
		string[] frames,
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
		string videoPath,
		double coarseHitSeconds,
		double durationSeconds,
		YoloOnnxDetector detector,
		Options options,
		bool useRoles,
		CancellationToken ct,
		Action<string> status)
	{
		var lookback = Math.Max(0.25, options.RefineLookbackSeconds ?? options.SampleIntervalSeconds);
		var lookahead = Math.Max(0, options.RefineLookaheadSeconds);
		var refineInterval = Math.Clamp(options.RefineIntervalSeconds, 0.1, Math.Max(0.1, options.SampleIntervalSeconds));

		var windowStart = Math.Max(0, coarseHitSeconds - lookback);
		var windowEnd = coarseHitSeconds + lookahead;
		if (durationSeconds > 0)
			windowEnd = Math.Min(durationSeconds, windowEnd);
		if (windowEnd < coarseHitSeconds)
			windowEnd = coarseHitSeconds;

		var windowDuration = Math.Max(refineInterval, windowEnd - windowStart + refineInterval);
		status?.Invoke(
			$"Refine window {FormatElapsed(windowStart)}–{FormatElapsed(Math.Min(windowStart + windowDuration, windowEnd + refineInterval))} " +
			$"(coarse hit {FormatElapsed(coarseHitSeconds)})");

		var refineDir = Path.Combine(TempPathHelper.GetTempPath(), $"innings_refine_{Guid.NewGuid():N}");
		Directory.CreateDirectory(refineDir);
		try
		{
			await ExtractRangeSampleFramesAsync(
					videoPath,
					refineDir,
					windowStart,
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

			// Collect candidate (time, score) frames where a batter/hitter is present.
			var candidates = new List<(double Time, float Confidence, bool InBox, bool AfterEmpty)>();
			var sawEmptyLeadIn = false;

			for (var i = 0; i < frames.Length; i++)
			{
				ct.ThrowIfCancellationRequested();
				var t = windowStart + i * refineInterval;
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
				{
					sawEmptyLeadIn = true;
					continue;
				}

				var confidence = BestBatterConfidence(detections, useRoles, mat.Width, mat.Height, options);
				var inBox = useRoles
					? CountClassInRoi(detections, "hitter", mat.Width, mat.Height, options.BatterBoxRoi) > 0
					: CountInRoi(detections, mat.Width, mat.Height, options.BatterBoxRoi) > 0;
				candidates.Add((t, confidence, inBox, sawEmptyLeadIn));
			}

			if (candidates.Count == 0)
				return coarseHitSeconds;

			// Prefer earliest empty→batter transition; then in-box + confidence.
			static double PickBest(IReadOnlyList<(double Time, float Confidence, bool InBox, bool AfterEmpty)> list, float minConfidence)
			{
				var ordered = list
					.OrderBy(c => c.Time)
					.ThenByDescending(c => c.InBox)
					.ThenByDescending(c => c.Confidence)
					.ToList();
				foreach (var c in ordered)
				{
					if (c.Confidence >= minConfidence && c.InBox)
						return c.Time;
				}

				foreach (var c in ordered)
				{
					if (c.Confidence >= minConfidence)
						return c.Time;
				}

				return ordered[0].Time;
			}

			var afterEmpty = candidates.Where(c => c.AfterEmpty).ToList();
			if (afterEmpty.Count > 0)
				return PickBest(afterEmpty, options.ConfidenceThreshold);

			// Window opened already on a batter — earliest confident / in-box sample.
			return PickBest(candidates, options.ConfidenceThreshold);
		}
		finally
		{
			TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(refineDir, recursive: true);
		}
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
