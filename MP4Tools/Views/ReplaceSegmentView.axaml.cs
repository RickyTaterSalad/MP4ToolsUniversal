using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MP4Tools.ViewModels;
using System.Collections.Generic;
using System.IO;

namespace MP4Tools.Views;

public partial class ReplaceSegmentView : UserControl
{
	public ReplaceSegmentView()
	{
		InitializeComponent();
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
		if (!FileDropHelper.HasFilePayload(e.DataTransfer) || DataContext is not ReplaceSegmentViewModel vm)
			return;

		var paths = await FileDropHelper.GetDroppedLocalPathsAsync(e.DataTransfer);
		foreach (var path in paths)
		{
			if (!FileDropHelper.IsVideoFilePath(path))
				continue;

			if (vm.AcceptDroppedVideoFile(path))
			{
				e.Handled = true;
				break;
			}
		}
	}

	private static List<string> VideoBrowseGlobPatterns()
	{
		string[] bases =
		[
			"mp4", "mkv", "mov", "webm", "m4v", "ts", "mts", "m2ts",
			"avi", "wmv", "flv", "mpg", "mpeg",
		];
		var list = new List<string>(bases.Length * 2);
		foreach (var b in bases)
		{
			list.Add($"*.{b}");
			list.Add($"*.{b.ToUpperInvariant()}");
		}
		return list;
	}

	private async void BrowseButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var file = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select video to edit",
			AllowMultiple = false,
			FileTypeFilter = new List<FilePickerFileType>
			{
				new FilePickerFileType("Common video")
				{
					Patterns = VideoBrowseGlobPatterns(),
				},
				new FilePickerFileType("All files")
				{
					Patterns = ["*.*"],
				},
			},
		});

		if (file.Count > 0)
			((MP4ViewModelBase)DataContext!).SetFileCommand.Execute(file[0].Path.LocalPath);
	}

	private async void ImportButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var file = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Import Replace Segment state",
			AllowMultiple = false,
			FileTypeFilter =
			[
				new FilePickerFileType("JSON Files")
				{
					Patterns = ["*.json"],
				},
			],
		});

		if (file.Count > 0 && DataContext is ReplaceSegmentViewModel vm)
			vm.ImportReplaceFile(file[0].Path.LocalPath);
	}

	private async void ExportButton_OnClick(object sender, RoutedEventArgs e)
	{
		if (DataContext is not ReplaceSegmentViewModel vm)
			return;

		var suggested = "ReplaceSegment";
		if (!string.IsNullOrWhiteSpace(vm.InputPath))
			suggested = Path.GetFileNameWithoutExtension(vm.InputPath) + "_replace_segment";

		var topLevel = TopLevel.GetTopLevel(this);
		var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Export Replace Segment state",
			DefaultExtension = "json",
			SuggestedFileName = suggested,
			FileTypeChoices =
			[
				new FilePickerFileType("JSON Files")
				{
					Patterns = ["*.json"],
				},
			],
		});

		if (file != null)
			vm.ExportState(file.Path.LocalPath);
	}
}
