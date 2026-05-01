namespace MP4ToolsLib;

/// <summary>
/// Per-operation hints read by <see cref="FFMpegUtils.ApplyForcedFfmpegArgs"/> for decode acceleration.
/// Set from the Encode tab before Trim/Combine runs.
/// </summary>
public static class FfmpegUserHints
{
	public static string HardwareAcceleration { get; set; } = "auto";
}
