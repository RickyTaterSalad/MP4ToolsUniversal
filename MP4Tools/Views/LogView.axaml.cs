using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using MP4Tools.ViewModels;

namespace MP4Tools.Views
{
    public partial class LogView : UserControl
    {
        private LogViewModel _boundVm;

        public LogView()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (_boundVm != null)
                _boundVm.LogContentUpdated -= OnLogContentUpdated;

            _boundVm = DataContext as LogViewModel;
            if (_boundVm != null)
                _boundVm.LogContentUpdated += OnLogContentUpdated;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (_boundVm != null)
            {
                _boundVm.LogContentUpdated -= OnLogContentUpdated;
                _boundVm = null;
            }

            base.OnDetachedFromVisualTree(e);
        }

        private void OnLogContentUpdated()
        {
            if (_boundVm == null || !_boundVm.AutoScrollToBottom)
                return;

            Dispatcher.UIThread.Post(ScrollLogToEnd, DispatcherPriority.Normal);
        }

        private void ScrollLogToEnd()
        {
            if (_boundVm == null || !_boundVm.AutoScrollToBottom || LogTextBox == null)
                return;

            var len = LogTextBox.Text?.Length ?? 0;
            LogTextBox.CaretIndex = len;
        }
    }
}