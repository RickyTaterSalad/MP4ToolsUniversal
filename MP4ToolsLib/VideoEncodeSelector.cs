using System;
using System.Collections.Generic;

namespace MP4ToolsLib;

public sealed class VideoEncodePlan
{
	public bool UseStreamCopy { get; init; }

	/// <summary>Diagnostic label written to log.</summary>
	public string EncoderSummary { get; init; } = "";

	public bool NeedsVaapiUploadFilter { get; init; }

	public IReadOnlyList<FfmpegOption?> VideoEncodeOptions { get; init; } = Array.Empty<FfmpegOption?>();
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
		if (dto == null || !dto.ReencodeOutput)
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

	public static VideoEncodePlan BuildPlan(EncodingSettingsDto dto, string inputVideoBitDepthUi, Action<string> log)
	{
		log ??= _ => { };

		if (dto == null || !dto.ReencodeOutput || IsCopyCodec(dto.VideoCodec))
		{
			return new VideoEncodePlan
			{
				UseStreamCopy = true,
				EncoderSummary = "copy",
				VideoEncodeOptions = Array.Empty<FfmpegOption?>(),
			};
		}

		bool hevc = IsHevcFamily(dto.VideoCodec);
		bool want10 = ResolveWantTenBit(dto.OutputBitDepth, inputVideoBitDepthUi);
		string encoder = PickEncoder(hevc, want10, NormalizeHwMode(dto.HardwareAcceleration), log);
		bool vaapi = encoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
		var tail = BuildEncodeTail(encoder, hevc, want10, log);

		return new VideoEncodePlan
		{
			UseStreamCopy = false,
			EncoderSummary = $"{encoder} (pix pipeline per tail)",
			NeedsVaapiUploadFilter = vaapi,
			VideoEncodeOptions = tail,
		};
	}

	/// <summary>
	/// Maps ffprobe <c>codec_name</c> (video) to the Encode-tab style codec used for intro encoder selection.
	/// Empty when unknown — caller keeps dto-only behavior.
	/// </summary>
	public static string MatchIntroVideoCodecFromProbe(string ffprobeVideoCodecName)
	{
		var x = ffprobeVideoCodecName?.Trim().ToLowerInvariant() ?? "";
		if (x is "hevc" or "h265" or "h.265" or "hev1" or "hvc1")
			return "hevc";
		if (x is "h264" or "avc" or "avc1")
			return "h264";
		return "";
	}

	/// <summary>
	/// When Combine finishes from an MPEG-TS intermediate, if the Encode tab asks for HEVC/H.264 transcode but the merged TS is already that codec,
	/// remux with <c>-c:v copy</c> instead of decoding/re-encoding (order-of-magnitude faster for 4K drone footage).
	/// </summary>
	public static bool ShouldStreamCopyVideoWhenRemuxingMergedTs(EncodingSettingsDto dto, string mergedTsFfprobeVideoCodecName)
	{
		if (dto == null || !dto.ReencodeOutput || IsCopyCodec(dto.VideoCodec))
			return false;

		var mergedFamily = MatchIntroVideoCodecFromProbe(mergedTsFfprobeVideoCodecName);
		if (string.IsNullOrEmpty(mergedFamily))
			return false;

		var tabFamily = MatchIntroVideoCodecFromProbe(dto.VideoCodec);
		return !string.IsNullOrEmpty(tabFamily) && mergedFamily == tabFamily;
	}

	/// <summary>Intro slide encode uses the same HW/software rules as the main encode tab (plus drawtext + VA-API hwupload when needed).</summary>
	public static VideoEncodePlan BuildIntroPlan(EncodingSettingsDto dto, Action<string> log) =>
		BuildIntroPlan(dto, matchMainClipFfprobeVideoCodec: null, log);

	/// <inheritdoc cref="BuildIntroPlan(EncodingSettingsDto, Action{string})"/>
	/// <param name="matchMainClipFfprobeVideoCodec">
	/// When set (ffprobe video <c>codec_name</c> for the clip concatenated after the intro), the intro is encoded with this codec family
	/// so MPEG-TS <c>concat:</c> matches stream-copied segments (e.g. HEVC drone + intro both HEVC).
	/// </param>
	public static VideoEncodePlan BuildIntroPlan(EncodingSettingsDto dto, string matchMainClipFfprobeVideoCodec, Action<string> log)
	{
		log ??= _ => { };
		var pseudo = new EncodingSettingsDto
		{
			ReencodeOutput = true,
			OutputBitDepth = dto?.OutputBitDepth ?? "auto",
			HardwareAcceleration = dto?.HardwareAcceleration ?? "auto",
			AudioCodec = dto?.AudioCodec ?? "copy",
		};

		var matched = MatchIntroVideoCodecFromProbe(matchMainClipFfprobeVideoCodec ?? "");
		if (!string.IsNullOrEmpty(matched))
		{
			pseudo.VideoCodec = matched;
			if (dto == null || !dto.ReencodeOutput || IsCopyCodec(dto.VideoCodec))
				pseudo.OutputBitDepth = string.IsNullOrWhiteSpace(dto?.OutputBitDepth) ? "8 bit" : dto.OutputBitDepth;
			else
				pseudo.OutputBitDepth = dto.OutputBitDepth ?? "auto";
		}
		else if (dto == null || !dto.ReencodeOutput || IsCopyCodec(dto.VideoCodec))
		{
			pseudo.VideoCodec = "h264";
			pseudo.OutputBitDepth = "8 bit";
		}
		else
		{
			pseudo.VideoCodec = dto.VideoCodec;
		}

		return BuildPlan(pseudo, string.Empty, log);
	}

	private static bool IsCopyCodec(string v) =>
		string.IsNullOrWhiteSpace(v) || string.Equals(v, "copy", StringComparison.OrdinalIgnoreCase);

	private static bool IsHevcFamily(string v)
	{
		var x = v?.Trim().ToLowerInvariant() ?? "";
		return x is "h265" or "hevc" or "h.265";
	}

	private static bool ResolveWantTenBit(string outputSetting, string inputUi)
	{
		if (string.Equals(outputSetting, "10 bit", StringComparison.OrdinalIgnoreCase))
			return true;
		if (string.Equals(outputSetting, "8 bit", StringComparison.OrdinalIgnoreCase))
			return false;

		var probe = inputUi ?? "";
		return probe.Contains("10", StringComparison.OrdinalIgnoreCase);
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

	private static string PickEncoder(bool hevc, bool want10Bit, string hwMode, Action<string> log)
	{
		string Try(params string[] ids)
		{
			foreach (var id in ids)
			{
				if (FfmpegEncoderCatalog.HasEncoder(id))
					return id;
			}

			return "";
		}

		string SoftwareFallback()
		{
			return hevc ? "libx265" : "libx264";
		}

		if (hwMode == "software")
			return SoftwareFallback();

		string NvEnc() => Try(hevc ? "hevc_nvenc" : "h264_nvenc");
		string Qsv() => Try(hevc ? "hevc_qsv" : "h264_qsv");
		string Vaapi() => Try(hevc ? "hevc_vaapi" : "h264_vaapi");
		string Vtb() => Try(hevc ? "hevc_videotoolbox" : "h264_videotoolbox");

		string PickForced(string mode)
		{
			switch (mode)
			{
				case "nvenc":
				{
					var e = NvEnc();
					if (!string.IsNullOrEmpty(e))
						return e;
					log($"NVENC H.{(hevc ? "265" : "264")} not available; using software encoder.");
					return SoftwareFallback();
				}
				case "qsv":
				{
					var e = Qsv();
					if (!string.IsNullOrEmpty(e))
						return e;
					log("Intel Quick Sync encoder not available; using software encoder.");
					return SoftwareFallback();
				}
				case "videotoolbox":
				{
					var e = Vtb();
					if (!string.IsNullOrEmpty(e))
						return e;
					log("VideoToolbox encoder not available; using software encoder.");
					return SoftwareFallback();
				}
				case "vaapi":
				{
					var e = Vaapi();
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

		// auto — FFmpeg lists NVENC even without an NVIDIA GPU; prefer VA-API first for AMD/Linux, then QSV, NVENC, macOS.
		foreach (var pick in new Func<string>[] { Vaapi, Qsv, NvEnc, Vtb })
		{
			var e = pick();
			if (!string.IsNullOrEmpty(e))
				return e;
		}

		log($"Hardware video encoder not found for H.{(hevc ? "265" : "264")}; using {SoftwareFallback()}.");
		return SoftwareFallback();
	}

	private static List<FfmpegOption?> BuildEncodeTail(string encoder, bool hevc, bool want10Bit, Action<string> log)
	{
		var list = new List<FfmpegOption?>
		{
			FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, encoder),
		};

		if (encoder.Contains("nvenc", StringComparison.OrdinalIgnoreCase))
		{
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoPreset, "p5"));
			list.Add(FfmpegOption.Pair("-cq", "26"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoBitrate, "12M"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoMaxBitrate, "24M"));

			if (hevc && want10Bit)
			{
				list.Add(FfmpegOption.Pair(FfmpegArguments.VideoProfile, "main10"));
				list.Add(FfmpegOption.Pair(FfmpegArguments.PixelFormat, "p010le"));
			}
			else
			{
				if (want10Bit && !hevc)
					log("10-bit H.264 is not used with NVENC here; output uses 8-bit yuv420p.");

				list.Add(FfmpegOption.Pair(FfmpegArguments.PixelFormat, "yuv420p"));
			}

			return list;
		}

		if (encoder.Contains("_qsv", StringComparison.OrdinalIgnoreCase))
		{
			list.Add(FfmpegOption.Pair("-global_quality", "26"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoBitrate, "12M"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoMaxBitrate, "24M"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.PixelFormat, hevc && want10Bit ? "p010le" : "nv12"));
			return list;
		}

		if (encoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase))
		{
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoBitrate, "12M"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoMaxBitrate, "24M"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.PixelFormat, hevc && want10Bit ? "p010le" : "nv12"));
			return list;
		}

		if (encoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
		{
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoBitrate, "12M"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoMaxBitrate, "24M"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoProfile, hevc
				? (want10Bit ? "main10" : "main")
				: "high"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VaapiRateControlMode, "3"));
			return list;
		}

		// libx264 / libx265
		list.Add(FfmpegOption.Pair(FfmpegArguments.VideoPreset, "fast"));
		list.Add(FfmpegOption.Pair(FfmpegArguments.VideoBitrate, "12M"));
		list.Add(FfmpegOption.Pair(FfmpegArguments.VideoMaxBitrate, "24M"));

		if (hevc)
		{
			list.Add(FfmpegOption.Pair(FfmpegArguments.PixelFormat, want10Bit ? "yuv420p10le" : "yuv420p"));
		}
		else
		{
			list.Add(FfmpegOption.Pair(FfmpegArguments.PixelFormat, want10Bit ? "yuv420p10le" : "yuv420p"));
			list.Add(FfmpegOption.Pair(FfmpegArguments.VideoProfile, want10Bit ? "high10" : "high"));
		}

		return list;
	}
}
