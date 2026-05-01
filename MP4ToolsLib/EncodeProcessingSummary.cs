using System;
using System.Collections.Generic;

namespace MP4ToolsLib;

/// <summary>Builds a multi-line log summary of encode / hwaccel settings after Trim or Combine.</summary>
public static class EncodeProcessingSummary
{
	public static IEnumerable<string> BuildLines(string operationTitle, EncodingSettingsDto dto, string inputVideoBitDepthUi)
	{
		dto ??= new EncodingSettingsDto();

		yield return $"--- Encode pipeline summary ({operationTitle}) ---";
		yield return $"Encode tab: reencode={dto.ReencodeOutput}, video={dto.VideoCodec ?? "copy"}, audio={dto.AudioCodec ?? "copy"}, output_bit_depth={dto.OutputBitDepth ?? "auto"}, hardware_acceleration={dto.HardwareAcceleration ?? "auto"}";

		var hwTab = string.IsNullOrWhiteSpace(dto.HardwareAcceleration) ? "auto" : dto.HardwareAcceleration.Trim();
		var hwResolved = FFMpegUtils.Instance.ResolveHwAccelLineFromUserHints().Replace("\n", " ", StringComparison.Ordinal);
		yield return $"FFmpeg decode injection (RunAndLogFFMpegAsync, non-stream-copy): tab \"{hwTab}\" -> \"{hwResolved}\" (-hwaccel / -vaapi_device per ApplyForcedFfmpegArgs)";

		var audioEff = VideoEncodeSelector.EffectiveAudioCodec(dto);
		yield return $"Effective audio for transcode/mux steps: {audioEff}";

		var plan = VideoEncodeSelector.BuildPlan(dto, inputVideoBitDepthUi ?? string.Empty, _ => { });
		if (plan.UseStreamCopy)
			yield return "Video plan: stream copy (-c:v copy) when output is not re-encoded.";
		else
		{
			yield return $"Video encoder tail: {SummarizeOptions(plan.VideoEncodeOptions)}";
			if (plan.NeedsVaapiUploadFilter)
				yield return "VA-API encode: uses hwupload filter (format=nv12,hwupload=derive_device=vaapi,…) before encoder.";
		}

		var introPlan = VideoEncodeSelector.BuildIntroPlan(dto, _ => { });
		yield return introPlan.UseStreamCopy
			? "Intro slide (when used): stream copy (unexpected)."
			: $"Intro slide (when used): {SummarizeOptions(introPlan.VideoEncodeOptions)} — VA-API encoder skipped for lavfi/drawtext path.";

		yield return "Note: RunCaptureFFMpegAsync (intro MP4/TS steps, ffprobe) does not prepend ApplyForcedFfmpegArgs hwaccel.";
		yield return "--- End encode pipeline summary ---";
	}

	private static string SummarizeOptions(IReadOnlyList<FfmpegOption?> opts)
	{
		if (opts == null || opts.Count == 0)
			return "(none)";

		var parts = new List<string>();
		foreach (var o in opts)
		{
			if (o is not { } x || x.IsSkipped)
				continue;
			if (!string.IsNullOrWhiteSpace(x.Flag))
				parts.Add(string.IsNullOrWhiteSpace(x.Value) ? x.Flag : $"{x.Flag} {x.Value}");
			else if (!string.IsNullOrWhiteSpace(x.Value))
				parts.Add(x.Value);
		}

		return parts.Count == 0 ? "(none)" : string.Join(" ", parts);
	}
}
