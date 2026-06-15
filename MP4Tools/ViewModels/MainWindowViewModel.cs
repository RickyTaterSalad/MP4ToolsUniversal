using MP4Tools.Services;

namespace MP4Tools.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	public CombineViewModel CombineViewModel { get; }
	public TrimViewModel TrimViewModel { get; }
	public OptionsViewModel OptionsViewModel { get; }
	public LogViewModel LogViewModel { get; } = new();

	public MainWindowViewModel(AppUserSettings settings)
	{
		CombineViewModel = new CombineViewModel();
		TrimViewModel = new TrimViewModel();
		OptionsViewModel = new OptionsViewModel(settings);
	}
}
