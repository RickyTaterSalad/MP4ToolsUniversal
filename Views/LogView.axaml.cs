using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MP4Tools.Views
{
    public partial class LogView : UserControl
    {
        public LogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}