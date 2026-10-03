using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MP4Tools.Services;
using MP4ToolsLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MP4Tools.ViewModels;

public partial class RewriteIntroViewModel : MP4ViewModelBase
{
	protected override LogOperationSource OperationLogSource => LogOperationSource.RewriteIntro;

	private const string DefaultIntroTitle = "{VISITOR} vs. {HOME}";
	private const string DefaultIntroDetails = "Final Score {SCORE} {WINNER}";

	public static IReadOnlyList<int> IntroDurationRange { get; } = Enumerable.Range(1, 59).ToList();

	public static IReadOnlyList<string> EventInfoDefaults => CombineViewModel.EventInfoDefaults;

	[ObservableProperty]
	private int _introDurationSeconds = 10;

	[ObservableProperty]
	private string _introTitle = string.Empty;

	[ObservableProperty]
	private string _introSubtitle = string.Empty;

	[ObservableProperty]
	private string _introDetails = string.Empty;

	[ObservableProperty]
	private string _outputPath = string.Empty;

	private string _homeName = string.Empty;
	public string HomeName
	{
		get => _homeName;
		set
		{
			if (SetProperty(ref _homeName, value))
			{
				UpdateIntroTitleFromGameInfo();
				UpdateIntroDetailsFromGameInfo();
				UpdateOutputPathFromGameInfo();
				RefreshCanRewrite();
			}
		}
	}

	private string _visitorName = string.Empty;
	public string VisitorName
	{
		get => _visitorName;
		set
		{
			if (SetProperty(ref _visitorName, value))
			{
				UpdateIntroTitleFromGameInfo();
				UpdateIntroDetailsFromGameInfo();
				UpdateOutputPathFromGameInfo();
				RefreshCanRewrite();
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
					RefreshCanRewrite();
				}
				return;
			}
			var newVal = Math.Max(0, value.Value);
			if (SetProperty(ref _visitorScore, newVal))
			{
				UpdateOutputPathFromGameInfo();
				UpdateIntroDetailsFromGameInfo();
				RefreshCanRewrite();
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
					RefreshCanRewrite();
				}
				return;
			}
			var newVal = Math.Max(0, value.Value);
			if (SetProperty(ref _homeScore, newVal))
			{
				UpdateOutputPathFromGameInfo();
				UpdateIntroDetailsFromGameInfo();
				RefreshCanRewrite();
			}
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
				RefreshCanRewrite();
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
				RefreshCanRewrite();
			}
		}
	}

	private string _operationStatus = string.Empty;
	public string OperationStatus
	{
		get => _operationStatus;
		set => SetProperty(ref _operationStatus, value);
	}

	private bool _canRewrite;
	public bool CanRewrite
	{
		get => _canRewrite;
		private set
		{
			if (SetProperty(ref _canRewrite, value))
				RewriteCommand?.NotifyCanExecuteChanged();
		}
	}

	private bool _canReadIntro;
	public bool CanReadIntro
	{
		get => _canReadIntro;
		private set
		{
			if (SetProperty(ref _canReadIntro, value))
				ReadIntroCommand?.NotifyCanExecuteChanged();
		}
	}

	public AsyncRelayCommand RewriteCommand { get; }
	public AsyncRelayCommand ReadIntroCommand { get; }

	public RewriteIntroViewModel()
	{
		RewriteCommand = new AsyncRelayCommand(RewriteAsync, () => CanRewrite);
		ReadIntroCommand = new AsyncRelayCommand(ReadIntroAsync, () => CanReadIntro);
		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(OutputPath) or nameof(IntroDurationSeconds)
				or nameof(IntroTitle) or nameof(IntroSubtitle) or nameof(IntroDetails)
				or nameof(InputPath))
			{
				RefreshCanRewrite();
			}
		};
		AppSettingsStore.SettingsChanged += (_, _) => RefreshCanRewrite();
		RefreshCanRewrite();
	}

	private bool HasIntroContent =>
		!string.IsNullOrWhiteSpace(IntroTitle)
		|| !string.IsNullOrWhiteSpace(IntroSubtitle)
		|| !string.IsNullOrWhiteSpace(IntroDetails);

	private bool HasGameInfo =>
		EventDate.HasValue
		|| !string.IsNullOrWhiteSpace(EventInfo)
		|| !string.IsNullOrWhiteSpace(VisitorName)
		|| !string.IsNullOrWhiteSpace(HomeName)
		|| VisitorScore.HasValue
		|| HomeScore.HasValue;

	private void RefreshCanRewrite()
	{
		CanRewrite = !string.IsNullOrWhiteSpace(InputPath)
			&& File.Exists(InputPath)
			&& !string.IsNullOrWhiteSpace(OutputPath)
			&& IntroDurationSeconds >= 1
			&& HasIntroContent;
		CanReadIntro = !string.IsNullOrWhiteSpace(InputPath)
			&& File.Exists(InputPath)
			&& IntroDurationSeconds >= 1
			&& !string.IsNullOrWhiteSpace(OpenRouterSettingsRuntime.ApiKey)
			&& !CanStop;
		CanClear = !string.IsNullOrWhiteSpace(InputPath)
			|| !string.IsNullOrWhiteSpace(OutputPath)
			|| HasIntroContent
			|| HasGameInfo;
	}

	private void ReportStep(string message)
	{
		var m = message ?? string.Empty;
		if (Dispatcher.UIThread.CheckAccess())
			OperationStatus = m;
		else
			Dispatcher.UIThread.Post(() => OperationStatus = m);
	}

	protected override void OnSettingsApplied()
	{
		UpdateOutputPathFromGameInfo();
	}

	protected override async void OnInputPathSet()
	{
		try
		{
			await ReadInputVideoBitDepthAsync().ConfigureAwait(true);
			UpdateOutputPathFromGameInfo();
			RefreshCanRewrite();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[RewriteIntro OnInputPathSet] {ex}");
			Logger.Log($"Rewrite intro input load: {ex.Message}");
		}
	}

	/// <summary>Uses a dropped video file as the rewrite source.</summary>
	public bool AcceptDroppedVideoFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			return false;

		SetFile(filePath);
		return true;
	}

	private void UpdateOutputPathFromGameInfo()
	{
		if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
			return;

		var baseDir = DefaultOutputPathRuntime.Directory;
		var gameName = FileUtils.BuildGameInfoOutputFileName(EventDate, VisitorName, HomeName);
		if (!string.IsNullOrWhiteSpace(gameName))
		{
			OutputPath = FFMpegUtils.Instance.CleanupPath(Path.Combine(baseDir, $"{gameName}_rewrite_intro.mp4"));
			return;
		}

		var leaf = Path.GetFileNameWithoutExtension(InputPath);
		OutputPath = FFMpegUtils.Instance.CleanupPath(Path.Combine(baseDir, $"{leaf}_rewrite_intro.mp4"));
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
			IntroSubtitle = $"{datePart} {EventInfo}";
		else if (!string.IsNullOrWhiteSpace(datePart))
			IntroSubtitle = datePart;
		else
			IntroSubtitle = EventInfo;
	}

	private void UpdateIntroTitleFromGameInfo()
	{
		if (!string.IsNullOrWhiteSpace(VisitorName) && !string.IsNullOrWhiteSpace(HomeName))
		{
			IntroTitle = DefaultIntroTitle.Replace("{VISITOR}", VisitorName).Replace("{HOME}", HomeName);
		}
	}

	private void UpdateIntroDetailsFromGameInfo()
	{
		if (string.IsNullOrWhiteSpace(VisitorName) || string.IsNullOrWhiteSpace(HomeName))
			return;
		if (!VisitorScore.HasValue || !HomeScore.HasValue)
			return;

		var visScore = VisitorScore.Value;
		var homeScore = HomeScore.Value;
		if (visScore <= 0 && homeScore <= 0)
			return;

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
			winner = "TIE";
			high = low = visScore;
		}
		var scoreStr = $"{high}-{low}";
		IntroDetails = DefaultIntroDetails.Replace("{WINNER}", winner).Replace("{SCORE}", scoreStr);
	}

	protected override Task Clear()
	{
		OperationStatus = string.Empty;
		OutputPath = string.Empty;
		IntroTitle = string.Empty;
		IntroSubtitle = string.Empty;
		IntroDetails = string.Empty;
		IntroDurationSeconds = 10;
		EventInfo = string.Empty;
		EventDate = null;
		VisitorName = string.Empty;
		VisitorScore = null;
		HomeName = string.Empty;
		HomeScore = null;
		return base.Clear();
	}

	/// <summary>Runs intro OCR after a drop confirmation or the Read Intro button.</summary>
	public Task ReadIntroFromVideoAsync() => ReadIntroAsync();

	private async Task ReadIntroAsync()
	{
		if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
		{
			ReportStep("Select a combined video first.");
			return;
		}
		if (string.IsNullOrWhiteSpace(OpenRouterSettingsRuntime.ApiKey))
		{
			ReportStep("Set an OpenRouter API key in Options first.");
			return;
		}

		var ct = BeginFfmpegOperation();
		string framePath = null;
		try
		{
			RefreshCanRewrite();
			ReportStep("Extracting intro frame…");
			framePath = await IntroFrameExtractor.ExtractIntroFrameAsync(
				InputPath,
				IntroDurationSeconds,
				ct,
				Logger.Log).ConfigureAwait(true);

			ReportStep("Reading intro text via OpenRouter…");
			var result = await OpenRouterIntroReader.ReadIntroFromImageAsync(
				framePath,
				OpenRouterSettingsRuntime.ApiKey,
				OpenRouterSettingsRuntime.Model,
				ct,
				Logger.Log).ConfigureAwait(true);

			ApplyReadResult(result);
			ReportStep("Intro fields filled from video — edit any typos, then Rewrite Intro.");
			Logger.Log(
				$"Read intro: title='{IntroTitle}', subtitle='{IntroSubtitle}', details='{IntroDetails}'");
		}
		catch (OperationCanceledException)
		{
			ReportStep("Cancelled.");
			Logger.Log("Read intro cancelled.");
		}
		catch (Exception ex)
		{
			ReportStep($"Failed to read intro: {ex.Message}");
			Logger.Log($"Read intro error: {ex.Message}");
			Debug.WriteLine(ex);
		}
		finally
		{
			TempPathHelper.DeleteTemporaryFileUnlessRetained(framePath);
			EndFfmpegOperation();
			RefreshCanRewrite();
		}
	}

	/// <summary>
	/// Apply game fields first (auto-fill helpers), then overwrite title/subtitle/details
	/// with the OCR text so typos are preserved for correction.
	/// </summary>
	private void ApplyReadResult(IntroScreenReadResult result)
	{
		if (result == null)
			return;

		if (result.EventDate.HasValue)
			EventDate = result.EventDate;
		if (!string.IsNullOrWhiteSpace(result.EventInfo))
			EventInfo = result.EventInfo;
		if (!string.IsNullOrWhiteSpace(result.VisitorName))
			VisitorName = result.VisitorName;
		if (!string.IsNullOrWhiteSpace(result.HomeName))
			HomeName = result.HomeName;
		if (result.VisitorScore.HasValue)
			VisitorScore = result.VisitorScore;
		if (result.HomeScore.HasValue)
			HomeScore = result.HomeScore;

		if (!string.IsNullOrWhiteSpace(result.Title))
			IntroTitle = result.Title;
		if (!string.IsNullOrWhiteSpace(result.Subtitle))
			IntroSubtitle = result.Subtitle;
		if (!string.IsNullOrWhiteSpace(result.Details))
			IntroDetails = result.Details;
	}

	private async Task RewriteAsync()
	{
		if (!CanRewrite)
			return;

		var ct = BeginFfmpegOperation();
		try
		{
			RefreshCanRewrite();
			ReportStep("Rewriting intro…");
			Logger.Log($"Rewrite intro: {InputPath} → {OutputPath} (strip/prepend {IntroDurationSeconds}s)");

			var outDir = Path.GetDirectoryName(OutputPath);
			if (!string.IsNullOrWhiteSpace(outDir) && !Directory.Exists(outDir))
				Directory.CreateDirectory(outDir);

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

			var sourcePath = InputPath;
			var encPrefs = EncodingSettingsRuntime.Current;
			await IntroVideoComposerAsync.ReplaceIntroAsync(
				sourcePath,
				effectiveTitle,
				effectiveSubtitle,
				OutputPath,
				IntroDurationSeconds,
				detailsText: effectiveDetails,
				log: Logger.Log,
				ct: ct,
				operationStep: ReportStep,
				hwAccelForIntro: encPrefs.HardwareAcceleration,
				resolveSafeEncoding: false).ConfigureAwait(false);

			// ReplaceIntro may delete the source after MPEG-TS temps are ready (never on SD/removable).
			if (!string.IsNullOrWhiteSpace(sourcePath) && !File.Exists(sourcePath)
				&& string.Equals(InputPath, sourcePath, StringComparison.Ordinal))
			{
				InputPath = string.Empty;
			}

			if (File.Exists(OutputPath))
			{
				ReportStep($"Done: {OutputPath}");
				Logger.Log($"Rewrite intro complete: {OutputPath}");
			}
			else
			{
				ReportStep("Failed: output was not created.");
				Logger.Log("Rewrite intro failed: output file missing.");
			}
		}
		catch (OperationCanceledException)
		{
			ReportStep("Cancelled.");
			Logger.Log("Rewrite intro cancelled.");
		}
		catch (Exception ex)
		{
			ReportStep($"Failed: {ex.Message}");
			Logger.Log($"Rewrite intro error: {ex.Message}");
			Debug.WriteLine(ex);
		}
		finally
		{
			EndFfmpegOperation();
			RefreshCanRewrite();
		}
	}
}
