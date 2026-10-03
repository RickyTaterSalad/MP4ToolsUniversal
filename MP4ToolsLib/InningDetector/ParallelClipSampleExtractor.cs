using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>
/// Extracts analysis snapshots from independent clips in parallel, then stitches them onto the
/// game-content timeline using <see cref="GameContentTimeline"/> bookkeeping.
/// </summary>
public static class ParallelClipSampleExtractor
{
	/// <summary>
	/// Max concurrent FFmpeg extractors. Kept modest so VAAPI decode on a single AMD device
	/// is not oversubscribed.
	/// </summary>
	public const int DefaultMaxParallelism = 2;

	public static async Task<IReadOnlyList<HalfInningDetector.TimedSampleFrame>> ExtractAsync(
		IReadOnlyList<GameContentSegment> segments,
		HalfInningDetector.Options options,
		string outputFrameDir,
		CancellationToken ct = default,
		Action<string> log = null,
		Action<string> status = null,
		Action<double> progress01 = null,
		int maxParallelism = DefaultMaxParallelism)
	{
		if (segments == null || segments.Count == 0)
			throw new ArgumentException("At least one game-content segment is required.", nameof(segments));
		if (string.IsNullOrWhiteSpace(outputFrameDir))
			throw new ArgumentException("Output frame directory is required.", nameof(outputFrameDir));

		options ??= new HalfInningDetector.Options();
		var interval = Math.Max(0.25, options.SampleIntervalSeconds);
		var gameDuration = GameContentTimeline.TotalDurationSeconds(segments);
		var sampleTimes = GameContentTimeline.BuildSampleTimes(gameDuration, interval);
		Directory.CreateDirectory(outputFrameDir);

		status?.Invoke(
			$"Extracting {sampleTimes.Count} snapshots from {segments.Count} clip(s) in parallel " +
			$"(max {Math.Max(1, maxParallelism)} concurrent, every {interval:0.###}s)…");

		var bySegment = new Dictionary<int, List<double>>();
		for (var i = 0; i < segments.Count; i++)
			bySegment[i] = new List<double>();

		foreach (var t in sampleTimes)
		{
			var segIndex = FindSegmentIndex(segments, t);
			bySegment[segIndex].Add(t);
		}

		var work = bySegment
			.Where(kv => kv.Value.Count > 0)
			.Select(kv => (SegmentIndex: kv.Key, Times: kv.Value))
			.ToList();

		var clipProgress = new double[work.Count];
		var gate = new SemaphoreSlim(Math.Max(1, maxParallelism));
		var tasks = new List<Task>(work.Count);

		for (var wi = 0; wi < work.Count; wi++)
		{
			var workIndex = wi;
			var (segIndex, times) = work[wi];
			var segment = segments[segIndex];
			tasks.Add(Task.Run(async () =>
			{
				await gate.WaitAsync(ct).ConfigureAwait(false);
				try
				{
					ct.ThrowIfCancellationRequested();
					var clipDir = Path.Combine(outputFrameDir, $"clip_{segment.ClipIndex:00}");
					Directory.CreateDirectory(clipDir);

					var firstGame = times[0];
					var lastGame = times[^1];
					var localStart = segment.ToLocalSeconds(firstGame);
					var localEnd = segment.ToLocalSeconds(lastGame);
					var extractDuration = Math.Max(interval, localEnd - localStart + interval);

					log?.Invoke(
						$"Clip {segment.ClipIndex}: {times.Count} sample(s), " +
						$"local {localStart:0.###}s–{localEnd:0.###}s → game {firstGame:0.###}s–{lastGame:0.###}s");

					await HalfInningDetector.ExtractTimedRangeAsync(
							segment.MediaPath,
							clipDir,
							localStart,
							extractDuration,
							interval,
							options.AnalysisWidth,
							ct,
							line =>
							{
								log?.Invoke(line);
								if (!FfmpegProgressParser.TryParseTimeSeconds(line, out var mediaSeconds))
									return;
								var frac = Math.Clamp(mediaSeconds / Math.Max(0.1, extractDuration), 0, 1);
								clipProgress[workIndex] = frac;
								ReportAggregateProgress(clipProgress, work, segments, progress01);
							},
							msg => log?.Invoke($"[clip {segment.ClipIndex}] {msg}"))
						.ConfigureAwait(false);

					clipProgress[workIndex] = 1;
					ReportAggregateProgress(clipProgress, work, segments, progress01);
				}
				finally
				{
					gate.Release();
				}
			}, ct));
		}

		await Task.WhenAll(tasks).ConfigureAwait(false);
		progress01?.Invoke(1);

		var samples = new List<HalfInningDetector.TimedSampleFrame>(sampleTimes.Count);
		var localCursor = new int[segments.Count];
		var clipFrameLists = new string[segments.Count][];
		for (var i = 0; i < segments.Count; i++)
		{
			var clipDir = Path.Combine(outputFrameDir, $"clip_{segments[i].ClipIndex:00}");
			clipFrameLists[i] = Directory.Exists(clipDir)
				? Directory.GetFiles(clipDir, "*.jpg").OrderBy(f => f, StringComparer.Ordinal).ToArray()
				: Array.Empty<string>();
		}

		foreach (var t in sampleTimes)
		{
			var segIndex = FindSegmentIndex(segments, t);
			var localIndex = localCursor[segIndex]++;
			var frames = clipFrameLists[segIndex];
			if (localIndex >= frames.Length)
			{
				log?.Invoke(
					$"Missing sample frame for game t={t:0.###}s " +
					$"(clip {segments[segIndex].ClipIndex}, local index {localIndex}).");
				continue;
			}

			samples.Add(new HalfInningDetector.TimedSampleFrame(frames[localIndex], t));
		}

		if (samples.Count == 0)
			throw new InvalidOperationException("Parallel clip extract produced no sample frames.");

		status?.Invoke(
			$"Parallel extract complete — {samples.Count} snapshot(s) across {segments.Count} clip(s) " +
			$"({FormatElapsed(gameDuration)} game content).");
		return samples;
	}

	private static int FindSegmentIndex(IReadOnlyList<GameContentSegment> segments, double gameSeconds)
	{
		for (var i = 0; i < segments.Count; i++)
		{
			var seg = segments[i];
			if (gameSeconds < seg.GameStartSeconds)
				continue;
			if (i < segments.Count - 1 && gameSeconds >= seg.GameEndSeconds)
				continue;
			return i;
		}

		return segments.Count - 1;
	}

	private static void ReportAggregateProgress(
		double[] clipProgress,
		IReadOnlyList<(int SegmentIndex, List<double> Times)> work,
		IReadOnlyList<GameContentSegment> segments,
		Action<double> progress01)
	{
		if (progress01 == null || work.Count == 0)
			return;

		double weightSum = 0;
		double done = 0;
		for (var i = 0; i < work.Count; i++)
		{
			var w = Math.Max(0.01, segments[work[i].SegmentIndex].ContributionSeconds);
			weightSum += w;
			done += w * Math.Clamp(clipProgress[i], 0, 1);
		}

		progress01(weightSum <= 0 ? 0 : done / weightSum);
	}

	private static string FormatElapsed(double totalSeconds)
	{
		var ts = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(totalSeconds)));
		return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
	}
}
