using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MP4Tools.Services;
using MP4ToolsLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MP4Tools.ViewModels;

public partial class ReplaceSegmentViewModel : MP4ViewModelBase
{
	public static IReadOnlyList<int> Range { get; } = TimeRange.Range;

	private readonly CombineViewModel _combineViewModel;
	private TimeSpan _inputDuration = TimeSpan.Zero;
	private ReplaceSegmentRange _rangeAwaitingIntro;

	[ObservableProperty]
	private bool _isShowingGenerateIntro;

	[ObservableProperty]
	private string _outputPath = string.Empty;

	[ObservableProperty]
	private string _durationDisplay = string.Empty;

	[ObservableProperty]
	private string _addRangeError = string.Empty;

	private string _operationStatus = string.Empty;
	public string OperationStatus
	{
		get => _operationStatus;
		set => SetProperty(ref _operationStatus, value);
	}

	private List<int> _videoHourRange = [.. TimeRange.Range];
	public IReadOnlyList<int> VideoHourRange
	{
		get => _videoHourRange;
		set
		{
			SetProperty(ref _videoHourRange, value == null ? [.. TimeRange.Range] : [.. value]);
			OnPropertyChanged(nameof(IsHoursRangeEnabled));
		}
	}

	private List<int> _videoMinuteRange = [.. TimeRange.Range];
	public IReadOnlyList<int> VideoMinuteRange
	{
		get => _videoMinuteRange;
		set => SetProperty(ref _videoMinuteRange, value == null ? [.. TimeRange.Range] : [.. value]);
	}

	public bool IsHoursRangeEnabled => VideoHourRange.Count > 1;

	public TimeRange DraftStart { get; } = new();
	public TimeRange DraftEnd { get; } = new();

	[ObservableProperty]
	private bool _draftReplaceWithText;

	public ObservableCollection<ReplaceSegmentRange> Segments { get; } = new();

	[ObservableProperty]
	private ReplaceSegmentRange _selectedSegment;

	public bool HasSelectedSegment => SelectedSegment != null;

	private bool _canApply;
	public bool CanApply
	{
		get => _canApply;
		private set
		{
			if (SetProperty(ref _canApply, value))
				ApplyCommand?.NotifyCanExecuteChanged();
		}
	}

	public GenerateIntroViewModel GenerateIntroViewModel { get; }

	public AsyncRelayCommand ApplyCommand { get; }
	public RelayCommand AddSegmentCommand { get; }
	public RelayCommand RemoveSelectedSegmentCommand { get; }
	public RelayCommand ClearSegmentsCommand { get; }
	public RelayCommand CreateIntroForSelectedCommand { get; }

	public ReplaceSegmentViewModel(CombineViewModel combineViewModel)
	{
		_combineViewModel = combineViewModel ?? throw new ArgumentNullException(nameof(combineViewModel));
		GenerateIntroViewModel = new GenerateIntroViewModel(_combineViewModel);
		GenerateIntroViewModel.IntroConfigured += OnIntroConfigured;
		GenerateIntroViewModel.EmbeddedCancelled += OnEmbeddedCancelled;

		ApplyCommand = new AsyncRelayCommand(ApplyAsync, () => CanApply);
		AddSegmentCommand = new RelayCommand(AddSegment);
		RemoveSelectedSegmentCommand = new RelayCommand(RemoveSelectedSegment, () => HasSelectedSegment);
		ClearSegmentsCommand = new RelayCommand(() =>
		{
			Segments.Clear();
			RefreshCanApply();
		});
		CreateIntroForSelectedCommand = new RelayCommand(CreateIntroForSelected, () =>
			SelectedSegment is { ReplaceWithText: true });

		Segments.CollectionChanged += (_, _) => RefreshCanApply();
		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(InputPath) or nameof(OutputPath) or nameof(SelectedSegment))
			{
				RefreshCanApply();
				RemoveSelectedSegmentCommand.NotifyCanExecuteChanged();
				CreateIntroForSelectedCommand.NotifyCanExecuteChanged();
				OnPropertyChanged(nameof(HasSelectedSegment));
			}
		};
	}

	partial void OnSelectedSegmentChanged(ReplaceSegmentRange value)
	{
		CreateIntroForSelectedCommand.NotifyCanExecuteChanged();
		RemoveSelectedSegmentCommand.NotifyCanExecuteChanged();
		OnPropertyChanged(nameof(HasSelectedSegment));
	}

	private void RefreshCanApply()
	{
		CanApply = !string.IsNullOrWhiteSpace(InputPath)
			&& File.Exists(InputPath)
			&& !string.IsNullOrWhiteSpace(OutputPath)
			&& Segments.Count > 0
			&& Segments.All(s => s.IsValid)
			&& Segments.Where(s => s.ReplaceWithText).All(s => s.HasReplacement)
			&& !CanStop
			&& !IsShowingGenerateIntro;
		CanClear = !string.IsNullOrWhiteSpace(InputPath)
			|| !string.IsNullOrWhiteSpace(OutputPath)
			|| Segments.Count > 0;
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
		UpdateOutputPathSuggestion(force: false);
	}

	protected override async void OnInputPathSet()
	{
		try
		{
			await ReadInputVideoBitDepthAsync().ConfigureAwait(true);
			await ReadDurationAsync().ConfigureAwait(true);
			UpdateOutputPathSuggestion(force: true);
			RefreshCanApply();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ReplaceSegment OnInputPathSet] {ex}");
			Logger.Log($"Replace segment input load: {ex.Message}");
		}
	}

	public bool AcceptDroppedVideoFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			return false;
		SetFile(filePath);
		return true;
	}

	private async Task ReadDurationAsync()
	{
		_inputDuration = TimeSpan.Zero;
		DurationDisplay = string.Empty;
		VideoHourRange = [.. Range];
		VideoMinuteRange = [.. Range];
		DraftStart.Hours = DraftStart.Minutes = DraftStart.Seconds = 0;
		DraftEnd.Hours = DraftEnd.Minutes = DraftEnd.Seconds = 0;

		if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
			return;

		var durationStr = await FFMpegUtils.Instance.GetFileDurationAsync(InputPath, default, Logger.Log)
			.ConfigureAwait(false);
		var tr = string.IsNullOrWhiteSpace(durationStr) ? null : TimeRange.FromString(durationStr.Trim());
		if (tr == null || tr.TotalSeconds <= 0)
			return;

		var parsedSeconds = Math.Max(0d, tr.TotalSeconds - 1);
		_inputDuration = TimeSpan.FromSeconds(Math.Floor(parsedSeconds));
		DurationDisplay = $"{(int)_inputDuration.TotalHours:00}:{_inputDuration.Minutes:00}:{_inputDuration.Seconds:00}";

		await Dispatcher.UIThread.InvokeAsync(() =>
		{
			var hours = (int)_inputDuration.TotalHours;
			VideoHourRange = [.. Enumerable.Range(0, hours + 1)];
			DraftEnd.Hours = hours;
			if (hours < 1)
				VideoMinuteRange = [.. Enumerable.Range(0, _inputDuration.Minutes + 1)];
			DraftEnd.Minutes = _inputDuration.Minutes;
			DraftEnd.Seconds = Math.Min(_inputDuration.Seconds + 1, 59);
		});
	}

	private void UpdateOutputPathSuggestion(bool force)
	{
		if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
			return;
		if (!force && !string.IsNullOrWhiteSpace(OutputPath))
			return;

		var baseDir = DefaultOutputPathRuntime.Directory;
		var leaf = Path.GetFileNameWithoutExtension(InputPath);
		OutputPath = FFMpegUtils.Instance.CleanupPath(Path.Combine(baseDir, $"{leaf}_edited.mp4"));
	}

	private void AddSegment()
	{
		AddRangeError = string.Empty;
		if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
		{
			AddRangeError = "Select an input video first.";
			return;
		}
		if (_inputDuration <= TimeSpan.Zero)
		{
			AddRangeError = "Video duration not loaded yet.";
			return;
		}

		var start = DraftStart.TotalSeconds;
		var end = DraftEnd.TotalSeconds;
		if (end <= start)
		{
			AddRangeError = "End must be after start.";
			return;
		}
		if (end > _inputDuration.TotalSeconds + 0.5)
		{
			AddRangeError = $"Range exceeds video duration (~{DurationDisplay}).";
			return;
		}

		if (OverlapsExisting(start, end, ignore: null))
		{
			AddRangeError = "Overlaps an existing range — overlaps are not allowed.";
			return;
		}

		var segment = new ReplaceSegmentRange
		{
			StartRange = DraftStart.Clone(),
			EndRange = DraftEnd.Clone(),
			ReplaceWithText = DraftReplaceWithText,
		};
		segment.PropertyChanged += (_, _) => RefreshCanApply();
		Segments.Add(segment);
		SelectedSegment = segment;
		RefreshCanApply();

		if (segment.ReplaceWithText)
			BeginIntroForRange(segment);
	}

	private bool OverlapsExisting(double start, double end, ReplaceSegmentRange ignore)
	{
		foreach (var s in Segments)
		{
			if (ReferenceEquals(s, ignore))
				continue;
			// Overlap if intervals intersect (touching endpoints OK).
			if (start < s.EndSeconds && end > s.StartSeconds)
				return true;
		}
		return false;
	}

	private void RemoveSelectedSegment()
	{
		if (SelectedSegment == null)
			return;
		Segments.Remove(SelectedSegment);
		SelectedSegment = null;
		RefreshCanApply();
	}

	private void CreateIntroForSelected()
	{
		if (SelectedSegment is not { ReplaceWithText: true })
			return;
		BeginIntroForRange(SelectedSegment);
	}

	private void BeginIntroForRange(ReplaceSegmentRange range)
	{
		_rangeAwaitingIntro = range;
		GenerateIntroViewModel.BeginEmbeddedSession(
			InputPath,
			$"Configure replacement intro for {range.StartRange} – {range.EndRange}. " +
			"Save returns here; intros are encoded when you click Apply Edits.",
			range.PendingIntro);
		IsShowingGenerateIntro = true;
		RefreshCanApply();
	}

	private void OnIntroConfigured(PendingIntroSpec spec)
	{
		void Finish()
		{
			if (_rangeAwaitingIntro != null && spec != null)
			{
				_rangeAwaitingIntro.PendingIntro = spec;
				_rangeAwaitingIntro.ReplaceWithText = true;
			}
			ExitGenerateIntroUi();
			ReportStep(spec != null
				? $"Intro settings saved for segment ({spec.DisplaySummary}). Will generate on Apply Edits."
				: "Returned without saving intro settings.");
		}

		if (Dispatcher.UIThread.CheckAccess())
			Finish();
		else
			Dispatcher.UIThread.Post(Finish);
	}

	private void OnEmbeddedCancelled()
	{
		void Finish()
		{
			ExitGenerateIntroUi();
			ReportStep("Returned without saving intro settings.");
		}

		if (Dispatcher.UIThread.CheckAccess())
			Finish();
		else
			Dispatcher.UIThread.Post(Finish);
	}

	private void ExitGenerateIntroUi()
	{
		GenerateIntroViewModel.EndEmbeddedSession();
		_rangeAwaitingIntro = null;
		IsShowingGenerateIntro = false;
		RefreshCanApply();
	}

	protected override Task Clear()
	{
		if (IsShowingGenerateIntro)
			ExitGenerateIntroUi();
		OperationStatus = string.Empty;
		AddRangeError = string.Empty;
		OutputPath = string.Empty;
		DurationDisplay = string.Empty;
		Segments.Clear();
		SelectedSegment = null;
		DraftReplaceWithText = false;
		DraftStart.Hours = DraftStart.Minutes = DraftStart.Seconds = 0;
		DraftEnd.Hours = DraftEnd.Minutes = DraftEnd.Seconds = 0;
		return base.Clear();
	}

	private async Task ApplyAsync()
	{
		if (!CanApply)
			return;

		var ct = BeginFfmpegOperation();
		var workDir = Path.Combine(TempPathHelper.GetTempPath(), $"replace_apply_{Guid.NewGuid():N}");
		var generatedIntros = new List<string>();
		try
		{
			RefreshCanApply();
			Directory.CreateDirectory(workDir);

			var ordered = Segments.OrderBy(s => s.StartSeconds).ToList();
			var introCount = ordered.Count(s => s.ReplaceWithText && s.PendingIntro != null);
			var introIndex = 0;
			var removals = new List<SegmentReplaceComposer.RemovalRange>();

			if (introCount > 0)
				ReportStep($"Apply Edits: generating {introCount} intro{(introCount == 1 ? "" : "s")}…");
			else
				ReportStep("Apply Edits: slicing video (no intros to generate)…");

			foreach (var segment in ordered)
			{
				ct.ThrowIfCancellationRequested();
				string replacementPath = null;
				if (segment.ReplaceWithText && segment.PendingIntro != null)
				{
					introIndex++;
					var label = $"{segment.StartRange}–{segment.EndRange}";
					ReportStep($"Generating intro {introIndex} of {introCount} ({label})…");
					var introPath = Path.Combine(workDir, $"intro_{introIndex:D3}.mp4");
					await EncodePendingIntroAsync(
						segment.PendingIntro,
						introPath,
						ct,
						sub => ReportStep($"Intro {introIndex}/{introCount} ({label}): {sub}"))
						.ConfigureAwait(false);
					generatedIntros.Add(introPath);
					replacementPath = introPath;
					ReportStep($"Intro {introIndex} of {introCount} ready ({label}).");
				}

				removals.Add(new SegmentReplaceComposer.RemovalRange(
					segment.StartSeconds,
					segment.EndSeconds,
					replacementPath));
			}

			ReportStep(introCount > 0
				? "Intros ready. Slicing keep segments and combining…"
				: "Slicing keep segments and combining…");

			await SegmentReplaceComposer.ApplyAsync(
				InputPath,
				OutputPath,
				removals,
				log: Logger.Log,
				ct: ct,
				operationStep: ReportStep).ConfigureAwait(false);

			if (File.Exists(OutputPath))
			{
				ReportStep($"Done: {OutputPath}");
				Logger.Log($"Replace segment complete: {OutputPath}");
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
			Logger.Log($"Replace segment error: {ex.Message}");
			Debug.WriteLine(ex);
		}
		finally
		{
			foreach (var intro in generatedIntros)
				TempPathHelper.DeleteTemporaryFileUnlessRetained(intro);
			TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(workDir, recursive: true);
			EndFfmpegOperation();
			RefreshCanApply();
		}
	}

	private async Task EncodePendingIntroAsync(
		PendingIntroSpec spec,
		string outputPath,
		CancellationToken ct,
		Action<string> step)
	{
		spec.NormalizeTitleFields(out var title, out var subtitle, out var details);
		await IntroVideoComposerAsync.GenerateIntroAsync(
			InputPath,
			outputPath,
			title,
			subtitle,
			details,
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
			operationStep: step).ConfigureAwait(false);

		if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
			throw new InvalidOperationException("Intro encode produced no output.");
	}
}
