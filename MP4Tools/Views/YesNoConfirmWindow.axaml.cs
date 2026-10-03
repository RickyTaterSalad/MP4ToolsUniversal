using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MP4Tools.Views;

public partial class YesNoConfirmWindow : Window
{
	public YesNoConfirmWindow()
		: this("Confirm?", "Confirm")
	{
	}

	public YesNoConfirmWindow(string message, string title = "Confirm")
	{
		InitializeComponent();
		Title = title ?? "Confirm";
		MessageText.Text = message ?? string.Empty;
	}

	private void YesButton_OnClick(object sender, RoutedEventArgs e) => Close(true);

	private void NoButton_OnClick(object sender, RoutedEventArgs e) => Close(false);
}
