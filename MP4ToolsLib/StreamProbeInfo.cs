namespace MP4ToolsLib;

public sealed class StreamProbeInfo
{
	public string CodecType { get; set; } = string.Empty;
	public string Width { get; set; } = string.Empty;
	public string Height { get; set; } = string.Empty;
	public string FrameRate { get; set; } = string.Empty;
	public string PixelFormat { get; set; } = string.Empty;
	public string CodecName { get; set; } = string.Empty;
	public string SampleRate { get; set; } = string.Empty;
	public string Channels { get; set; } = string.Empty;
	public string ChannelLayout { get; set; } = string.Empty;
	public string BitDepth { get; set; }
}
