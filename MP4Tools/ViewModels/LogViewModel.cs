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
            });
        }
        public void ClearLog()
        {
            LogEntries.Clear();
        }
    }
}