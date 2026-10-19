# Graph Report - MP4ToolsUniversal  (2026-10-19)

## Corpus Check
- 97 files · ~55,141 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1535 nodes · 3105 edges · 83 communities (70 shown, 13 thin omitted)
- Extraction: 96% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 108 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `ae58ee8a`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- .InsertVideoAtFront
- .HasFilePayload
- .Log
- FFMpegUtils
- CombineViewModel
- RecordingJsonElapsedOffset
- TrimViewModel
- ReplaceSegmentViewModel
- AppUserSettings
- BoxScoreUploader
- Resolve MP4 Compatibility
- DrawTextPosition
- MP4Tools.csproj
- MainWindowViewModel
- GenerateIntroViewModel
- .OnPropertyChanged
- TimeRange
- CombineEditMap
- .GetTempPath
- IntroScreenReadResult
- RewriteIntroViewModel
- YoloOnnxDetector
- PendingIntroSpec
- CombineView
- OptionsViewModel
- ExportReplaceSegmentState
- EventInfoDefaultsWindow
- StartStopRange
- .WriteSessionAndYoutubeAsync
- MP4ToolsLib
- LogViewModel
- MP4ViewModelBase
- StreamProbeInfo
- ModifiedRecordingUploader
- InningDetectorView
- .DetectFromSamplesAsync
- .FindEarliestBatterStartInSamples
- MP4Tools.ViewModels
- .TrimAndCombineAsyncInternal
- LrfInningDetectorViewModel
- Options
- UiBehaviorSettingsRuntime
- MP4Tools
- UserControl
- .Build
- ExportPendingIntro
- .Apply
- InningDetectorViewModel
- BaseballLoggerSettingsRuntime
- InningDetectionArtifactManifest
- App.axaml.cs
- RewriteIntroView
- TrimView
- .EnsureDetectorModelAsync
- ExportTrimState
- .ImportReplaceFile
- .ScheduleEdgeDurationRefresh
- CombineEditMapIO
- LrfInningDetectorView
- ReplaceSegmentView
- GenerateIntroView
- RelayCommand
- .DetectWithResolvedLrfAsync
- HalfInningDetector
- YesNoConfirmWindow
- .ExtractAsync
- InningDetectionResult
- IReadOnlyList
- .BeginFfmpegOperation
- .PickRefineBatterRunStart
- DefaultOutputPathRuntime
- .ApplyFrom
- ArtifactSegmentInfo
- Phase
- InningDetectionRecordingWriter
- export_baseballcv_onnx.sh
- .RunAndLogFFMpegAsync
- .FormatHalfInning
- .BuildGameInfoOutputFileName
- .ApplyDefaultSkipRanges
- .OpenContainingFolderIfExists
- FfmpegProgressParser

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 112 edges
2. `MP4ToolsLib` - 62 edges
3. `ReplaceSegmentViewModel` - 56 edges
4. `TrimViewModel` - 53 edges
5. `Options` - 46 edges
6. `GenerateIntroViewModel` - 45 edges
7. `HalfInningDetector` - 38 edges
8. `RewriteIntroViewModel` - 36 edges
9. `InningDetectionArtifactManifest` - 35 edges
10. `InningDetectionArtifactSession` - 33 edges

## Surprising Connections (you probably didn't know these)
- `CombineViewModel` --references--> `CombineFile`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/CombineFile.cs
- `CombineViewModel` --references--> `CombineFileSortMode`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/CombineFile.cs
- `CombineViewModel` --references--> `TimeRange`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/TimeRange.cs
- `ReplaceSegmentViewModel` --references--> `TimeRange`  [EXTRACTED]
  MP4Tools/ViewModels/ReplaceSegmentViewModel.cs → MP4ToolsLib/TimeRange.cs
- `ExportTrimState` --references--> `StartStopRange`  [EXTRACTED]
  MP4Tools/ViewModels/TrimViewModel.cs → MP4ToolsLib/Ranges.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **DaVinci Resolve crash mux/timing factors in 1.mp4** — mp4tools_notes_1_vs_1_working_analysis_1_mp4, mp4tools_notes_1_vs_1_working_analysis_ctts_box, mp4tools_notes_1_vs_1_working_analysis_edit_list, mp4tools_notes_1_vs_1_working_analysis_open_gop, mp4tools_notes_1_vs_1_working_analysis_broken_mp4_timeline, mp4tools_notes_1_vs_1_working_analysis_davinci_resolve [EXTRACTED 1.00]
- **Resolve-safe re-encode remediation path** — mp4tools_notes_1_vs_1_working_analysis_libx265_reencode, mp4tools_notes_1_vs_1_working_analysis_1_working_mp4, mp4tools_notes_1_vs_1_working_analysis_ffmpeg, mp4tools_notes_1_vs_1_working_analysis_hevc_main_8bit [EXTRACTED 1.00]
- **1.mp4 vs 1_working.mp4 container contrast** — mp4tools_notes_1_vs_1_working_analysis_1_mp4, mp4tools_notes_1_vs_1_working_analysis_1_working_mp4, mp4tools_notes_1_vs_1_working_analysis_ctts_box, mp4tools_notes_1_vs_1_working_analysis_colr_box, mp4tools_notes_1_vs_1_working_analysis_hevc_main_10, mp4tools_notes_1_vs_1_working_analysis_hevc_main_8bit [EXTRACTED 1.00]

## Communities (83 total, 13 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.06
Nodes (31): IProgress, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments, FfmpegCommandLine, FfmpegOption, IsSkipped (+23 more)

### Community 1 - ".InsertVideoAtFront"
Cohesion: 0.10
Nodes (8): IEnumerable, IReadOnlyList, IEnumerable, IReadOnlyList, List, CombineFileSortMode, Creation, FileName

### Community 2 - ".HasFilePayload"
Cohesion: 0.16
Nodes (8): IDataTransfer, DragEventArgs, HashSet, IReadOnlyList, Task, FileDropHelper, DragEventArgs, DragEventArgs

### Community 3 - ".Log"
Cohesion: 0.24
Nodes (5): Exception, IReadOnlyDictionary, CancellationToken, JsonNode, Task

### Community 4 - "FFMpegUtils"
Cohesion: 0.14
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 5 - "CombineViewModel"
Cohesion: 0.05
Nodes (32): ConcurrentDictionary, AsyncRelayCommand, DateTime, LogOperationSource, ObservableCollection, RelayCommand, CombineViewModel, CanCombine (+24 more)

### Community 6 - "RecordingJsonElapsedOffset"
Cohesion: 0.17
Nodes (13): Elapsed, Action, CancellationToken, DateTime, HashSet, JsonNode, JsonObject, JsonSerializerOptions (+5 more)

### Community 7 - "TrimViewModel"
Cohesion: 0.07
Nodes (30): AsyncRelayCommand, IReadOnlyList, LogOperationSource, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand (+22 more)

### Community 8 - "ReplaceSegmentViewModel"
Cohesion: 0.07
Nodes (25): AsyncRelayCommand, IReadOnlyList, List, LogOperationSource, ObservableCollection, RelayCommand, TimeSpan, ReplaceSegmentViewModel (+17 more)

### Community 9 - "AppUserSettings"
Cohesion: 0.12
Nodes (18): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+10 more)

### Community 10 - "BoxScoreUploader"
Cohesion: 0.12
Nodes (16): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreSessionCreator, CreateBoxScoreSessionRequest (+8 more)

### Community 11 - "Resolve MP4 Compatibility"
Cohesion: 0.22
Nodes (16): 1.mp4, 1_working.mp4, B-frames, Broken MP4 timeline, colr box, ctts box, DaVinci Resolve, Edit list (+8 more)

### Community 12 - "DrawTextPosition"
Cohesion: 0.16
Nodes (12): IReadOnlyList, DrawTextPositionOption, DisplayName, Value, DrawTextUtils, DrawTextPositionOptions, DrawTextPositions, DrawTextPosition (+4 more)

### Community 13 - "MP4Tools.csproj"
Cohesion: 0.11
Nodes (16): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.4), Avalonia.Desktop (12.1.4), Avalonia.Fonts.Inter (12.1.4), Avalonia.Themes.Simple (12.1.4) (+8 more)

### Community 14 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (15): AiViewModel, InningDetectorViewModel, LrfInningDetectorViewModel, EditViewModel, ReplaceSegmentViewModel, RewriteIntroViewModel, MainWindowViewModel, AiViewModel (+7 more)

### Community 15 - "GenerateIntroViewModel"
Cohesion: 0.05
Nodes (34): Bitmap, AsyncRelayCommand, CancellationToken, CancellationTokenSource, IReadOnlyList, LogOperationSource, RelayCommand, Task (+26 more)

### Community 17 - "TimeRange"
Cohesion: 0.10
Nodes (16): Guid, INotifyPropertyChanged, PropertyChangedEventArgs, ReplaceSegmentRange, EndSeconds, HasReplacement, Id, IsValid (+8 more)

### Community 18 - "CombineEditMap"
Cohesion: 0.15
Nodes (12): DateTimeOffset, CombineEditMap, Clips, CombineMapPath, CreatedUtc, EndKeepSeconds, IntroApplied, OutputVideoPath (+4 more)

### Community 19 - ".GetTempPath"
Cohesion: 0.09
Nodes (15): CheckResult, EndSeconds, Estimate, Task, DiskSpaceWarning, IEnumerable, CheckResult, DiskSpaceEstimator (+7 more)

### Community 20 - "IntroScreenReadResult"
Cohesion: 0.06
Nodes (28): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+20 more)

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.08
Nodes (21): AsyncRelayCommand, DateTime, IReadOnlyList, LogOperationSource, RewriteIntroViewModel, CanReadIntro, CanRewrite, EventDate (+13 more)

### Community 22 - "YoloOnnxDetector"
Cohesion: 0.11
Nodes (21): DenseTensor, IDisposable, InferenceSession, HashSet, IReadOnlyList, List, Mat, Rect (+13 more)

### Community 23 - "PendingIntroSpec"
Cohesion: 0.11
Nodes (15): PendingIntroSpec, BackgroundColor, Details, DetailsFontSize, DisplaySummary, DurationSeconds, HasContent, LineGap (+7 more)

### Community 24 - "CombineView"
Cohesion: 0.10
Nodes (16): DispatcherTimer, IPointer, ListBoxItem, EventArgs, IReadOnlyList, List, PointerPressedEventArgs, RoutedEventArgs (+8 more)

### Community 25 - "OptionsViewModel"
Cohesion: 0.17
Nodes (5): CancellationTokenSource, OptionsViewModel, ApiKeyPasswordChar, OpenRouterApiKeyPasswordChar, SettingsFilePathDisplay

### Community 26 - "ExportReplaceSegmentState"
Cohesion: 0.11
Nodes (18): List, ExportReplaceSegmentRange, End, Label, PendingIntro, ReplaceWithText, Start, ExportReplaceSegmentState (+10 more)

### Community 27 - "EventInfoDefaultsWindow"
Cohesion: 0.23
Nodes (5): KeyEventArgs, RoutedEventArgs, SelectionChangedEventArgs, EventInfoDefaultsWindow, TappedEventArgs

### Community 28 - "StartStopRange"
Cohesion: 0.15
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 29 - ".WriteSessionAndYoutubeAsync"
Cohesion: 0.33
Nodes (6): JsonPath, YoutubeDescriptionPath, Action, CancellationToken, JsonObject, Task

### Community 30 - "MP4ToolsLib"
Cohesion: 0.12
Nodes (6): MP4Tools.Services, MP4ToolsLib, FileUtils, CombineEditMapEmbedder, IntroTitleCardClassifier, IntroFrameExtractor

### Community 31 - "LogViewModel"
Cohesion: 0.07
Nodes (24): ConcurrentQueue, LogOperationSource, Combine, GenerateIntro, InningDetector, LrfInningDetector, None, ReplaceSegment (+16 more)

### Community 32 - "MP4ViewModelBase"
Cohesion: 0.10
Nodes (12): EventArgs, RelayCommand, MP4ViewModelBase, CanClear, CanStop, ClearCommand, InputPath, InputProbeVideoCodecName (+4 more)

### Community 33 - "StreamProbeInfo"
Cohesion: 0.14
Nodes (12): StreamProbeInfo, BitDepth, ChannelLayout, Channels, CodecName, CodecType, FrameRate, Height (+4 more)

### Community 34 - "ModifiedRecordingUploader"
Cohesion: 0.20
Nodes (7): Action, CancellationToken, JsonElement, Task, Uri, ModifiedRecordingUploader, UploadRevisionResult

### Community 35 - "InningDetectorView"
Cohesion: 0.38
Nodes (3): DragEventArgs, RoutedEventArgs, InningDetectorView

### Community 36 - ".DetectFromSamplesAsync"
Cohesion: 0.08
Nodes (26): List, Action, CancellationToken, IReadOnlyList, JsonSerializerOptions, List, Options, Rect2d (+18 more)

### Community 37 - ".FindEarliestBatterStartInSamples"
Cohesion: 0.40
Nodes (3): TimedSampleFrame, Mat, Vec3b

### Community 39 - ".TrimAndCombineAsyncInternal"
Cohesion: 0.23
Nodes (3): CancellationToken, List, Task

### Community 40 - "LrfInningDetectorViewModel"
Cohesion: 0.17
Nodes (10): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, LrfInningDetectorViewModel, DetectCommand, DetectedEventLines, MatchedLrfLines (+2 more)

### Community 41 - "Options"
Cohesion: 0.05
Nodes (42): Options, AnalysisWidth, AssumeTopFirstAtGameStart, BatterApproachRoi, BatterBoxRoi, ConfidenceThreshold, EmptyFieldHoldSeconds, FieldRoi (+34 more)

### Community 42 - "UiBehaviorSettingsRuntime"
Cohesion: 0.29
Nodes (6): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, LrfInningSampleIntervalSeconds, OpenOutputFolderOnComplete, SaveInningDetectionArtifacts, WarnOnInsufficientDiskSpace

### Community 43 - "MP4Tools"
Cohesion: 0.22
Nodes (5): Control, MP4Tools, IDataTemplate, Logger, ViewLocator

### Community 44 - "UserControl"
Cohesion: 0.14
Nodes (7): AiView, EditView, EventArgs, LogView, OptionsView, UserControl, VisualTreeAttachmentEventArgs

### Community 45 - ".Build"
Cohesion: 0.17
Nodes (10): Path, IReadOnlyList, List, CombineEditMapClip, Index, SourceDurationSeconds, SourceFileName, SourcePath (+2 more)

### Community 46 - "ExportPendingIntro"
Cohesion: 0.18
Nodes (11): ExportPendingIntro, BackgroundColor, Details, DetailsFontSize, DurationSeconds, LineGap, Subtitle, SubtitleFontSize (+3 more)

### Community 47 - ".Apply"
Cohesion: 0.14
Nodes (11): EncodingSettingsRuntime, Current, OpenRouterSettingsRuntime, ApiKey, Model, EncodingSettingsDto, AudioCodec, HardwareAcceleration (+3 more)

### Community 48 - "InningDetectorViewModel"
Cohesion: 0.18
Nodes (9): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, InningDetectorViewModel, DetectCommand, DetectedEventLines, OperationLogSource (+1 more)

### Community 49 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 50 - "InningDetectionArtifactManifest"
Cohesion: 0.07
Nodes (27): DateTimeOffset, InningDetectionArtifactManifest, CombineMapCopyPath, CombineMapPath, CreatedLocal, DetectorOptionsPath, EventsCombinedPath, EventsGamePath (+19 more)

### Community 51 - "App.axaml.cs"
Cohesion: 0.17
Nodes (6): AppBuilder, Application, App, Program, MainWindow, STAThread

### Community 52 - "RewriteIntroView"
Cohesion: 0.53
Nodes (3): List, RoutedEventArgs, RewriteIntroView

### Community 53 - "TrimView"
Cohesion: 0.22
Nodes (6): DataFormat, DragEventArgs, List, PointerPressedEventArgs, RoutedEventArgs, TrimView

### Community 54 - ".EnsureDetectorModelAsync"
Cohesion: 0.28
Nodes (6): Action, CancellationToken, IEnumerable, Task, ResolvedYoloModel, YoloModelStore

### Community 55 - "ExportTrimState"
Cohesion: 0.33
Nodes (4): ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 58 - "CombineEditMapIO"
Cohesion: 0.36
Nodes (5): Action, CancellationToken, JsonSerializerOptions, Task, CombineEditMapIO

### Community 59 - "LrfInningDetectorView"
Cohesion: 0.36
Nodes (3): DragEventArgs, RoutedEventArgs, LrfInningDetectorView

### Community 60 - "ReplaceSegmentView"
Cohesion: 0.48
Nodes (3): List, RoutedEventArgs, ReplaceSegmentView

### Community 61 - "GenerateIntroView"
Cohesion: 0.31
Nodes (4): DragEventArgs, List, RoutedEventArgs, GenerateIntroView

### Community 63 - ".DetectWithResolvedLrfAsync"
Cohesion: 0.33
Nodes (6): Action, CancellationToken, IReadOnlyList, List, Task, LrfInningDetector

### Community 64 - "HalfInningDetector"
Cohesion: 0.21
Nodes (8): IRefineMediaSource, Action, CancellationToken, Task, TimeSpan, HalfInningDetector, IRefineMediaSource, TimedSampleFrame

### Community 65 - "YesNoConfirmWindow"
Cohesion: 0.40
Nodes (3): RoutedEventArgs, YesNoConfirmWindow, Window

### Community 66 - ".ExtractAsync"
Cohesion: 0.12
Nodes (16): IReadOnlyList, List, GameContentRefineMediaSource, GameContentSegment, GameEndSeconds, GameContentTimeline, Action, CancellationToken (+8 more)

### Community 67 - "InningDetectionResult"
Cohesion: 0.17
Nodes (12): IReadOnlyList, InningDetectionEvent, ElapsedSeconds, Kind, Label, InningDetectionResult, ArtifactHandoffPath, ArtifactSessionDirectory (+4 more)

### Community 68 - "IReadOnlyList"
Cohesion: 0.31
Nodes (7): FrameSignals, IReadOnlyList, Rect, Rect2d, FrameSignals, DetectedObject, Point

### Community 70 - ".PickRefineBatterRunStart"
Cohesion: 0.50
Nodes (3): Confidence, InBox, Time

### Community 71 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 73 - "ArtifactSegmentInfo"
Cohesion: 0.20
Nodes (8): ArtifactSegmentInfo, ClipIndex, ContributionSeconds, GameStartSeconds, LocalStartSeconds, MediaPath, InningDetectionArtifactRuntime, Enabled

### Community 74 - "Phase"
Cohesion: 0.50
Nodes (4): Phase, FieldClearing, Playing, Warmup

### Community 75 - "InningDetectionRecordingWriter"
Cohesion: 0.36
Nodes (4): DateTime, IReadOnlyList, JsonSerializerOptions, InningDetectionRecordingWriter

### Community 77 - ".RunAndLogFFMpegAsync"
Cohesion: 0.33
Nodes (4): Action, CancellationToken, Process, Task

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **419 isolated node(s):** `None`, `Combine`, `Trim`, `RewriteIntro`, `GenerateIntro` (+414 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 597 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **13 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `.InsertVideoAtFront`, `FFMpegUtils`, `RecordingJsonElapsedOffset`, `BoxScoreUploader`, `DrawTextPosition`, `GenerateIntroViewModel`, `TimeRange`, `.GetTempPath`, `IntroScreenReadResult`, `YoloOnnxDetector`, `StreamProbeInfo`, `ModifiedRecordingUploader`, `MP4Tools.ViewModels`, `.Build`, `.Apply`, `App.axaml.cs`, `.EnsureDetectorModelAsync`, `.DetectWithResolvedLrfAsync`, `HalfInningDetector`, `.ExtractAsync`, `InningDetectionResult`, `ArtifactSegmentInfo`, `InningDetectionRecordingWriter`, `.FormatHalfInning`, `FfmpegProgressParser`?**
  _High betweenness centrality (0.255) - this node is a cross-community bridge._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.InsertVideoAtFront`, `.HasFilePayload`, `.Log`, `ReplaceSegmentViewModel`, `MainWindowViewModel`, `.BuildGameInfoOutputFileName`, `.ApplyDefaultSkipRanges`, `.OnPropertyChanged`, `TimeRange`, `InningDetectorViewModel`, `GenerateIntroViewModel`, `CombineView`, `.ScheduleEdgeDurationRefresh`, `MP4ToolsLib`?**
  _High betweenness centrality (0.202) - this node is a cross-community bridge._
- **Why does `MP4Tools.ViewModels` connect `MP4Tools.ViewModels` to `MP4Tools`, `MainWindowViewModel`, `App.axaml.cs`, `PendingIntroSpec`, `ExportReplaceSegmentState`, `MP4ToolsLib`, `LogViewModel`?**
  _High betweenness centrality (0.073) - this node is a cross-community bridge._
- **What connects `None`, `Combine`, `Trim` to the rest of the system?**
  _419 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.061142217245240764 - nodes in this community are weakly interconnected._
- **Should `.InsertVideoAtFront` be split into smaller, more focused modules?**
  _Cohesion score 0.10317460317460317 - nodes in this community are weakly interconnected._