using MP4ToolsLib;

namespace MP4Tools.Services;

public static class EncodingSettingsRuntime
{
	public static EncodingSettingsDto Current { get; private set; } = new();

	public static void Apply(AppUserSettings settings)
	{
		Current = new EncodingSettingsDto();
		var resolved = settings ?? new AppUserSettings();
		DefaultOutputPathRuntime.Apply(resolved);
		UiBehaviorSettingsRuntime.Apply(resolved);
	}
}
