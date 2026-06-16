using System;
using System.Collections.Generic;

namespace MP4ToolsLib;

/// <summary>
/// Builds ffmpeg options for HEVC MP4 output that DaVinci Resolve can import reliably.
/// Stream-copy trims of 10-bit HEVC often lack ctts/colr and carry bad edit lists.
/// </summary>
public static class DaVinciOutputEncoding
{
	public const int SoftwareCrf = 18;
	public const string PixelFormat8Bit = "yuv420p";
	public const string PixelFormat10Bit = "yuv420p10le";
	public const string VaapiUploadFormat8Bit = "nv12";
	public const string VaapiUploadFormat10Bit = "p010le";
	public const string HevcProfileMain10 = "main10";
	public const string VaapiFilterDeviceName = "va";

	public const string VaapiRateControlMode = "CQP";
	public const int VaapiQp = 28;
	public const int VaapiAsyncDepth = 16;

	public static bool UsesHardwareAcceleration(string hwAccel) =>
		hwAccel is "vaapi" or "amf";

	public static bool UsesVaapi(string hwAccel) =>
		string.Equals(hwAccel, "vaapi", StringComparison.OrdinalIgnoreCase);

	public static string GetHardwareAccelApi(string hwAccel) =>
		hwAccel?.ToLowerInvariant() switch
		{
			"vaapi" => "vaapi",
			"amf" => "d3d11va",
			_ => string.Empty
		};

	public static string GetHevcVideoEncoder(string hwAccel) =>
		hwAccel?.ToLowerInvariant() switch
		{
			"vaapi" => "hevc_vaapi",
			"amf" => "hevc_amf",
			_ => "libx265"
		};

	/// <summary>
	/// VAAPI device init before <c>-i</c>. Uses CPU decode + <c>hwupload</c> encode (not
	/// <c>-hwaccel_output_format vaapi</c>), which breaks on trimmed HEVC with <c>-ss</c> before <c>-i</c>.
	/// </summary>
	public static void AppendPreInputHwOptions(ICollection<FfmpegOption?> parts, string hwAccel)
	{
		if (!UsesVaapi(hwAccel))
		{
			var api = GetHardwareAccelApi(hwAccel);
			if (!string.IsNullOrWhiteSpace(api))
				parts.Add(FfmpegOption.Pair(FfmpegArguments.HardwareAcceleration, api));
			return;
		}

		parts.Add(FfmpegOption.Pair(
			FfmpegArguments.InitHardwareDevice,
			$"vaapi={VaapiFilterDeviceName}:{FFMpegUtils.GetPreferredRenderDevice()}"));
		parts.Add(FfmpegOption.Pair(FfmpegArguments.FilterHardwareDevice, VaapiFilterDeviceName));
	}

	public static void AppendPostInputHwOptions(ICollection<FfmpegOption?> parts, string hwAccel)
	{
	}

	public static void AppendMp4OutputOptions(ICollection<FfmpegOption?> parts)
	{
		parts.Add(FfmpegOption.Pair(FfmpegArguments.MuxerFlags, "write_colr"));
	}

	public static string GetVaapiHwUploadFilterChain(bool encodeTenBit) =>
		$"format={(encodeTenBit ? VaapiUploadFormat10Bit : VaapiUploadFormat8Bit)},hwupload";

	public static string GetSoftwareToVaapiUploadFilterSuffix(string hwAccel, bool encodeTenBit = false) =>
		UsesVaapi(hwAccel) && OperatingSystem.IsLinux()
			? $",{GetVaapiHwUploadFilterChain(encodeTenBit)}"
			: string.Empty;

	public static void AppendVideoEncodeOptions(ICollection<FfmpegOption?> parts, string hwAccel, bool encodeTenBit = false)
	{
		var encoder = GetHevcVideoEncoder(hwAccel);
		parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, encoder));
		if (encoder == "libx265")
		{
			parts.Add(FfmpegOption.Pair(FfmpegArguments.ConstantRateFactor, SoftwareCrf.ToString()));
			parts.Add(FfmpegOption.Pair(
				FfmpegArguments.PixelFormat,
				encodeTenBit ? PixelFormat10Bit : PixelFormat8Bit));
			AppendColorMetadata(parts);
		}
		else if (encoder == "hevc_vaapi")
		{
			parts.Add(FfmpegOption.Pair(FfmpegArguments.RateControlMode, VaapiRateControlMode));
			parts.Add(FfmpegOption.Pair(FfmpegArguments.QuantizationParameter, VaapiQp.ToString()));
			parts.Add(FfmpegOption.Pair(FfmpegArguments.AsyncDepth, VaapiAsyncDepth.ToString()));
			if (encodeTenBit)
				parts.Add(FfmpegOption.Pair(FfmpegArguments.VideoProfile, HevcProfileMain10));
			// No -color_primaries on VAAPI surfaces; colr comes from +write_colr on MP4 muxer.
		}
		else
		{
			AppendColorMetadata(parts);
		}
	}

	public static void AppendColorMetadata(ICollection<FfmpegOption?> parts)
	{
		parts.Add(FfmpegOption.Pair(FfmpegArguments.ColorPrimaries, "bt709"));
		parts.Add(FfmpegOption.Pair(FfmpegArguments.ColorTransfer, "bt709"));
		parts.Add(FfmpegOption.Pair(FfmpegArguments.ColorSpace, "bt709"));
	}

	/// <param name="softwareFrameInput">True for lavfi/CPU frames (intro); false for file input.</param>
	public static FfmpegOption BuildVideoFilterOption(
		string drawTextFilter,
		string hwAccel,
		bool softwareFrameInput = false,
		bool encodeTenBit = false)
	{
		if (softwareFrameInput)
		{
			var uploadSuffix = GetSoftwareToVaapiUploadFilterSuffix(hwAccel, encodeTenBit);
			if (!string.IsNullOrEmpty(drawTextFilter))
				return FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{drawTextFilter}{uploadSuffix}\"");
			if (!string.IsNullOrEmpty(uploadSuffix))
				return FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{uploadSuffix.TrimStart(',')}\"");
			return default;
		}

		if (UsesVaapi(hwAccel) && OperatingSystem.IsLinux())
		{
			var upload = GetVaapiHwUploadFilterChain(encodeTenBit);
			if (!string.IsNullOrEmpty(drawTextFilter))
				return FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{drawTextFilter},{upload}\"");

			return FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{upload}\"");
		}

		if (string.IsNullOrEmpty(drawTextFilter))
			return default;

		return FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{drawTextFilter}\"");
	}
}
