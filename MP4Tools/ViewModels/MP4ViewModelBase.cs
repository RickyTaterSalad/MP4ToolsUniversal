using CommunityToolkit.Mvvm.Input;
using MP4Tools.Services;
using MP4Tools.ViewModels;
using MP4ToolsLib;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace MP4Tools
{

	public partial class MP4ViewModelBase : ViewModelBase
	{

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


		private string _videoBitDepth = string.Empty;
		public string VideoBitDepth
		{
			get => _videoBitDepth;
			set => SetProperty(ref _videoBitDepth, value);
		}

		private bool _isInput10Bit;
		public bool IsInput10Bit
		{
			get => _isInput10Bit;
			private set => SetProperty(ref _isInput10Bit, value);
		}

		private string _inputProbeVideoCodecName = string.Empty;
		/// <summary>ffprobe <c>codec_name</c> for the current input video (Trim overlay / intro matching).</summary>
		public string InputProbeVideoCodecName
		{
			get => _inputProbeVideoCodecName;
			set => SetProperty(ref _inputProbeVideoCodecName, value);
		}

		private CancellationTokenSource _ffmpegOperationCts;

		internal MP4ViewModelBase()
		{
			StopCommand = new RelayCommand(CancelFfmpegOperation);
			ClearCommand = new RelayCommand(() => Clear());
			AppSettingsStore.SettingsChanged += OnAppSettingsChanged;
		}

		private void OnAppSettingsChanged(object sender, EventArgs e) => OnSettingsApplied();

		protected virtual void OnSettingsApplied()
		{
		}

		/// <summary>Starts a cancellable ffmpeg/ffprobe operation scope (Stop cancels the token).</summary>
		protected CancellationToken BeginFfmpegOperation()
		{
			try
			{
				_ffmpegOperationCts?.Cancel();
			}
			catch
			{
				// ignored
			}

			_ffmpegOperationCts?.Dispose();
			_ffmpegOperationCts = new CancellationTokenSource();
			CanStop = true;
			return _ffmpegOperationCts.Token;
		}

		protected void EndFfmpegOperation()
		{
			CanStop = false;
			try
			{
				_ffmpegOperationCts?.Dispose();
			}
			finally
			{
				_ffmpegOperationCts = null;
			}
		}

		protected void CancelFfmpegOperation()
		{
			try
			{
				_ffmpegOperationCts?.Cancel();
			}
			catch
			{
				// ignored
			}
		}

		protected virtual Task Clear()
		{
			InputPath = string.Empty;
			return Task.CompletedTask;
		}

		protected virtual void OnInputPathSet()
		{
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

		private void HandleProcessOutput(Process process, string data)
		{
			if (string.IsNullOrWhiteSpace(data))
			{
				return;
			}

			Debug.WriteLine($"[ffmpeg] {data}");
			Logger.Log(data);
		}


		protected async Task ReadInputVideoBitDepthAsync()
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(InputPath) && File.Exists(InputPath))
				{
					var (video, _) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(InputPath, CancellationToken.None, Logger.Log);
					InputProbeVideoCodecName = video?.CodecName?.Trim() ?? "";
					IsInput10Bit = VideoBitDepthHelper.IsTenBitVideo(video);
					VideoBitDepth = VideoBitDepthHelper.FormatDisplay(video);
				}
				else
				{
					InputProbeVideoCodecName = "";
					IsInput10Bit = false;
					VideoBitDepth = string.Empty;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ReadInputVideoBitDepth][Error] {ex}");
				Logger.Log($"Read Input Video Bit Depth Error: {ex.Message}");
			}
		}

		public async Task RunAndLogFFMpegAsync(string args, CancellationToken cancellationToken, string workingDirectory = "")
		{
			var workDir = !string.IsNullOrWhiteSpace(workingDirectory)
				? workingDirectory
				: System.IO.Path.GetDirectoryName(InputPath ?? string.Empty) ?? string.Empty;
			try
			{
				await FFMpegUtils.Instance
					.RunAndLogFFMpegAsync(args, HandleProcessOutput, Logger.Log, cancellationToken, workDir)
					.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[RunAndLogFFMpegAsync][Error] {ex}");
				Logger.Log($"** Error: {ex.Message} **");
			}
		}
	}
}
