using CommunityToolkit.Mvvm.ComponentModel;

namespace MP4Tools.ViewModels;

public partial class AiViewModel : ViewModelBase
{
	public InningDetectorViewModel InningDetectorViewModel { get; }
	public LrfInningDetectorViewModel LrfInningDetectorViewModel { get; }

	[ObservableProperty]
	private int _selectedTabIndex;

	public AiViewModel()
	{
		InningDetectorViewModel = new InningDetectorViewModel();
		LrfInningDetectorViewModel = new LrfInningDetectorViewModel();
	}
}
