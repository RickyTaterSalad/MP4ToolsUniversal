using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MP4Tools.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP4Tools.Views;

public partial class InningDetectorView : UserControl
{
	public InningDetectorView()
	{
		InitializeComponent();
	}

	private async void BrowseButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select Video",
			AllowMultiple = false,
			FileTypeFilter = new List<FilePickerFileType>
			{
				new("Video Files")
				{
					Patterns = new List<string>
					{
						"*.mp4", "*.mkv", "*.mov", "*.webm", "*.m4v", "*.ts", "*.mts", "*.m2ts",
						"*.avi", "*.wmv", "*.mpg", "*.mpeg",
					},
				},
			},
		});

		if (files.Count > 0 && DataContext is InningDetectorViewModel vm)
			vm.SetFile(files[0].Path.LocalPath);
	}

	private async void BrowseFramesFolderButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var folders = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = "Select existing frames / inning-detect session folder",
			AllowMultiple = false,
		});

		if (folders.Count > 0 && DataContext is InningDetectorViewModel vm)
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
		if (DataContext is not InningDetectorViewModel vm)
			return;

		var paths = await FileDropHelper.GetDroppedLocalPathsAsync(e.DataTransfer);
		if (paths.Count == 0)
			return;

		var video = paths.FirstOrDefault(FileDropHelper.IsVideoFilePath);
		var folder = paths.FirstOrDefault(Directory.Exists);

		if (!string.IsNullOrWhiteSpace(video))
			vm.SetFile(video);

		if (!string.IsNullOrWhiteSpace(folder))
			vm.ExistingFramesFolderPath = folder;

		e.Handled = true;
	}
}
