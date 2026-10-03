using CommunityToolkit.Mvvm.ComponentModel;

namespace MP4Tools.ViewModels;

public partial class EditViewModel : ViewModelBase
{
	public RewriteIntroViewModel RewriteIntroViewModel { get; }
	public ReplaceSegmentViewModel ReplaceSegmentViewModel { get; }

	[ObservableProperty]
	private int _selectedTabIndex;

	public EditViewModel(CombineViewModel combineViewModel)
	{
		RewriteIntroViewModel = new RewriteIntroViewModel();
		ReplaceSegmentViewModel = new ReplaceSegmentViewModel(combineViewModel);
	}
}
