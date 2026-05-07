namespace MP4Tools.Services;

public static class UiBehaviorSettingsRuntime
{
	public static bool OpenOutputFolderOnComplete { get; private set; }

	public static void Apply(AppUserSettings settings)
	{
		OpenOutputFolderOnComplete = settings?.OpenOutputFolderOnComplete ?? false;
	}
}

