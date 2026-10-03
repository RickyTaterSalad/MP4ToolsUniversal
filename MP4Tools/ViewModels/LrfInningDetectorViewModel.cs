using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MP4Tools.Services;
using MP4ToolsLib;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace MP4Tools.ViewModels;

public partial class LrfInningDetectorViewModel : MP4ViewModelBase
{
	protected override LogOperationSource OperationLogSource => LogOperationSource.LrfInningDetector;

	[ObservableProperty]
	private string _lrfFolderPath = string.Empty;

	[ObservableProperty]
	private string _existingFramesFolderPath = string.Empty;

	[ObservableProperty]
	private string _combinedVideoPath = string.Empty;

	[ObservableProperty]
	private string _combineMapPath = string.Empty;

	[ObservableProperty]
	private string _outputJsonPath = string.Empty;

	[ObservableProperty]
	private string _outputYoutubePath = string.Empty;

	[ObservableProperty]
	private string _operationStatus = string.Empty;

	[ObservableProperty]
	private double _progress;

	[ObservableProperty]
	private bool _showProgress;

	[ObservableProperty]
	private bool _canDetect;

	public ObservableCollection<string> MatchedLrfLines { get; } = new();
	public ObservableCollection<string> DetectedEventLines { get; } = new();

	public AsyncRelayCommand DetectCommand { get; }

	public bool ReuseExistingFrames =>
		!string.IsNullOrWhiteSpace(ExistingFramesFolderPath)
		&& HalfInningDetector.TryResolveExistingFramesDirectory(ExistingFramesFolderPath, out _);

	public LrfInningDetectorViewModel()
	{
		DetectCommand = new AsyncRelayCommand(DetectAsync, () => CanDetect);
	}

	partial void OnLrfFolderPathChanged(string value) => RefreshFromInputs();

	partial void OnCombinedVideoPathChanged(string value) => RefreshFromInputs();

	partial void OnExistingFramesFolderPathChanged(string value) => RefreshFromInputs();

	protected override Task Clear()
	{
		LrfFolderPath = string.Empty;
		ExistingFramesFolderPath = string.Empty;
		CombinedVideoPath = string.Empty;
		CombineMapPath = string.Empty;
		OutputJsonPath = string.Empty;
		OutputYoutubePath = string.Empty;
		OperationStatus = string.Empty;
		Progress = 0;
		ShowProgress = false;
		MatchedLrfLines.Clear();
		DetectedEventLines.Clear();
		RefreshCanDetect();
		return base.Clear();
	}

	private void RefreshFromInputs()
	{
		MatchedLrfLines.Clear();
		DetectedEventLines.Clear();
		Progress = 0;
		ShowProgress = false;

		CombineMapPath = string.Empty;
		OutputJsonPath = string.Empty;
		OutputYoutubePath = string.Empty;

		if (!string.IsNullOrWhiteSpace(CombinedVideoPath) && File.Exists(CombinedVideoPath))
		{
			try
			{
				var (json, youtube) = InningDetectionRecordingWriter.BuildOutputPaths(CombinedVideoPath);
				OutputJsonPath = json;
				OutputYoutubePath = youtube;
			}
			catch
			{
				OutputJsonPath = string.Empty;
				OutputYoutubePath = string.Empty;
			}
		}

		if (ReuseExistingFrames
			&& HalfInningDetector.TryResolveExistingFramesDirectory(ExistingFramesFolderPath, out var framesDir))
		{
			var count = Directory.GetFiles(framesDir, "*.jpg").Length;
			OperationStatus =
				$"Will reuse {count} sample frame(s) from {framesDir} — skip LRF extract; " +
				"recompute on the combined-video timeline.";
			RefreshCanDetect();
			return;
		}

		if (!string.IsNullOrWhiteSpace(ExistingFramesFolderPath))
		{
			OperationStatus = Directory.Exists(ExistingFramesFolderPath)
				? "Frames folder has no .jpg samples (expected session/frames or a JPEG directory)."
				: "Frames folder not found.";
			RefreshCanDetect();
			return;
		}

		if (!string.IsNullOrWhiteSpace(CombinedVideoPath) && File.Exists(CombinedVideoPath))
		{
			// Same-folder sidecar written by Combine: {videoStem}.combine-map.json
			if (CombineEditMapIO.TryFindMapPath(CombinedVideoPath, out var mapPath))
			{
				CombineMapPath = mapPath;
				OperationStatus = $"Found combine map: {Path.GetFileName(mapPath)}";
			}
			else
			{
				var expected = CombineEditMapIO.GetExpectedMapPath(CombinedVideoPath);
				OperationStatus =
					$"No combine map in {Path.GetDirectoryName(CombinedVideoPath)}. " +
					$"Expected sibling file: {Path.GetFileName(expected)}";
			}

			if (!string.IsNullOrWhiteSpace(CombineMapPath))
				_ = PreviewLrfMatchesAsync();
		}

		RefreshCanDetect();
	}

	private async Task PreviewLrfMatchesAsync()
	{
		var video = CombinedVideoPath;
		var folder = LrfFolderPath;
		if (string.IsNullOrWhiteSpace(video)
			|| string.IsNullOrWhiteSpace(folder)
			|| !File.Exists(video)
			|| !Directory.Exists(folder)
			|| !CombineEditMapIO.TryFindMapPath(video, out _)
			|| ReuseExistingFrames)
		{
			return;
		}

		try
		{
			var map = await CombineEditMapIO.LoadAsync(video).ConfigureAwait(false);
			var paths = LrfInningDetector.ResolveLrfPaths(map, folder);
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				MatchedLrfLines.Clear();
				for (var i = 0; i < paths.Count; i++)
					MatchedLrfLines.Add($"{i + 1}. {Path.GetFileName(paths[i])}");
			});
		}
		catch (Exception ex)
		{
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				MatchedLrfLines.Clear();
				MatchedLrfLines.Add($"Match preview: {ex.Message}");
			});
		}
	}

	private void RefreshCanDetect()
	{
		var videoOk = !string.IsNullOrWhiteSpace(CombinedVideoPath) && File.Exists(CombinedVideoPath);
		if (ReuseExistingFrames)
		{
			CanDetect = videoOk;
		}
		else
		{
			CanDetect = videoOk
				&& !string.IsNullOrWhiteSpace(LrfFolderPath)
				&& Directory.Exists(LrfFolderPath)
				&& CombineEditMapIO.TryFindMapPath(CombinedVideoPath, out _)
				&& (string.IsNullOrWhiteSpace(ExistingFramesFolderPath)
					|| HalfInningDetector.TryResolveExistingFramesDirectory(ExistingFramesFolderPath, out _));
		}

		DetectCommand?.NotifyCanExecuteChanged();
		CanClear = !string.IsNullOrWhiteSpace(CombinedVideoPath)
			|| !string.IsNullOrWhiteSpace(LrfFolderPath)
			|| !string.IsNullOrWhiteSpace(ExistingFramesFolderPath)
			|| DetectedEventLines.Count > 0;
	}

	private void ReportStatus(string message)
	{
		var m = message ?? string.Empty;
		if (Dispatcher.UIThread.CheckAccess())
			OperationStatus = m;
		else
			Dispatcher.UIThread.Post(() => OperationStatus = m);
	}

	private async Task DetectAsync()
	{
		if (!CanDetect)
			return;

		var ct = BeginFfmpegOperation();
		DetectedEventLines.Clear();
		Progress = 0;
		ShowProgress = true;
		if (string.IsNullOrWhiteSpace(OutputJsonPath))
			RefreshFromInputs();

		var reuseFrames = ReuseExistingFrames;
		ReportStatus(reuseFrames
			? "Starting inning detection (reusing sample frames)…"
			: "Starting LRF inning detection…");

		try
		{
			InningDetectionResult result;
			if (reuseFrames)
			{
				// Reused frames are on the combined-video timeline (not LRF game-content),
				// so use combined-MP4 defaults (incl. intro title-card skip). Interval is
				// inferred from duration ÷ frame count inside DetectFromExistingFramesAsync.
				result = await HalfInningDetector.DetectFromExistingFramesAsync(
					CombinedVideoPath,
					ExistingFramesFolderPath,
					ct,
					log: Logger.Log,
					progress01: p =>
					{
						var value = Math.Clamp(p, 0, 1);
						if (Dispatcher.UIThread.CheckAccess())
							Progress = value;
						else
							Dispatcher.UIThread.Post(() => Progress = value);
					},
					options: new HalfInningDetector.Options(),
					outputJsonPath: OutputJsonPath,
					status: ReportStatus).ConfigureAwait(false);
			}
			else
			{
				var sampleInterval = UiBehaviorSettingsRuntime.LrfInningSampleIntervalSeconds;
				result = await LrfInningDetector.DetectAsync(
					CombinedVideoPath,
					LrfFolderPath,
					ct,
					log: Logger.Log,
					progress01: p =>
					{
						var value = Math.Clamp(p, 0, 1);
						if (Dispatcher.UIThread.CheckAccess())
							Progress = value;
						else
							Dispatcher.UIThread.Post(() => Progress = value);
					},
					options: new LrfInningDetector.Options
					{
						DetectorOptions = LrfInningDetector.Options.CreateDefaultDetectorOptions(sampleInterval),
					},
					outputJsonPath: OutputJsonPath,
					status: ReportStatus).ConfigureAwait(false);
			}

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				OutputJsonPath = result.OutputJsonPath ?? string.Empty;
				OutputYoutubePath = result.OutputYoutubeDescriptionPath ?? string.Empty;
				DetectedEventLines.Clear();
				foreach (var ev in result.Events)
				{
					var elapsed = TimeSpan.FromSeconds(Math.Floor(Math.Max(0, ev.ElapsedSeconds)));
					var stamp = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
					DetectedEventLines.Add($"{stamp}  {ev.Label}");
				}

				OperationStatus = string.IsNullOrWhiteSpace(result.ArtifactHandoffPath)
					? $"Done. {result.Events.Count} events → JSON + YouTube bookmarks (combined timeline)."
					: $"Done. {result.Events.Count} events. Cursor handoff: {result.ArtifactHandoffPath}";
				RefreshCanDetect();
			});

			if (!string.IsNullOrWhiteSpace(result.ArtifactHandoffPath))
				Logger.Log($"Inning-detect Cursor handoff: {result.ArtifactHandoffPath}");

			if (UiBehaviorSettingsRuntime.OpenOutputFolderOnComplete)
				FolderOpener.OpenContainingFolderIfExists(result.OutputJsonPath);
		}
		catch (OperationCanceledException)
		{
			ReportStatus("LRF inning detection cancelled.");
			Logger.Log("LRF inning detection cancelled.");
		}
		catch (Exception ex)
		{
			ReportStatus($"Failed: {ex.Message}");
			Logger.Log($"LRF inning detection failed: {ex.Message}");
		}
		finally
		{
			EndFfmpegOperation();
			await Dispatcher.UIThread.InvokeAsync(RefreshCanDetect);
		}
	}
}
