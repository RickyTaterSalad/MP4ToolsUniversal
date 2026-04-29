using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MP4Tools.ViewModels;
using MP4ToolsLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MP4Tools
{

	public partial class MP4ViewModelBase : ViewModelBase
	{
		// ====================
		// PROPERTIES (Top)
		// ====================

		private static int _ffmpegFrameLineCounter = 0;

		public RelayCommand ClearCommand { get; private set; }
		public RelayCommand StopCommand { get; private set; }


		private string _inputPath = string.Empty;
		public string InputPath
		{
			get => _inputPath;
			set
			{
				SetProperty(ref _inputPath, value);
				OnInputPathSet();
			}
		}

		private bool _canStop = false;
		public bool CanStop
		{
			get => _canStop;
			set
			{
				SetProperty(ref _canStop, value);
			}
		}

		private bool _canClear = false;
		public bool CanClear
		{
			get => _canClear;
			set
			{
				SetProperty(ref _canClear, value);
			}
		}

		private string _selectedAudioCodec = "copy";
		public string SelectedAudioCodec
		{
			get => _selectedAudioCodec;
			set
			{
				SetProperty(ref _selectedAudioCodec, value);
			}
		}

		public IReadOnlyList<string> AvailableAudioCodecs {get;} = new[] { "copy", /*"aac",*/ "libopus" };

		// ====================
		// METHODS (Bottom)
		// ====================

		internal MP4ViewModelBase()
		{
			StopCommand = new RelayCommand(() =>
			{
				FFMpegUtils.Instance.KillCurrentRunningProcess();
			});
			ClearCommand = new RelayCommand(() => Clear());
		}

		protected virtual Task Clear()
		{
			InputPath = string.Empty;
			return Task.CompletedTask;
		}

		protected virtual void OnInputPathSet()
		{
		}

		protected void HandleBrowseToDirectory()
		{
			/*
			FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
			{
				SetDirectory(folderBrowserDialog1.SelectedPath);
			}
			*/
		}

		protected void HandleBrowseToFile()
		{
			/*
			// Configure open file dialog box
			var dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.FileName = "MP4"; // Default file name
			dialog.DefaultExt = ".mp4"; // Default file extension
			dialog.Filter = "MP4 Files|*.mp4"; // Filter files by extension

			// Show open file dialog box
			bool? result = dialog.ShowDialog();

			// Process open file dialog box results
			if (result == true)
			{
				// Open document
				string filename = dialog.FileName;
				if (System.IO.File.Exists(filename))
				{
					SetFile(filename);
				}
			}
			*/
		}

		public virtual void SetDirectory(string directory)
		{
			InputPath = directory;
		}

		[RelayCommand]
		public virtual void SetFile(string file)
		{
			InputPath = file ?? string.Empty;
		}

		internal static int GetConfiguredLogMaxLines()
		{
			return 500;//return Settings.Default.LogMaxLines > 0 ? Settings.Default.LogMaxLines : 500;
		}

		internal static bool ShouldReportFfmpegLogLine(string line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				return false;
			}

			if (line.StartsWith("ffmpeg version", StringComparison.OrdinalIgnoreCase)) return false;
			if (line.StartsWith("configuration:", StringComparison.OrdinalIgnoreCase)) return false;
			if (line.Contains("Press [q]", StringComparison.OrdinalIgnoreCase)) return false;

			if (line.Contains("frame=", StringComparison.OrdinalIgnoreCase))
			{
				var count = Interlocked.Increment(ref _ffmpegFrameLineCounter);
				return count % 10 == 0;
			}

			if (line.Contains("time=", StringComparison.OrdinalIgnoreCase)) return false;

			return line.Contains("error", StringComparison.OrdinalIgnoreCase)
				|| line.Contains("failed", StringComparison.OrdinalIgnoreCase)
				|| line.Contains("invalid", StringComparison.OrdinalIgnoreCase)
				|| line.Contains("warning", StringComparison.OrdinalIgnoreCase);
		}

		private void HandleProcessOutput(Process process, string streamName, string data)
		{
			if (string.IsNullOrWhiteSpace(data))
			{
				return;
			}

			Debug.WriteLine($"[ffmpeg][{streamName}] {data}");
			if (ShouldReportFfmpegLogLine(data))
			{
				Logger.Log(data);
			}
		}

		public void RunAndLogFFMpeg(string args, string workingDirectory = "")
		{
			CanStop = true;
			try
			{
				var workDir = !string.IsNullOrWhiteSpace(workingDirectory) ? workingDirectory : System.IO.Path.GetDirectoryName(InputPath ?? string.Empty) ?? string.Empty;
				FFMpegUtils.Instance.RunAndLogFFMpeg(args, HandleProcessOutput, Logger.Log, workDir);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[RunAndLogFFMpeg][Error] {ex}");
				Logger.Log($"** Error: {ex.Message} **");
			}
			finally
			{
				CanStop = false;
			}
		}
	}
}
