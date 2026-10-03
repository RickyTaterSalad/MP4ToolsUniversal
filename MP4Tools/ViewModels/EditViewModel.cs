using CommunityToolkit.Mvvm.ComponentModel;

namespace MP4Tools.ViewModels;

public partial class EditViewModel : ViewModelBase
{
	public RewriteIntroViewModel RewriteIntroViewModel { get; }
	public GenerateIntroViewModel GenerateIntroViewModel { get; }
	public ReplaceSegmentViewModel ReplaceSegmentViewModel { get; }

	[ObservableProperty]
	private int _selectedTabIndex;

	public EditViewModel(CombineViewModel combineViewModel)
	{
		RewriteIntroViewModel = new RewriteIntroViewModel();
		GenerateIntroViewModel = new GenerateIntroViewModel(combineViewModel);
		ReplaceSegmentViewModel = new ReplaceSegmentViewModel(combineViewModel);
	}
}
