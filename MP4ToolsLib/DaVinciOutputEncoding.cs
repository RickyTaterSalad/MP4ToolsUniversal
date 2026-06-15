using System;
using System.Collections.Generic;

namespace MP4ToolsLib;

/// <summary>
/// Builds ffmpeg options for 8-bit HEVC MP4 output that DaVinci Resolve can import reliably.
/// Stream-copy trims of 10-bit HEVC often lack ctts/colr and carry bad edit lists.
/// </summary>
public static class DaVinciOutputEncoding
{
	public const int SoftwareCrf = 18;
	public const string PixelFormat8Bit = "yuv420p";

	public static bool UsesHardwareAcceleration(string hwAccel) =>
		hwAccel is "vaapi" or "amf";

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

	public static void AppendPreInputHwOptions(ICollection<FfmpegOption?> parts, string hwAccel)
	{
		var api = GetHardwareAccelApi(hwAccel);
		if (string.IsNullOrWhiteSpace(api))
			return;
		parts.Add(FfmpegOption.Pair(FfmpegArguments.HardwareAcceleration, api));
	}

	public static void AppendPostInputHwOptions(ICollection<FfmpegOption?> parts, string hwAccel)
	{
		var api = GetHardwareAccelApi(hwAccel);
		if (string.IsNullOrWhiteSpace(api) || !OperatingSystem.IsLinux())
			return;
		parts.Add(FfmpegOption.Pair(FfmpegArguments.InitHardwareDevice, $"{api}={FFMpegUtils.GetPreferredRenderDevice()}"));
		parts.Add(FfmpegOption.Pair(FfmpegArguments.FilterHardwareDevice, FFMpegUtils.GetPreferredRenderDevice()));
	}

	/// <summary>Suffix appended to -vf when uploading frames for VAAPI on Linux.</summary>
	public static string GetHwUploadVideoFilterSuffix(string hwAccel) =>
		UsesHardwareAcceleration(hwAccel) && OperatingSystem.IsLinux()
			? ",format=nv12,hwupload"
			: string.Empty;

	public static void AppendVideoEncodeOptions(ICollection<FfmpegOption?> parts, string hwAccel)
	{
		var encoder = GetHevcVideoEncoder(hwAccel);
		parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, encoder));
		if (encoder == "libx265")
		{
			parts.Add(FfmpegOption.Pair(FfmpegArguments.ConstantRateFactor, SoftwareCrf.ToString()));
			parts.Add(FfmpegOption.Pair(FfmpegArguments.PixelFormat, PixelFormat8Bit));
		}
		AppendColorMetadata(parts);
	}

	public static void AppendColorMetadata(ICollection<FfmpegOption?> parts)
	{
		parts.Add(FfmpegOption.Pair(FfmpegArguments.ColorPrimaries, "bt709"));
		parts.Add(FfmpegOption.Pair(FfmpegArguments.ColorTransfer, "bt709"));
		parts.Add(FfmpegOption.Pair(FfmpegArguments.ColorSpace, "bt709"));
	}

	public static FfmpegOption BuildVideoFilterOption(string drawTextFilter, string hwAccel)
	{
		var suffix = GetHwUploadVideoFilterSuffix(hwAccel);
		if (!string.IsNullOrEmpty(drawTextFilter))
			return FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{drawTextFilter}{suffix}\"");
		if (!string.IsNullOrEmpty(suffix))
			return FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{suffix.TrimStart(',')}\"");
		return default;
	}
}
