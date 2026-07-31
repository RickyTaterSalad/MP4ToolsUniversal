namespace MP4ToolsLib;

/// <summary>
/// Selects the correct MP4→Annex-B video bitstream filter for MPEG-TS remux.
/// Applying <c>hevc_mp4toannexb</c> to non-HEVC streams (e.g. AV1) fails.
/// </summary>
public static class MpegTsVideoBitstream
{
	/// <summary>
	/// Returns <c>-bsf:v …_mp4toannexb</c> for H.264/HEVC, or a skipped option for codecs
	/// that do not use that filter (AV1, etc. — ffmpeg's mpegts muxer handles them).
	/// </summary>
	public static FfmpegOption GetMp4ToAnnexBOption(string codecNameOrEncoder)
	{
		var filter = GetMp4ToAnnexBFilterName(codecNameOrEncoder);
		return string.IsNullOrEmpty(filter)
			? default
			: FfmpegOption.Pair(FfmpegArguments.VideoBitstreamFilter, filter);
	}

	public static string GetMp4ToAnnexBFilterName(string codecNameOrEncoder)
	{
		var x = codecNameOrEncoder?.Trim().ToLowerInvariant() ?? "";
		if (x.Contains("hevc") || x is "h265" or "hev1" or "hvc1" || x.Contains("h265"))
			return "hevc_mp4toannexb";
		if (x.Contains("h264") || x is "avc" or "avc1" || x.Contains("avc"))
			return "h264_mp4toannexb";
		return null;
	}
}
