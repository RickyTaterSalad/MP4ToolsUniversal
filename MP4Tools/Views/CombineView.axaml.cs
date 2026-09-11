using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MP4Tools.ViewModels;
using MP4ToolsLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP4Tools.Views;

public partial class CombineView : UserControl
{
	private const double DragThreshold = 4;
	private const double AutoScrollEdge = 40;
	private const double AutoScrollMaxStep = 22;

	private Point? _dragStartPoint;
	private CombineFile _dragSourceFile;
	private List<CombineFile> _dragFiles = [];
	private bool _isReordering;
	private bool _canDropReorder;
	private bool _preserveMultiSelection;
	private int _insertIndex;
	private IPointer _capturedPointer;
	private DispatcherTimer _autoScrollTimer;
	private Point _lastListPointerPos;
	private Point _lastOverlayPointerPos;
	private bool _hasLastPointerPos;

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
		else
		{
			e.DragEffects = DragDropEffects.None;
		}
	}

	private void OnInputFilesSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isReordering || _preserveMultiSelection)
			return;
		if (DataContext is not CombineViewModel vm || sender is not ListBox list)
			return;

		vm.SyncSelectedInputFiles(list.SelectedItems.OfType<CombineFile>());
	}

	private void OnListBoxItemPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			return;
		if (sender is not Control control || control.DataContext is not CombineFile data)
			return;
		if (DataContext is not CombineViewModel vm)
			return;

		_dragStartPoint = e.GetPosition(InputFilesHost);
		_dragSourceFile = data;
		_isReordering = false;
		_canDropReorder = false;
		_preserveMultiSelection = false;

		// Snapshot selection before ListBox collapses a multi-select on press.
		var selected = InputFilesList.SelectedItems
			.OfType<CombineFile>()
			.Where(f => vm.InputFiles.Contains(f))
			.OrderBy(f => vm.InputFiles.IndexOf(f))
			.ToList();

		var modifiers = e.KeyModifiers;
		var hasRangeModifier = modifiers.HasFlag(KeyModifiers.Control)
			|| modifiers.HasFlag(KeyModifiers.Meta)
			|| modifiers.HasFlag(KeyModifiers.Shift);

		if (!hasRangeModifier && selected.Count > 1 && selected.Contains(data))
		{
			_dragFiles = selected;
			_preserveMultiSelection = true;
			// Keep the multi-selection intact so a drag moves the whole set.
			e.Handled = true;
		}
		else
		{
			_dragFiles = [data];
		}
	}

	private void OnInputFilesHostPointerMoved(object sender, PointerEventArgs e)
	{
		if (_dragStartPoint == null || _dragSourceFile == null)
			return;
		if (!e.GetCurrentPoint(InputFilesHost).Properties.IsLeftButtonPressed)
			return;
		if (DataContext is not CombineViewModel vm)
			return;

		var position = e.GetPosition(InputFilesHost);
		if (!_isReordering)
		{
			var delta = position - _dragStartPoint.Value;
			if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
				return;

			BeginReorder(vm, e.Pointer);
		}

		UpdateReorderVisuals(vm, e);
	}

	private void OnInputFilesHostPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		if (!_isReordering)
		{
			// Plain click on an item inside a multi-selection: select only that item.
			if (_preserveMultiSelection && _dragSourceFile != null)
			{
				_preserveMultiSelection = false;
				InputFilesList.SelectedItems.Clear();
				InputFilesList.SelectedItems.Add(_dragSourceFile);
				if (DataContext is CombineViewModel clickVm)
					clickVm.SyncSelectedInputFiles([_dragSourceFile]);
			}

			ClearReorderState();
			return;
		}

		if (DataContext is CombineViewModel vm && _canDropReorder && _dragFiles.Count > 0)
		{
			vm.MoveInputFilesToIndex(_dragFiles, _insertIndex);
			RestoreListSelection(_dragFiles);
		}

		EndReorder();
	}

	private void OnInputFilesHostPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
	{
		if (_isReordering)
			EndReorder();
		else
			ClearReorderState();
	}

	private void BeginReorder(CombineViewModel vm, IPointer pointer)
	{
		_isReordering = true;

		if (_preserveMultiSelection && _dragFiles.Count > 1)
		{
			_dragFiles = _dragFiles
				.Where(f => vm.InputFiles.Contains(f))
				.OrderBy(f => vm.InputFiles.IndexOf(f))
				.ToList();
		}
		else
		{
			_dragFiles = GetFilesToDrag(vm, _dragSourceFile);
		}

		if (_dragFiles.Count == 0 && _dragSourceFile != null)
			_dragFiles = [_dragSourceFile];

		_capturedPointer = pointer;
		pointer.Capture(InputFilesHost);
		InputFilesHost.Cursor = new Cursor(StandardCursorType.Arrow);

		DragGhostText.Text = _dragFiles.Count == 1
			? _dragFiles[0].Name
			: $"{_dragFiles.Count} files";
		DragGhost.IsVisible = true;
		DropIndicator.IsVisible = false;
		StartAutoScrollTimer();
	}

	private void UpdateReorderVisuals(CombineViewModel vm, PointerEventArgs e)
	{
		_lastOverlayPointerPos = e.GetPosition(ReorderOverlay);
		_lastListPointerPos = e.GetPosition(InputFilesList);
		_hasLastPointerPos = true;

		Canvas.SetLeft(DragGhost, _lastOverlayPointerPos.X + 12);
		Canvas.SetTop(DragGhost, _lastOverlayPointerPos.Y + 8);

		TryAutoScroll(_lastListPointerPos);

		_canDropReorder = TryGetInsertIndex(vm, _lastListPointerPos, out _insertIndex);
		if (!_canDropReorder)
		{
			DropIndicator.IsVisible = false;
			DragGhost.Opacity = 0.45;
			return;
		}

		DragGhost.Opacity = 0.92;
		UpdateDropIndicator(vm, _insertIndex);
	}

	private void StartAutoScrollTimer()
	{
		if (_autoScrollTimer == null)
		{
			_autoScrollTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(16)
			};
			_autoScrollTimer.Tick += OnAutoScrollTick;
		}

		if (!_autoScrollTimer.IsEnabled)
			_autoScrollTimer.Start();
	}

	private void StopAutoScrollTimer()
	{
		_autoScrollTimer?.Stop();
		_hasLastPointerPos = false;
	}

	private void OnAutoScrollTick(object sender, EventArgs e)
	{
		if (!_isReordering || !_hasLastPointerPos || DataContext is not CombineViewModel vm)
		{
			StopAutoScrollTimer();
			return;
		}

		if (!TryAutoScroll(_lastListPointerPos))
			return;

		Canvas.SetLeft(DragGhost, _lastOverlayPointerPos.X + 12);
		Canvas.SetTop(DragGhost, _lastOverlayPointerPos.Y + 8);

		_canDropReorder = TryGetInsertIndex(vm, _lastListPointerPos, out _insertIndex);
		if (_canDropReorder)
		{
			DragGhost.Opacity = 0.92;
			UpdateDropIndicator(vm, _insertIndex);
		}
		else
		{
			DropIndicator.IsVisible = false;
			DragGhost.Opacity = 0.45;
		}
	}

	private bool TryAutoScroll(Point positionInList)
	{
		var scrollViewer = GetInputFilesScrollViewer();
		if (scrollViewer == null)
			return false;

		var viewportHeight = scrollViewer.Viewport.Height;
		if (viewportHeight <= 0)
			viewportHeight = InputFilesList.Bounds.Height;
		if (viewportHeight <= 0)
			return false;

		var maxOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
		if (maxOffset <= 0)
			return false;

		double step = 0;
		if (positionInList.Y < AutoScrollEdge)
		{
			var intensity = 1 - Math.Clamp(positionInList.Y / AutoScrollEdge, 0, 1);
			step = -AutoScrollMaxStep * intensity;
		}
		else if (positionInList.Y > viewportHeight - AutoScrollEdge)
		{
			var distanceFromBottom = viewportHeight - positionInList.Y;
			var intensity = 1 - Math.Clamp(distanceFromBottom / AutoScrollEdge, 0, 1);
			step = AutoScrollMaxStep * intensity;
		}

		if (Math.Abs(step) < 0.1)
			return false;

		var nextY = Math.Clamp(scrollViewer.Offset.Y + step, 0, maxOffset);
		if (Math.Abs(nextY - scrollViewer.Offset.Y) < 0.1)
			return false;

		scrollViewer.Offset = scrollViewer.Offset.WithY(nextY);
		return true;
	}

	private ScrollViewer GetInputFilesScrollViewer() =>
		InputFilesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

	private bool TryGetInsertIndex(CombineViewModel vm, Point positionInList, out int insertIndex)
	{
		insertIndex = 0;
		var listBounds = new Rect(InputFilesList.Bounds.Size);
		if (listBounds.Width <= 0 || listBounds.Height <= 0)
			return false;

		// Allow a little vertical slack so edge-dragging still targets first/last slots.
		if (positionInList.X < 0 || positionInList.X > listBounds.Width)
			return false;
		if (positionInList.Y < -AutoScrollEdge || positionInList.Y > listBounds.Height + AutoScrollEdge)
			return false;

		if (positionInList.Y <= 0)
		{
			insertIndex = 0;
			return true;
		}

		if (positionInList.Y >= listBounds.Height)
		{
			insertIndex = vm.InputFiles.Count;
			return true;
		}

		var hitPos = new Point(
			Math.Clamp(positionInList.X, 0, listBounds.Width),
			Math.Clamp(positionInList.Y, 0, listBounds.Height));

		ListBoxItem hitItem = null;
		foreach (var visual in InputFilesList.GetVisualsAt(hitPos))
		{
			hitItem = visual as ListBoxItem ?? visual.FindAncestorOfType<ListBoxItem>();
			if (hitItem != null)
				break;
		}

		if (hitItem?.DataContext is CombineFile file)
		{
			var index = vm.InputFiles.IndexOf(file);
			if (index < 0)
				return false;

			var itemOrigin = hitItem.TranslatePoint(new Point(0, 0), InputFilesList);
			if (itemOrigin == null)
				return false;

			var midY = itemOrigin.Value.Y + (hitItem.Bounds.Height / 2);
			insertIndex = hitPos.Y < midY ? index : index + 1;
			return true;
		}

		insertIndex = vm.InputFiles.Count;
		return true;
	}

	private void UpdateDropIndicator(CombineViewModel vm, int insertIndex)
	{
		double y;
		if (vm.InputFiles.Count == 0)
		{
			y = 4;
		}
		else if (insertIndex >= vm.InputFiles.Count)
		{
			var last = FindListBoxItem(vm.InputFiles[^1]);
			if (last == null)
			{
				DropIndicator.IsVisible = false;
				return;
			}

			var origin = last.TranslatePoint(new Point(0, 0), ReorderOverlay);
			if (origin == null)
			{
				DropIndicator.IsVisible = false;
				return;
			}

			y = origin.Value.Y + last.Bounds.Height;
		}
		else
		{
			var item = FindListBoxItem(vm.InputFiles[insertIndex]);
			if (item == null)
			{
				DropIndicator.IsVisible = false;
				return;
			}

			var origin = item.TranslatePoint(new Point(0, 0), ReorderOverlay);
			if (origin == null)
			{
				DropIndicator.IsVisible = false;
				return;
			}

			y = origin.Value.Y;
		}

		var listOrigin = InputFilesList.TranslatePoint(new Point(0, 0), ReorderOverlay) ?? default;
		DropIndicator.Width = Math.Max(0, InputFilesList.Bounds.Width - 8);
		Canvas.SetLeft(DropIndicator, listOrigin.X + 4);
		Canvas.SetTop(DropIndicator, y - 1);
		DropIndicator.IsVisible = true;
	}

	private ListBoxItem FindListBoxItem(CombineFile file)
	{
		foreach (var child in InputFilesList.GetVisualDescendants().OfType<ListBoxItem>())
		{
			if (ReferenceEquals(child.DataContext, file))
				return child;
		}

		return null;
	}

	private void RestoreListSelection(IReadOnlyList<CombineFile> files)
	{
		InputFilesList.SelectedItems.Clear();
		foreach (var file in files)
		{
			if (InputFilesList.Items.Contains(file))
				InputFilesList.SelectedItems.Add(file);
		}
	}

	private void EndReorder()
	{
		StopAutoScrollTimer();
		if (_capturedPointer != null)
		{
			_capturedPointer.Capture(null);
			_capturedPointer = null;
		}

		InputFilesHost.Cursor = Cursor.Default;
		DragGhost.IsVisible = false;
		DropIndicator.IsVisible = false;
		ClearReorderState();
	}

	private void ClearReorderState()
	{
		StopAutoScrollTimer();
		_dragStartPoint = null;
		_dragSourceFile = null;
		_dragFiles = [];
		_isReordering = false;
		_canDropReorder = false;
		_preserveMultiSelection = false;
		_insertIndex = 0;
	}

	private static List<CombineFile> GetFilesToDrag(CombineViewModel vm, CombineFile pressed)
	{
		var selected = vm.SelectedInputFiles
			.Where(f => f != null && vm.InputFiles.Contains(f))
			.OrderBy(f => vm.InputFiles.IndexOf(f))
			.ToList();

		if (selected.Count > 0 && selected.Contains(pressed))
			return selected;

		return [pressed];
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
