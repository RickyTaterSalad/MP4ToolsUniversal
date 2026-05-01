using System.Collections.Generic;

namespace MP4ToolsLib;

/// <summary>
/// Opus (and libopus) copied into MPEG-TS then concat tends to spam FFmpeg
/// "Error parsing opus packet header". Re-encode those streams to AAC for TS intermediates only.
/// </summary>
public static class MpegTsConcatAudio
{
	public static bool ShouldTranscodeAudioMp4ToTs(string ffmpegCodecOrEncoderLabel)
	{
		var x = ffmpegCodecOrEncoderLabel?.Trim().ToLowerInvariant() ?? "";
		return x.Contains("opus");
	}

	public static void AppendMp4ToTsAudioOptions(ICollection<FfmpegOption?> parts, string ffmpegCodecOrEncoderLabel)
	{
		if (ShouldTranscodeAudioMp4ToTs(ffmpegCodecOrEncoderLabel))
		{
			parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, "aac"));
			parts.Add(FfmpegOption.Pair(FfmpegArguments.AudioBitrate, "192k"));
		}
		else
			parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, FfmpegArguments.StreamCopy));
	}

	/// <summary>After MP4→TS, audio is AAC if source was AAC (copy) or Opus (transcoded to AAC).</summary>
	public static bool IntermediateTsAudioIsAac(string ffmpegCodecOrEncoderLabel)
	{
		var x = ffmpegCodecOrEncoderLabel?.Trim().ToLowerInvariant() ?? "";
		if (x.Contains("opus"))
			return true;
		return x is "aac" or "mp4a";
	}
}
