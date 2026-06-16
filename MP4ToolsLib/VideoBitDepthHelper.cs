using System;

namespace MP4ToolsLib;

public static class VideoBitDepthHelper
{
	public static bool IsTenBitVideo(StreamProbeInfo video)
	{
		if (video == null)
			return false;

		if (!string.IsNullOrWhiteSpace(video.BitDepth)
		    && int.TryParse(video.BitDepth, out var depth)
		    && depth >= 10)
			return true;

		var pix = video.PixelFormat ?? string.Empty;
		return pix.Contains("10", StringComparison.Ordinal);
	}

	public static string FormatDisplay(StreamProbeInfo video)
	{
		if (video == null || string.IsNullOrWhiteSpace(video.PixelFormat))
			return string.Empty;

		var label = IsTenBitVideo(video) ? "10-bit" : "8-bit";
		return $"{label} ({video.PixelFormat})";
	}
}
