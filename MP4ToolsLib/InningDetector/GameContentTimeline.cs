using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP4ToolsLib;

/// <summary>
/// One source/proxy clip's contribution on the combined game-content timeline
/// (after start skip / end keep, before intro offset).
/// </summary>
public readonly record struct GameContentSegment(
	int ClipIndex,
	string MediaPath,
	double LocalStartSeconds,
	double ContributionSeconds,
	double GameStartSeconds)
{
	public double GameEndSeconds => GameStartSeconds + ContributionSeconds;

	public double ToLocalSeconds(double gameSeconds) =>
		LocalStartSeconds + (gameSeconds - GameStartSeconds);
}

/// <summary>
/// Builds and queries the game-content timeline from a Combine edit map + media paths.
/// </summary>
public static class GameContentTimeline
{
	public static IReadOnlyList<GameContentSegment> BuildSegments(
		CombineEditMap map,
		IReadOnlyList<string> mediaPaths)
	{
		if (map?.Clips == null || map.Clips.Count == 0)
			throw new InvalidOperationException("Combine edit map has no clips.");
		if (mediaPaths == null || mediaPaths.Count != map.Clips.Count)
			throw new ArgumentException("Media path count must match combine map clips.", nameof(mediaPaths));

		var clips = map.Clips.OrderBy(c => c.Index).ToList();
		var startSkip = map.TrimFirstVideo ? Math.Max(0, map.StartSkipSeconds) : 0;
		var segments = new List<GameContentSegment>(clips.Count);
		var gameStart = 0.0;

		for (var i = 0; i < clips.Count; i++)
		{
			var clip = clips[i];
			var path = mediaPaths[i];
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
				throw new FileNotFoundException($"Media for clip {clip.Index} not found.", path);

			var sourceDur = Math.Max(0, clip.SourceDurationSeconds);
			var localStart = i == 0 ? startSkip : 0;
			if (localStart >= sourceDur && sourceDur > 0)
				throw new InvalidOperationException(
					$"Start skip {localStart:0.###}s exceeds clip {clip.Index} duration {sourceDur:0.###}s.");

			var localEnd = sourceDur;
			if (i == clips.Count - 1 && map.TrimLastVideo)
				localEnd = Math.Min(localEnd, Math.Max(0, map.EndKeepSeconds));

			var contribution = Math.Max(0, localEnd - localStart);
			if (contribution <= 0.01)
				continue;

			segments.Add(new GameContentSegment(
				clip.Index,
				Path.GetFullPath(path),
				localStart,
				contribution,
				gameStart));
			gameStart += contribution;
		}

		if (segments.Count == 0)
			throw new InvalidOperationException("Combine edit map implies empty game content.");

		return segments;
	}

	public static double TotalDurationSeconds(IReadOnlyList<GameContentSegment> segments) =>
		segments == null || segments.Count == 0
			? 0
			: segments[^1].GameEndSeconds;

	public static bool TryMapGameTime(
		IReadOnlyList<GameContentSegment> segments,
		double gameSeconds,
		out string mediaPath,
		out double localSeconds)
	{
		mediaPath = null;
		localSeconds = 0;
		if (segments == null || segments.Count == 0)
			return false;

		var t = Math.Max(0, gameSeconds);
		for (var i = 0; i < segments.Count; i++)
		{
			var seg = segments[i];
			var isLast = i == segments.Count - 1;
			if (t < seg.GameStartSeconds)
				continue;
			if (!isLast && t >= seg.GameEndSeconds)
				continue;

			mediaPath = seg.MediaPath;
			localSeconds = Math.Clamp(seg.ToLocalSeconds(t), seg.LocalStartSeconds, seg.LocalStartSeconds + seg.ContributionSeconds);
			return true;
		}

		var last = segments[^1];
		mediaPath = last.MediaPath;
		localSeconds = last.LocalStartSeconds + last.ContributionSeconds;
		return true;
	}

	/// <summary>
	/// Global sample times on a fixed interval grid across the full game-content duration.
	/// </summary>
	public static List<double> BuildSampleTimes(double gameDurationSeconds, double intervalSeconds)
	{
		var interval = Math.Max(0.25, intervalSeconds);
		var duration = Math.Max(0, gameDurationSeconds);
		var times = new List<double>();
		if (duration <= 0)
			return times;

		for (var t = 0.0; t < duration - 1e-6; t += interval)
			times.Add(t);
		if (times.Count == 0)
			times.Add(0);
		return times;
	}
}

/// <summary>Resolves a game-content timestamp to a concrete media file + local time for refine extracts.</summary>
public sealed class GameContentRefineMediaSource : HalfInningDetector.IRefineMediaSource
{
	private readonly IReadOnlyList<GameContentSegment> _segments;

	public GameContentRefineMediaSource(IReadOnlyList<GameContentSegment> segments)
	{
		_segments = segments ?? throw new ArgumentNullException(nameof(segments));
	}

	public bool TryResolve(double gameSeconds, out string mediaPath, out double localSeconds) =>
		GameContentTimeline.TryMapGameTime(_segments, gameSeconds, out mediaPath, out localSeconds);
}
