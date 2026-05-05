using System;
using System.Collections.Generic;

namespace MP4ToolsLib;

public sealed class VideoEncodePlan
{
	public bool UseStreamCopy { get; init; }

	/// <summary>Diagnostic label written to log.</summary>
	public string EncoderSummary { get; init; } = "";

	public bool NeedsVaapiUploadFilter { get; init; }

	//public IReadOnlyList<FfmpegOption?> VideoEncodeOptions { get; init; } = Array.Empty<FfmpegOption?>();
}

/// <summary>Selects video encoder names and tails from persisted encoding preferences.</summary>
public static class VideoEncodeSelector
{
	public const string VaapiUploadSuffix = "format=nv12,hwupload=derive_device=vaapi:extra_hw_frames=64";

	/// <summary>-vf chain required before <c>hevc_vaapi</c>/<c>h264_vaapi</c> when decoding to system memory (same as Trim re-encode).</summary>
	public static FfmpegOption VaapiUploadVideoFilterOption =>
		FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{VaapiUploadSuffix}\"");

	public static string EffectiveAudioCodec(EncodingSettingsDto dto)
	{
		return "libopus";
		/*
		if (dto == null || !dto.ReencodeOutput)
			return FfmpegArguments.StreamCopy;

		return dto.AudioCodec?.ToLowerInvariant() switch
		{
			"aac" => "aac",
			"libopus" => "libopus",
			"copy" => FfmpegArguments.StreamCopy,
			_ => "aac",
		};
		*/
	}

	/// <summary>Audio for Trim segment FFmpeg steps; uses <see cref="EncodingSettingsDto.TrimAudioCodec"/> (libopus when unset), not Encode-tab <see cref="EncodingSettingsDto.AudioCodec"/>.</summary>
	public static string EffectiveAudioCodecForTrim(EncodingSettingsDto dto)
	{
		if (dto == null)
			return FfmpegArguments.StreamCopy;

		var raw = string.IsNullOrWhiteSpace(dto.TrimAudioCodec) ? "libopus" : dto.TrimAudioCodec.Trim();
		return raw.ToLowerInvariant() switch
		{
			"aac" => "aac",
			"libopus" or "opus" => "libopus",
			"copy" => FfmpegArguments.StreamCopy,
			_ => "aac",
		};
	}

	public static VideoEncodePlan BuildPlan(EncodingSettingsDto dto, Action<string> log)
	{
		log ??= _ => { };

		if (dto == null || IsCopyCodec(dto.VideoCodec))
		{
			return new VideoEncodePlan
			{
				UseStreamCopy = true,
				EncoderSummary = "copy",
				//	VideoEncodeOptions = Array.Empty<FfmpegOption?>(),
			};
		}

		bool hevc = IsHevcFamily(dto.VideoCodec);
		string encoder = PickEncoder(hevc, NormalizeHwMode(dto.HardwareAcceleration), log);
		bool vaapi = encoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
		//var tail = BuildEncodeTail(encoder, hevc, log);

		return new VideoEncodePlan
		{
			UseStreamCopy = false,
			EncoderSummary = $"{encoder} (pix pipeline per tail)",
			NeedsVaapiUploadFilter = vaapi//,
										  //	VideoEncodeOptions = tail,
		};
	}

	public static VideoEncodePlan BuildIntroPlan(EncodingSettingsDto dto, Action<string> log)
	{
		log ??= _ => { };
		var pseudo = new EncodingSettingsDto
		{
			HardwareAcceleration = dto?.HardwareAcceleration ?? "auto",
			AudioCodec = dto?.AudioCodec ?? "copy",
			VideoCodec = "h265"
		};


		return BuildPlan(pseudo, log);
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

	private static List<FfmpegOption?> BuildEncodeTail(string encoder, bool hevc, Action<string> log)
	{
		var list = new List<FfmpegOption?>
		{
			FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, encoder),
		};
		/*
				if (encoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
				{
					list.Add(FfmpegOption.Pair(FfmpegArguments.VideoBitrate, "12M"));
					list.Add(FfmpegOption.Pair(FfmpegArguments.VideoMaxBitrate, "24M"));
					list.Add(FfmpegOption.Pair(FfmpegArguments.VideoProfile, "main10"));
					list.Add(FfmpegOption.Pair(FfmpegArguments.VaapiRateControlMode, "3"));
					return list;
				}
				*/

		return list;
	}
}
