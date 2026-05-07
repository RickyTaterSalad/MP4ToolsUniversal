using System;
using System.Diagnostics;
using System.IO;

namespace MP4Tools.Services;

public static class FolderOpener
{
	public static void OpenContainingFolderIfExists(string fileOrFolderPath)
	{
		if (string.IsNullOrWhiteSpace(fileOrFolderPath))
			return;

		string target = fileOrFolderPath.Trim();
		try
		{
			target = Path.GetFullPath(target);
		}
		catch
		{
			// keep original
		}

		if (File.Exists(target))
			target = Path.GetDirectoryName(target) ?? string.Empty;

		if (string.IsNullOrWhiteSpace(target) || !Directory.Exists(target))
			return;

		try
		{
			if (OperatingSystem.IsWindows())
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "explorer.exe",
					Arguments = $"\"{target}\"",
					UseShellExecute = true,
				});
			}
			else if (OperatingSystem.IsMacOS())
			{
				Process.Start("open", target);
			}
			else
			{
				Process.Start("xdg-open", target);
			}
		}
		catch (Exception ex)
		{
			MP4Tools.Logger.Log($"Could not open output folder: {ex.Message}");
		}
	}
}

