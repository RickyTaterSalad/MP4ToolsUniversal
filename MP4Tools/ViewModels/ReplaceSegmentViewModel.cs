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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MP4Tools.ViewModels;

public partial class ReplaceSegmentViewModel : MP4ViewModelBase
{
	protected override LogOperationSource OperationLogSource => LogOperationSource.ReplaceSegment;

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

	public void ExportState(string outputFile)
	{
		try
		{
			var path = FFMpegUtils.Instance.CleanupPath(Path.ChangeExtension(outputFile, ".json"));
			var state = new ExportReplaceSegmentState
			{
				InputFile = InputPath ?? string.Empty,
				OutputPath = OutputPath ?? string.Empty,
				DraftReplaceWithText = DraftReplaceWithText,
				DraftStart = ToExportTime(DraftStart),
				DraftEnd = ToExportTime(DraftEnd),
			};

			foreach (var segment in Segments)
			{
				state.Segments.Add(new ExportReplaceSegmentRange
				{
					Start = ToExportTime(segment.StartRange),
					End = ToExportTime(segment.EndRange),
					ReplaceWithText = segment.ReplaceWithText,
					Label = segment.Label ?? string.Empty,
					PendingIntro = ToExportIntro(segment.PendingIntro),
				});
			}

			var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(path, json);
			ReportStep($"Exported Replace Segment state: {path}");
			Logger.Log($"Replace segment export: {path}");
		}
		catch (Exception ex)
		{
			ReportStep($"Export failed: {ex.Message}");
			Logger.Log($"Replace segment export error: {ex.Message}");
			Debug.WriteLine(ex);
		}
	}

	public void ImportReplaceFile(string file)
	{
		if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
		{
			ReportStep("Import failed: file not found.");
			return;
		}

		try
		{
			if (IsShowingGenerateIntro)
				ExitGenerateIntroUi();

			var state = JsonSerializer.Deserialize<ExportReplaceSegmentState>(
				File.ReadAllText(file, System.Text.Encoding.UTF8));
			if (state == null)
			{
				ReportStep("Import failed: empty or invalid JSON.");
				return;
			}

			SelectedSegment = null;
			Segments.Clear();
			AddRangeError = string.Empty;

			DraftReplaceWithText = state.DraftReplaceWithText;
			ApplyExportTime(DraftStart, state.DraftStart);
			ApplyExportTime(DraftEnd, state.DraftEnd);

			if (!string.IsNullOrWhiteSpace(state.OutputPath))
				OutputPath = state.OutputPath;

			if (!string.IsNullOrWhiteSpace(state.InputFile) && File.Exists(state.InputFile))
				SetFile(state.InputFile);
			else if (!string.IsNullOrWhiteSpace(state.InputFile))
			{
				InputPath = state.InputFile;
				ReportStep($"Imported path not found on disk: {state.InputFile}");
			}

			if (state.Segments != null)
			{
				foreach (var exported in state.Segments)
				{
					var segment = new ReplaceSegmentRange
					{
						StartRange = FromExportTime(exported.Start),
						EndRange = FromExportTime(exported.End),
						Label = exported.Label ?? string.Empty,
					};
					segment.ReplaceWithText = exported.ReplaceWithText;
					if (exported.ReplaceWithText)
						segment.PendingIntro = FromExportIntro(exported.PendingIntro);
					segment.PropertyChanged += (_, _) => RefreshCanApply();
					Segments.Add(segment);
				}
			}

			RefreshCanApply();
			ReportStep($"Imported Replace Segment state ({Segments.Count} range(s)): {file}");
			Logger.Log($"Replace segment import: {file}");
		}
		catch (Exception ex)
		{
			ReportStep($"Import failed: {ex.Message}");
			Logger.Log($"Replace segment import error: {ex.Message}");
			Debug.WriteLine(ex);
		}
	}

	private static ExportTimeParts ToExportTime(TimeRange range) => new()
	{
		Hours = range?.Hours ?? 0,
		Minutes = range?.Minutes ?? 0,
		Seconds = range?.Seconds ?? 0,
	};

	private static TimeRange FromExportTime(ExportTimeParts parts) => new()
	{
		Hours = parts?.Hours ?? 0,
		Minutes = parts?.Minutes ?? 0,
		Seconds = parts?.Seconds ?? 0,
	};

	private static void ApplyExportTime(TimeRange target, ExportTimeParts parts)
	{
		if (target == null)
			return;
		target.Hours = parts?.Hours ?? 0;
		target.Minutes = parts?.Minutes ?? 0;
		target.Seconds = parts?.Seconds ?? 0;
	}

	private static ExportPendingIntro ToExportIntro(PendingIntroSpec spec)
	{
		if (spec == null)
			return null;
		return new ExportPendingIntro
		{
			Title = spec.Title ?? string.Empty,
			Subtitle = spec.Subtitle ?? string.Empty,
			Details = spec.Details ?? string.Empty,
			DurationSeconds = spec.DurationSeconds,
			TitleFontSize = spec.TitleFontSize,
			SubtitleFontSize = spec.SubtitleFontSize,
			DetailsFontSize = spec.DetailsFontSize,
			LineGap = spec.LineGap,
			BackgroundColor = spec.BackgroundColor ?? "0x1E1E1E",
			TextColor = spec.TextColor ?? "white",
		};
	}

	private static PendingIntroSpec FromExportIntro(ExportPendingIntro dto)
	{
		if (dto == null)
			return null;
		return new PendingIntroSpec
		{
			Title = dto.Title ?? string.Empty,
			Subtitle = dto.Subtitle ?? string.Empty,
			Details = dto.Details ?? string.Empty,
			DurationSeconds = dto.DurationSeconds > 0 ? dto.DurationSeconds : 10,
			TitleFontSize = dto.TitleFontSize > 0 ? dto.TitleFontSize : 128,
			SubtitleFontSize = dto.SubtitleFontSize > 0 ? dto.SubtitleFontSize : 64,
			DetailsFontSize = dto.DetailsFontSize > 0 ? dto.DetailsFontSize : 48,
			LineGap = dto.LineGap > 0 ? dto.LineGap : 36,
			BackgroundColor = string.IsNullOrWhiteSpace(dto.BackgroundColor) ? "0x1E1E1E" : dto.BackgroundColor,
			TextColor = string.IsNullOrWhiteSpace(dto.TextColor) ? "white" : dto.TextColor,
		};
	}

	private async Task ApplyAsync()
	{
		if (!CanApply)
			return;

		if (UiBehaviorSettingsRuntime.WarnOnInsufficientDiskSpace)
		{
			var segmentEstimates = Segments.Select(s => (
				s.StartSeconds,
				s.EndSeconds,
				s.ReplaceWithText && s.PendingIntro != null
					? Math.Max(1, s.PendingIntro.DurationSeconds)
					: 0));
			var estimate = DiskSpaceEstimator.EstimateReplaceSegment(
				InputPath,
				OutputPath,
				_inputDuration.TotalSeconds,
				segmentEstimates);
			var check = DiskSpaceEstimator.Evaluate(estimate);
			if (!check.IsSufficient)
			{
				Logger.Log(
					$"Disk space check (replace): need ~{DiskSpaceEstimator.FormatBytes(check.RequiredTempBytes)} peak " +
					$"(final {DiskSpaceEstimator.FormatBytes(estimate.FinalBytes)}, temp {DiskSpaceEstimator.FormatBytes(estimate.TempBytes)}); " +
					$"free temp {DiskSpaceEstimator.FormatBytes(check.FreeTempBytes)}, free output {DiskSpaceEstimator.FormatBytes(check.FreeOutputBytes)}.");
				var proceed = await DiskSpaceWarning.ConfirmContinueIfNeededAsync(check).ConfigureAwait(true);
				if (!proceed)
				{
					ReportStep("Cancelled: insufficient disk space.");
					Logger.Log("Replace segment cancelled: user declined to continue with insufficient disk space.");
					return;
				}
			}
		}

		var ct = BeginFfmpegOperation();
		var workDir = Path.Combine(TempPathHelper.GetTempPath(), $"replace_apply_{Guid.NewGuid():N}");
		var generatedIntros = new List<string>();
		try
		{
			RefreshCanApply();
			Directory.CreateDirectory(workDir);

			var ordered = Segments.OrderBy(s => s.StartSeconds).ToList();
			var missingIntro = ordered.Where(s => s.ReplaceWithText && (s.PendingIntro == null || !s.PendingIntro.HasContent)).ToList();
			if (missingIntro.Count > 0)
				throw new InvalidOperationException(
					$"{missingIntro.Count} text-replacement range(s) are missing intro settings. Edit intro and Save first.");

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
					if (!File.Exists(introPath) || new FileInfo(introPath).Length == 0)
						throw new InvalidOperationException($"Intro encode failed for {label}.");
					generatedIntros.Add(introPath);
					replacementPath = introPath;
					Logger.Log($"Replace segment: queued intro for {label} → {introPath}");
					ReportStep($"Intro {introIndex} of {introCount} ready ({label}).");
				}

				removals.Add(new SegmentReplaceComposer.RemovalRange(
					segment.StartSeconds,
					segment.EndSeconds,
					replacementPath));
			}

			var queuedIntros = removals.Count(r => !string.IsNullOrWhiteSpace(r.ReplacementPath));
			if (queuedIntros != introCount)
				throw new InvalidOperationException($"Expected {introCount} intro(s) but queued {queuedIntros}.");

			ReportStep(introCount > 0
				? $"Intros ready ({introCount}). Slicing keep segments and combining via MPEG-TS…"
				: "Slicing keep segments and combining…");

			await SegmentReplaceComposer.ApplyAsync(
				InputPath,
				OutputPath,
				removals,
				log: Logger.Log,
				ct: ct,
				operationStep: ReportStep).ConfigureAwait(false);

			ct.ThrowIfCancellationRequested();

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
		catch (Exception) when (ct.IsCancellationRequested)
		{
			ReportStep("Cancelled.");
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

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				EndFfmpegOperation();
				RefreshCanApply();
			});
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
		{
			ct.ThrowIfCancellationRequested();
			throw new InvalidOperationException("Intro encode produced no output.");
		}
	}
}
