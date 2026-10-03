using CommunityToolkit.Mvvm.ComponentModel;
using MP4Tools.Services;

namespace MP4Tools.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	public CombineViewModel CombineViewModel { get; }
	public TrimViewModel TrimViewModel { get; }
	public OptionsViewModel OptionsViewModel { get; }
	public EditViewModel EditViewModel { get; }
	public AiViewModel AiViewModel { get; }
	public LogViewModel LogViewModel { get; }

	[ObservableProperty]
	private int _selectedTabIndex;

	public MainWindowViewModel(AppUserSettings settings)
	{
		CombineViewModel = new CombineViewModel();
		TrimViewModel = new TrimViewModel();
		OptionsViewModel = new OptionsViewModel(settings);
		EditViewModel = new EditViewModel(CombineViewModel);
		AiViewModel = new AiViewModel();
		LogViewModel = new LogViewModel(NavigateToLogSource);
	}

	private void NavigateToLogSource()
	{
		switch (LogOperationTracker.Current)
		{
			case LogOperationSource.Combine:
				SelectedTabIndex = 0;
				break;
			case LogOperationSource.Trim:
				SelectedTabIndex = 1;
				break;
			case LogOperationSource.RewriteIntro:
				SelectedTabIndex = 2;
				EditViewModel.SelectedTabIndex = 0;
				break;
			case LogOperationSource.GenerateIntro:
			case LogOperationSource.ReplaceSegment:
				SelectedTabIndex = 2;
				EditViewModel.SelectedTabIndex = 1;
				break;
			case LogOperationSource.InningDetector:
				SelectedTabIndex = 3;
				AiViewModel.SelectedTabIndex = 0;
				break;
			case LogOperationSource.LrfInningDetector:
				SelectedTabIndex = 3;
				AiViewModel.SelectedTabIndex = 1;
				break;
		}
	}
}
