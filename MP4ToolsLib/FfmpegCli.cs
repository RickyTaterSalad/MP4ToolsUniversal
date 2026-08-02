using System.Collections.Generic;

namespace MP4ToolsLib;

/// <summary>
/// FFmpeg CLI flag and common literal values used when building command lines.
/// Names describe purpose; values match ffmpeg verbatim.
/// </summary>
public static class FfmpegArguments
{
	public const string DisableInteractiveStdin = "-nostdin";
	public const string OverwriteOutputFile = "-y";
	public const string Input = "-i";
	public const string InitHardwareDevice = "-init_hw_device";
	public const string FilterHardwareDevice = "-filter_hw_device";

	public const string HardwareAcceleration = "-hwaccel";
	public const string HardwareAccelerationOutputFormat = "-hwaccel_output_format";

	public const string SeekInputTimestamp = "-ss";
	public const string LimitOutputDuration = "-t";

	public const string InputFormat = "-f";
	public const string InputFormatConcatDemuxer = "concat";
	public const string InputFormatMpegTs = "mpegts";
	public const string InputFormatLavfi = "lavfi";

	/// <summary>Concat demuxer path-safety; pair with <see cref="ConcatDemuxerAllowAnyPath"/>.</summary>
	public const string ConcatDemuxerSafeFlag = "-safe";
	public const string ConcatDemuxerAllowAnyPath = "0";

	public const string SelectCodec = "-c";
	public const string SelectVideoCodec = "-c:v";
	public const string SelectAudioCodec = "-c:a";
	public const string StreamCopy = "copy";

	public const string VideoBitstreamFilter = "-bsf:v";
	public const string AudioBitstreamFilter = "-bsf:a";
	public const string BitstreamFilterAacAdtsToAsc = "aac_adtstoasc";

	public const string VideoFilter = "-vf";

	public const string AudioSampleRate = "-ar";
	public const string AudioChannels = "-ac";
	public const string AudioBitrate = "-b:a";

	public const string VideoBitrate = "-b:v";

	public const string Vbr = "-vbr";

	public const string OutputVideoFrameRate = "-r";
	public const string StopEncodingWhenShortestStreamEnds = "-shortest";

	public const string ConstantRateFactor = "-crf";
	public const string EncoderPreset = "-preset";
	public const string X265Params = "-x265-params";
	public const string RateControlMode = "-rc_mode";
	public const string QuantizationParameter = "-qp";

	public const string GlobalQuality = "-global_quality";

	public const string VideoProfile = "-profile:v";
	public const string AsyncDepth = "-async_depth";
	public const string PixelFormat = "-pix_fmt";
	public const string ColorPrimaries = "-color_primaries";
	public const string ColorTransfer = "-color_trc";
	public const string ColorSpace = "-colorspace";
	public const string MuxerFlags = "-movflags";

	public const string MapStreams = "-map";
	public const string MapMetadata = "-map_metadata";
	public const string MapChapters = "-map_chapters";
}

/// <summary>
/// One ffmpeg CLI segment: unary flag, flag+value, or a positional tail token (e.g. output path).
/// Use <see cref="string.Empty"/> for the unused half of <see cref="Unary"/> or <see cref="Positional"/>.
/// </summary>
public readonly record struct FfmpegOption(string Flag, string Value)
{
	public bool IsSkipped =>
		string.IsNullOrWhiteSpace(Flag) && string.IsNullOrWhiteSpace(Value);

	public static FfmpegOption Unary(string flag) => new(flag, string.Empty);

	public static FfmpegOption Pair(string flag, string value) => new(flag, value);

	/// <summary>Argument with no leading flag (typically the output filename).</summary>
	public static FfmpegOption Positional(string argumentToken) => new(string.Empty, argumentToken);

	internal IEnumerable<string> ToArgvTokens()
	{
		if (IsSkipped)
			yield break;
		if (!string.IsNullOrWhiteSpace(Flag))
		{
			yield return Flag;
			if (!string.IsNullOrWhiteSpace(Value))
				yield return Value;
			yield break;
		}
		if (!string.IsNullOrWhiteSpace(Value))
			yield return Value;
	}
}

/// <summary>Assembles a single ffmpeg <c>Arguments</c> string from option pairs.</summary>
public static class FfmpegCommandLine
{
	public static string Build(params FfmpegOption?[] options) => Build((IEnumerable<FfmpegOption?>)options);

	public static string Build(IEnumerable<FfmpegOption?> options)
	{
		var parts = new List<string>();
		foreach (var opt in options)
		{
			if (opt is not { } o || o.IsSkipped)
				continue;
			parts.AddRange(o.ToArgvTokens());
		}
		return string.Join(" ", parts);
	}

	public static string Quoted(string value) => $"\"{value}\"";
}
