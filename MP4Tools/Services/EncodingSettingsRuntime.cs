using MP4ToolsLib;

namespace MP4Tools.Services;

/// <summary>In-memory encoding prefs shared by Trim, Combine, and FFmpeg hwaccel hints.</summary>
public static class EncodingSettingsRuntime
{
	public static EncodingSettingsDto Current { get; private set; } = new();

	public static void Apply(AppUserSettings settings)
	{
		Current = new EncodingSettingsDto();
		FfmpegUserHints.HardwareAcceleration = string.IsNullOrWhiteSpace(Current.HardwareAcceleration)
			? "auto"
			: Current.HardwareAcceleration;
		DefaultOutputPathRuntime.Apply(settings ?? new AppUserSettings());
	}
}
