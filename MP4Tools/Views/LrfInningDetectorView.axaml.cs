using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MP4Tools.ViewModels;
using MP4ToolsLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP4Tools.Views;

public partial class LrfInningDetectorView : UserControl
{
	public LrfInningDetectorView()
	{
		InitializeComponent();
	}

	private async void BrowseVideoButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select Combined Video",
			AllowMultiple = false,
			FileTypeFilter = new List<FilePickerFileType>
			{
				new("Video Files")
				{
					Patterns = new List<string> { "*.mp4", "*.mov", "*.mkv", "*.m4v" },
				},
			},
		});

		if (files.Count > 0 && DataContext is LrfInningDetectorViewModel vm)
			vm.CombinedVideoPath = files[0].Path.LocalPath;
	}

	private async void BrowseFolderButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var folders = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = "Select LRF Folder",
			AllowMultiple = false,
		});

		if (folders.Count > 0 && DataContext is LrfInningDetectorViewModel vm)
			vm.LrfFolderPath = folders[0].Path.LocalPath;
	}

	private async void BrowseFramesFolderButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var folders = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = "Select existing frames / inning-detect session folder",
			AllowMultiple = false,
		});

		if (folders.Count > 0 && DataContext is LrfInningDetectorViewModel vm)
			vm.ExistingFramesFolderPath = folders[0].Path.LocalPath;
	}

	private void OnDragOver(object sender, DragEventArgs e)
	{
		if (FileDropHelper.HasFilePayload(e.DataTransfer))
		{
			e.DragEffects = DragDropEffects.Copy;
			e.Handled = true;
		}
		else
		{
			e.DragEffects = DragDropEffects.None;
		}
	}

	private async void OnDrop(object sender, DragEventArgs e)
	{
		if (DataContext is not LrfInningDetectorViewModel vm)
			return;

		var paths = await FileDropHelper.GetDroppedLocalPathsAsync(e.DataTransfer);
		if (paths.Count == 0)
			return;

		var folder = paths.FirstOrDefault(Directory.Exists);
		var video = paths.FirstOrDefault(FileDropHelper.IsVideoFilePath);
		var map = paths.FirstOrDefault(p =>
			p.EndsWith(CombineEditMap.FileSuffix, System.StringComparison.OrdinalIgnoreCase));

		if (!string.IsNullOrWhiteSpace(video))
			vm.CombinedVideoPath = video;
		else if (!string.IsNullOrWhiteSpace(map))
		{
			// Dropping the map: derive sibling video stem when possible.
			var stem = Path.GetFileName(map);
			if (stem.EndsWith(CombineEditMap.FileSuffix, System.StringComparison.OrdinalIgnoreCase))
			{
				stem = stem[..^CombineEditMap.FileSuffix.Length];
				var dir = Path.GetDirectoryName(map) ?? string.Empty;
				var candidate = Path.Combine(dir, stem + ".mp4");
				if (File.Exists(candidate))
					vm.CombinedVideoPath = candidate;
			}
		}

		if (!string.IsNullOrWhiteSpace(folder))
		{
			// Prefer frames/session packs over LRF folders when the drop has JPEGs.
			if (HalfInningDetector.TryResolveExistingFramesDirectory(folder, out _))
				vm.ExistingFramesFolderPath = folder;
			else
				vm.LrfFolderPath = folder;
		}

		e.Handled = true;
	}
}
