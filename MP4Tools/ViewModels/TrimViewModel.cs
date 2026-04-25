using CommunityToolkit.Mvvm.Input;
using MP4Tools;
using MP4ToolsLib;
using MP4ToolsLib.Preset;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace mp4tools_universal.ViewModels;

internal class ExportTrimState
{

	public List<StartStopRange> StartStopRanges { get; set; } = new List<StartStopRange>();
	public string? InputFile { get; set; }

	public string? OutputFolderName { get; set; }
	public string? IntroTitle { get; set; }
	public string? IntroSubtitle { get; set; }
	public string? IntroDetails { get; set; }
	public int? IntroDurationSeconds { get; set; }
}

public partial class TrimViewModel : MP4ViewModelBase
{
	public static IReadOnlyList<int> Range { get; } = TimeRange.Range;


	private TimeSpan _inputDuration = TimeSpan.Zero;

	public ObservableCollection<StartStopRange> TrimRanges { get; set; }

	private StartStopRange? _selectedStartStopRange;
	public StartStopRange? SelectedStartStopRange
	{
		get => _selectedStartStopRange;
		set
		{
			SetProperty(ref _selectedStartStopRange, value);
			OnPropertyChanged(nameof(HasSelectedRange));
		}
	}
	public bool IsHoursRangeEnabled => VideoHourRange.Count > 1;

	private List<int>? _videoHourRange;

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
			SetProperty(ref _videoHourRange, (List<int>?)value);
			OnPropertyChanged(nameof(IsHoursRangeEnabled));
		}
	}
	private List<int>? _videMinuteRange;

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
			SetProperty(ref _videMinuteRange, (List<int>?)value);
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
	private string? _rangeLabel;
	public string? RangeLabel
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

	public RelayCommand BrowseToFileCommand { get; private set; }


	public AsyncRelayCommand TrimCommand { get; private set; }
	public AsyncRelayCommand TrimAndCombineCommand { get; private set; }

	public RelayCommand AddTimeRangeCommand { get; private set; }
	public RelayCommand RemoveTimeRangeCommand { get; private set; }

	public RelayCommand ClearTimeRangesCommand { get; private set; }
	public RelayCommand ResetTimeRangeCommand { get; private set; }


	public TrimViewModel()
	{
		_selectedStartStopRange = null;
		TrimRanges = new ObservableCollection<StartStopRange>();

		_videoHourRange = new List<int>(TimeRange.Range);
		_videMinuteRange = new List<int>(TimeRange.Range);
		_rangeLabel = string.Empty;
		BrowseToFileCommand = new RelayCommand(HandleBrowseToFile);

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
		TrimCommand = new AsyncRelayCommand(async () => await TrimAsync());
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
		_ = Task.Run(ReadFileInfoAsync);
	}


	private async Task TrimAndCombineAsync()
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
		var outFolder = Path.GetDirectoryName(InputPath) ?? string.Empty;
		var outTrimmedFolder = Path.Combine(outFolder, OutputFolderName);
		if (!Directory.Exists(outTrimmedFolder))
		{
			Directory.CreateDirectory(outTrimmedFolder);
		}
		LoggingLine = "Starting trim and combine...";
		CanTrim = false;
		List<string> outputPaths = new List<string>();
		foreach (var trim in validRanges)
		{
			LoggingLine = $"Trimming range: {trim.StartRange} - {trim.EndRange}";
			outputPaths.Add(await TrimRangeAsync(trim, outTrimmedFolder, forceReencode: true));
		}

		var tempFilesToDelete = new List<string>();
		var shouldAddIntro = !string.IsNullOrWhiteSpace(IntroTitle) || !string.IsNullOrWhiteSpace(IntroSubtitle) || !string.IsNullOrWhiteSpace(IntroDetails);
		if (shouldAddIntro && outputPaths.Count > 0)
		{
			LoggingLine = "Adding intro to first trimmed segment...";
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

			var introFirstFile = Path.Combine(Path.GetTempPath(), $"first_trim_intro_{Guid.NewGuid():N}.mp4");
			await IntroVideoComposerAsync.PrependIntroAsync(
				outputPaths[0],
				effectiveTitle,
				effectiveSubtitle,
				introFirstFile,
				IntroDurationSeconds,
				detailsText: effectiveDetails,
				log: (line) => { LoggingLine = line; });
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
			LoggingLine = "Only one trimmed output. Copying to final output...";
			try
			{
				if (File.Exists(outputPaths[0]))
				{
					File.Copy(outputPaths[0], finalCombinedPath, overwrite: true);
					LoggingLine = "Final Output:";
					LoggingLine = finalCombinedPath;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[TrimAndCombineAsync][SingleOutputCopyError] {ex}");
				LoggingLine = $"Copy Error: {ex.Message}";
			}
		}
		else if (outputPaths.Count > 1)
		{
			LoggingLine = "Combining trimmed files...";

			var tempTsFiles = new List<string>();
			var videoCodec = await FFMpegUtils.Instance.GetFirstVideoCodecNameAsync(outputPaths.FirstOrDefault() ?? string.Empty);
			var videoBsf = string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase) ? "hevc_mp4toannexb" : "h264_mp4toannexb";

			foreach (var input in outputPaths)
			{
				LoggingLine = $"Remuxing trimmed segment to TS: {Path.GetFileName(input)}";
				var tsFile = Path.Combine(Path.GetTempPath(), $"trim_part_{Guid.NewGuid():N}.ts");
				RunAndLogFFMpeg($"-y -i \"{input}\" -c copy -bsf:v {videoBsf} -f mpegts \"{tsFile}\"");
				if (File.Exists(tsFile))
				{
					tempTsFiles.Add(tsFile);
				}
			}

			if (tempTsFiles.Count > 1)
			{
				LoggingLine = "Concatenating TS segments...";
				var concatInput = string.Join("|", tempTsFiles);
				RunAndLogFFMpeg($"-y -i \"concat:{concatInput}\" -c copy -bsf:a aac_adtstoasc \"{finalCombinedPath}\"");
			}

			try
			{
				foreach (var tempFile in tempTsFiles)
				{
					if (File.Exists(tempFile))
					{
						File.Delete(tempFile);
					}
				}
			}
			catch
			{

			}

			LoggingLine = "Completed combining trimmed files.";
			LoggingLine = "Final Output:";
			LoggingLine = finalCombinedPath;
		}

		try
		{
			foreach (var tempFile in tempFilesToDelete)
			{
				if (File.Exists(tempFile))
				{
					File.Delete(tempFile);
				}
			}
		}
		catch
		{

		}
		CanTrim = true;
		ClearTimeRanges();
		LoggingLine = "Trim and combine complete.";
	}

	private async Task<string> TrimRangeAsync(StartStopRange range, string outFolder, bool forceReencode = false)
	{
		string trimOutputPath = string.Empty;
		if (InputPath == null)
		{
			return trimOutputPath;
		}
		await Task.Run(() =>
		{
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

					if (!forceReencode && string.IsNullOrWhiteSpace(range.Label))
					{
						args = $"-ss {range.StartRange.AsInputParameterString()} -i \"{inputFileName}\" -t {endString} -c:v copy -c:a copy \"{trimOutputPath}\"";
					}
					else
					{
						var vfArg = string.Empty;
						if (!string.IsNullOrWhiteSpace(range.Label))
						{
							var drawTextString = DrawTextUtils.CreateVideoOverlayText(range.Label, range.SelectedDrawTextPosition);
							vfArg = $"-vf {drawTextString}";
						}
						args = $"-ss {range.StartRange.AsInputParameterString()} -i \"{inputFileName}\" -t {endString} {vfArg} -c:v {EncoderPresets.EncoderPreset.CV} -preset:v {EncoderPresets.EncoderPreset.PresetV} -tune:v {EncoderPresets.EncoderPreset.TuneV} -rc:v {EncoderPresets.EncoderPreset.RCV} -b:v {EncoderPresets.EncoderPreset.BV} -maxrate {EncoderPresets.EncoderPreset.MaxRate} -profile:v {EncoderPresets.EncoderPreset.ProfileV} -c:a {EncoderPresets.EncoderPreset.CA} \"{trimOutputPath}\"";
					}

					RunAndLogFFMpeg(args);
				}
				else
				{
					if (File.Exists(trimOutputPath))
					{
						LoggingLine = $"Output Already Exists: {trimOutputPath}";
						LoggingLine = "Stopping....";
					}
				}
			}
			catch
			{

			}
		});
		return trimOutputPath;
	}

	private async Task TrimAsync()
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
		LoggingLine = "Starting trim only...";
		CanTrim = false;
		var outFolder = Path.GetDirectoryName(InputPath) ?? string.Empty;
		var outTrimmedFolder = Path.Combine(outFolder, OutputFolderName);
		if (!Directory.Exists(outTrimmedFolder))
		{
			Directory.CreateDirectory(outTrimmedFolder);
		}
		LoggingLine = $"Output folder: {outTrimmedFolder}";
		var outExportName = System.IO.Path.ChangeExtension(Path.GetFileName(InputPath), ".json");
		var path = FFMpegUtils.Instance.CleanupPath(Path.Combine(outFolder, outExportName));
		WriteTrimExport(path);
		LoggingLine = $"Exported trim state: {path}";
		foreach (var trim in validRanges)
		{
			LoggingLine = $"Trimming range: {trim.StartRange} - {trim.EndRange}";
			await TrimRangeAsync(trim, outTrimmedFolder);
		}
		CanTrim = true;
		ClearTimeRanges();
		LoggingLine = "Trim complete.";
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
			//
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
			//
		}
		return System.IO.Path.GetTempFileName();
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
			LoggingLine = "End seconds is longer than video duration";
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
				LoggingLine = $"Import Error: {e.Message}";
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
		VideoHourRange = new List<int>(Range);
		VideoMinuteRange = new List<int>(Range);
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
					_inputDuration = TimeSpan.FromSeconds(Math.Round(parsedDuration.TotalSeconds));
				}
				CanClear = true;
				CanTrim = true;
				var hmsSplitMs = duration.Split(".");
				if (hmsSplitMs != null && hmsSplitMs.Length > 0)
				{
					var hmsSplit = hmsSplitMs[0].Split(":");
					if (hmsSplit != null && hmsSplit.Length == 3)
					{
						if (int.TryParse(hmsSplit[0], out var hours))
						{
							VideoHourRange = Enumerable.Range(0, hours + 1).ToList();
							EndRange.Hours = hours;

						}
						if (int.TryParse(hmsSplit[1], out var min))
						{
							if (hours < 1)
							{
								VideoMinuteRange = Enumerable.Range(0, min + 1).ToList();
							}
							EndRange.Minutes = min;
						}
						if (int.TryParse(hmsSplit[2], out var sec))
						{
							EndRange.Seconds = Math.Min(sec + 1, 59);
						}

					}
				}
			}

		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ReadFileInfo][Error] {ex}");
			LoggingLine = $"Read File Info Error: {ex.Message}";
		}
		finally
		{
			//	OnPropertyChanged(nameof(IsHoursRangeEnabled));
		}

	}
}
