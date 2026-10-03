using Avalonia.Media.Imaging;
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
using System.Threading;
using System.Threading.Tasks;

namespace MP4Tools.ViewModels;

public partial class GenerateIntroViewModel : MP4ViewModelBase
{
	protected override LogOperationSource OperationLogSource => LogOperationSource.GenerateIntro;

	public static IReadOnlyList<int> IntroDurationRange { get; } = Enumerable.Range(1, 59).ToList();
	public static IReadOnlyList<int> FontSizeRange { get; } = Enumerable.Range(16, 241).Where(n => n % 4 == 0).ToList();
	public static IReadOnlyList<int> LineGapRange { get; } = Enumerable.Range(8, 73).Where(n => n % 2 == 0).ToList();
	public static IReadOnlyList<IntroTheme> Themes { get; } = IntroTheme.Presets;

	private readonly CombineViewModel _combineViewModel;
	private CancellationTokenSource _previewCts;

	[ObservableProperty]
	private IntroTheme _selectedTheme = IntroTheme.Presets[0];

	[ObservableProperty]
	private string _introTitle = string.Empty;

	[ObservableProperty]
	private string _introSubtitle = string.Empty;

	[ObservableProperty]
	private string _introDetails = string.Empty;

	[ObservableProperty]
	private int _introDurationSeconds = 10;

	[ObservableProperty]
	private int _titleFontSize = 128;

	[ObservableProperty]
	private int _subtitleFontSize = 64;

	[ObservableProperty]
	private int _detailsFontSize = 48;

	[ObservableProperty]
	private int _lineGap = 36;

	[ObservableProperty]
	private string _backgroundColor = "0x1E1E1E";

	[ObservableProperty]
	private string _textColor = "white";

	[ObservableProperty]
	private string _outputPath = string.Empty;

	[ObservableProperty]
	private string _referenceProbeSummary = string.Empty;

	[ObservableProperty]
	private Bitmap _previewImage;

	[ObservableProperty]
	private string _previewStatus = "Preview updates as you edit.";

	[ObservableProperty]
	private bool _hasPreviewImage;

	/// <summary>When true, hide Add to Combine / Generate; Apply saves params for later encode.</summary>
	[ObservableProperty]
	private bool _isEmbeddedMode;

	[ObservableProperty]
	private string _embeddedBanner = string.Empty;

	/// <summary>Raised when embedded mode saves intro settings (no file encoded yet).</summary>
	public event Action<PendingIntroSpec> IntroConfigured;

	/// <summary>User cancelled embedded configure without saving.</summary>
	public event Action EmbeddedCancelled;

	private string _operationStatus = string.Empty;
	public string OperationStatus
	{
		get => _operationStatus;
		set => SetProperty(ref _operationStatus, value);
	}

	private bool _canGenerate;
	public bool CanGenerate
	{
		get => _canGenerate;
		private set
		{
			if (SetProperty(ref _canGenerate, value))
			{
				GenerateCommand?.NotifyCanExecuteChanged();
				AddToCombineCommand?.NotifyCanExecuteChanged();
			}
		}
	}

	private bool _canAddToCombine;
	public bool CanAddToCombine
	{
		get => _canAddToCombine;
		private set
		{
			if (SetProperty(ref _canAddToCombine, value))
				AddToCombineCommand?.NotifyCanExecuteChanged();
		}
	}

	public AsyncRelayCommand GenerateCommand { get; }
	public RelayCommand AddToCombineCommand { get; }
	public RelayCommand CancelEmbeddedCommand { get; }
	public RelayCommand ConfirmEmbeddedCommand { get; }

	private bool _canConfirmEmbedded;
	public bool CanConfirmEmbedded
	{
		get => _canConfirmEmbedded;
		private set
		{
			if (SetProperty(ref _canConfirmEmbedded, value))
				ConfirmEmbeddedCommand?.NotifyCanExecuteChanged();
		}
	}

	public GenerateIntroViewModel(CombineViewModel combineViewModel)
	{
		_combineViewModel = combineViewModel ?? throw new ArgumentNullException(nameof(combineViewModel));
		GenerateCommand = new AsyncRelayCommand(GenerateAsync, () => CanGenerate && !IsEmbeddedMode);
		AddToCombineCommand = new RelayCommand(AddToCombine, () => CanAddToCombine && !IsEmbeddedMode);
		CancelEmbeddedCommand = new RelayCommand(() => EmbeddedCancelled?.Invoke());
		ConfirmEmbeddedCommand = new RelayCommand(ConfirmEmbedded, () => CanConfirmEmbedded);
		ApplyTheme(SelectedTheme);
		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(InputPath) or nameof(OutputPath)
				or nameof(IntroTitle) or nameof(IntroSubtitle) or nameof(IntroDetails)
				or nameof(IntroDurationSeconds) or nameof(IsEmbeddedMode))
			{
				RefreshCanGenerate();
			}

			if (e.PropertyName is nameof(InputPath) or nameof(IntroTitle) or nameof(IntroSubtitle)
				or nameof(IntroDetails) or nameof(TitleFontSize) or nameof(SubtitleFontSize)
				or nameof(DetailsFontSize) or nameof(LineGap) or nameof(BackgroundColor)
				or nameof(TextColor) or nameof(SelectedTheme))
			{
				SchedulePreviewRefresh();
			}
		};
		RefreshCanGenerate();
		SchedulePreviewRefresh();
	}

	/// <summary>Prepares this VM as an in-place intro editor for Replace Segment (settings only).</summary>
	public void BeginEmbeddedSession(string referenceVideoPath, string banner, PendingIntroSpec existing = null)
	{
		IsEmbeddedMode = true;
		EmbeddedBanner = banner ?? string.Empty;
		if (existing != null)
		{
			ApplySpec(existing);
		}
		else
		{
			IntroTitle = string.Empty;
			IntroSubtitle = string.Empty;
			IntroDetails = string.Empty;
			IntroDurationSeconds = 10;
			SelectedTheme = Themes[0];
			ApplyTheme(SelectedTheme);
		}
		if (!string.IsNullOrWhiteSpace(referenceVideoPath) && File.Exists(referenceVideoPath))
			SetFile(referenceVideoPath);
		RefreshCanGenerate();
		SchedulePreviewRefresh();
	}

	public void EndEmbeddedSession()
	{
		IsEmbeddedMode = false;
		EmbeddedBanner = string.Empty;
		RefreshCanGenerate();
	}

	public PendingIntroSpec CaptureSpec() => new()
	{
		Title = IntroTitle?.Trim() ?? string.Empty,
		Subtitle = IntroSubtitle?.Trim() ?? string.Empty,
		Details = IntroDetails?.Trim() ?? string.Empty,
		DurationSeconds = IntroDurationSeconds,
		TitleFontSize = TitleFontSize,
		SubtitleFontSize = SubtitleFontSize,
		DetailsFontSize = DetailsFontSize,
		LineGap = LineGap,
		BackgroundColor = BackgroundColor,
		TextColor = TextColor,
	};

	public void ApplySpec(PendingIntroSpec spec)
	{
		if (spec == null)
			return;
		IntroTitle = spec.Title ?? string.Empty;
		IntroSubtitle = spec.Subtitle ?? string.Empty;
		IntroDetails = spec.Details ?? string.Empty;
		IntroDurationSeconds = Math.Clamp(spec.DurationSeconds, 1, 59);
		TitleFontSize = spec.TitleFontSize;
		SubtitleFontSize = spec.SubtitleFontSize;
		DetailsFontSize = spec.DetailsFontSize;
		LineGap = spec.LineGap;
		BackgroundColor = spec.BackgroundColor ?? "0x1E1E1E";
		TextColor = spec.TextColor ?? "white";
	}

	private void ConfirmEmbedded()
	{
		if (!CanConfirmEmbedded)
			return;
		var spec = CaptureSpec();
		IntroConfigured?.Invoke(spec);
	}

	partial void OnSelectedThemeChanged(IntroTheme value)
	{
		if (value != null)
			ApplyTheme(value);
	}

	private void ApplyTheme(IntroTheme theme)
	{
		if (theme == null)
			return;

		BackgroundColor = theme.BackgroundColor;
		TextColor = theme.TextColor;
		TitleFontSize = theme.TitleFontSize;
		SubtitleFontSize = theme.SubtitleFontSize;
		DetailsFontSize = theme.DetailsFontSize;
		LineGap = theme.LineGap;
	}

	private bool HasIntroContent =>
		!string.IsNullOrWhiteSpace(IntroTitle)
		|| !string.IsNullOrWhiteSpace(IntroSubtitle)
		|| !string.IsNullOrWhiteSpace(IntroDetails);

	private void RefreshCanGenerate()
	{
		CanGenerate = !IsEmbeddedMode
			&& !string.IsNullOrWhiteSpace(InputPath)
			&& File.Exists(InputPath)
			&& !string.IsNullOrWhiteSpace(OutputPath)
			&& IntroDurationSeconds >= 1
			&& HasIntroContent
			&& !CanStop;
		CanConfirmEmbedded = IsEmbeddedMode
			&& IntroDurationSeconds >= 1
			&& HasIntroContent;
		CanAddToCombine = !IsEmbeddedMode
			&& !string.IsNullOrWhiteSpace(OutputPath)
			&& File.Exists(OutputPath)
			&& CombineFile.IsSupportedVideoFile(OutputPath);
		CanClear = !string.IsNullOrWhiteSpace(InputPath)
			|| !string.IsNullOrWhiteSpace(OutputPath)
			|| HasIntroContent;
	}

	private void ReportStep(string message)
	{
		var m = message ?? string.Empty;
		if (Dispatcher.UIThread.CheckAccess())
			OperationStatus = m;
		else
			Dispatcher.UIThread.Post(() => OperationStatus = m);
	}

	private void SchedulePreviewRefresh()
	{
		_previewCts?.Cancel();
		_previewCts?.Dispose();
		var cts = new CancellationTokenSource();
		_previewCts = cts;
		_ = RefreshPreviewDebouncedAsync(cts);
	}

	private async Task RefreshPreviewDebouncedAsync(CancellationTokenSource cts)
	{
		try
		{
			await Task.Delay(450, cts.Token).ConfigureAwait(true);
			PreviewStatus = "Updating preview…";
			await RefreshPreviewAsync(cts.Token).ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			// superseded
		}
		catch (Exception ex)
		{
			PreviewStatus = $"Preview failed: {ex.Message}";
			Debug.WriteLine($"[Intro preview] {ex}");
		}
	}

	private async Task RefreshPreviewAsync(CancellationToken ct)
	{
		string path = null;
		try
		{
			path = await IntroVideoComposerAsync.RenderIntroPreviewAsync(
				InputPath,
				IntroTitle,
				IntroSubtitle,
				IntroDetails,
				TitleFontSize,
				SubtitleFontSize,
				DetailsFontSize,
				LineGap,
				BackgroundColor,
				TextColor,
				scaleFontsToResolution: true,
				maxPreviewWidth: 960,
				log: _ => { },
				ct: ct).ConfigureAwait(false);

			var bitmap = new Bitmap(path);

			var previous = PreviewImage;
			PreviewImage = bitmap;
			HasPreviewImage = true;
			PreviewStatus = string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath)
				? "Preview (default 1920×1080 — set a reference for exact sizing)"
				: "Preview";
			previous?.Dispose();
		}
		finally
		{
			TempPathHelper.DeleteTemporaryFileUnlessRetained(path);
		}
	}

	protected override void OnSettingsApplied()
	{
		UpdateOutputPathSuggestion(force: false);
	}

	protected override async void OnInputPathSet()
	{
		try
		{
			await ReadInputVideoBitDepthAsync().ConfigureAwait(true);
			await RefreshReferenceProbeAsync().ConfigureAwait(true);
			UpdateOutputPathSuggestion(force: true);
			RefreshCanGenerate();
			SchedulePreviewRefresh();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[GenerateIntro OnInputPathSet] {ex}");
			Logger.Log($"Generate intro reference load: {ex.Message}");
		}
	}

	public bool AcceptDroppedVideoFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			return false;
		SetFile(filePath);
		return true;
	}

	private async Task RefreshReferenceProbeAsync()
	{
		if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
		{
			ReferenceProbeSummary = string.Empty;
			return;
		}

		try
		{
			var (video, audio) = await FFMpegUtils.Instance.ProbeMediaInfoAsync(InputPath, default, Logger.Log)
				.ConfigureAwait(true);
			var vEnc = IntroCodecMatching.ResolveVideoEncoder(video?.CodecName);
			var aEnc = IntroCodecMatching.ResolveAudioEncoder(audio?.CodecName);
			ReferenceProbeSummary =
				$"{video?.Width ?? "?"}x{video?.Height ?? "?"} @ {video?.FrameRate ?? "?"} · " +
				$"video {video?.CodecName ?? "?"}→{vEnc} · audio {audio?.CodecName ?? "?"}→{aEnc}";
		}
		catch (Exception ex)
		{
			ReferenceProbeSummary = $"Probe failed: {ex.Message}";
		}
	}

	private void UpdateOutputPathSuggestion(bool force)
	{
		if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
			return;
		if (!force && !string.IsNullOrWhiteSpace(OutputPath))
			return;

		var baseDir = DefaultOutputPathRuntime.Directory;
		var leaf = Path.GetFileNameWithoutExtension(InputPath);
		OutputPath = FFMpegUtils.Instance.CleanupPath(Path.Combine(baseDir, $"{leaf}_intro.mp4"));
	}

	protected override Task Clear()
	{
		_previewCts?.Cancel();
		OperationStatus = string.Empty;
		OutputPath = string.Empty;
		IntroTitle = string.Empty;
		IntroSubtitle = string.Empty;
		IntroDetails = string.Empty;
		IntroDurationSeconds = 10;
		ReferenceProbeSummary = string.Empty;
		SelectedTheme = Themes[0];
		ApplyTheme(SelectedTheme);
		var previous = PreviewImage;
		PreviewImage = null;
		HasPreviewImage = false;
		PreviewStatus = "Preview updates as you edit.";
		previous?.Dispose();
		return base.Clear();
	}

	private async Task GenerateAsync()
	{
		if (!CanGenerate || IsEmbeddedMode)
			return;

		var ct = BeginFfmpegOperation();
		try
		{
			RefreshCanGenerate();
			ReportStep("Generating intro…");

			var spec = CaptureSpec();
			spec.NormalizeTitleFields(out var effectiveTitle, out var effectiveSubtitle, out var effectiveDetails);

			await IntroVideoComposerAsync.GenerateIntroAsync(
				InputPath,
				OutputPath,
				effectiveTitle,
				effectiveSubtitle,
				effectiveDetails,
				spec.DurationSeconds,
				spec.TitleFontSize,
				spec.SubtitleFontSize,
				spec.DetailsFontSize,
				spec.LineGap,
				spec.BackgroundColor,
				spec.TextColor,
				scaleFontsToResolution: true,
				log: Logger.Log,
				ct: ct,
				operationStep: ReportStep).ConfigureAwait(false);

			if (File.Exists(OutputPath))
			{
				ReportStep($"Done: {OutputPath}");
				Logger.Log($"Generate intro complete: {OutputPath}");
				SchedulePreviewRefresh();
			}
			else
			{
				ReportStep("Failed: output was not created.");
			}
		}
		catch (OperationCanceledException)
		{
			ReportStep("Cancelled.");
		}
		catch (Exception ex)
		{
			ReportStep($"Failed: {ex.Message}");
			Logger.Log($"Generate intro error: {ex.Message}");
			Debug.WriteLine(ex);
		}
		finally
		{
			EndFfmpegOperation();
			RefreshCanGenerate();
		}
	}

	private void AddToCombine()
	{
		if (!CanAddToCombine)
			return;

		if (_combineViewModel.InsertVideoAtFront(OutputPath))
			ReportStep($"Added to Combine as first clip: {Path.GetFileName(OutputPath)}");
		else
			ReportStep("Could not add to Combine (unsupported file).");
	}
}
