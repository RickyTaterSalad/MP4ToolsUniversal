using System;

namespace MP4ToolsLib;

public sealed class VideoEncodePlan
{
	public bool UseStreamCopy { get; init; }

}

public static class VideoEncodeSelector
{
	public const string VaapiUploadSuffix = "format=nv12,hwupload=derive_device=vaapi:extra_hw_frames=64";

	public const string AmfUploadSuffix = "";

	/// <summary>-vf chain required before <c>hevc_vaapi</c>/<c>h264_vaapi</c> when decoding to system memory (same as Trim re-encode).</summary>
	public static FfmpegOption VaapiUploadVideoFilterOption =>
		FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{VaapiUploadSuffix}\"");
	public static FfmpegOption AmfUploadVideoFilterOption =>
		FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{AmfUploadSuffix}\"");


	public static VideoEncodePlan BuildPlan(EncodingSettingsDto dto, Action<string> log)
	{
		log ??= _ => { };

		if (dto == null || IsCopyCodec(dto.VideoCodec))
		{
			return new VideoEncodePlan
			{
				UseStreamCopy = true
			};
		}

		bool hevc = IsHevcFamily(dto.VideoCodec);
		string encoder = PickEncoder(hevc, NormalizeHwMode(dto.HardwareAcceleration), log);
		bool vaapi = encoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
		bool amf = encoder.Contains("amf", StringComparison.OrdinalIgnoreCase);
		return new VideoEncodePlan
		{
			UseStreamCopy = false
		};
	}

	public static VideoEncodePlan BuildIntroPlan(EncodingSettingsDto dto, Action<string> log)
	{

		return BuildPlan(dto, log);
	}

	private static bool IsCopyCodec(string v) =>
		string.IsNullOrWhiteSpace(v) || string.Equals(v, "copy", StringComparison.OrdinalIgnoreCase);

	private static bool IsHevcFamily(string v)
	{
		var x = v?.Trim().ToLowerInvariant() ?? "";
		return x is "h265" or "hevc" or "h.265";
	}

	private static string NormalizeHwMode(string h)
	{
		var x = (h ?? "auto").Trim().ToLowerInvariant();
		return x switch
		{
			"nvidia" or "cuda" => "nvenc",
			_ => x,
		};
	}

	private static string PickEncoder(bool hevc, string hwMode, Action<string> log)
	{
		string SoftwareFallback()
		{
			return hevc ? "libx265" : "libx264";
		}


		string PickForced(string mode)
		{
			switch (mode)
			{
				case "amf":
					{
						var e = "hevc_amf";
						if (!string.IsNullOrEmpty(e))
							return e;
						log("AMF encoder not available; using software encoder.");
						return SoftwareFallback();
					}
				case "vaapi":
					{
						var e = "hevc_vaapi";
						if (!string.IsNullOrEmpty(e))
							return e;
						log("VA-API encoder not available; using software encoder.");
						return SoftwareFallback();
					}
				default:
					return "";
			}
		}

		if (hwMode != "auto")
		{
			var forced = PickForced(hwMode);
			return string.IsNullOrEmpty(forced) ? SoftwareFallback() : forced;
		}
		return SoftwareFallback();
	}
}
