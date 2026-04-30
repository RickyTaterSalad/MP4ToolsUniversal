using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MP4Tools;
using MP4ToolsLib;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MP4Tools.ViewModels;

public partial class CombineViewModel : MP4ViewModelBase
{

	private const string DefaultIntroTitle = "{VISITOR} vs. {HOME}";
	private const string DefaultIntroDetails = "Final Score {SCORE} {WINNER}";

	[ObservableProperty]
	private int _introDurationSeconds = 10;

	[ObservableProperty]
	private string _introTitle;
	[ObservableProperty]
	private string _introDetails;
	[ObservableProperty]
	private string _introSubtitle;

	[ObservableProperty]
	private string _outputPath;
	private string _homeName;

	public string HomeName
	{
		get => _homeName;
		set
		{
			if (SetProperty(ref _homeName, value))
			{
				UpdateIntroTitleFromGameInfo();
			}
		}
	}


	private string _visitorName;



	public string VisitorName
	{
		get => _visitorName;
		set
		{
			if (SetProperty(ref _visitorName, value))
			{
				UpdateIntroTitleFromGameInfo();
			}
		}
	}

	[ObservableProperty]
	private string _folderPath;

	[ObservableProperty]
	private bool _trimFirstVideo;

	[ObservableProperty]
	private bool _trimLastVideo;

	[ObservableProperty]
	private TimeRange _startRange = new TimeRange();

	[ObservableProperty]
	private TimeRange _endRange = new TimeRange();

	private CombineFile _selectedInputFile;
	public CombineFile SelectedInputFile
	{
		get => _selectedInputFile;
		set
		{
			SetProperty(ref _selectedInputFile, value);
			OnPropertyChanged(nameof(HasSelectedInputFile));
		}
	}

	public bool HasSelectedInputFile => SelectedInputFile != null;

	public static IReadOnlyList<int> IntroDurationRange { get; } = Enumerable.Range(1, 59).ToList();

	private ObservableCollection<CombineFile> _inputFiles = new ObservableCollection<CombineFile>();
	public ObservableCollection<CombineFile> InputFiles
	{
		get => _inputFiles;
		set
		{
			SetProperty(ref _inputFiles, value);
			if (value != null && value.Count > 1)
			{
				CanCombine = true;
			}
			OnPropertyChanged(nameof(HasSelectedInputFile));
		}
	}

	public AsyncRelayCommand CombineCommand { get; private set; }
	public RelayCommand BrowseToFolderCommand { get; private set; }
	public RelayCommand RemoveSelectedFileCommand { get; private set; }

	public CombineViewModel()
	{
		CombineCommand = new AsyncRelayCommand(async () => await Combine());
		BrowseToFolderCommand = new RelayCommand(HandleBrowseToDirectory);
		RemoveSelectedFileCommand = new RelayCommand(RemoveSelectedFile);
	}

	private void RemoveSelectedFile()
	{
		if (SelectedInputFile != null)
		{
			InputFiles.Remove(SelectedInputFile);
			SelectedInputFile = null;
			CanCombine = InputFiles.Count > 0;
		}
	}

	private bool _canCombine = false;
	public bool CanCombine
	{
		get => !string.IsNullOrWhiteSpace(OutputPath) && _canCombine;
		set
		{
			SetProperty(ref _canCombine, value);
			if (!_canCombine && CanClear)
			{
				CanClear = false;
			}
			if (!CanClear && _canCombine && Directory.Exists(InputPath))
			{
				CanClear = true;
			}
		}
	}



	private int? _visitorScore;
	public int? VisitorScore
	{
		get => _visitorScore;
		set
		{
			if (!value.HasValue)
			{
				if (SetProperty(ref _visitorScore, null))
				{
					UpdateOutputPathFromGameInfo();
					UpdateIntroDetailsFromGameInfo();
				}
				return;
			}
			var newVal = Math.Max(0, value.Value);
			if (SetProperty(ref _visitorScore, newVal))
			{
				UpdateOutputPathFromGameInfo();
				UpdateIntroDetailsFromGameInfo();
			}
		}
	}

	private int? _homeScore;
	public int? HomeScore
	{
		get => _homeScore;
		set
		{
			if (!value.HasValue)
			{
				if (SetProperty(ref _homeScore, null))
				{
					UpdateOutputPathFromGameInfo();
					UpdateIntroDetailsFromGameInfo();
				}
				return;
			}
			var newVal = Math.Max(0, value.Value);
			if (SetProperty(ref _homeScore, newVal))
			{
				UpdateOutputPathFromGameInfo();
				UpdateIntroDetailsFromGameInfo();
			}
		}
	}


	private List<int> _startVideoSecondRange;
	public IReadOnlyList<int> StartVideoSecondRange
	{
		get
		{
			if (_startVideoSecondRange == null)
			{
				_startVideoSecondRange = new List<int>(TimeRange.Range);
			}
			return _startVideoSecondRange;
		}
		set
		{
			SetProperty(ref _startVideoSecondRange, (List<int>)value);
		}
	}

	private List<int> _startVideoMinuteRange;
	public IReadOnlyList<int> StartVideoMinuteRange
	{
		get
		{
			if (_startVideoMinuteRange == null)
			{
				_startVideoMinuteRange = new List<int>(TimeRange.Range);
			}
			return _startVideoMinuteRange;
		}
		set
		{
			SetProperty(ref _startVideoMinuteRange, (List<int>)value);
		}
	}

	private List<int> _endVideoMinuteRange;
	public IReadOnlyList<int> EndVideoMinuteRange
	{
		get
		{
			if (_endVideoMinuteRange == null)
			{
				_endVideoMinuteRange = new List<int>(TimeRange.Range);
			}
			return _endVideoMinuteRange;
		}
		set
		{
			SetProperty(ref _endVideoMinuteRange, (List<int>)value);
		}
	}
	private List<int> _endVideoSecondRange;
	public IReadOnlyList<int> EndVideoSecondRange
	{
		get
		{
			if (_endVideoSecondRange == null)
			{
				_endVideoSecondRange = new List<int>(TimeRange.Range);
			}
			return _endVideoSecondRange;
		}
		set
		{
			SetProperty(ref _endVideoSecondRange, (List<int>)value);
		}
	}

	private string _eventInfo = string.Empty;
	public string EventInfo
	{
		get => _eventInfo;
		set
		{
			if (SetProperty(ref _eventInfo, value ?? string.Empty))
			{
				UpdateSubtitleFromEventInfoAndDate();
			}
		}
	}
	private DateTime? _eventDate;

	public DateTime? EventDate
	{
		get => _eventDate;
		set
		{
			if (SetProperty(ref _eventDate, value))
			{
				UpdateOutputPathFromGameInfo();
				UpdateSubtitleFromEventInfoAndDate();
			}
		}
	}
	private void UpdateOutputPathFromGameInfo()
	{
		if (string.IsNullOrWhiteSpace(InputPath))
		{
			return;
		}
		var dir = InputPath;
		if (File.Exists(InputPath))
		{
			dir = Path.GetDirectoryName(InputPath) ?? InputPath;
		}

		var gameName = BuildGameInfoOutputFileName();
		if (!string.IsNullOrWhiteSpace(gameName))
		{
			OutputPath = FFMpegUtils.Instance.CleanupPath(Path.Combine(dir, $"{gameName}.mp4"));
		}
		else
		{
			OutputPath = FFMpegUtils.Instance.CleanupPath(Path.Combine(dir, "combined.mp4"));
		}
	}
	private void UpdateSubtitleFromEventInfoAndDate()
	{
		if (!EventDate.HasValue && string.IsNullOrWhiteSpace(EventInfo))
		{
			IntroSubtitle = string.Empty;
			return;
		}
		var datePart = EventDate.HasValue ? EventDate.Value.ToString("MM/dd/yyyy") : string.Empty;
		if (!string.IsNullOrWhiteSpace(datePart) && !string.IsNullOrWhiteSpace(EventInfo))
		{
			IntroSubtitle = $"{datePart} {EventInfo}";
		}
		else if (!string.IsNullOrWhiteSpace(datePart))
		{
			IntroSubtitle = datePart;
		}
		else
		{
			IntroSubtitle = EventInfo;
		}
	}

	private string BuildGameInfoOutputFileName()
	{
		return FileUtils.BuildGameInfoOutputFileName(EventDate, VisitorName, HomeName, VisitorScore, HomeScore);
	}
	private void UpdateIntroDetailsFromGameInfo()
	{
		if (!string.IsNullOrWhiteSpace(VisitorName) && !string.IsNullOrWhiteSpace(HomeName))
		{
			if (VisitorScore.HasValue && HomeScore.HasValue)
			{
				var visScore = VisitorScore.Value;
				var homeScore = HomeScore.Value;
				// Only update if at least one score is set
				if (visScore > 0 || homeScore > 0)
				{
					string winner;
					int high, low;
					if (visScore > homeScore)
					{
						winner = VisitorName;
						high = visScore;
						low = homeScore;
					}
					else if (homeScore > visScore)
					{
						winner = HomeName;
						high = homeScore;
						low = visScore;
					}
					else
					{
						// Tie case
						winner = "TIE";
						high = low = visScore;
					}
					var scoreStr = $"{high}-{low}";
					IntroDetails = DefaultIntroDetails.Replace("{WINNER}", winner).Replace("{SCORE}", scoreStr);
				}
			}
		}
	}
	protected override async void OnInputPathSet()
	{
		if (!string.IsNullOrWhiteSpace(InputPath))
		{
			CanClear = true;
			// Read bit depth from first file if it's a file, otherwise first file in folder
			if (File.Exists(InputPath))
			{
				var (video, _) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(InputPath, CancellationToken.None);
				if (video != null)
				{
					var detectedBitDepth = video.BitDepth;
					if (!string.IsNullOrWhiteSpace(detectedBitDepth))
					{
						if (detectedBitDepth == "10" || detectedBitDepth.Contains("10"))
						{
							InputVideoBitDepth = "10 bit";
						}
						else if (detectedBitDepth == "8" || detectedBitDepth.Contains("8"))
						{
							InputVideoBitDepth = "8 bit";
						}
						else
						{
							InputVideoBitDepth = "Unknown";
						}
					}
				}
			}
			else if (Directory.Exists(InputPath))
			{
				var fileList = CombineFile.FromFolder(InputPath);
				if (fileList.Count > 0)
				{
					var firstFile = fileList[0].Path;
					var (video, _) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(firstFile, CancellationToken.None, Logger.Log);
					if (video != null)
					{
						var detectedBitDepth = video.BitDepth;
						if (!string.IsNullOrWhiteSpace(detectedBitDepth))
						{
							if (detectedBitDepth == "10" || detectedBitDepth.Contains("10"))
							{
								InputVideoBitDepth = "10 bit";
							}
							else if (detectedBitDepth == "8" || detectedBitDepth.Contains("8"))
							{
								InputVideoBitDepth = "8 bit";
							}
							else
							{
								InputVideoBitDepth = "Unknown";
							}
						}
					}
				}
			}
		}
		var fileListResult = CombineFile.FromFolder(InputPath);
		if (Directory.Exists(InputPath))
		{
			OutputPath = FFMpegUtils.Instance.CleanupPath(Path.Combine(InputPath, "combined.mp4"));
		}
		CanCombine = fileListResult.Count > 0;
		InputFiles.Clear();
		foreach (var file in fileListResult)
		{
			InputFiles.Add(file);
		}
	}
	protected override Task Clear()
	{
		OutputPath = string.Empty;
		EventInfo = string.Empty;
		TrimFirstVideo = TrimLastVideo = false;
		StartRange.Minutes = 0;
		StartRange.Seconds = 0;
		IntroTitle = string.Empty;
		IntroSubtitle = string.Empty;
		IntroDetails = string.Empty;
		IntroDurationSeconds = 10;
		EventDate = null;
		VisitorName = string.Empty;
		VisitorScore = null;
		HomeName = string.Empty;
		HomeScore = null;

		EndRange.Minutes = 0;
		EndRange.Seconds = 0;
		return base.Clear();
	}

	private async Task Combine()
	{
		var logOutputPath = !string.IsNullOrWhiteSpace(OutputPath) ? Path.ChangeExtension(OutputPath, ".log") : null;
		var logged_ffmpeg_output = new List<string>();
		EventHandler<string> handler = (s, msg) =>
		{
			if (!string.IsNullOrWhiteSpace(msg))
				logged_ffmpeg_output.Add(msg);
		};
		Logger.LogMessageReceived += handler;
		try
		{
			await Task.Run(() => CombineInternal());
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
				Console.WriteLine($"Unexpected error during log file handling: {ex.Message}");
			}
		}
		Logger.LogMessageReceived -= handler;
	}

	private async Task CombineInternal()
	{

		if (!CanCombine || string.IsNullOrWhiteSpace(OutputPath))
		{
			return;
		}

		CanCombine = false;
		try
		{
			Logger.Log("Starting combine...");
			if (InputPath == null)
			{
				Logger.Log("Input path is null.");
				return;
			}
			OutputPath = FFMpegUtils.Instance.CleanupPath(OutputPath);
			if (File.Exists(OutputPath) || InputFiles.Count == 0)
			{
				if (File.Exists(OutputPath))
				{
					Logger.Log($"Output Already Exists: {OutputPath}");
				}
				else
				{
					Logger.Log("No input files to combine.");
				}
				return;
			}
			var outputFolder = Path.GetDirectoryName(OutputPath) ?? string.Empty;
			if (!Directory.Exists(outputFolder))
			{
				try
				{
					Directory.CreateDirectory(outputFolder);

				}
				catch
				{
					Logger.Log($"Failed to create output folder: {outputFolder}");
					return;
				}
			}
			var GetTempPath = () =>
			{
				/*
				try
				{
					var tempPath = Path.Combine(outputFolder, "temp");
					if (!Directory.Exists(tempPath))
					{
						Directory.CreateDirectory(tempPath);
					}
					return tempPath;
				}
				catch
				{
				}
				*/
				return TempPathHelper.GetTempPath();

			};

			Logger.Log("Preparing input files...");
			var writeFiles = new List<CombineFile>();
			writeFiles.AddRange(InputFiles);
			Logger.Log($"Prepared {writeFiles.Count} files.");

			int totalDurationSeconds = 0;
			double dblTotalDuration = 0;
			var videoDurations = new Dictionary<string, TimeRange>();
			Logger.Log("Reading input durations...");
			foreach (var file in writeFiles)
			{
				try
				{
					var vidDurationString = await FFMpegUtils.Instance.GetFileDurationAsync(file.Path);
					var tr = TimeRange.FromString(vidDurationString ?? "");
					if (tr != null)
					{
						videoDurations[file.Path] = tr;
						dblTotalDuration += tr.TotalSeconds;
					}
				}
				catch
				{
					Debugger.Break();
				}
			}
			totalDurationSeconds = (int)dblTotalDuration;

			var combineOutputFile = OutputPath;
			var shouldAddIntro = !string.IsNullOrWhiteSpace(IntroTitle) || !string.IsNullOrWhiteSpace(IntroSubtitle) || !string.IsNullOrWhiteSpace(IntroDetails);
			var scanStart = TrimFirstVideo ? $"-ss {StartRange.AsInputParameterString()}" : string.Empty;
			var tempFilesToDelete = new List<string>();

			if (shouldAddIntro && writeFiles.Count > 0)
			{
				Logger.Log("Building intro clip...");
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

				var firstFile = writeFiles.First();
				var introInputFile = firstFile.Path;
				if (!string.IsNullOrWhiteSpace(scanStart))
				{
					Logger.Log("Applying first-file start trim before intro...");
					var trimmedFirstFile = Path.Combine(GetTempPath(), $"first_trim_{Guid.NewGuid():N}.mp4");
					RunAndLogFFMpeg($"{scanStart} -i \"{firstFile.Path}\" -c copy \"{trimmedFirstFile}\"");
					if (File.Exists(trimmedFirstFile))
					{
						introInputFile = trimmedFirstFile;
						tempFilesToDelete.Add(trimmedFirstFile);
						scanStart = string.Empty;
					}
				}

				var introFirstFile = Path.Combine(GetTempPath(), $"first_intro_{Guid.NewGuid():N}.mp4");
				await IntroVideoComposerAsync.PrependIntroAsync(
					introInputFile,
					effectiveTitle,
					effectiveSubtitle,
					introFirstFile,
					IntroDurationSeconds,
					detailsText: effectiveDetails,
					log: Logger.Log);
				if (File.Exists(introFirstFile))
				{
					writeFiles[0] = new CombineFile { Name = Path.GetFileName(introFirstFile), Path = introFirstFile };
					tempFilesToDelete.Add(introFirstFile);
					scanStart = string.Empty;
				}
			}

			var trimEndPart = string.Empty;
			if (TrimLastVideo)
			{
				Logger.Log("Applying end-duration trim...");
				if (videoDurations.TryGetValue(writeFiles.LastOrDefault()?.Path ?? string.Empty, out var lastVideoTimeSpan))
				{
					var finalVideoDuration = new TimeSpan(lastVideoTimeSpan.Hours, lastVideoTimeSpan.Minutes, lastVideoTimeSpan.Seconds);
					var endSpan = new TimeSpan(EndRange.Hours, EndRange.Minutes, EndRange.Seconds);
					var durationToTrimOffFinal = finalVideoDuration - endSpan;
					var fullVideoSpan = TimeSpan.FromSeconds(totalDurationSeconds);
					var finalDelta = fullVideoSpan - new TimeSpan(Math.Abs(durationToTrimOffFinal.Hours), Math.Abs(durationToTrimOffFinal.Minutes), Math.Abs(durationToTrimOffFinal.Seconds));
					var endString = $"{finalDelta.Hours:00}:{finalDelta.Minutes:00}:{finalDelta.Seconds:00}";
					trimEndPart = $"-t {endString}";
				}
			}

			if (!shouldAddIntro)
			{
				Logger.Log("Combining with concat demuxer (no intro)...");
				var fileList = TempPathHelper.GetTempFileName();
				try
				{
					File.WriteAllLines(fileList, writeFiles.Select(x => $"file '{x.Path}'"));
					var encodingParms = $"-c:v copy -c:a {SelectedAudioCodec}";
					/*
					if (ReEncodeVideo)
					{
						// Set pixel format based on bit depth
						var pixelFormat = VideoBitDepth == "10 bit" ? "yuv420p10le" : "yuv420p";
						encodingParms = $"-map 0:v:0 -map 0:a:0? -c:v hevc_nvenc -preset p7 -cq 24 -b:v 0 -pix_fmt {pixelFormat} -c:a copy -movflags +faststart";
					}
					*/
					RunAndLogFFMpeg($"{scanStart} -f concat -safe 0 -i \"{fileList}\" {trimEndPart} {encodingParms} \"{combineOutputFile}\"");
				}
				catch (Exception e)
				{
					Logger.Log($"Error during combine: {e.Message}");
				}
				finally
				{
					FileUtils.TryDeleteFile(fileList);
					foreach (var tempFile in tempFilesToDelete)
					{
						FileUtils.TryDeleteFile(tempFile);
					}
				}
			}
			else
			{
				Logger.Log("Combining with incremental TS merge (intro enabled)...");
				string mergedTsFile = null;
				try
				{
					var videoCodec = await FFMpegUtils.Instance.GetFirstVideoCodecNameAsync(writeFiles.FirstOrDefault()?.Path ?? string.Empty);
					//var videoBsf = string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase) ? "hevc_mp4toannexb,h265_metadata=audit_packet=1" : "h264_mp4toannexb,h264_metadata=audit_packet=1";
					var videoBsf = string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase) ? "hevc_mp4toannexb" : "h264_mp4toannexb";

					for (int i = 0; i < writeFiles.Count; i++)
					{
						var input = writeFiles[i].Path;
						Logger.Log($"Remuxing part {i + 1}/{writeFiles.Count}: {Path.GetFileName(input)}");
						var partTsFile = Path.Combine(GetTempPath(), $"combine_part_{Guid.NewGuid():N}.ts");
						var scanStartPart = i == 0 && !string.IsNullOrWhiteSpace(scanStart) ? $"{scanStart} " : string.Empty;
						RunAndLogFFMpeg($"-y {scanStartPart}-i \"{input}\" -c copy -bsf:v {videoBsf} -f mpegts \"{partTsFile}\"");
						if (!File.Exists(partTsFile))
						{
							continue;
						}

						if (string.IsNullOrWhiteSpace(mergedTsFile))
						{
							mergedTsFile = partTsFile;
							continue;
						}

						var nextMergedTs = Path.Combine(GetTempPath(), $"combine_merge_{Guid.NewGuid():N}.ts");
						RunAndLogFFMpeg($"-y -i \"concat:{mergedTsFile}|{partTsFile}\" -c copy -bsf:v {videoBsf} -f mpegts \"{nextMergedTs}\"");
						if (File.Exists(nextMergedTs))
						{
							FileUtils.TryDeleteFile(mergedTsFile);
							FileUtils.TryDeleteFile(partTsFile);
							mergedTsFile = nextMergedTs;
						}
						else
						{
							FileUtils.TryDeleteFile(partTsFile);
							FileUtils.TryDeleteFile(nextMergedTs);
						}
					}

					if (!string.IsNullOrWhiteSpace(mergedTsFile) && File.Exists(mergedTsFile))
					{
						Logger.Log("Finalizing merged TS into MP4...");
						var aacBsf = SelectedAudioCodec.Contains("aac", StringComparison.OrdinalIgnoreCase) ? "-bsf:a aac_adtstoasc" : string.Empty;
						RunAndLogFFMpeg($"-y -i \"{mergedTsFile}\" -c:v copy -c:a {SelectedAudioCodec} -bsf:v {videoBsf} {aacBsf} {trimEndPart} -movflags +faststart \"{combineOutputFile}\"");
					}
				}
				catch (Exception e)
				{
					Logger.Log($"Error during combine: {e.Message}");
				}
				finally
				{
					try
					{
						var tempFolder = GetTempPath();
						if (Path.GetDirectoryName(GetTempPath())?.Equals(Path.GetDirectoryName(OutputPath)) ?? false)
						{
							Directory.Delete(tempFolder, true);
						}
						FileUtils.TryDeleteFile(mergedTsFile);
						foreach (var tempFile in tempFilesToDelete)
						{
							FileUtils.TryDeleteFile(tempFile);
						}
					}
					catch (Exception e)
					{
						Logger.Log(e.Message);
					}
				}
			}

			OutputPath = FFMpegUtils.Instance.CleanupPath(Path.Combine(InputPath, "combined.mp4"));
			Logger.Log("Combine complete.");
		}
		catch
		{
			Logger.Log("Combine failed.");
		}
		finally
		{
			CanCombine = true;
		}
	}
	private void UpdateIntroTitleFromGameInfo()
	{
		if (!string.IsNullOrWhiteSpace(VisitorName) && !string.IsNullOrWhiteSpace(HomeName))
		{
			var newTitle = DefaultIntroTitle.Replace("{VISITOR}", VisitorName).Replace("{HOME}", HomeName);
			IntroTitle = newTitle;
		}
	}

}
