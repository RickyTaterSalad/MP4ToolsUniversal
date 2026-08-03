namespace MP4Tools.Services;

public static class BaseballLoggerSettingsRuntime
{
	public static string ServerUrl { get; private set; } = "";

	public static string ApiKey { get; private set; } = "";

	public static void Apply(AppUserSettings settings)
	{
		ServerUrl = settings?.BaseballLoggerServerUrl?.Trim() ?? "";
		ApiKey = settings?.BaseballLoggerApiKey?.Trim() ?? "";
	}
}
