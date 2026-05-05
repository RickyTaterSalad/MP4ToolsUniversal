using MP4ToolsLib;
using MP4ToolsLib.Tests.Support;
using Xunit;

namespace MP4ToolsLib.Tests;

[Collection("FfmpegGlobalState")]
public sealed class EncodeProcessingSummaryTests : IDisposable
{
	private readonly FfmpegTestEnvironment _env = new(dryRunExternalCommands: true);

	public void Dispose() => _env.Dispose();

	[Fact]
	public void BuildLines_includes_hardware_and_intro_notes()
	{
		var dto = new EncodingSettingsDto
		{
			VideoCodec = "h265",
			AudioCodec = "aac",
			HardwareAcceleration = OperatingSystem.IsLinux() ? "vaapi" : "amf"
		};
		var lines = EncodeProcessingSummary.BuildLines("UnitTest", dto, "8 bit").ToArray();
		Assert.Contains(lines, l => l.Contains("hardware_acceleration=vaapi", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(lines, l => l.Contains("Intro slide", StringComparison.OrdinalIgnoreCase));
	}
}
