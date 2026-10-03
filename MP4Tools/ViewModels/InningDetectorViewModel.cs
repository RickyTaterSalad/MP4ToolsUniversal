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

public partial class InningDetectorViewModel : MP4ViewModelBase
{
	protected override LogOperationSource OperationLogSource => LogOperationSource.InningDetector;

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
	private bool _skipIntroTitleCards = true;

	[ObservableProperty]
	private bool _canDetect;

	public ObservableCollection<string> DetectedEventLines { get; } = new();

	public AsyncRelayCommand DetectCommand { get; }

	public InningDetectorViewModel()
	{
		DetectCommand = new AsyncRelayCommand(DetectAsync, () => CanDetect);
	}

	protected override void OnInputPathSet()
	{
		base.OnInputPathSet();
		DetectedEventLines.Clear();
		Progress = 0;
		ShowProgress = false;
		PopulateOutputPathsFromInput();
		RefreshCanDetect();
		_ = ReadInputVideoBitDepthAsync();
	}

	private void PopulateOutputPathsFromInput()
	{
		if (string.IsNullOrWhiteSpace(InputPath) || !File.Exists(InputPath))
		{
			OutputJsonPath = string.Empty;
			OutputYoutubePath = string.Empty;
			return;
		}

		try
		{
			var (jsonPath, youtubePath) = InningDetectionRecordingWriter.BuildOutputPaths(InputPath);
			OutputJsonPath = jsonPath;
			OutputYoutubePath = youtubePath;
		}
		catch
		{
			OutputJsonPath = string.Empty;
			OutputYoutubePath = string.Empty;
		}
	}

	protected override Task Clear()
	{
		OperationStatus = string.Empty;
		OutputJsonPath = string.Empty;
		OutputYoutubePath = string.Empty;
		Progress = 0;
		ShowProgress = false;
		DetectedEventLines.Clear();
		return base.Clear();
	}

	private void RefreshCanDetect()
	{
		CanDetect = !string.IsNullOrWhiteSpace(InputPath) && File.Exists(InputPath);
		DetectCommand?.NotifyCanExecuteChanged();
		CanClear = !string.IsNullOrWhiteSpace(InputPath)
			|| !string.IsNullOrWhiteSpace(OutputJsonPath)
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
			PopulateOutputPathsFromInput();
		ReportStatus("Starting inning detection…");

		try
		{
			var result = await HalfInningDetector.DetectAsync(
				InputPath,
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
				options: new HalfInningDetector.Options
				{
					SkipIntroTitleCards = SkipIntroTitleCards,
				},
				outputJsonPath: OutputJsonPath,
				status: ReportStatus).ConfigureAwait(false);

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
					? $"Done. {result.Events.Count} events → JSON + YouTube bookmarks."
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
			ReportStatus("Inning detection cancelled.");
			Logger.Log("Inning detection cancelled.");
		}
		catch (Exception ex)
		{
			ReportStatus($"Failed: {ex.Message}");
			Logger.Log($"Inning detection failed: {ex.Message}");
		}
		finally
		{
			EndFfmpegOperation();
			await Dispatcher.UIThread.InvokeAsync(RefreshCanDetect);
		}
	}
}
