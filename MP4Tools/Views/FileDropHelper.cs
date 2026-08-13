using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MP4Tools.Views;

internal static class FileDropHelper
{
	private static readonly HashSet<string> VideoExtensions = new(System.StringComparer.OrdinalIgnoreCase)
	{
		".mp4", ".mkv", ".mov", ".webm", ".m4v", ".ts", ".mts", ".m2ts",
		".avi", ".wmv", ".flv", ".mpg", ".mpeg",
	};

	public static bool HasFilePayload(IDataTransfer dataTransfer) =>
		dataTransfer != null && dataTransfer.Contains(DataFormat.File);

	public static async Task<IReadOnlyList<string>> GetDroppedLocalPathsAsync(IDataTransfer dataTransfer)
	{
		var paths = new List<string>();
		if (dataTransfer == null)
			return paths;

		IStorageItem[] items = null;
		if (dataTransfer is IAsyncDataTransfer asyncTransfer)
			items = await asyncTransfer.TryGetFilesAsync().ConfigureAwait(true);

		items ??= dataTransfer.TryGetFiles();
		if (items == null || items.Length == 0)
			return paths;

		foreach (var item in items)
		{
			if (item == null)
				continue;

			var local = item.TryGetLocalPath();
			if (string.IsNullOrWhiteSpace(local))
				local = item.Path?.LocalPath;

			if (!string.IsNullOrWhiteSpace(local))
				paths.Add(local);
		}

		return paths;
	}

	public static bool IsVideoFilePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return false;

		var ext = System.IO.Path.GetExtension(path);
		return !string.IsNullOrEmpty(ext) && VideoExtensions.Contains(ext);
	}
}
