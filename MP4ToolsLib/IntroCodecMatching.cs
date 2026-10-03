using System;

namespace MP4ToolsLib;

/// <summary>Maps probed codec names to ffmpeg encoders for intro clips that must stream-copy-concat with a reference.</summary>
public static class IntroCodecMatching
{
	public static string ResolveVideoEncoder(string probedCodecName)
	{
		var c = (probedCodecName ?? "").Trim().ToLowerInvariant();
		return c switch
		{
			"hevc" or "h265" or "hev1" or "hvc1" => "libx265",
			"h264" or "avc" or "avc1" => "libx264",
			"vp9" or "vp9.0" => "libvpx-vp9",
			"av1" or "av01" => "libsvtav1",
			"mpeg4" or "mp4v" => "mpeg4",
			"mpeg2video" or "mpeg2" => "mpeg2video",
			_ when !string.IsNullOrWhiteSpace(c) => c,
			_ => "libx265",
		};
	}

	public static string ResolveAudioEncoder(string probedCodecName)
	{
		var c = (probedCodecName ?? "").Trim().ToLowerInvariant();
		return c switch
		{
			"aac" or "mp4a" => "aac",
			"opus" => "libopus",
			"mp3" or "mp3float" => "libmp3lame",
			"ac3" => "ac3",
			"flac" => "flac",
			"pcm_s16le" => "pcm_s16le",
			_ when c.Contains("opus") => "libopus",
			_ when c.Contains("aac") => "aac",
			_ when !string.IsNullOrWhiteSpace(c) => c,
			_ => "aac",
		};
	}

	public static string NormalizeProbedVideoCodecTag(string probedCodecName)
	{
		var c = (probedCodecName ?? "").Trim().ToLowerInvariant();
		return c switch
		{
			"hevc" or "h265" or "hev1" or "hvc1" => "hevc",
			"h264" or "avc" or "avc1" => "h264",
			_ => c,
		};
	}
}
