using System;
using System.Collections.Generic;

namespace MP4ToolsLib;

/// <summary>Builds a multi-line log summary of encode / hwaccel settings after Trim or Combine.</summary>
public static class EncodeProcessingSummary
{
	public static IEnumerable<string> BuildLines(string operationTitle, EncodingSettingsDto dto, bool trimSegmentAudioSummary = false)
	{
		dto ??= new EncodingSettingsDto();

		yield return $"--- Encode pipeline summary ({operationTitle}) ---";
		var audioEff = string.IsNullOrWhiteSpace(dto.TrimAudioCodec) ? "libopus" : dto.TrimAudioCodec.Trim();
		yield return trimSegmentAudioSummary
			? $"Encode tab: video={dto.VideoCodec ?? "copy"}, audio={dto.AudioCodec ?? "copy"}, trim_segment_audio={audioEff}, hardware_acceleration={dto.HardwareAcceleration ?? "auto"}"
			: $"Encode tab: video={dto.VideoCodec ?? "copy"}, audio={dto.AudioCodec ?? "copy"}, hardware_acceleration={dto.HardwareAcceleration ?? "auto"}";

		var hwTab = string.IsNullOrWhiteSpace(dto.HardwareAcceleration) ? "auto" : dto.HardwareAcceleration.Trim();
		var hwResolved = FFMpegUtils.Instance.ResolveHwAccelLineFromUserHints().Replace("\n", " ", StringComparison.Ordinal);
		yield return $"FFmpeg decode injection (RunAndLogFFMpegAsync, non-stream-copy): tab \"{hwTab}\" -> \"{hwResolved}\" (-hwaccel / -vaapi_device per ApplyForcedFfmpegArgs)";

		yield return trimSegmentAudioSummary
			? $"Effective audio for trim segment transcodes: {audioEff}"
			: $"Effective audio for transcode/mux steps: {audioEff}";

		var plan = VideoEncodeSelector.BuildPlan(dto, _ => { });
		if (plan.UseStreamCopy)
			yield return "Video plan: stream copy (-c:v copy) when output is not re-encoded.";
		else if(dto?.VideoCodec?.Contains("vaapi", StringComparison.OrdinalIgnoreCase) ?? false){
				yield return "VA-API encode: uses hwupload filter (format=nv12,hwupload=derive_device=vaapi,…) before encoder.";
		}

		var introPlan = VideoEncodeSelector.BuildIntroPlan(dto, _ => { });
		yield return introPlan.UseStreamCopy
			? "Intro slide (when used): stream copy (unexpected)."
			: $"";

		yield return "Note: RunCaptureFFMpegAsync applies ApplyForcedFfmpegArgs like RunAndLogFFMpegAsync; ffprobe capture steps do not.";
		yield return "--- End encode pipeline summary ---";
	}
}
