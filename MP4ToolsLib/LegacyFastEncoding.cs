using System;
using System.Collections.Generic;

namespace MP4ToolsLib;

/// <summary>
/// Stream-copy and minimal re-encode paths used before Resolve-safe output (fast, but bad MP4 timing in NLEs).
/// </summary>
public static class LegacyFastEncoding
{
	public static void AppendTrimOptions(
		ICollection<FfmpegOption?> trimParts,
		ICollection<FfmpegOption?> encodingTail,
		string hwAccel,
		string trimAudioCodec,
		string seekTimestamp,
		string quotedInput,
		string duration,
		string drawTextFilter)
	{
		var isVaapi = string.Equals(hwAccel, "vaapi", StringComparison.OrdinalIgnoreCase);
		var isAmf = string.Equals(hwAccel, "amf", StringComparison.OrdinalIgnoreCase);
		var accelerationApi = isVaapi ? "vaapi" : (isAmf ? "d3d11va" : "");
		var isStreamCopy = string.IsNullOrWhiteSpace(drawTextFilter);

		var videoCodecOpt = FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy);
		if (!isStreamCopy)
		{
			if (isVaapi)
				videoCodecOpt = FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, "hevc_vaapi");
			else if (isAmf)
				videoCodecOpt = FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, "hevc_amf");
		}

		FfmpegOption vfOpt = default;
		if (!isStreamCopy)
		{
			var suffix = OperatingSystem.IsLinux() ? ",format=nv12,hwupload" : string.Empty;
			vfOpt = FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{drawTextFilter}{suffix}\"");
		}

		if (!string.IsNullOrWhiteSpace(accelerationApi))
			trimParts.Add(FfmpegOption.Pair(FfmpegArguments.HardwareAcceleration, accelerationApi));

		trimParts.Add(FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, seekTimestamp));
		trimParts.Add(FfmpegCommandLine.DefaultInputThreadQueue());
		trimParts.Add(FfmpegOption.Pair(FfmpegArguments.Input, quotedInput));
		// Omit -t when duration is empty so ffmpeg keeps through EOF (end-trim disabled).
		if (!string.IsNullOrWhiteSpace(duration))
			trimParts.Add(FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, duration));
		if (!vfOpt.IsSkipped)
			trimParts.Add(vfOpt);

		if (!isStreamCopy && !string.IsNullOrWhiteSpace(accelerationApi) && OperatingSystem.IsLinux())
		{
			trimParts.Add(FfmpegOption.Pair(FfmpegArguments.InitHardwareDevice, $"{accelerationApi}={FFMpegUtils.GetPreferredRenderDevice()}"));
			trimParts.Add(FfmpegOption.Pair(FfmpegArguments.FilterHardwareDevice, FFMpegUtils.GetPreferredRenderDevice()));
		}

		encodingTail.Add(videoCodecOpt);
		encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, trimAudioCodec));
		encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.AudioBitrate, "192k"));
	}

	public static void AppendStreamCopyTail(ICollection<FfmpegOption?> parts)
	{
		parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy));
		parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, FfmpegArguments.StreamCopy));
	}
}
