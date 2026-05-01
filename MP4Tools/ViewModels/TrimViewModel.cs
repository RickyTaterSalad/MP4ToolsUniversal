using Avalonia.Controls.Embedding.Offscreen;
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

	/// <summary>High-level trim/combine progress shown above the action buttons.</summary>
	[ObservableProperty]
	private string _operationStatus = string.Empty;

	private TimeSpan _inputDuration = TimeSpan.Zero;

	private void ReportTrimStep(string message)
	{
		var m = message ?? string.Empty;
		if (Dispatcher.UIThread.CheckAccess())
			OperationStatus = m;
		else
			Dispatcher.UIThread.Post(() => OperationStatus = m);
	}

	protected override Task Clear()
	{
		OperationStatus = string.Empty;
		return base.Clear();
	}

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
			var list = value == null ? new List<int>(TimeRange.Range) : new List<int>(value);
			SetProperty(ref _videoHourRange, list);
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
			var list = value == null ? new List<int>(TimeRange.Range) : new List<int>(value);
			SetProperty(ref _videMinuteRange, list);
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
			else if (!range.IsValidRange())
				Logger.Log("Add range: end time must be after start.");
			else if (_inputDuration <= TimeSpan.Zero)
				Logger.Log("Add range: video duration not loaded yet — wait for the input file to finish probing.");
			else
				Logger.Log($"Add range: selection exceeds clip duration (~{_inputDuration}).");
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
		try
		{
			await ReadInputVideoBitDepthAsync().ConfigureAwait(true);
			await ReadFileInfoAsync().ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[Trim OnInputPathSet] {ex}");
			Logger.Log($"Trim input load: {ex.Message}");
		}
	}

	private async Task TrimAndCombineAsync(bool combine = true)
	{
		var outFolder = DefaultOutputPathRuntime.Directory;
		if (!Directory.Exists(outFolder))
			Directory.CreateDirectory(outFolder);
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
		ReportTrimStep(combine ? "Starting trim and combine…" : "Starting trim…");
		try
		{
			await Task.Run(async () => await TrimAndCombineAsyncInternal(outTrimmedFolder, combine, ct), ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			Logger.Log("Trim cancelled.");
			ReportTrimStep("Cancelled.");
		}
		catch (Exception ex)
		{
			Logger.Log($"Error occurred while combining files: {ex.Message}");
			ReportTrimStep($"Failed: {ex.Message}");
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
			ReportTrimStep("Stopped: nothing to process.");
			return;
		}
		var validRanges = TrimRanges.Where(IsRangeWithinInputBounds).ToList();
		if (!validRanges.Any())
		{
			ReportTrimStep("Stopped: no valid time ranges.");
			return;
		}

		var trimPipelineSucceeded = true;
		Logger.Log("Starting trim and combine...");
		FfmpegUserHints.HardwareAcceleration = EncodingSettingsRuntime.Current.HardwareAcceleration ?? "auto";
		var hwDecodeMap = FFMpegUtils.Instance.ResolveHwAccelLineFromUserHints().Replace("\n", " ", StringComparison.Ordinal);
		Logger.Log($"Hardware acceleration: Encode tab=\"{FfmpegUserHints.HardwareAcceleration}\", ffmpeg decode hint=\"{hwDecodeMap}\"");
		CanTrim = false;
		var tempFilesToDelete = new List<string>();
		try
		{
			List<string> outputPaths = new List<string>();
			for (var ri = 0; ri < validRanges.Count; ri++)
			{
				var trim = validRanges[ri];
				ct.ThrowIfCancellationRequested();
				ReportTrimStep($"Trimming segment {ri + 1} of {validRanges.Count}…");
				Logger.Log($"Trimming range: {trim.StartRange} - {trim.EndRange}");
				outputPaths.Add(await TrimRangeAsync(trim, outTrimmedFolder, ct).ConfigureAwait(false));
			}

			if (outputPaths.Any(static p => string.IsNullOrWhiteSpace(p) || !File.Exists(p)))
			{
				trimPipelineSucceeded = false;
				Logger.Log("Trim failed: segment file(s) missing. FFmpeg steps above may show ** Exit Code ** — RunAndLog does not stop the pipeline when encode fails.");
				ReportTrimStep("Failed: trim did not produce all segment files.");
				ClearTimeRanges();
				foreach (var line in EncodeProcessingSummary.BuildLines(combine ? "Trim and combine" : "Trim", EncodingSettingsRuntime.Current, InputVideoBitDepth))
					Logger.Log(line);
				Logger.Log(combine ? "Trim and combine complete." : "Trim complete.");
				return;
			}

			if (!combine)
			{
				ClearTimeRanges();
				foreach (var line in EncodeProcessingSummary.BuildLines("Trim", EncodingSettingsRuntime.Current, InputVideoBitDepth))
					Logger.Log(line);
				Logger.Log("Trim complete.");
				ReportTrimStep("Trim finished successfully.");
				return;
			}

			var shouldAddIntro = !string.IsNullOrWhiteSpace(IntroTitle) || !string.IsNullOrWhiteSpace(IntroSubtitle) || !string.IsNullOrWhiteSpace(IntroDetails);
			if (shouldAddIntro && outputPaths.Count > 0)
			{
				ReportTrimStep("Adding intro…");
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
					ct: ct,
					encodingPrefs: EncodingSettingsRuntime.Current,
					operationStep: ReportTrimStep).ConfigureAwait(false);
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
				ReportTrimStep("Writing final file…");
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
					trimPipelineSucceeded = false;
					Debug.WriteLine($"[TrimAndCombineAsync][SingleOutputCopyError] {ex}");
					Logger.Log($"Copy Error: {ex.Message}");
					ReportTrimStep($"Failed: {ex.Message}");
				}
			}
			else if (outputPaths.Count > 1)
			{
				ReportTrimStep("Combining trimmed segments…");
				Logger.Log("Combining trimmed files...");

				var tempTsFiles = new List<string>();
				var videoCodec = await FFMpegUtils.Instance.GetFirstVideoCodecNameAsync(outputPaths.FirstOrDefault() ?? string.Empty, ct, Logger.Log).ConfigureAwait(false);
				var videoBsf = string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase) ? "hevc_mp4toannexb" : "h264_mp4toannexb";

				var audioCodecByPath = new Dictionary<string, string>();
				foreach (var path in outputPaths)
				{
					var ac = await FFMpegUtils.Instance.GetFirstAudioCodecNameAsync(path, ct, Logger.Log).ConfigureAwait(false);
					audioCodecByPath[path] = ac;
				}

				for (var ti = 0; ti < outputPaths.Count; ti++)
				{
					var input = outputPaths[ti];
					ct.ThrowIfCancellationRequested();
					ReportTrimStep($"Remuxing segment {ti + 1} of {outputPaths.Count} to MPEG-TS…");
					Logger.Log($"Remuxing trimmed segment to TS: {Path.GetFileName(input)}");
					var tsFile = Path.Combine(TempPathHelper.GetTempPath(), $"trim_part_{Guid.NewGuid():N}.ts");
					var aProbe = audioCodecByPath[input];
					if (MpegTsConcatAudio.ShouldTranscodeAudioMp4ToTs(aProbe))
						Logger.Log($"Opus in segment — re-encoding audio to AAC for MPEG-TS (reduces concat parsing errors).");
					var tsParts = new List<FfmpegOption?>
					{
						FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
						FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(input)),
						FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy),
						FfmpegOption.Pair(FfmpegArguments.VideoBitstreamFilter, videoBsf),
					};
					MpegTsConcatAudio.AppendMp4ToTsAudioOptions(tsParts, aProbe);
					tsParts.Add(FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs));
					tsParts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(tsFile)));
					await RunAndLogFFMpegAsync(FfmpegCommandLine.Build(tsParts), ct).ConfigureAwait(false);
					if (File.Exists(tsFile))
					{
						tempTsFiles.Add(tsFile);
					}
				}

				if (tempTsFiles.Count > 1)
				{
					ReportTrimStep("Concatenating MPEG-TS segments…");
					Logger.Log("Concatenating TS segments...");
					var concatInput = string.Join("|", tempTsFiles);
					var allAacInTs = outputPaths.All(p => MpegTsConcatAudio.IntermediateTsAudioIsAac(audioCodecByPath[p]));
					var aacBsfOpt = allAacInTs
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
			foreach (var line in EncodeProcessingSummary.BuildLines("Trim and combine", EncodingSettingsRuntime.Current, InputVideoBitDepth))
				Logger.Log(line);
			Logger.Log("Trim and combine complete.");
			if (combine && trimPipelineSucceeded)
				ReportTrimStep("Finished successfully.");
		}
		catch (OperationCanceledException)
		{
			Logger.Log("Trim cancelled.");
			throw;
		}
		catch (Exception ex)
		{
			ReportTrimStep($"Failed: {ex.Message}");
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
			var inputFullPath = Path.GetFullPath(InputPath);
			if (!File.Exists(inputFullPath))
			{
				Logger.Log($"Trim skipped: input not found: {inputFullPath}");
				return string.Empty;
			}

			if (!File.Exists(trimOutputPath))
			{
				TimeSpan endSpan = new TimeSpan(range.EndRange.Hours, range.EndRange.Minutes, range.EndRange.Seconds);
				TimeSpan startSpan = new TimeSpan(range.StartRange.Hours, range.StartRange.Minutes, range.StartRange.Seconds);
				var dif = endSpan - startSpan;
				var endString = $"{dif.Hours:00}:{dif.Minutes:00}:{dif.Seconds:00}";
				var quotedInput = FfmpegCommandLine.Quoted(inputFullPath);

				var encPrefs = EncodingSettingsRuntime.Current;
				var videoPlan = VideoEncodeSelector.BuildPlan(encPrefs, InputVideoBitDepth, Logger.Log);
				var audioEnc = VideoEncodeSelector.EffectiveAudioCodec(encPrefs);

				bool streamCopyOk = string.IsNullOrWhiteSpace(range.Label)
					&& !encPrefs.ReencodeOutput
					&& videoPlan.UseStreamCopy
					&& string.Equals(audioEnc, FfmpegArguments.StreamCopy, StringComparison.OrdinalIgnoreCase);

				string args;
				if (streamCopyOk)
				{
					args = FfmpegCommandLine.Build(
						FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, range.StartRange.AsInputParameterString()),
						FfmpegOption.Pair(FfmpegArguments.Input, quotedInput),
						FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, endString),
						FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy),
						FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, FfmpegArguments.StreamCopy),
						FfmpegOption.Positional(FfmpegCommandLine.Quoted(trimOutputPath)));
				}
				else
				{
					// drawtext / hwupload require decoding; cannot pair -vf with -c:v copy.
					var effectiveVideoPlan = videoPlan;
					if (!string.IsNullOrWhiteSpace(range.Label) && videoPlan.UseStreamCopy)
					{
						effectiveVideoPlan = VideoEncodeSelector.BuildIntroPlan(encPrefs, InputProbeVideoCodecName, Logger.Log);
						Logger.Log("Segment label uses drawtext — video is re-encoded for burn-in (stream copy not compatible with filters).");
					}

					FfmpegOption vfOpt = default;

					string drawInner = null;
					if (!string.IsNullOrWhiteSpace(range.Label))
					{
						drawInner = DrawTextUtils.CreateVideoOverlayText(range.Label, range.SelectedDrawTextPosition).Trim('"');
					}

					if (effectiveVideoPlan.NeedsVaapiUploadFilter)
					{
						vfOpt = string.IsNullOrEmpty(drawInner)
							? VideoEncodeSelector.VaapiUploadVideoFilterOption
							: FfmpegOption.Pair(FfmpegArguments.VideoFilter, $"\"{drawInner},{VideoEncodeSelector.VaapiUploadSuffix}\"");
					}
					else if (!string.IsNullOrEmpty(drawInner))
					{
						vfOpt = FfmpegOption.Pair(FfmpegArguments.VideoFilter, DrawTextUtils.CreateVideoOverlayText(range.Label, range.SelectedDrawTextPosition));
					}

					var encodingTail = new List<FfmpegOption?>();
					if (!effectiveVideoPlan.UseStreamCopy)
					{
						foreach (var o in effectiveVideoPlan.VideoEncodeOptions)
							encodingTail.Add(o);
					}
					else
					{
						encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy));
					}

					encodingTail.Add(FfmpegOption.Pair(FfmpegArguments.SelectAudioCodec, audioEnc));

					var trimParts = new List<FfmpegOption?>
					{
						FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, range.StartRange.AsInputParameterString()),
						FfmpegOption.Pair(FfmpegArguments.Input, quotedInput),
						FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, endString),
						vfOpt,
					};
					trimParts.AddRange(encodingTail);
					trimParts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(trimOutputPath)));

					args = FfmpegCommandLine.Build(trimParts);
				}

				await RunAndLogFFMpegAsync(args, ct).ConfigureAwait(false);
				if (!File.Exists(trimOutputPath))
				{
					Logger.Log($"Trim failed: expected output was not created: {trimOutputPath}");
					return string.Empty;
				}
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
			var durationStr = await FFMpegUtils.Instance.GetFileDurationAsync(InputPath, CancellationToken.None, Logger.Log)
				.ConfigureAwait(false);

			// FFprobe -sexagesimal matches Combine (TimeRange.FromString); TimeSpan.TryParse fails on H:MM:SS.xxx.
			var tr = string.IsNullOrWhiteSpace(durationStr) ? null : TimeRange.FromString(durationStr.Trim());
			if (tr == null || tr.TotalSeconds <= 0)
				return;

			var parsedSeconds = Math.Max(0d, tr.TotalSeconds - 1); // same 1s margin as before
			var parsedDuration = TimeSpan.FromSeconds(Math.Round(parsedSeconds));

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				_inputDuration = parsedDuration;
				CanClear = true;
				CanTrim = true;
				var hours = (int)_inputDuration.TotalHours;
				var min = _inputDuration.Minutes;
				var sec = _inputDuration.Seconds;
				VideoHourRange = [.. Enumerable.Range(0, hours + 1)];
				EndRange.Hours = hours;

				if (hours < 1)
					VideoMinuteRange = [.. Enumerable.Range(0, min + 1)];
				EndRange.Minutes = min;
				EndRange.Seconds = Math.Min(sec + 1, 59);
			});
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[ReadFileInfo][Error] {ex}");
			Logger.Log($"Read File Info Error: {ex.Message}");
		}
	}
}
