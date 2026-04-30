using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace MP4Tools.ViewModels
{
    public class LogViewModel : MP4ViewModelBase
    {
        private ObservableCollection<string> _logEntries;
        private string _logText;
        private readonly int _maxLogEntries;


        public RelayCommand ClearLogCommand { get; private set; }
        public ObservableCollection<string> LogEntries
        {
            get => _logEntries;
            set => SetProperty(ref _logEntries, value);
        }

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public LogViewModel()
        {
            _maxLogEntries = GetConfiguredLogMaxLines();
            LogEntries = new ObservableCollection<string>();
            ClearLogCommand = new RelayCommand(ClearLog);   
            // Subscribe to the static logging event
            Logger.LogMessageReceived += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(object sender, string message)
        {
            // Ensure UI updates happen on the UI thread
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                TrimOldEntries();
            });
        }

        private void TrimOldEntries()
        {
            if (LogEntries.Count <= _maxLogEntries)
            {
                return;
            }

            var entriesToRemove = LogEntries.Count - _maxLogEntries;
            for (int i = 0; i < entriesToRemove; i++)
            {
                LogEntries.RemoveAt(0);
            }
        }

        public void ClearLog()
        {
            LogEntries.Clear();
        }
    }
}
