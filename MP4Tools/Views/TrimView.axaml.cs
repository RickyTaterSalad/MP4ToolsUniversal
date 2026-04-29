using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MP4Tools;
using MP4Tools.ViewModels;
using MP4ToolsLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MP4Tools.Views;

public partial class TrimView : UserControl
{
	private static readonly DataFormat<StartStopRange> StartStopRangeFormat = DataFormat.CreateInProcessFormat<StartStopRange>("StartStopRange");

	public TrimView()
	{
		InitializeComponent();
	}
	private async void ImportButton_OnClick(object sender, RoutedEventArgs e)
	{
		var topLevel = TopLevel.GetTopLevel(this);
		var file = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Import Trim",
			AllowMultiple = false,
			FileTypeFilter
			= new List<FilePickerFileType>
			{
				new FilePickerFileType("JSON Files")
				{
					Patterns = new List<string> { "*.json" }
				}
		}
		});

		if (file.Count > 0)
		{
			((TrimViewModel)DataContext!).ImportTrimFile(file[0].Path.LocalPath);
		}

	}
	private async void ExportButton_OnClick(object sender, RoutedEventArgs e)

	{

		var topLevel = TopLevel.GetTopLevel(this);
		var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Select File",
			DefaultExtension = "json",
			SuggestedFileName = "TrimExport",
			FileTypeChoices = new List<FilePickerFileType>
			{
				new FilePickerFileType("JSON Files")
				{
					Patterns = new List<string> { "*.json" }
				}
			}
		});
		if (file != null)
		{
			((TrimViewModel)DataContext!).ExportState(file.Path.LocalPath);
		}
	}

	private void OnDragOver(object sender, DragEventArgs e)
	{
		if (e.DataTransfer.Contains(DataFormat.File))
		{
			e.DragEffects = DragDropEffects.Copy;
		}
		else if (e.DataTransfer.Contains(StartStopRangeFormat))
		{
			e.DragEffects = DragDropEffects.Move;
		}
	}

	private async void OnListBoxItemPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (sender is Control control && control.DataContext is StartStopRange data)
		{
			var dragData = new DataTransfer();
			var dragItem = new DataTransferItem();
			dragItem.Set(StartStopRangeFormat, data);
			dragData.Add(dragItem);
			var result = await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Move);
		}
	}

	private void OnListBoxItemDragOver(object sender, DragEventArgs e)
	{
		if (e.DataTransfer.Contains(StartStopRangeFormat))
		{
			e.DragEffects = DragDropEffects.Move;
		}
	}

	private async void OnListBoxItemDrop(object sender, DragEventArgs e)
	{
		if (sender is Control control && control.DataContext is StartStopRange targetData)
		{
			var sourceData = await ((IAsyncDataTransfer)e.DataTransfer).TryGetValueAsync(StartStopRangeFormat);
			if (sourceData != null && sourceData != targetData)
			{
				var vm = (TrimViewModel)DataContext!;
				var index = vm.TrimRanges.IndexOf(targetData);
				vm.TrimRanges.Remove(sourceData);
				vm.TrimRanges.Insert(index, sourceData);
				vm.SelectedStartStopRange = sourceData;
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
					if (!string.IsNullOrWhiteSpace(droppedFile?.Path?.LocalPath) && File.Exists(droppedFile?.Path?.LocalPath))
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
		var file = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Select File To Trim",
			AllowMultiple = false,
			FileTypeFilter
			= new List<FilePickerFileType>
			{
				new FilePickerFileType("Video Files")
				{
					Patterns = new List<string> { "*.mp4" }
				}
		}
		});

		if (file.Count > 0)
		{
			((MP4ViewModelBase)DataContext!).SetFileCommand.Execute(file[0].Path.LocalPath);
		}
	}
}
