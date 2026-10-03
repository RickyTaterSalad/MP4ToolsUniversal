using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

public static class IntroFrameExtractor
{
	/// <summary>
	/// Extracts a single JPEG frame from near the middle of the intro region
	/// (<paramref name="introDurationSeconds"/>) for OCR / vision reading.
	/// </summary>
	public static async Task<string> ExtractIntroFrameAsync(
		string videoPath,
		int introDurationSeconds,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
			throw new FileNotFoundException("Video not found.", videoPath);

		var duration = Math.Max(1, introDurationSeconds);
		var sampleSeconds = Math.Clamp(duration / 2.0, 0.5, Math.Max(0.5, duration - 0.25));
		var seek = TimeSpan.FromSeconds(sampleSeconds);
		var seekTs = string.Create(CultureInfo.InvariantCulture, $"{(int)seek.TotalHours:00}:{seek.Minutes:00}:{seek.Seconds:00}.{seek.Milliseconds:000}");

		var outPath = Path.Combine(TempPathHelper.GetTempPath(), $"intro_frame_{Guid.NewGuid():N}.jpg");
		log?.Invoke($"Extracting intro frame at {seekTs} → {outPath}");

		var parts = new List<FfmpegOption?>
		{
			FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
			FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
			FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, seekTs),
			FfmpegCommandLine.DefaultInputThreadQueue(),
			FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(videoPath)),
			FfmpegOption.Pair(FfmpegArguments.OutputVideoFrameCount, "1"),
			FfmpegOption.Pair(FfmpegArguments.OutputImageQuality, "2"),
			FfmpegOption.Positional(FfmpegCommandLine.Quoted(outPath)),
		};

		await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(parts), ct, log).ConfigureAwait(false);

		if (!File.Exists(outPath) || new FileInfo(outPath).Length == 0)
			throw new InvalidOperationException("Failed to extract intro frame from video.");

		return outPath;
	}
}
