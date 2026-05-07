namespace MP4Tools.Services;

public static class UiBehaviorSettingsRuntime
{
	public static bool OpenOutputFolderOnComplete { get; private set; }

	public static bool DeleteTrimSegmentsAfterTrimAndCombine { get; private set; } = true;

	public static void Apply(AppUserSettings settings)
	{
		OpenOutputFolderOnComplete = settings?.OpenOutputFolderOnComplete ?? false;
		DeleteTrimSegmentsAfterTrimAndCombine = settings?.DeleteTrimSegmentsAfterTrimAndCombine ?? true;
	}
}

