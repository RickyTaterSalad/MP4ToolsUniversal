namespace MP4ToolsLib;

/// <summary>User encoding preferences (persisted in app settings JSON).</summary>
public sealed class EncodingSettingsDto
{
	public bool ReencodeOutput { get; set; }

	/// <summary>copy | h264 | h265 (hevc)</summary>
	public string VideoCodec { get; set; } = "copy";

	/// <summary>copy | aac | libopus</summary>
	public string AudioCodec { get; set; } = "copy";

	/// <summary>auto | 8 bit | 10 bit</summary>
	public string OutputBitDepth { get; set; } = "auto";

	/// <summary>auto | nvenc | qsv | vaapi | videotoolbox | software — auto picks first available HW encoder from FFmpeg; software forces CPU.</summary>
	public string HardwareAcceleration { get; set; } = "auto";
}
