using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MP4Tools.Views;
using MP4ToolsLib;

namespace MP4Tools.Services;

/// <summary>
/// UI helper: when the disk-space check fails, show a Yes/No confirm dialog.
/// Returns true if the user wants to continue (or the check is disabled / unavailable).
/// </summary>
public static class DiskSpaceWarning
{
	public static async Task<bool> ConfirmContinueIfNeededAsync(DiskSpaceEstimator.CheckResult check)
	{
		if (!UiBehaviorSettingsRuntime.WarnOnInsufficientDiskSpace || check.IsSufficient)
			return true;

		return await Dispatcher.UIThread.InvokeAsync(async () =>
		{
			if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
			    {
				    MainWindow: { } window
			    })
			{
				// No UI owner — log and allow continue rather than silently blocking.
				Logger.Log(DiskSpaceEstimator.FormatWarningMessage(check).Replace('\n', ' '));
				return true;
			}

			var dialog = new YesNoConfirmWindow(
				DiskSpaceEstimator.FormatWarningMessage(check),
				"Disk space warning");
			var result = await dialog.ShowDialog<bool?>(window).ConfigureAwait(true);
			return result == true;
		}).ConfigureAwait(false);
	}
}
