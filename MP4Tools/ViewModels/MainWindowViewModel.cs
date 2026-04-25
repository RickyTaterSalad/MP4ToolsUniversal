namespace mp4tools_universal.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public CombineViewModel CombineViewModel { get; } = new();
    public TrimViewModel TrimViewModel { get; } = new();
}
