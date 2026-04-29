using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MP4Tools.ViewModels;
using MP4ToolsLib;
using System.IO;
using System.Linq;

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
		if (e.DataTransfer.Contains(DataFormat.File))
		{
			e.DragEffects = DragDropEffects.Copy;
		}
		else if (e.DataTransfer.Contains(CombineFileFormat))
		{
			e.DragEffects = DragDropEffects.Move;
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

	private void OnDrop(object sender, DragEventArgs e)
	{
		if (e.DataTransfer.Contains(DataFormat.File))
		{
			var files = e.DataTransfer.GetItems(DataFormat.File);
			if (files != null)
			{
				foreach (var file in files)
				{
					var droppedFile = file.TryGetFile();
					if (!string.IsNullOrWhiteSpace(droppedFile?.Path?.LocalPath) && Directory.Exists(droppedFile?.Path?.LocalPath))
					{
						((MP4ViewModelBase)DataContext!).SetFileCommand.Execute(droppedFile.Path.LocalPath);
						break;
					}
				}
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
}
