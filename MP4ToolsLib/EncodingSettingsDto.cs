namespace MP4ToolsLib;

/// <summary>User encoding preferences (persisted in app settings JSON).</summary>
public sealed class EncodingSettingsDto
{
	public string VideoCodec { get; set; } = "copy";

	public string AudioCodec { get; set; } = "copy";

	public string TrimAudioCodec { get; set; } = "libopus";

	public string HardwareAcceleration { get; set; } = System.OperatingSystem.IsLinux() ? "vaapi" : "amf";
}
