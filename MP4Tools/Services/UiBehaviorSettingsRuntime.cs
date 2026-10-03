using System;

namespace MP4Tools.Services;

public static class UiBehaviorSettingsRuntime
{
	public static bool OpenOutputFolderOnComplete { get; private set; }

	public static bool DeleteTrimSegmentsAfterTrimAndCombine { get; private set; } = true;

	public static bool WarnOnInsufficientDiskSpace { get; private set; } = true;

	/// <summary>LRF inning-detector sample interval (seconds). Default 3.</summary>
	public static double LrfInningSampleIntervalSeconds { get; private set; } = 3;

	/// <summary>Keep named inning-detect debug packages (default on).</summary>
	public static bool SaveInningDetectionArtifacts { get; private set; } = true;

	public static void Apply(AppUserSettings settings)
	{
		OpenOutputFolderOnComplete = settings?.OpenOutputFolderOnComplete ?? false;
		DeleteTrimSegmentsAfterTrimAndCombine = settings?.DeleteTrimSegmentsAfterTrimAndCombine ?? true;
		WarnOnInsufficientDiskSpace = settings?.WarnOnInsufficientDiskSpace ?? true;
		var interval = settings?.LrfInningSampleIntervalSeconds ?? 3;
		if (double.IsNaN(interval) || double.IsInfinity(interval))
			interval = 3;
		LrfInningSampleIntervalSeconds = Math.Clamp(interval, 0.5, 30);
		SaveInningDetectionArtifacts = settings?.SaveInningDetectionArtifacts ?? true;
	}
}

