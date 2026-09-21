using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MP4Tools.Views;

public partial class EventInfoDefaultsWindow : Window
{
	public EventInfoDefaultsWindow()
		: this(null)
	{
	}

	public EventInfoDefaultsWindow(IEnumerable<string> defaults)
	{
		InitializeComponent();
		DefaultsList.ItemsSource = defaults ?? [];
	}

	private void AcceptSelection()
	{
		if (DefaultsList.SelectedItem is not string value || string.IsNullOrWhiteSpace(value))
			return;

		Close(value);
	}

	private void CancelButton_OnClick(object sender, RoutedEventArgs e)
	{
		Close(null);
	}

	private void SelectButton_OnClick(object sender, RoutedEventArgs e)
	{
		AcceptSelection();
	}

	private void DefaultsList_OnDoubleTapped(object sender, TappedEventArgs e)
	{
		AcceptSelection();
	}

	private void DefaultsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SelectButton.IsEnabled = DefaultsList.SelectedItem is string value && !string.IsNullOrWhiteSpace(value);
	}

	private void DefaultsList_OnKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key is Key.Enter or Key.Return)
		{
			AcceptSelection();
			e.Handled = true;
		}
		else if (e.Key == Key.Escape)
		{
			Close(null);
			e.Handled = true;
		}
	}
}
