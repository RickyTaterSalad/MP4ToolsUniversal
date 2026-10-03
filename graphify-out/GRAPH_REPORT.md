# Graph Report - MP4ToolsUniversal  (2026-10-03)

## Corpus Check
- 97 files · ~51,202 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1512 nodes · 3042 edges · 79 communities (65 shown, 13 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 107 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `6b611b68`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- CombineFile
- .HasFilePayload
- .Log
- FFMpegUtils
- CombineViewModel
- RecordingJsonElapsedOffset
- TrimViewModel
- ReplaceSegmentViewModel
- AppUserSettings
- .CreateAsync
- Resolve MP4 Compatibility
- DrawTextPosition
- MP4Tools.csproj
- MainWindowViewModel
- GenerateIntroViewModel
- .OnPropertyChanged
- TimeRange
- .GetTempPath
- .EstimateReplaceSegment
- BoxScoreUploader
- RewriteIntroViewModel
- .DetectFromSamplesAsync
- PendingIntroSpec
- CombineView
- OptionsViewModel
- ExportReplaceSegmentState
- VolumePathHelper
- StartStopRange
- .WriteSessionAndYoutubeAsync
- MP4ToolsLib
- LogViewModel
- MP4ViewModelBase
- StreamProbeInfo
- ModifiedRecordingUploader
- InningDetectorView
- InningDetectionArtifactSession
- LrfInningDetectorViewModel
- MP4Tools
- CombineEditMap
- ArtifactSegmentInfo
- Options
- UiBehaviorSettingsRuntime
- .BuildAvaloniaApp
- UserControl
- RelayCommand
- ExportPendingIntro
- .Apply
- App
- BaseballLoggerSettingsRuntime
- InningDetectionArtifactManifest
- ViewModelBase
- RewriteIntroView
- TrimView
- .EnsureDetectorModelAsync
- ExportTrimState
- .ImportReplaceFile
- .ScheduleEdgeDurationRefresh
- LogView
- LrfInningDetectorView
- ReplaceSegmentView
- EventInfoDefaultsWindow
- DefaultOutputPathRuntime
- .DetectCoreAsync
- .TrimAndCombineAsyncInternal
- YesNoConfirmWindow
- .ExtractAsync
- InningDetectionResult
- EditViewModel
- IntroScreenReadResult
- .OnInputPathSet
- .ApplyFrom
- CombineEditMapEmbedder.cs
- .ReadIntroAsync
- .Combine
- export_baseballcv_onnx.sh
- IntroTitleCardClassifier.cs
- IntroFrameExtractor.cs

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 112 edges
2. `MP4ToolsLib` - 62 edges
3. `ReplaceSegmentViewModel` - 56 edges
4. `TrimViewModel` - 53 edges
5. `GenerateIntroViewModel` - 45 edges
6. `RewriteIntroViewModel` - 36 edges
7. `Options` - 36 edges
8. `InningDetectionArtifactManifest` - 35 edges
9. `InningDetectionArtifactSession` - 33 edges
10. `CombineView` - 32 edges

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

## Communities (79 total, 13 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.06
Nodes (31): IProgress, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments, FfmpegCommandLine, FfmpegOption, IsSkipped (+23 more)

### Community 1 - "CombineFile"
Cohesion: 0.11
Nodes (11): IEnumerable, IReadOnlyList, SelectionChangedEventArgs, IEnumerable, IReadOnlyList, List, CombineFile, Name (+3 more)

### Community 2 - ".HasFilePayload"
Cohesion: 0.16
Nodes (8): IDataTransfer, HashSet, IReadOnlyList, Task, FileDropHelper, DragEventArgs, DragEventArgs, DragEventArgs

### Community 3 - ".Log"
Cohesion: 0.23
Nodes (6): Exception, IReadOnlyDictionary, CancellationToken, JsonNode, List, Task

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

### Community 10 - ".CreateAsync"
Cohesion: 0.21
Nodes (8): Action, CancellationToken, JsonElement, Task, Uri, BoxScoreSessionCreator, CreateBoxScoreSessionRequest, CreateBoxScoreSessionResult

### Community 11 - "Resolve MP4 Compatibility"
Cohesion: 0.22
Nodes (16): 1.mp4, 1_working.mp4, B-frames, Broken MP4 timeline, colr box, ctts box, DaVinci Resolve, Edit list (+8 more)

### Community 12 - "DrawTextPosition"
Cohesion: 0.16
Nodes (12): IReadOnlyList, DrawTextPositionOption, DisplayName, Value, DrawTextUtils, DrawTextPositionOptions, DrawTextPositions, DrawTextPosition (+4 more)

### Community 13 - "MP4Tools.csproj"
Cohesion: 0.11
Nodes (16): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.3), Avalonia.Desktop (12.1.3), Avalonia.Fonts.Inter (12.1.3), Avalonia.Themes.Simple (12.1.3) (+8 more)

### Community 14 - "MainWindowViewModel"
Cohesion: 0.25
Nodes (7): MainWindowViewModel, AiViewModel, CombineViewModel, EditViewModel, LogViewModel, OptionsViewModel, TrimViewModel

### Community 15 - "GenerateIntroViewModel"
Cohesion: 0.05
Nodes (34): Bitmap, AsyncRelayCommand, CancellationToken, CancellationTokenSource, IReadOnlyList, LogOperationSource, RelayCommand, Task (+26 more)

### Community 17 - "TimeRange"
Cohesion: 0.10
Nodes (16): Guid, INotifyPropertyChanged, PropertyChangedEventArgs, ReplaceSegmentRange, EndSeconds, HasReplacement, Id, IsValid (+8 more)

### Community 18 - ".GetTempPath"
Cohesion: 0.30
Nodes (3): RemovalRange, Action, TempPathHelper

### Community 19 - ".EstimateReplaceSegment"
Cohesion: 0.27
Nodes (7): EndSeconds, Estimate, IEnumerable, CheckResult, DiskSpaceEstimator, Estimate, StartSeconds

### Community 20 - "BoxScoreUploader"
Cohesion: 0.09
Nodes (25): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult (+17 more)

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.08
Nodes (21): AsyncRelayCommand, DateTime, IReadOnlyList, LogOperationSource, RewriteIntroViewModel, CanReadIntro, CanRewrite, EventDate (+13 more)

### Community 22 - ".DetectFromSamplesAsync"
Cohesion: 0.06
Nodes (47): DenseTensor, FrameSignals, IDisposable, InferenceSession, IRefineMediaSource, Action, CancellationToken, IReadOnlyList (+39 more)

### Community 23 - "PendingIntroSpec"
Cohesion: 0.11
Nodes (15): PendingIntroSpec, BackgroundColor, Details, DetailsFontSize, DisplaySummary, DurationSeconds, HasContent, LineGap (+7 more)

### Community 24 - "CombineView"
Cohesion: 0.08
Nodes (15): DispatcherTimer, IPointer, ListBoxItem, RelayCommand, DragEventArgs, EventArgs, IReadOnlyList, List (+7 more)

### Community 25 - "OptionsViewModel"
Cohesion: 0.17
Nodes (5): CancellationTokenSource, OptionsViewModel, ApiKeyPasswordChar, OpenRouterApiKeyPasswordChar, SettingsFilePathDisplay

### Community 26 - "ExportReplaceSegmentState"
Cohesion: 0.11
Nodes (18): List, ExportReplaceSegmentRange, End, Label, PendingIntro, ReplaceWithText, Start, ExportReplaceSegmentState (+10 more)

### Community 28 - "StartStopRange"
Cohesion: 0.16
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 29 - ".WriteSessionAndYoutubeAsync"
Cohesion: 0.19
Nodes (10): JsonPath, YoutubeDescriptionPath, Action, CancellationToken, DateTime, IReadOnlyList, JsonObject, JsonSerializerOptions (+2 more)

### Community 30 - "MP4ToolsLib"
Cohesion: 0.15
Nodes (4): MP4Tools.ViewModels, MP4Tools.Services, MP4Tools.Views, MP4ToolsLib

### Community 31 - "LogViewModel"
Cohesion: 0.07
Nodes (24): ConcurrentQueue, LogOperationSource, Combine, GenerateIntro, InningDetector, LrfInningDetector, None, ReplaceSegment (+16 more)

### Community 32 - "MP4ViewModelBase"
Cohesion: 0.10
Nodes (15): Action, CancellationToken, EventArgs, Process, Task, MP4ViewModelBase, CanClear, CanStop (+7 more)

### Community 33 - "StreamProbeInfo"
Cohesion: 0.14
Nodes (12): StreamProbeInfo, BitDepth, ChannelLayout, Channels, CodecName, CodecType, FrameRate, Height (+4 more)

### Community 34 - "ModifiedRecordingUploader"
Cohesion: 0.20
Nodes (7): Action, CancellationToken, JsonElement, Task, Uri, ModifiedRecordingUploader, UploadRevisionResult

### Community 35 - "InningDetectorView"
Cohesion: 0.40
Nodes (3): DragEventArgs, RoutedEventArgs, InningDetectorView

### Community 36 - "InningDetectionArtifactSession"
Cohesion: 0.08
Nodes (22): Action, CancellationToken, IReadOnlyList, JsonSerializerOptions, List, Rect2d, Task, InningDetectionArtifactSession (+14 more)

### Community 37 - "LrfInningDetectorViewModel"
Cohesion: 0.07
Nodes (24): FolderOpener, AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, InningDetectorViewModel, DetectCommand, DetectedEventLines (+16 more)

### Community 38 - "MP4Tools"
Cohesion: 0.22
Nodes (5): Control, MP4Tools, IDataTemplate, Logger, ViewLocator

### Community 39 - "CombineEditMap"
Cohesion: 0.09
Nodes (23): Path, DateTimeOffset, IReadOnlyList, List, CombineEditMap, Clips, CombineMapPath, CreatedUtc (+15 more)

### Community 40 - "ArtifactSegmentInfo"
Cohesion: 0.20
Nodes (8): ArtifactSegmentInfo, ClipIndex, ContributionSeconds, GameStartSeconds, LocalStartSeconds, MediaPath, InningDetectionArtifactRuntime, Enabled

### Community 41 - "Options"
Cohesion: 0.06
Nodes (33): Options, AnalysisWidth, AssumeTopFirstAtGameStart, BatterApproachRoi, BatterBoxRoi, ConfidenceThreshold, EmptyFieldHoldSeconds, FieldRoi (+25 more)

### Community 42 - "UiBehaviorSettingsRuntime"
Cohesion: 0.29
Nodes (6): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, LrfInningSampleIntervalSeconds, OpenOutputFolderOnComplete, SaveInningDetectionArtifacts, WarnOnInsufficientDiskSpace

### Community 43 - ".BuildAvaloniaApp"
Cohesion: 0.32
Nodes (4): AppBuilder, Program, STAThread, WaylandPlatformOptions

### Community 44 - "UserControl"
Cohesion: 0.20
Nodes (7): AiView, EditView, List, RoutedEventArgs, GenerateIntroView, OptionsView, UserControl

### Community 46 - "ExportPendingIntro"
Cohesion: 0.18
Nodes (11): ExportPendingIntro, BackgroundColor, Details, DetailsFontSize, DurationSeconds, LineGap, Subtitle, SubtitleFontSize (+3 more)

### Community 47 - ".Apply"
Cohesion: 0.14
Nodes (11): EncodingSettingsRuntime, Current, OpenRouterSettingsRuntime, ApiKey, Model, EncodingSettingsDto, AudioCodec, HardwareAcceleration (+3 more)

### Community 48 - "App"
Cohesion: 0.29
Nodes (4): Application, App, MainWindow, Window

### Community 49 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 50 - "InningDetectionArtifactManifest"
Cohesion: 0.07
Nodes (27): DateTimeOffset, InningDetectionArtifactManifest, CombineMapCopyPath, CombineMapPath, CreatedLocal, DetectorOptionsPath, EventsCombinedPath, EventsGamePath (+19 more)

### Community 51 - "ViewModelBase"
Cohesion: 0.29
Nodes (5): AiViewModel, InningDetectorViewModel, LrfInningDetectorViewModel, ViewModelBase, ObservableObject

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

### Community 58 - "LogView"
Cohesion: 0.29
Nodes (3): EventArgs, LogView, VisualTreeAttachmentEventArgs

### Community 59 - "LrfInningDetectorView"
Cohesion: 0.38
Nodes (3): DragEventArgs, RoutedEventArgs, LrfInningDetectorView

### Community 60 - "ReplaceSegmentView"
Cohesion: 0.48
Nodes (3): List, RoutedEventArgs, ReplaceSegmentView

### Community 61 - "EventInfoDefaultsWindow"
Cohesion: 0.23
Nodes (5): KeyEventArgs, RoutedEventArgs, SelectionChangedEventArgs, EventInfoDefaultsWindow, TappedEventArgs

### Community 62 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 63 - ".DetectCoreAsync"
Cohesion: 0.27
Nodes (8): Action, CancellationToken, IReadOnlyList, List, Task, LrfInningDetector, Options, DetectorOptions

### Community 64 - ".TrimAndCombineAsyncInternal"
Cohesion: 0.19
Nodes (3): CancellationToken, List, Task

### Community 66 - ".ExtractAsync"
Cohesion: 0.10
Nodes (18): Regex, FfmpegProgressParser, IReadOnlyList, List, GameContentRefineMediaSource, GameContentSegment, GameEndSeconds, GameContentTimeline (+10 more)

### Community 67 - "InningDetectionResult"
Cohesion: 0.17
Nodes (12): IReadOnlyList, InningDetectionEvent, ElapsedSeconds, Kind, Label, InningDetectionResult, ArtifactHandoffPath, ArtifactSessionDirectory (+4 more)

### Community 68 - "EditViewModel"
Cohesion: 0.50
Nodes (3): EditViewModel, ReplaceSegmentViewModel, RewriteIntroViewModel

### Community 69 - "IntroScreenReadResult"
Cohesion: 0.17
Nodes (11): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+3 more)

### Community 75 - ".Combine"
Cohesion: 0.21
Nodes (4): CheckResult, Task, DiskSpaceWarning, CancellationTokenSource

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **409 isolated node(s):** `None`, `Combine`, `Trim`, `RewriteIntro`, `GenerateIntro` (+404 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 586 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **13 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `CombineFile`, `FFMpegUtils`, `RecordingJsonElapsedOffset`, `.CreateAsync`, `DrawTextPosition`, `GenerateIntroViewModel`, `TimeRange`, `.GetTempPath`, `.EstimateReplaceSegment`, `BoxScoreUploader`, `.DetectFromSamplesAsync`, `VolumePathHelper`, `.WriteSessionAndYoutubeAsync`, `StreamProbeInfo`, `ModifiedRecordingUploader`, `CombineEditMap`, `ArtifactSegmentInfo`, `.Apply`, `.EnsureDetectorModelAsync`, `.DetectCoreAsync`, `.ExtractAsync`, `InningDetectionResult`, `IntroScreenReadResult`, `.OnInputPathSet`, `CombineEditMapEmbedder.cs`, `IntroTitleCardClassifier.cs`, `IntroFrameExtractor.cs`?**
  _High betweenness centrality (0.234) - this node is a cross-community bridge._
- **Why does `CombineViewModel` connect `CombineViewModel` to `CombineFile`, `.Log`, `LrfInningDetectorViewModel`, `.ApplyDefaultSkipRanges`, `ReplaceSegmentViewModel`, `.Combine`, `MainWindowViewModel`, `GenerateIntroViewModel`, `.OnPropertyChanged`, `TimeRange`, `CombineView`, `.ScheduleEdgeDurationRefresh`, `MP4ToolsLib`?**
  _High betweenness centrality (0.210) - this node is a cross-community bridge._
- **Why does `ReplaceSegmentViewModel` connect `ReplaceSegmentViewModel` to `.ImportReplaceFile`, `CombineViewModel`, `LrfInningDetectorViewModel`, `GenerateIntroViewModel`, `.OnPropertyChanged`, `TimeRange`, `.GetTempPath`, `PendingIntroSpec`, `CombineView`, `ExportReplaceSegmentState`, `MP4ToolsLib`?**
  _High betweenness centrality (0.082) - this node is a cross-community bridge._
- **What connects `None`, `Combine`, `Trim` to the rest of the system?**
  _409 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.060087719298245613 - nodes in this community are weakly interconnected._
- **Should `CombineFile` be split into smaller, more focused modules?**
  _Cohesion score 0.11494252873563218 - nodes in this community are weakly interconnected._