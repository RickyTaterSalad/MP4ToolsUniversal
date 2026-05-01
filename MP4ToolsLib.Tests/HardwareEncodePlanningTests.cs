using MP4ToolsLib;
using MP4ToolsLib.Tests.Support;
using Xunit;

namespace MP4ToolsLib.Tests;

/// <summary>Encoder selection with dry-run catalog (no FFmpeg subprocess for <c>-encoders</c>).</summary>
[Collection("FfmpegGlobalState")]
public sealed class HardwareEncodePlanningTests : IDisposable
{
	private readonly FfmpegTestEnvironment _env = new(dryRunExternalCommands: true);

	public void Dispose() => _env.Dispose();

	private static EncodingSettingsDto HwHevc(string hw = "auto") => new()
	{
		ReencodeOutput = true,
		VideoCodec = "h265",
		AudioCodec = "aac",
		OutputBitDepth = "8 bit",
		HardwareAcceleration = hw,
	};

	[Fact]
	public void Auto_does_not_pick_software_hevc_when_catalog_lists_hardware_encoders()
	{
		var plan = VideoEncodeSelector.BuildPlan(HwHevc("auto"), "8 bit", _ => { });
		Assert.False(plan.UseStreamCopy);
		Assert.DoesNotContain("libx265", plan.EncoderSummary, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("_nvenc", plan.EncoderSummary, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Software_forces_libx265_even_when_hardware_encoders_exist_in_dry_run_catalog()
	{
		var plan = VideoEncodeSelector.BuildPlan(HwHevc("software"), "8 bit", _ => { });
		Assert.False(plan.UseStreamCopy);
		Assert.Contains("libx265", plan.EncoderSummary, StringComparison.OrdinalIgnoreCase);
		Assert.False(plan.NeedsVaapiUploadFilter);
	}

	[Fact]
	public void Vaapi_hevc_sets_upload_filter_and_encoder_tail()
	{
		var plan = VideoEncodeSelector.BuildPlan(HwHevc("vaapi"), "8 bit", _ => { });
		Assert.False(plan.UseStreamCopy);
		Assert.Contains("vaapi", plan.EncoderSummary, StringComparison.OrdinalIgnoreCase);
		Assert.True(plan.NeedsVaapiUploadFilter);
		Assert.Contains(plan.VideoEncodeOptions, o => o is { } x && x.Flag == "-rc_mode");
	}

	[Fact]
	public void Intro_plan_matches_main_codec_and_can_use_vaapi()
	{
		var dto = HwHevc("vaapi");
		var intro = VideoEncodeSelector.BuildIntroPlan(dto, _ => { });
		Assert.False(intro.UseStreamCopy);
		Assert.Contains("vaapi", intro.EncoderSummary, StringComparison.OrdinalIgnoreCase);
		Assert.True(intro.NeedsVaapiUploadFilter);
	}

	[Fact]
	public void Stream_copy_skips_encoder_tail()
	{
		var dto = new EncodingSettingsDto
		{
			ReencodeOutput = false,
			VideoCodec = "copy",
			HardwareAcceleration = "auto",
		};
		var plan = VideoEncodeSelector.BuildPlan(dto, "8 bit", _ => { });
		Assert.True(plan.UseStreamCopy);
		Assert.Empty(plan.VideoEncodeOptions);
	}

	[Fact]
	public void Nvenc_mode_selects_nvenc_in_summary()
	{
		var plan = VideoEncodeSelector.BuildPlan(HwHevc("nvenc"), "8 bit", _ => { });
		Assert.False(plan.UseStreamCopy);
		Assert.Contains("nvenc", plan.EncoderSummary, StringComparison.OrdinalIgnoreCase);
		Assert.False(plan.NeedsVaapiUploadFilter);
	}
}
