namespace MP4ToolsLib;

public sealed class EncodingSettingsDto
{
	public string VideoCodec { get;  } = "copy";

	public  string AudioCodec { get;  } = "copy";
	public string TrimAudioCodec { get; } = "libopus";

	public string HardwareAcceleration{ get; } = System.OperatingSystem.IsLinux() ? "vaapi" : "amf";

	public bool UseResolveSafeEncoding { get; init; } = true;
}
