using MP4ToolsLib;
using MP4ToolsLib.Tests.Configuration;
using MP4ToolsLib.Tests.Support;
using Xunit;

namespace MP4ToolsLib.Tests;

/// <summary>
/// Runs real FFmpeg against folders from <see cref="TestMediaConfiguration"/> — skips when unset or when no HW encoder is available.
/// </summary>
[Collection("FfmpegGlobalState")]
public sealed class FfmpegHardwareIntegrationTests : IDisposable
{
	private readonly bool _prevDry;

	public FfmpegHardwareIntegrationTests()
	{
		_prevDry = FFMpegUtils.DryRunExternalCommands;
		FFMpegUtils.DryRunExternalCommands = false;
		FfmpegEncoderCatalog.InvalidateCache();
	}

	public void Dispose()
	{
		FFMpegUtils.DryRunExternalCommands = _prevDry;
		FfmpegEncoderCatalog.InvalidateCache();
	}

	private static EncodingSettingsDto HevcHardwareAuto() => new()
	{
		VideoCodec = "h265",
		AudioCodec = "aac",
		HardwareAcceleration = "auto",
	};

	[SkippableFact]
	public async Task Trim_folder_video_transcodes_with_hardware_encoder_inside_long_clip_window()
	{
		Skip.IfNot(TestMediaConfiguration.HasTrimMedia,
			"Set MP4TOOLS_TEST_TRIM_INPUT_DIR to a folder with ≥10 min MP4(s).");
		Skip.IfNot(FfmpegCliRunner.TryGetFfmpegExecutable(out _), "ffmpeg not on PATH.");

		var input = TestMediaConfiguration.FirstTrimVideoOrEmpty();
		Skip.If(string.IsNullOrEmpty(input), "No .mp4 under trim folder.");

		var duration = await FfmpegCliRunner.ProbeDurationSecondsAsync(input).ConfigureAwait(false);
		Skip.If(duration < TestMediaConfiguration.MinimumDurationSecondsForBoundsTests,
			$"Video shorter than {TestMediaConfiguration.MinimumDurationSecondsForBoundsTests}s (got {duration:F1}s).");

		var prefs = HevcHardwareAuto();
		var plan = VideoEncodeSelector.BuildPlan(prefs, "8 bit", _ => { });
		Skip.If(plan.UseStreamCopy, "Unexpected stream-copy plan.");
		Skip.If(plan.EncoderSummary.Contains("libx265", StringComparison.OrdinalIgnoreCase),
			"No hardware HEVC encoder reported by ffmpeg -encoders on this machine.");

		Directory.CreateDirectory(TestMediaConfiguration.OutputDirectory);
		var output = Path.Combine(TestMediaConfiguration.OutputDirectory, $"trim_hw_{Guid.NewGuid():N}.mp4");

		var args = HardwareEncodeScenarioBuilder.BuildTrimHardwareTranscodeArgs(input, output, prefs, "8 bit");
		var exit = await FfmpegCliRunner.RunFfmpegAsync(args).ConfigureAwait(false);
		Assert.Equal(0, exit);
		Assert.True(File.Exists(output));
		File.Delete(output);
	}

	[SkippableFact]
	public async Task Combine_folder_concat_demuxer_transcodes_with_hardware_encoder()
	{
		Skip.IfNot(TestMediaConfiguration.HasCombineMedia,
			"Set MP4TOOLS_TEST_COMBINE_INPUT_DIR to a folder with at least two MP4 files.");
		Skip.IfNot(FfmpegCliRunner.TryGetFfmpegExecutable(out _), "ffmpeg not on PATH.");

		var files = TestMediaConfiguration.CombineVideosTopLevel();
		Skip.If(files.Count < 2, "Need at least two .mp4 files in combine folder.");

		foreach (var f in files.Take(2))
		{
			var d = await FfmpegCliRunner.ProbeDurationSecondsAsync(f).ConfigureAwait(false);
			Skip.If(d < TestMediaConfiguration.MinimumDurationSecondsForBoundsTests,
				$"Each combine source should be ≥ {TestMediaConfiguration.MinimumDurationSecondsForBoundsTests}s (got {d:F1}s for {Path.GetFileName(f)}).");
		}

		var prefs = HevcHardwareAuto();
		var plan = VideoEncodeSelector.BuildPlan(prefs, "8 bit", _ => { });
		Skip.If(plan.UseStreamCopy, "Unexpected stream-copy plan.");
		Skip.If(plan.EncoderSummary.Contains("libx265", StringComparison.OrdinalIgnoreCase),
			"No hardware HEVC encoder reported by ffmpeg -encoders on this machine.");

		var listPath = Path.Combine(Path.GetTempPath(), $"mp4tools_combine_test_{Guid.NewGuid():N}.txt");
		await File.WriteAllLinesAsync(listPath,
			files.Take(2).Select(p => $"file '{Path.GetFullPath(p)}'")).ConfigureAwait(false);

		try
		{
			Directory.CreateDirectory(TestMediaConfiguration.OutputDirectory);
			var output = Path.Combine(TestMediaConfiguration.OutputDirectory, $"combine_hw_{Guid.NewGuid():N}.mp4");
			var args = HardwareEncodeScenarioBuilder.BuildConcatHardwareTranscodeArgs(listPath, output, prefs, "8 bit");
			var exit = await FfmpegCliRunner.RunFfmpegAsync(args).ConfigureAwait(false);
			Assert.Equal(0, exit);
			Assert.True(File.Exists(output));
			File.Delete(output);
		}
		finally
		{
			try
			{
				File.Delete(listPath);
			}
			catch
			{
				// ignored
			}
		}
	}
}
