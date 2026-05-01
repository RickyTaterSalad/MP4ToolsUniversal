using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MP4Tools;
using MP4ToolsLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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

	/// <summary>Max representable skip/keep in the minute+second combo boxes (UI uses 0–59 per field).</summary>
	private const int MaxUiSeconds = 59 * 60 + 59;

	private CancellationTokenSource _edgeDurationRefreshCts;
	private double? _firstClipDurationSeconds;
	private double? _lastClipDurationSeconds;

	private List<int> _startVideoMinuteRange = new List<int>(TimeRange.Range);
	private List<int> _startVideoSecondRange = new List<int>(TimeRange.Range);
	private List<int> _endVideoMinuteRange = new List<int>(TimeRange.Range);
	private List<int> _endVideoSecondRange = new List<int>(TimeRange.Range);

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
	public RelayCommand RemoveSelectedFileCommand { get; private set; }

	public CombineViewModel()
	{
		CombineCommand = new AsyncRelayCommand(async () => await Combine());
		RemoveSelectedFileCommand = new RelayCommand(RemoveSelectedFile);
		EventDate = DateTime.Today;
		_inputFiles.CollectionChanged += OnInputFilesCollectionChanged;
		_startRange.PropertyChanged += OnStartRangeTimePartChanged;
		_endRange.PropertyChanged += OnEndRangeTimePartChanged;
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

	private void OnInputFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		ScheduleEdgeDurationRefresh();
	}

	private void OnStartRangeTimePartChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(TimeRange.Minutes) or nameof(TimeRange.Hours))
		{
			RebuildStartSecondRangeOnly();
		}
	}

	private void OnEndRangeTimePartChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(TimeRange.Minutes) or nameof(TimeRange.Hours))
		{
			RebuildEndSecondRangeOnly();
		}
	}

	private void CancelEdgeDurationRefresh()
	{
		try
		{
			_edgeDurationRefreshCts?.Cancel();
		}
		catch
		{
			// ignored
		}

		_edgeDurationRefreshCts?.Dispose();
		_edgeDurationRefreshCts = null;
	}

	private void ScheduleEdgeDurationRefresh()
	{
		try
		{
			_edgeDurationRefreshCts?.Cancel();
		}
		catch
		{
			// ignored
		}

		_edgeDurationRefreshCts?.Dispose();
		_edgeDurationRefreshCts = new CancellationTokenSource();
		var token = _edgeDurationRefreshCts.Token;
		_ = RunEdgeRefreshAsync(token);
	}

	private async Task RunEdgeRefreshAsync(CancellationToken ct)
	{
		try
		{
			await Task.Delay(50, ct).ConfigureAwait(false);

			string[] pathsSnapshot = await Dispatcher.UIThread.InvokeAsync(
				() => _inputFiles.Select(f => f.Path).ToArray());

			if (pathsSnapshot.Length == 0)
			{
				await Dispatcher.UIThread.InvokeAsync(() =>
				{
					if (!ct.IsCancellationRequested)
					{
						_firstClipDurationSeconds = null;
						_lastClipDurationSeconds = null;
						ApplyDefaultSkipRanges();
					}
				});
				return;
			}

			string firstPath = pathsSnapshot[0];
			string lastPath = pathsSnapshot[^1];

			double? firstDur = null;
			double? lastDur = null;

			try
			{
				var durStr = await FFMpegUtils.Instance.GetFileDurationAsync(firstPath, ct, Logger.Log).ConfigureAwait(false);
				var tr = TimeRange.FromString(durStr ?? string.Empty);
				if (tr != null)
				{
					firstDur = tr.TotalSeconds;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				// keep null — fall back to full combo ranges
			}

			if (string.Equals(firstPath, lastPath, StringComparison.OrdinalIgnoreCase))
			{
				lastDur = firstDur;
			}
			else
			{
				try
				{
					var durStr = await FFMpegUtils.Instance.GetFileDurationAsync(lastPath, ct, Logger.Log).ConfigureAwait(false);
					var tr = TimeRange.FromString(durStr ?? string.Empty);
					if (tr != null)
					{
						lastDur = tr.TotalSeconds;
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch
				{
					// ignored
				}
			}

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (ct.IsCancellationRequested)
				{
					return;
				}

				_firstClipDurationSeconds = firstDur;
				_lastClipDurationSeconds = lastDur;
				RebuildAllEdgeSkipRanges();
			});
		}
		catch (OperationCanceledException)
		{
			// refresh superseded or Clear()
		}
	}

	private void ApplyDefaultSkipRanges()
	{
		_startVideoMinuteRange = new List<int>(TimeRange.Range);
		_startVideoSecondRange = new List<int>(TimeRange.Range);
		_endVideoMinuteRange = new List<int>(TimeRange.Range);
		_endVideoSecondRange = new List<int>(TimeRange.Range);
		OnPropertyChanged(nameof(StartVideoMinuteRange));
		OnPropertyChanged(nameof(StartVideoSecondRange));
		OnPropertyChanged(nameof(EndVideoMinuteRange));
		OnPropertyChanged(nameof(EndVideoSecondRange));
	}

	/// <summary>
	/// UI max for skip (start) or keep-from-end (end): one second less than reported duration so the trim never consumes the entire clip.
	/// </summary>
	private static int MaxSkipOrKeepSecondsFromDuration(double durationSeconds)
	{
		var durFloor = (int)Math.Floor(durationSeconds);
		return Math.Max(0, durFloor - 1);
	}

	private int GetMaxStartSkipSecondsBounded()
	{
		if (!_firstClipDurationSeconds.HasValue)
		{
			return MaxUiSeconds;
		}

		return Math.Min(MaxUiSeconds, MaxSkipOrKeepSecondsFromDuration(_firstClipDurationSeconds.Value));
	}

	private int GetMaxEndKeepSecondsBounded()
	{
		if (!_lastClipDurationSeconds.HasValue)
		{
			return MaxUiSeconds;
		}

		return Math.Min(MaxUiSeconds, MaxSkipOrKeepSecondsFromDuration(_lastClipDurationSeconds.Value));
	}

	private static void ClampTimeRangeToMax(TimeRange range, int maxTotalSeconds)
	{
		maxTotalSeconds = Math.Max(0, maxTotalSeconds);
		var cur = (int)range.TotalSeconds;
		if (cur <= maxTotalSeconds)
		{
			return;
		}

		range.Hours = 0;
		range.Minutes = Math.Min(59, maxTotalSeconds / 60);
		range.Seconds = Math.Min(59, maxTotalSeconds - range.Minutes * 60);
	}

	private void RebuildStartSkipRangesAndClamp()
	{
		int maxSec = GetMaxStartSkipSecondsBounded();
		ClampTimeRangeToMax(StartRange, maxSec);
		int maxMin = Math.Min(59, maxSec / 60);
		_startVideoMinuteRange = Enumerable.Range(0, maxMin + 1).ToList();
		if (StartRange.Minutes > maxMin)
		{
			StartRange.Minutes = maxMin;
		}

		RebuildStartSecondRangeOnly();
		OnPropertyChanged(nameof(StartVideoMinuteRange));
	}

	private void RebuildStartSecondRangeOnly()
	{
		int maxSec = GetMaxStartSkipSecondsBounded();
		int maxMin = Math.Min(59, maxSec / 60);
		int selMin = Math.Clamp(StartRange.Minutes, 0, maxMin);
		if (StartRange.Minutes != selMin)
		{
			StartRange.Minutes = selMin;
		}

		int secMax = Math.Min(59, Math.Max(0, maxSec - selMin * 60));
		_startVideoSecondRange = Enumerable.Range(0, secMax + 1).ToList();
		if (StartRange.Seconds > secMax)
		{
			StartRange.Seconds = secMax;
		}

		OnPropertyChanged(nameof(StartVideoSecondRange));
	}

	private void RebuildEndSkipRangesAndClamp()
	{
		int maxSec = GetMaxEndKeepSecondsBounded();
		ClampTimeRangeToMax(EndRange, maxSec);
		int maxMin = Math.Min(59, maxSec / 60);
		_endVideoMinuteRange = Enumerable.Range(0, maxMin + 1).ToList();
		if (EndRange.Minutes > maxMin)
		{
			EndRange.Minutes = maxMin;
		}

		RebuildEndSecondRangeOnly();
		OnPropertyChanged(nameof(EndVideoMinuteRange));
	}

	private void RebuildEndSecondRangeOnly()
	{
		int maxSec = GetMaxEndKeepSecondsBounded();
		int maxMin = Math.Min(59, maxSec / 60);
		int selMin = Math.Clamp(EndRange.Minutes, 0, maxMin);
		if (EndRange.Minutes != selMin)
		{
			EndRange.Minutes = selMin;
		}

		int secMax = Math.Min(59, Math.Max(0, maxSec - selMin * 60));
		_endVideoSecondRange = Enumerable.Range(0, secMax + 1).ToList();
		if (EndRange.Seconds > secMax)
		{
			EndRange.Seconds = secMax;
		}

		OnPropertyChanged(nameof(EndVideoSecondRange));
	}

	private void RebuildAllEdgeSkipRanges()
	{
		RebuildStartSkipRangesAndClamp();
		RebuildEndSkipRangesAndClamp();
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


	public IReadOnlyList<int> StartVideoMinuteRange => _startVideoMinuteRange;

	public IReadOnlyList<int> StartVideoSecondRange => _startVideoSecondRange;

	public IReadOnlyList<int> EndVideoMinuteRange => _endVideoMinuteRange;

	public IReadOnlyList<int> EndVideoSecondRange => _endVideoSecondRange;

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
				var (video, _) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(InputPath, FfmpegOperationCancellationToken, Logger.Log);
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
					var (video, _) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(firstFile, FfmpegOperationCancellationToken, Logger.Log);
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
		CancelEdgeDurationRefresh();
		OutputPath = string.Empty;
		EventInfo = string.Empty;
		TrimFirstVideo = TrimLastVideo = false;
		StartRange.Minutes = 0;
		StartRange.Seconds = 0;
		IntroTitle = string.Empty;
		IntroSubtitle = string.Empty;
		IntroDetails = string.Empty;
		IntroDurationSeconds = 10;
		EventDate = DateTime.Today;
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
		var ct = BeginFfmpegOperation();
		try
		{
			await Task.Run(async () => await CombineInternal(ct), ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			Logger.Log("Combine cancelled.");
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

	private async Task CombineInternal(CancellationToken ct)
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
					ct.ThrowIfCancellationRequested();
					var vidDurationString = await FFMpegUtils.Instance.GetFileDurationAsync(file.Path, ct, Logger.Log).ConfigureAwait(false);
					var tr = TimeRange.FromString(vidDurationString ?? "");
					if (tr != null)
					{
						videoDurations[file.Path] = tr;
						dblTotalDuration += tr.TotalSeconds;
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch
				{
					Debugger.Break();
				}
			}
			totalDurationSeconds = (int)dblTotalDuration;

			var combineOutputFile = OutputPath;
			var shouldAddIntro = !string.IsNullOrWhiteSpace(IntroTitle) || !string.IsNullOrWhiteSpace(IntroSubtitle) || !string.IsNullOrWhiteSpace(IntroDetails);
			var scanOpt = TrimFirstVideo
				? FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, StartRange.AsInputParameterString())
				: default;
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
				if (!scanOpt.IsSkipped)
				{
					Logger.Log("Applying first-file start trim before intro...");
					var trimmedFirstFile = Path.Combine(TempPathHelper.GetTempPath(), $"first_trim_{Guid.NewGuid():N}.mp4");
					await RunAndLogFFMpegAsync(FfmpegCommandLine.Build(
						scanOpt,
						FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(firstFile.Path)),
						FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
						FfmpegOption.Positional(FfmpegCommandLine.Quoted(trimmedFirstFile))), ct).ConfigureAwait(false);
					if (File.Exists(trimmedFirstFile))
					{
						introInputFile = trimmedFirstFile;
						tempFilesToDelete.Add(trimmedFirstFile);
						scanOpt = default;
					}
				}

				var introFirstFile = Path.Combine(TempPathHelper.GetTempPath(), $"first_intro_{Guid.NewGuid():N}.mp4");
				await IntroVideoComposerAsync.PrependIntroAsync(
					introInputFile,
					effectiveTitle,
					effectiveSubtitle,
					introFirstFile,
					IntroDurationSeconds,
					detailsText: effectiveDetails,
					log: Logger.Log,
					ct: ct).ConfigureAwait(false);
				if (File.Exists(introFirstFile))
				{
					writeFiles[0] = new CombineFile { Name = Path.GetFileName(introFirstFile), Path = introFirstFile };
					tempFilesToDelete.Add(introFirstFile);
					scanOpt = default;
				}
			}

			var trimEndOpt = default(FfmpegOption);
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
					trimEndOpt = FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, endString);
				}
			}

			if (!shouldAddIntro)
			{
				Logger.Log("Combining with concat demuxer (no intro)...");
				var fileList = TempPathHelper.GetTempFileName();
				try
				{
					File.WriteAllLines(fileList, writeFiles.Select(x => $"file '{x.Path}'"));
					await RunAndLogFFMpegAsync(FfmpegCommandLine.Build(
						scanOpt,
						FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatConcatDemuxer),
						FfmpegOption.Pair(FfmpegArguments.ConcatDemuxerSafeFlag, FfmpegArguments.ConcatDemuxerAllowAnyPath),
						FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(fileList)),
						trimEndOpt,
						FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy),
						FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, SelectedAudioCodec),
						FfmpegOption.Positional(FfmpegCommandLine.Quoted(combineOutputFile))), ct).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					Logger.Log($"Error during combine: {e.Message}");
				}
				finally
				{
					TempPathHelper.DeleteTemporaryFileUnlessRetained(fileList);
					foreach (var tempFile in tempFilesToDelete)
					{
						TempPathHelper.DeleteTemporaryFileUnlessRetained(tempFile);
					}
				}
			}
			else
			{
				Logger.Log("Combining with incremental TS merge (intro enabled)...");
				string mergedTsFile = null;
				try
				{
					var videoCodec = await FFMpegUtils.Instance.GetFirstVideoCodecNameAsync(writeFiles.FirstOrDefault()?.Path ?? string.Empty, ct, Logger.Log).ConfigureAwait(false);
					//var videoBsf = string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase) ? "hevc_mp4toannexb,h265_metadata=audit_packet=1" : "h264_mp4toannexb,h264_metadata=audit_packet=1";
					var videoBsf = string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase) ? "hevc_mp4toannexb" : "h264_mp4toannexb";

					for (int i = 0; i < writeFiles.Count; i++)
					{
						ct.ThrowIfCancellationRequested();
						var input = writeFiles[i].Path;
						Logger.Log($"Remuxing part {i + 1}/{writeFiles.Count}: {Path.GetFileName(input)}");
						var partTsFile = Path.Combine(TempPathHelper.GetTempPath(), $"combine_part_{Guid.NewGuid():N}.ts");
						await RunAndLogFFMpegAsync(FfmpegCommandLine.Build(
							FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
							i == 0 ? scanOpt : default,
							FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(input)),
							FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
							FfmpegOption.Pair(FfmpegArguments.VideoBitstreamFilter, videoBsf),
							FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs),
							FfmpegOption.Positional(FfmpegCommandLine.Quoted(partTsFile))), ct).ConfigureAwait(false);
						if (!File.Exists(partTsFile))
						{
							continue;
						}

						if (string.IsNullOrWhiteSpace(mergedTsFile))
						{
							mergedTsFile = partTsFile;
							continue;
						}

						var nextMergedTs = Path.Combine(TempPathHelper.GetTempPath(), $"combine_merge_{Guid.NewGuid():N}.ts");
						await RunAndLogFFMpegAsync(FfmpegCommandLine.Build(
							FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
							FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted($"concat:{mergedTsFile}|{partTsFile}")),
							FfmpegOption.Pair(FfmpegArguments.SelectCodec, FfmpegArguments.StreamCopy),
							FfmpegOption.Pair(FfmpegArguments.VideoBitstreamFilter, videoBsf),
							FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs),
							FfmpegOption.Positional(FfmpegCommandLine.Quoted(nextMergedTs))), ct).ConfigureAwait(false);
						if (File.Exists(nextMergedTs))
						{
							TempPathHelper.DeleteTemporaryFileUnlessRetained(mergedTsFile);
							TempPathHelper.DeleteTemporaryFileUnlessRetained(partTsFile);
							mergedTsFile = nextMergedTs;
						}
						else
						{
							TempPathHelper.DeleteTemporaryFileUnlessRetained(partTsFile);
							TempPathHelper.DeleteTemporaryFileUnlessRetained(nextMergedTs);
						}
					}

					if (!string.IsNullOrWhiteSpace(mergedTsFile) && File.Exists(mergedTsFile))
					{
						Logger.Log("Finalizing merged TS into MP4...");
						var aacBsfOpt = SelectedAudioCodec.Contains("aac", StringComparison.OrdinalIgnoreCase)
							? FfmpegOption.Pair(FfmpegArguments.AudioBitstreamFilter, FfmpegArguments.BitstreamFilterAacAdtsToAsc)
							: (FfmpegOption?)null;
						await RunAndLogFFMpegAsync(FfmpegCommandLine.Build(
							FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
							FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(mergedTsFile)),
							FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy),
							FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, SelectedAudioCodec),
							FfmpegOption.Pair(FfmpegArguments.VideoBitstreamFilter, videoBsf),
							aacBsfOpt,
							trimEndOpt,
							FfmpegOption.Pair(FfmpegArguments.Movflags, FfmpegArguments.MovflagFastStart),
							FfmpegOption.Positional(FfmpegCommandLine.Quoted(combineOutputFile))), ct).ConfigureAwait(false);
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					Logger.Log($"Error during combine: {e.Message}");
				}
				finally
				{
					try
					{
						var tempFolder = TempPathHelper.GetTempPath();
						if (Path.GetDirectoryName(TempPathHelper.GetTempPath())?.Equals(Path.GetDirectoryName(OutputPath)) ?? false)
						{
							TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(tempFolder, recursive: true);
						}
						TempPathHelper.DeleteTemporaryFileUnlessRetained(mergedTsFile);
						foreach (var tempFile in tempFilesToDelete)
						{
							TempPathHelper.DeleteTemporaryFileUnlessRetained(tempFile);
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
		catch (OperationCanceledException)
		{
			Logger.Log("Combine cancelled.");
			throw;
		}
		catch (Exception ex)
		{
			Logger.Log($"Combine failed: {ex.Message}");
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
