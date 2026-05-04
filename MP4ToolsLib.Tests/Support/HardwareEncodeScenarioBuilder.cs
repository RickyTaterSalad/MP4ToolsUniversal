using System.Collections.Generic;
using MP4ToolsLib;

namespace MP4ToolsLib.Tests.Support;

/// <summary>Builds FFmpeg CLI fragments aligned with Trim / Combine hardware transcode paths.</summary>
public static class HardwareEncodeScenarioBuilder
{
	/// <summary>Seek + limited duration suitable for ≥10 minute sources (5–9.5 min window).</summary>
	public const string SeekInsideLongClip = "00:05:00";

	public const string SegmentDuration = "00:04:30";

	public static string BuildTrimHardwareTranscodeArgs(
		string inputPath,
		string outputPath,
		EncodingSettingsDto prefs,
		string inputBitDepthUi)
	{
		FfmpegUserHints.HardwareAcceleration = prefs.HardwareAcceleration ?? "auto";
		var plan = VideoEncodeSelector.BuildPlan(prefs, inputBitDepthUi ?? string.Empty, _ => { });

		var parts = new List<FfmpegOption?>
		{
			FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
			FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
			FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, SeekInsideLongClip),
			FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(PathFull(inputPath))),
			FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, SegmentDuration),
		};

		if (!plan.UseStreamCopy && plan.NeedsVaapiUploadFilter)
			parts.Add(VideoEncodeSelector.VaapiUploadVideoFilterOption);

		if (!plan.UseStreamCopy)
		{
			foreach (var o in plan.VideoEncodeOptions)
				parts.Add(o);
			parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec,
				VideoEncodeSelector.EffectiveAudioCodec(prefs)));
		}
		else
		{
			parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy));
			parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, FfmpegArguments.StreamCopy));
		}

		parts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(PathFull(outputPath))));

		var core = FfmpegCommandLine.Build(parts);
		return FFMpegUtils.Instance.ApplyForcedFfmpegArgs(core);
	}

	public static string BuildConcatHardwareTranscodeArgs(
		string concatListFile,
		string outputPath,
		EncodingSettingsDto prefs,
		string inputBitDepthUi)
	{
		FfmpegUserHints.HardwareAcceleration = prefs.HardwareAcceleration ?? "auto";
		var plan = VideoEncodeSelector.BuildPlan(prefs, inputBitDepthUi ?? string.Empty, _ => { });

		var parts = new List<FfmpegOption?>
		{
			FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
			FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
			FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatConcatDemuxer),
			FfmpegOption.Pair(FfmpegArguments.ConcatDemuxerSafeFlag, FfmpegArguments.ConcatDemuxerAllowAnyPath),
			FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(PathFull(concatListFile))),
		};

		if (!plan.UseStreamCopy && plan.NeedsVaapiUploadFilter)
			parts.Add(VideoEncodeSelector.VaapiUploadVideoFilterOption);

		if (!plan.UseStreamCopy)
		{
			foreach (var o in plan.VideoEncodeOptions)
				parts.Add(o);
			parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec,
				VideoEncodeSelector.EffectiveAudioCodec(prefs)));
		}
		else
		{
			parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy));
			parts.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, FfmpegArguments.StreamCopy));
		}

		var aacBsf = VideoEncodeSelector.EffectiveAudioCodec(prefs).Contains("aac", StringComparison.OrdinalIgnoreCase)
			? FfmpegOption.Pair(FfmpegArguments.AudioBitstreamFilter, FfmpegArguments.BitstreamFilterAacAdtsToAsc)
			: (FfmpegOption?)null;
		parts.Add(aacBsf);
		parts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(PathFull(outputPath))));

		var core = FfmpegCommandLine.Build(parts);
		return FFMpegUtils.Instance.ApplyForcedFfmpegArgs(core);
	}

	private static string PathFull(string path) =>
		string.IsNullOrWhiteSpace(path) ? path : Path.GetFullPath(path);
}
