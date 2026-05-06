namespace MP4ToolsLib;

/// <summary>User encoding preferences (persisted in app settings JSON).</summary>
public sealed class EncodingSettingsDto
{
	public string VideoCodec { get;  } = "copy";

	public string AudioCodec { get;  } = "copy";
	public string TrimAudioCodec { get; } = "libopus";

	public string HardwareAcceleration { get; } = System.OperatingSystem.IsLinux() ? "vaapi" : "amf";
}
