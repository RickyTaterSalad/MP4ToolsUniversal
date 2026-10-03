namespace MP4Tools.ViewModels;

public partial class EditViewModel : ViewModelBase
{
	public RewriteIntroViewModel RewriteIntroViewModel { get; }
	public GenerateIntroViewModel GenerateIntroViewModel { get; }
	public ReplaceSegmentViewModel ReplaceSegmentViewModel { get; }

	public EditViewModel(CombineViewModel combineViewModel)
	{
		RewriteIntroViewModel = new RewriteIntroViewModel();
		GenerateIntroViewModel = new GenerateIntroViewModel(combineViewModel);
		ReplaceSegmentViewModel = new ReplaceSegmentViewModel(combineViewModel);
	}
}
