using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Input;
using System.Windows.Input;
using System.Linq;

namespace MP4Tools.ViewModels
{
    public class LogViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> _logEntries;
        private string _logText;
        private string _filterText;
        private ICommand _clearLogCommand;

        public ObservableCollection<string> LogEntries
        {
            get => _logEntries;
            set
            {
                _logEntries = value;
                OnPropertyChanged();
            }
        }

        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                OnPropertyChanged();
            }
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        private ObservableCollection<string> _filteredLogEntries;
        public ObservableCollection<string> FilteredLogEntries
        {
            get => _filteredLogEntries;
            set
            {
                _filteredLogEntries = value;
                OnPropertyChanged();
            }
        }

        public ICommand ClearLogCommand
        {
            get
            {
                return _clearLogCommand ??= new RelayCommand(ClearLog);
            }
        }

        public LogViewModel()
        {
            LogEntries = new ObservableCollection<string>();
            FilteredLogEntries = new ObservableCollection<string>();
            // Subscribe to the static logging event
            Logger.LogMessageReceived += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(object sender, string message)
        {
            // Ensure UI updates happen on the UI thread
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                ApplyFilter();
            });
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                FilteredLogEntries = LogEntries;
            }
            else
            {
                var filtered = LogEntries.Where(entry => entry.Contains(FilterText, StringComparison.OrdinalIgnoreCase)).ToList();
                FilteredLogEntries = new ObservableCollection<string>(filtered);
            }
        }

        public void ClearLog()
        {
            LogEntries.Clear();
            FilteredLogEntries.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}