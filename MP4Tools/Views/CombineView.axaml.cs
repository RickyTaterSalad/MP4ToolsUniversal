using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MP4Tools.ViewModels;
using MP4ToolsLib;
using System.Collections.Generic;

namespace MP4Tools.Views;

public partial class CombineView : UserControl
{
	private static readonly DataFormat<CombineFile> CombineFileFormat = DataFormat.CreateInProcessFormat<CombineFile>("CombineFile");

    public CombineView()
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
		else if (e.DataTransfer.Contains(CombineFileFormat))
		{
			e.DragEffects = DragDropEffects.Move;
			e.Handled = true;
		}
		else
		{
			e.DragEffects = DragDropEffects.None;
		}
	}

	private async void OnListBoxItemPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (sender is Control control && control.DataContext is CombineFile data)
		{
			var dragData = new DataTransfer();
			var dragItem = new DataTransferItem();
			dragItem.Set(CombineFileFormat, data);
			dragData.Add(dragItem);
			var result = await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Move);
		}
	}

	private void OnListBoxItemDragOver(object sender, DragEventArgs e)
	{
		if (e.DataTransfer.Contains(CombineFileFormat))
		{
			e.DragEffects = DragDropEffects.Move;
		}
	}

	private async void OnListBoxItemDrop(object sender, DragEventArgs e)
	{
		if (sender is Control control && control.DataContext is CombineFile targetData)
		{
			var sourceData = await ((IAsyncDataTransfer)e.DataTransfer).TryGetValueAsync(CombineFileFormat);
			if (sourceData != null && sourceData != targetData)
			{
				var vm = (CombineViewModel)DataContext!;
				var index = vm.InputFiles.IndexOf(targetData);
				vm.InputFiles.Remove(sourceData);
				vm.InputFiles.Insert(index, sourceData);
				vm.SelectedInputFile = sourceData;
			}
		}
	}

	private async void OnDrop(object sender, DragEventArgs e)
	{
		if (!FileDropHelper.HasFilePayload(e.DataTransfer) || DataContext is not CombineViewModel vm)
			return;

		var paths = await FileDropHelper.GetDroppedLocalPathsAsync(e.DataTransfer);
		foreach (var path in paths)
		{
			if (vm.AcceptDroppedFolder(path) || vm.AcceptDroppedVideoFile(path))
			{
				e.Handled = true;
				break;
			}
		}
	}

	private async void BrowseButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var folder = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = "Select Folder",
			AllowMultiple = false
		});

		if (folder.Count > 0)
		{
			((MP4ViewModelBase)DataContext!).SetFileCommand.Execute(folder[0].Path.LocalPath);
		}
	}

	private async void BrowseJsonButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select Recording JSON",
			AllowMultiple = false,
			FileTypeFilter = new List<FilePickerFileType>
			{
				new FilePickerFileType("JSON Files")
				{
					Patterns = new List<string> { "*.json" }
				},
				new FilePickerFileType("All files")
				{
					Patterns = new List<string> { "*.*" }
				}
			}
		});

		if (files.Count > 0 && DataContext is CombineViewModel vm)
		{
			vm.RecordingJsonPath = files[0].Path.LocalPath;
		}
	}

	private async void BrowseBoxScoreButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select Box Score Image",
			AllowMultiple = false,
			FileTypeFilter = new List<FilePickerFileType>
			{
				new FilePickerFileType("Images")
				{
					Patterns = new List<string> { "*.jpg", "*.jpeg", "*.png", "*.webp", "*.gif" }
				},
				new FilePickerFileType("All files")
				{
					Patterns = new List<string> { "*.*" }
				}
			}
		});

		if (files.Count > 0 && DataContext is CombineViewModel vm)
		{
			vm.BoxScoreImagePath = files[0].Path.LocalPath;
		}
	}
}
