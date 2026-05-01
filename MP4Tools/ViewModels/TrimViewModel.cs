using Avalonia.Controls.Embedding.Offscreen;
using CommunityToolkit.Mvvm.Input;
using MP4ToolsLib;
using MP4ToolsLib.Preset;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MP4Tools.ViewModels;

internal class ExportTrimState
{
	public List<StartStopRange> StartStopRanges { get; set; } = new List<StartStopRange>();
	public string InputFile { get; set; }
	public string OutputFolderName { get; set; }
	public string IntroTitle { get; set; }
	public string IntroSubtitle { get; set; }
	public string IntroDetails { get; set; }
	public int? IntroDurationSeconds { get; set; }
}

public partial class TrimViewModel : MP4ViewModelBase
{
	public static IReadOnlyList<int> Range { get; } = TimeRange.Range;


	private TimeSpan _inputDuration = TimeSpan.Zero;

	public ObservableCollection<StartStopRange> TrimRanges { get; set; }

	private StartStopRange _selectedStartStopRange;
	public StartStopRange SelectedStartStopRange
	{
		get => _selectedStartStopRange;
		set
		{
			SetProperty(ref _selectedStartStopRange, value);
			OnPropertyChanged(nameof(HasSelectedRange));
		}
	}
	public bool IsHoursRangeEnabled => VideoHourRange.Count > 1;

	private List<int> _videoHourRange;

	public IReadOnlyList<int> VideoHourRange
	{
		get
		{
			if (_videoHourRange == null)
			{
				_videoHourRange = new List<int>(TimeRange.Range);
			}
			return _videoHourRange;
		}
		set
		{
			SetProperty(ref _videoHourRange, (List<int>)value);
			OnPropertyChanged(nameof(IsHoursRangeEnabled));
		}
	}

	private List<int> _videMinuteRange;

	public IReadOnlyList<int> VideoMinuteRange
	{
		get
		{
			if (_videMinuteRange == null)
			{
				_videMinuteRange = new List<int>(TimeRange.Range);
			}
			return _videMinuteRange;
		}
		set
		{
			SetProperty(ref _videMinuteRange, (List<int>)value);
		}
	}


	private string _introTitle = string.Empty;
	public string IntroTitle
	{
		get => _introTitle;
		set => SetProperty(ref _introTitle, value ?? string.Empty);
	}

	private string _introSubtitle = string.Empty;
	public string IntroSubtitle
	{
		get => _introSubtitle;
		set => SetProperty(ref _introSubtitle, value ?? string.Empty);
	}

	private string _introDetails = string.Empty;
	public string IntroDetails
	{
		get => _introDetails;
		set => SetProperty(ref _introDetails, value ?? string.Empty);
	}

	private int _introDurationSeconds = 10;
	public int IntroDurationSeconds
	{
		get => _introDurationSeconds;
		set => SetProperty(ref _introDurationSeconds, Math.Max(1, value));
	}

	private bool _canTrim = false;
	public bool CanTrim
	{
		get => _canTrim;
		set
		{
			SetProperty(ref _canTrim, value);
			if (!_canTrim && CanClear)
			{
				CanClear = false;
			}
			if (!CanClear && _canTrim && File.Exists(InputPath))
			{
				CanClear = true;
			}
		}
	}

	private string _rangeLabel;
	public string RangeLabel
	{
		get => _rangeLabel;
		set => SetProperty(ref _rangeLabel, value);
	}

	private string _outputFolderName = string.Empty;
	public string OutputFolderName
	{
		get
		{
			if (string.IsNullOrWhiteSpace(_outputFolderName))
			{
				_outputFolderName = $"trim_combine_{DateTime.Now:yyyyMMddTHHmmss}";
			}
			return _outputFolderName;
		}
		set
		{
			SetProperty(ref _outputFolderName, value);
		}
	}

	public bool HasSelectedRange => _selectedStartStopRange != null;

	public TimeRange StartRange { get; } = new TimeRange();
	public TimeRange EndRange { get; } = new TimeRange();

	public AsyncRelayCommand TrimCommand { get; private set; }
	public AsyncRelayCommand TrimAndCombineCommand { get; private set; }

	public RelayCommand AddTimeRangeCommand { get; private set; }

	public RelayCommand RemoveTimeRangeCommand { get; private set; }

	public RelayCommand ClearTimeRangesCommand { get; private set; }

	public RelayCommand ResetTimeRangeCommand { get; private set; }


	public TrimViewModel()
	{
		SelectedAudioCodec = "libopus";
		_selectedStartStopRange = null;
		TrimRanges = new ObservableCollection<StartStopRange>();

		_videoHourRange = new List<int>(TimeRange.Range);
		_videMinuteRange = new List<int>(TimeRange.Range);
		_rangeLabel = string.Empty;

		ResetTimeRangeCommand = new RelayCommand(() =>
		{
			StartRange.Hours = 0;
			StartRange.Minutes = 0;
			StartRange.Seconds = 0;
			EndRange.Hours = 0;
			EndRange.Minutes = 0;
			EndRange.Seconds = 0;
		});

		TrimAndCombineCommand = new AsyncRelayCommand(async () => await TrimAndCombineAsync());
		TrimCommand = new AsyncRelayCommand(async () => await TrimAndCombineAsync(false));
		AddTimeRangeCommand = new RelayCommand(() =>
		{
			var range = new StartStopRange(StartRange.Clone(), EndRange.Clone(), RangeLabel ?? string.Empty)
			{
				SelectedDrawTextPosition = DrawTextPosition.TopLeft
				//SelectedDrawTextPosition = DrawTextUtils.DrawTextPositionOptions.FirstOrDefault(x => x.DisplayName == SelectedDrawTextPosition)?.Value ?? DrawTextPosition.TopLeft
			};
			if (range.IsValidRange() && IsRangeWithinInputBounds(range))
			{
				TrimRanges.Add(range);
				RangeLabel = string.Empty;
			}
		});

		RemoveTimeRangeCommand = new RelayCommand(() =>
		{
			if (SelectedStartStopRange != null)
			{
				try
				{
					TrimRanges.Remove(SelectedStartStopRange);
				}
				catch
				{
					//
				}
				SelectedStartStopRange = null;
			}
		});
		ClearTimeRangesCommand = new RelayCommand(() =>
		{
			ClearTimeRanges();
		});
	}

	protected override async void OnInputPathSet()
	{
		_ = Task.Run(async () =>
		{
			await ReadInputVideoBitDepthAsync();
			await Task.Run(ReadFileInfoAsync);
		});
	}

	private async Task TrimAndCombineAsync(bool combine = true)
	{
		var outFolder = Path.GetDirectoryName(InputPath) ?? string.Empty;
		var outTrimmedFolder = Path.Combine(outFolder, OutputFolderName);
		var logOutputPath = Path.Combine(outTrimmedFolder, "log.txt");
		if (!Directory.Exists(outTrimmedFolder))
		{
			Directory.CreateDirectory(outTrimmedFolder);
		}
		var logged_ffmpeg_output = new List<string>();
		EventHandler<string> handler = (s, msg) =>
		{
			if (!string.IsNullOrWhiteSpace(msg))
				logged_ffmpeg_output.Add(msg);
		};
		Logger.LogMessageReceived += handler;
		var ct = BeginFfmpegOperation();
		try
		{
			await Task.Run(async () => await TrimAndCombineAsyncInternal(outTrimmedFolder, combine, ct), ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			Logger.Log("Trim cancelled.");
		}
		catch (Exception ex)
		{
			Logger.Log($"Error occurred while combining files: {ex.Message}");
		}
		finally
		{
			try
			{
				if (logged_ffmpeg_output.Count > 0 && !string.IsNullOrWhiteSpace(logOutputPath) && Path.GetDirectoryName(logOutputPath) != null && Directory.Exists(Path.GetDirectoryName(logOutputPath)))
				{
					try
					{
						File.WriteAllLines(logOutputPath, logged_ffmpeg_output);
					}
					catch (Exception ex)
					{
						Logger.Log($"Failed to write log file: {ex.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log($"Unexpected error during log file handling: {ex.Message}");
			}

			EndFfmpegOperation();
		}
		Logger.LogMessageReceived -= handler;
	}

	private async Task TrimAndCombineAsyncInternal(string outTrimmedFolder, bool combine, CancellationToken ct)
	{
		if (!CanTrim || !TrimRanges.Any() || InputPath == null)
		{
			return;
		}
		var validRanges = TrimRanges.Where(IsRangeWithinInputBounds).ToList();
		if (!validRanges.Any())
		{
			return;
		}

		Logger.Log("Starting trim and combine...");
		CanTrim = false;
		var tempFilesToDelete = new List<string>();
		try
		{
			List<string> outputPaths = new List<string>();
			foreach (var trim in validRanges)
			{
				ct.ThrowIfCancellationRequested();
				Logger.Log($"Trimming range: {trim.StartRange} - {trim.EndRange}");
				outputPaths.Add(await TrimRangeAsync(trim, outTrimmedFolder, ct).ConfigureAwait(false));
			}

			if (!combine)
			{
				ClearTimeRanges();
				Logger.Log("Trim complete.");
				return;
			}

			var shouldAddIntro = !string.IsNullOrWhiteSpace(IntroTitle) || !string.IsNullOrWhiteSpace(IntroSubtitle) || !string.IsNullOrWhiteSpace(IntroDetails);
			if (shouldAddIntro && outputPaths.Count > 0)
			{
				Logger.Log("Adding intro to first trimmed segment...");
				var effectiveTitle = IntroTitle?.Trim() ?? string.Empty;
				var effectiveSubtitle = IntroSubtitle?.Trim() ?? string.Empty;
				var effectiveDetails = IntroDetails?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(effectiveTitle) && !string.IsNullOrWhiteSpace(effectiveSubtitle))
				{
					effectiveTitle = effectiveSubtitle;
					effectiveSubtitle = string.Empty;
				}
				if (string.IsNullOrWhiteSpace(effectiveTitle) && !string.IsNullOrWhiteSpace(effectiveDetails) && string.IsNullOrWhiteSpace(effectiveSubtitle))
				{
					effectiveTitle = effectiveDetails;
					effectiveDetails = string.Empty;
				}

				var introFirstFile = Path.Combine(TempPathHelper.GetTempPath(), $"first_trim_intro_{Guid.NewGuid():N}.mp4");
				await IntroVideoComposerAsync.PrependIntroAsync(
					outputPaths[0],
					effectiveTitle,
					effectiveSubtitle,
					introFirstFile,
					IntroDurationSeconds,
					detailsText: effectiveDetails,
					log: Logger.Log,
					ct: ct).ConfigureAwait(false);
				if (File.Exists(introFirstFile))
				{
					outputPaths[0] = introFirstFile;
					tempFilesToDelete.Add(introFirstFile);
				}
			}

			var finalCombinedPath = FFMpegUtils.Instance.CleanupPath(System.IO.Path.Combine(outTrimmedFolder, "trim_combined.mp4"));
			WriteTrimExport(finalCombinedPath);
			if (outputPaths.Count == 1)
			{
				Logger.Log("Only one trimmed output. Copying to final output...");
				try
				{
					if (File.Exists(outputPaths[0]))
					{
						File.Copy(outputPaths[0], finalCombinedPath, overwrite: true);
						Logger.Log("Final Output:");
						Logger.Log(finalCombinedPath);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[TrimAndCombineAsync][SingleOutputCopyError] {ex}");
					Logger.Log($"Copy Error: {ex.Message}");
				}
			}
			else if (outputPaths.Count > 1)
			{
				Logger.Log("Combining trimmed files...");

				var tempTsFiles = new List<string>();
				var videoCodec = await FFMpegUtils.Instance.GetFirstVideoCodecNameAsync(outputPaths.FirstOrDefault() ?? string.Empty, ct).ConfigureAwait(false);
				var videoBsf = string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase) ? "hevc_mp4toannexb" : "h264_mp4toannexb";

				foreach (var input in outputPaths)
				{
					ct.ThrowIfCancellationRequested();
					Logger.Log($"Remuxing trimmed segment to TS: {Path.GetFileName(input)}");
					var tsFile = Path.Combine(TempPathHelper.GetTempPath(), $"trim_part_{Guid.NewGuid():N}.ts");
					await RunAndLogFFMpegAsync(FfmpegCommandLine.Build(
						FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
						FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(input)),
						FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
						FfmpegOption.Pair(FfmpegArguments.VideoBitstreamFilter, videoBsf),
						FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs),
						FfmpegOption.Positional(FfmpegCommandLine.Quoted(tsFile))), ct).ConfigureAwait(false);
					if (File.Exists(tsFile))
					{
						tempTsFiles.Add(tsFile);
					}
				}

				if (tempTsFiles.Count > 1)
				{
					Logger.Log("Concatenating TS segments...");
					var concatInput = string.Join("|", tempTsFiles);
					var aacBsfOpt = SelectedAudioCodec.Contains("aac", StringComparison.OrdinalIgnoreCase)
						? FfmpegOption.Pair(FfmpegArguments.AudioBitstreamFilter, FfmpegArguments.BitstreamFilterAacAdtsToAsc)
						: (FfmpegOption?)null;
					await RunAndLogFFMpegAsync(FfmpegCommandLine.Build(
						FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
						FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"concat:{concatInput}")),
						FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
						aacBsfOpt,
						FfmpegOption.Positional(FfmpegCommandLine.Quoted(finalCombinedPath))), ct).ConfigureAwait(false);
				}

				try
				{
					foreach (var tempFile in tempTsFiles)
					{
						if (File.Exists(tempFile))
						{
							TempPathHelper.DeleteTemporaryFileUnlessRetained(tempFile);
						}
					}
				}
				catch (Exception e)
				{
					Logger.Log($"Error deleting temporary TS files: {e.Message}");
				}

				Logger.Log("Completed combining trimmed files.");
				Logger.Log("Final Output:");
				Logger.Log(finalCombinedPath);
			}

			try
			{
				foreach (var tempFile in tempFilesToDelete)
				{
					if (File.Exists(tempFile))
					{
						TempPathHelper.DeleteTemporaryFileUnlessRetained(tempFile);
					}
				}
			}
			catch
			{
				Logger.Log("Error deleting temporary files");
			}

			ClearTimeRanges();
			Logger.Log("Trim and combine complete.");
		}
		catch (OperationCanceledException)
		{
			Logger.Log("Trim cancelled.");
			throw;
		}
		catch (Exception ex)
		{
			Logger.Log($"Trim failed: {ex.Message}");
		}
		finally
		{
			CanTrim = true;
		}
	}

	private async Task<string> TrimRangeAsync(StartStopRange range, string outFolder, CancellationToken ct)
	{
		string trimOutputPath = string.Empty;
		if (string.IsNullOrWhiteSpace(InputPath))
		{
			return trimOutputPath;
		}

		try
		{
			trimOutputPath = CreateTrimOutputPath(InputPath, outFolder, range);
			if (File.Exists(InputPath) && !File.Exists(trimOutputPath))
			{
				var args = string.Empty;
				TimeSpan endSpan = new TimeSpan(range.EndRange.Hours, range.EndRange.Minutes, range.EndRange.Seconds);
				TimeSpan startSpan = new TimeSpan(range.StartRange.Hours, range.StartRange.Minutes, range.StartRange.Seconds);
				var dif = endSpan - startSpan;
				var endString = $"{dif.Hours:00}:{dif.Minutes:00}:{dif.Seconds:00}";
				var inputFileName = System.IO.Path.GetFileName(InputPath);

				if (string.IsNullOrWhiteSpace(range.Label) && FfmpegArguments.StreamCopy.Equals(SelectedAudioCodec, StringComparison.OrdinalIgnoreCase))
				{
					args = FfmpegCommandLine.Build(
						FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, range.StartRange.AsInputParameterString()),
						FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(inputFileName)),
						FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, endString),
						FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy),
						FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, FfmpegArguments.StreamCopy),
						FfmpegOption.Positional(FfmpegCommandLine.Quoted(trimOutputPath)));
				}
				else
				{
					FfmpegOption vfOpt = default;
					if (!string.IsNullOrWhiteSpace(range.Label))
					{
						var drawTextString = DrawTextUtils.CreateVideoOverlayText(range.Label, range.SelectedDrawTextPosition);
						if (EncoderPresets.EncoderPreset.CV.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
						{
							var innerFilter = drawTextString.Trim('\"');
							vfOpt = FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{innerFilter},format=nv12,hwupload=derive_device=vaapi:extra_hw_frames=64\"");
						}
						else
						{
							vfOpt = FfmpegOption.Pair(FfmpegArguments.VideoFilter, drawTextString);
						}
					}
					else if (EncoderPresets.EncoderPreset.CV.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
					{
						vfOpt = FfmpegOption.Pair(FfmpegArguments.VideoFilter, "format=nv12,hwupload");
					}
					if (EncoderPresets.EncoderPreset.CV.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
					{
						args = FfmpegCommandLine.Build(
							FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, range.StartRange.AsInputParameterString()),
							FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(inputFileName)),
							FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, endString),
							vfOpt,
							FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, EncoderPresets.EncoderPreset.CV),
							FfmpegOption.Pair(FfmpegArguments.VideoBitrate, EncoderPresets.EncoderPreset.BV),
							FfmpegOption.Pair(FfmpegArguments.VideoMaxBitrate, EncoderPresets.EncoderPreset.MaxRate),
							FfmpegOption.Pair(FfmpegArguments.VideoProfile, EncoderPresets.EncoderPreset.ProfileV),
							FfmpegOption.Pair(FfmpegArguments.VaapiRateControlMode, "3"),
							FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, SelectedAudioCodec),
							FfmpegOption.Positional(FfmpegCommandLine.Quoted(trimOutputPath)));
					}
					else
					{
						var encodingTail = new List<FfmpegOption?>
						{
							FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, EncoderPresets.EncoderPreset.CV),
						};
						if (!string.IsNullOrWhiteSpace(EncoderPresets.EncoderPreset.PresetV))
							encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.VideoPreset, EncoderPresets.EncoderPreset.PresetV));
						if (!string.IsNullOrWhiteSpace(EncoderPresets.EncoderPreset.TuneV))
							encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.VideoTune, EncoderPresets.EncoderPreset.TuneV));
						if (!string.IsNullOrWhiteSpace(EncoderPresets.EncoderPreset.RCV))
							encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.VideoRateControl, EncoderPresets.EncoderPreset.RCV));
						var pixelFormat = VideoBitDepth == "10 bit" ? "yuv420p10le" : "yuv420p";
						encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.PixelFormat, pixelFormat));
						encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.VideoBitrate, EncoderPresets.EncoderPreset.BV));
						encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.VideoMaxBitrate, EncoderPresets.EncoderPreset.MaxRate));
						encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.VideoProfile, EncoderPresets.EncoderPreset.ProfileV));
						encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, SelectedAudioCodec));

						args = FfmpegCommandLine.Build(
							new FfmpegOption?[]
							{
								FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, range.StartRange.AsInputParameterString()),
								FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(inputFileName)),
								FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, endString),
								vfOpt,
							}.Concat(encodingTail));
					}
				}

				await RunAndLogFFMpegAsync(args, ct).ConfigureAwait(false);
			}
			else if (File.Exists(trimOutputPath))
			{
				Logger.Log($"Output Already Exists: {trimOutputPath}");
				Logger.Log("Stopping....");
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception e)
		{
			Logger.Log($"Error trimming range {range.StartRange} - {range.EndRange}: {e.Message}");
		}

		return trimOutputPath;
	}

	private void WriteTrimExport(string exportFile)
	{
		ExportState(FFMpegUtils.Instance.CleanupPath(System.IO.Path.ChangeExtension(exportFile, ".json")));


	}

	private void ClearTimeRanges()
	{
		try
		{
			SelectedStartStopRange = null;
			TrimRanges.Clear();
		}
		catch
		{
			Logger.Log("Error clearing time ranges");
		}
	}

	private string CreateTrimOutputPath(string inputPath, string outputFolder, StartStopRange startStopRange)
	{
		try
		{
			var fileName = System.IO.Path.GetFileNameWithoutExtension(inputPath);
			var ext = System.IO.Path.GetExtension(inputPath);
			var outFileName = $"{fileName}_{startStopRange.StartRange}-{startStopRange.EndRange}{ext}".Replace(":", "_");
			return System.IO.Path.Combine(outputFolder, outFileName);
		}
		catch
		{
			Logger.Log("Error creating trim output path, using temp file name");
		}
		return TempPathHelper.GetTempFileName();
	}

	private bool IsRangeWithinInputBounds(StartStopRange range)
	{
		if (range == null || range.StartRange == null || range.EndRange == null)
		{
			return false;
		}
		if (_inputDuration <= TimeSpan.Zero)
		{
			return false;
		}
		var startSeconds = range.StartRange.TotalSeconds;
		var endSeconds = range.EndRange.TotalSeconds;
		var durationSeconds = _inputDuration.TotalSeconds;
		if (endSeconds > durationSeconds)
		{
			Logger.Log("End seconds is longer than video duration");
		}
		return startSeconds >= 0 && endSeconds > startSeconds && endSeconds <= durationSeconds;
	}

	public void ImportTrimFile(string file)
	{
		if (File.Exists(file))
		{
			try
			{
				var exportObj = JsonSerializer.Deserialize<ExportTrimState>(File.ReadAllText(file, encoding: System.Text.Encoding.UTF8));
				TrimRanges.Clear();
				InputPath = string.Empty;
				IntroTitle = string.Empty;
				IntroSubtitle = string.Empty;
				IntroDetails = string.Empty;

				if (exportObj != null)
				{
					if (!string.IsNullOrWhiteSpace(exportObj.InputFile) && File.Exists(exportObj.InputFile))
					{
						InputPath = exportObj.InputFile;
					}
					if (exportObj.StartStopRanges != null)
					{
						foreach (var range in exportObj.StartStopRanges)
						{
							if (range?.IsValidRange() == true && IsRangeWithinInputBounds(range))
							{
								TrimRanges.Add(range);
							}
						}
					}
					if (!string.IsNullOrWhiteSpace(exportObj.OutputFolderName))
					{
						OutputFolderName = exportObj.OutputFolderName;
					}
					if (!string.IsNullOrWhiteSpace(exportObj.IntroTitle))
					{
						IntroTitle = exportObj.IntroTitle;
					}
					if (!string.IsNullOrWhiteSpace(exportObj.IntroSubtitle))
					{
						IntroSubtitle = exportObj.IntroSubtitle;
					}
					if (!string.IsNullOrWhiteSpace(exportObj.IntroDetails))
					{
						IntroDetails = exportObj.IntroDetails;
					}
					if (exportObj.IntroDurationSeconds.HasValue)
					{
						IntroDurationSeconds = Math.Clamp(exportObj.IntroDurationSeconds.Value, 1, 59);
					}
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"[ImportTrimFile][Error] {e}");
				Logger.Log($"Import Error: {e.Message}");
			}
		}
	}


	public void ExportState(string outputFile)
	{
		var state = new ExportTrimState()
		{
			InputFile = InputPath,
			OutputFolderName = OutputFolderName,
		};
		state.StartStopRanges.AddRange(TrimRanges);
		if (!string.IsNullOrWhiteSpace(IntroTitle) || !string.IsNullOrWhiteSpace(IntroSubtitle) || !string.IsNullOrWhiteSpace(IntroDetails) || IntroDurationSeconds != 3)
		{
			state.IntroTitle = IntroTitle;
			state.IntroSubtitle = IntroSubtitle;
			state.IntroDetails = IntroDetails;
			state.IntroDurationSeconds = IntroDurationSeconds;
		}
		var asJson = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(outputFile, asJson, encoding: System.Text.Encoding.UTF8);
	}

	private async Task ReadFileInfoAsync()
	{
		VideoHourRange = [.. Range];
		VideoMinuteRange = [.. Range];
		StartRange.Hours = 0;
		StartRange.Minutes = 0;
		StartRange.Seconds = 0;
		EndRange.Hours = 0;
		EndRange.Minutes = 0;
		EndRange.Seconds = 0;
		CanClear = false;
		CanTrim = false;
		_inputDuration = TimeSpan.Zero;
		try
		{
			var duration = await FFMpegUtils.Instance.GetFileDurationAsync(InputPath);
			if (!string.IsNullOrWhiteSpace(duration))
			{
				if (TimeSpan.TryParse(duration, out var parsedDuration))
				{
					parsedDuration -= TimeSpan.FromSeconds(1); // Subtract 1 second to ensure trimming within bounds
					_inputDuration = TimeSpan.FromSeconds(Math.Round(parsedDuration.TotalSeconds));
				}
				CanClear = true;
				CanTrim = true;
				// Use _inputDuration (already adjusted) to set hour/minute/second ranges
				int hours = (int)_inputDuration.TotalHours;
				int min = _inputDuration.Minutes;
				int sec = _inputDuration.Seconds;
				VideoHourRange = [.. Enumerable.Range(0, hours + 1)];
				EndRange.Hours = hours;

				if (hours < 1)
				{
					VideoMinuteRange = [.. Enumerable.Range(0, min + 1)];
				}
				EndRange.Minutes = min;
				EndRange.Seconds = Math.Min(sec + 1, 59);
			}

		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ReadFileInfo][Error] {ex}");
			Logger.Log($"Read File Info Error: {ex.Message}");
		}
	}
}
