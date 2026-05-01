using MP4ToolsLib;

namespace MP4Tools.Services;

/// <summary>In-memory encoding prefs shared by Trim, Combine, and FFmpeg hwaccel hints.</summary>
public static class EncodingSettingsRuntime
{
	public static EncodingSettingsDto Current { get; private set; } = new();

	public static void Apply(AppUserSettings settings)
	{
		Current = settings?.Encoding ?? new EncodingSettingsDto();
		FfmpegUserHints.HardwareAcceleration = string.IsNullOrWhiteSpace(Current.HardwareAcceleration)
			? "auto"
			: Current.HardwareAcceleration;
		var nextDry = settings?.DryRunFfmpegCommands ?? false;
		if (FFMpegUtils.DryRunExternalCommands != nextDry)
			FfmpegEncoderCatalog.InvalidateCache();
		FFMpegUtils.DryRunExternalCommands = nextDry;
		DefaultOutputPathRuntime.Apply(settings ?? new AppUserSettings());
	}
}
