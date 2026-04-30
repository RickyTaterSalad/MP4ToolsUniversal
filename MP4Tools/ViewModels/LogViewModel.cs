using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Threading;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace MP4Tools.ViewModels
{
    public class LogViewModel : MP4ViewModelBase
    {
        private ObservableCollection<string> _logEntries;
        private string _logText;
        private readonly int _maxLogEntries;
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private readonly object _batchLock = new object();
        private bool _isBatchProcessing = false;
        private readonly TimeSpan _batchDelay = TimeSpan.FromMilliseconds(50);

        // For tracking repeated ffmpeg lines
        private string _lastFfmpegProgressLine = "";
        private int _ffmpegProgressSkipCount = 0;
        private const int _ffmpegProgressSkipEvery = 20;


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

        private bool ShouldSkipFfmpegProgress(string message)
        {
            // Check if this looks like ffmpeg progress output (frame= NNN fps=...)
            if (message.Contains("frame=") && message.Contains("fps="))
            {
                // Check if it's the same as the last line
                if (message == _lastFfmpegProgressLine)
                {
                    _ffmpegProgressSkipCount++;
                    if (_ffmpegProgressSkipCount >= _ffmpegProgressSkipEvery)
                    {
                        _ffmpegProgressSkipCount = 0;
                        return true; // Skip this line, log the 20th
                    }
                    return true; // Skip this line
                }
                else
                {
                    // Format changed or new line, reset
                    _lastFfmpegProgressLine = message;
                    _ffmpegProgressSkipCount = 0;
                    return false;
                }
            }

            // Not an ffmpeg progress line, don't skip
            _lastFfmpegProgressLine = "";
            _ffmpegProgressSkipCount = 0;
            return false;
        }

        private void OnLogMessageReceived(object sender, string message)
        {
            // Filter ffmpeg progress lines - skip every 20th repeated line
            if (ShouldSkipFfmpegProgress(message))
            {
                return;
            }

            _logQueue.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");

            // Schedule batch processing if not already running
            lock (_batchLock)
            {
                if (!_isBatchProcessing)
                {
                    _isBatchProcessing = true;
                    Dispatcher.UIThread.InvokeAsync(ProcessLogBatch, DispatcherPriority.Background);
                }
            }
        }

        private async System.Threading.Tasks.Task ProcessLogBatch()
        {
            // Wait for more logs to arrive
            await System.Threading.Tasks.Task.Delay(_batchDelay);

            // Collect all queued logs
            var batch = new System.Collections.Generic.List<string>();
            while (_logQueue.TryDequeue(out var entry))
            {
                batch.Add(entry);
            }

            if (batch.Count > 0)
            {
                // Process batch on UI thread
                Dispatcher.UIThread.Invoke(() =>
                {
                    foreach (var entry in batch)
                    {
                        LogEntries.Add(entry);
                    }
                    TrimOldEntries();
                });
            }

            lock (_batchLock)
            {
                _isBatchProcessing = false;

                // Check if more logs arrived during processing
                if (_logQueue.Count > 0)
                {
                    Dispatcher.UIThread.InvokeAsync(ProcessLogBatch, DispatcherPriority.Background);
                }
            }
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
