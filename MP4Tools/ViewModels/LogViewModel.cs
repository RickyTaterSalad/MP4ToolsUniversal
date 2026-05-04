using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace MP4Tools.ViewModels
{
    public class LogViewModel : MP4ViewModelBase
    {
        private string _logText = string.Empty;
        private readonly StringBuilder _logBuffer = new();
        private readonly int _maxLogLines;
        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly object _batchLock = new();
        private bool _isBatchProcessing;

        private readonly TimeSpan _batchDelay = TimeSpan.FromMilliseconds(50);

        private string _lastFfmpegProgressLine = "";
        private int _ffmpegProgressSkipCount;
        private const int _ffmpegProgressSkipEvery = 20;

        private bool _autoScrollToBottom = true;

        public RelayCommand ClearLogCommand { get; }

        /// <summary>Fires on the UI thread after <see cref="LogText"/> was replaced (append batch or clear).</summary>
        public event Action LogContentUpdated;

        public bool AutoScrollToBottom
        {
            get => _autoScrollToBottom;
            set => SetProperty(ref _autoScrollToBottom, value);
        }

        public string LogText
        {
            get => _logText;
            private set => SetProperty(ref _logText, value);
        }

        public LogViewModel()
        {
            _maxLogLines = GetConfiguredLogMaxLines();
            ClearLogCommand = new RelayCommand(ClearLog);
            Logger.LogMessageReceived += OnLogMessageReceived;
        }

        private bool ShouldSkipFfmpegProgress(string message)
        {
            if (message.Contains("frame=") && message.Contains("fps="))
            {
                if (message == _lastFfmpegProgressLine)
                {
                    _ffmpegProgressSkipCount++;
                    if (_ffmpegProgressSkipCount >= _ffmpegProgressSkipEvery)
                    {
                        _ffmpegProgressSkipCount = 0;
                        return true;
                    }

                    return true;
                }

                _lastFfmpegProgressLine = message;
                _ffmpegProgressSkipCount = 0;
                return false;
            }

            _lastFfmpegProgressLine = "";
            _ffmpegProgressSkipCount = 0;
            return false;
        }

        private void OnLogMessageReceived(object sender, string message)
        {
            if (ShouldSkipFfmpegProgress(message))
                return;

            _logQueue.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");

            lock (_batchLock)
            {
                if (!_isBatchProcessing)
                {
                    _isBatchProcessing = true;
                    _ = ProcessLogBatchAsync();
                }
            }
        }

        private async Task ProcessLogBatchAsync()
        {
            await Task.Delay(_batchDelay).ConfigureAwait(false);

            var batch = new List<string>();
            while (_logQueue.TryDequeue(out var entry))
                batch.Add(entry);

            try
            {
                if (batch.Count > 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        foreach (var entry in batch)
                        {
                            if (_logBuffer.Length > 0)
                                _logBuffer.AppendLine();
                            _logBuffer.Append(entry);
                        }

                        TrimExcessLines();
                        LogText = _logBuffer.Length == 0 ? string.Empty : _logBuffer.ToString();
                        LogContentUpdated?.Invoke();
                    }, DispatcherPriority.Background);
                }
            }
            finally
            {
                lock (_batchLock)
                {
                    _isBatchProcessing = false;
                    if (_logQueue.Count > 0)
                        _ = ProcessLogBatchAsync();
                }
            }
        }

        private static int CountLines(StringBuilder sb)
        {
            if (sb.Length == 0)
                return 0;
            var n = 1;
            for (var i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '\n')
                    n++;
            }

            return n;
        }

        private void TrimExcessLines()
        {
            var lines = CountLines(_logBuffer);
            var excess = lines - _maxLogLines;
            if (excess <= 0)
                return;

            var removed = 0;
            var removeThrough = 0;
            for (var i = 0; i < _logBuffer.Length && removed < excess; i++)
            {
                if (_logBuffer[i] != '\n')
                    continue;
                removed++;
                removeThrough = i + 1;
            }

            if (removeThrough > 0)
                _logBuffer.Remove(0, removeThrough);
        }

        public void ClearLog()
        {
            _logBuffer.Clear();
            LogText = string.Empty;
            LogContentUpdated?.Invoke();
        }
    }
}
