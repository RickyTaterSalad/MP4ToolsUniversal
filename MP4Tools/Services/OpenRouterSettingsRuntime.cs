using MP4ToolsLib;

namespace MP4Tools.Services;

public static class OpenRouterSettingsRuntime
{
	public static string ApiKey { get; private set; } = "";

	public static string Model { get; private set; } = OpenRouterIntroReader.DefaultModel;

	public static void Apply(AppUserSettings settings)
	{
		ApiKey = settings?.OpenRouterApiKey?.Trim() ?? "";
		var model = settings?.OpenRouterModel?.Trim() ?? "";
		Model = string.IsNullOrWhiteSpace(model) ? OpenRouterIntroReader.DefaultModel : model;
	}
}
