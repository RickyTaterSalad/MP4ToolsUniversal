namespace MP4Tools.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	public CombineViewModel CombineViewModel { get; } = new();
	public TrimViewModel TrimViewModel { get; } = new();
	public EncodeViewModel EncodeViewModel { get; } = new();
	public OptionsViewModel OptionsViewModel { get; } = new();
	public LogViewModel LogViewModel { get; } = new();
}
