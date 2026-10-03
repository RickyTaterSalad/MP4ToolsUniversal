# Graph Report - MP4ToolsUniversal  (2026-10-03)

## Corpus Check
- 94 files · ~46,937 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1392 nodes · 2804 edges · 71 communities (60 shown, 10 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 87 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `ec3f09dc`
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
- DrawTextUtils
- MP4Tools.csproj
- MainWindowViewModel
- GenerateIntroViewModel
- .RebuildEndSkipRangesAndClamp
- TimeRange
- ReplaceSegmentRange
- .EstimateReplaceSegment
- BoxScoreUploader
- RewriteIntroViewModel
- .DetectAsync
- PendingIntroSpec
- CombineView
- OptionsViewModel
- ExportReplaceSegmentState
- GenerateIntroView
- StartStopRange
- .WriteSessionAndYoutubeAsync
- MP4ToolsLib
- LogViewModel
- MP4ViewModelBase
- StreamProbeInfo
- ModifiedRecordingUploader
- InningDetectorView
- MP4Tools.ViewModels
- LrfInningDetectorViewModel
- ViewLocator
- CombineEditMap
- IntroScreenReadResult
- Options
- UiBehaviorSettingsRuntime
- MP4Tools
- UserControl
- InningDetectorViewModel
- ExportPendingIntro
- .Apply
- .Build
- DefaultOutputPathRuntime
- .BeginFfmpegOperation
- RelayCommand
- RewriteIntroView
- TrimView
- .EnsureDetectorModelAsync
- ExportTrimState
- .InsertVideoAtFront
- .ApplyDefaultSkipRanges
- LrfInningDetectorView
- ReplaceSegmentView
- YesNoConfirmWindow
- BaseballLoggerSettingsRuntime
- .DetectCoreAsync
- .ApplyFrom
- .RunAndLogFFMpegAsync
- .ReadFileInfoAsync
- InningDetectionResult
- FfmpegProgressParser
- FolderOpener.cs
- export_baseballcv_onnx.sh

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 112 edges
2. `MP4ToolsLib` - 59 edges
3. `ReplaceSegmentViewModel` - 56 edges
4. `TrimViewModel` - 53 edges
5. `GenerateIntroViewModel` - 45 edges
6. `RewriteIntroViewModel` - 36 edges
7. `Options` - 35 edges
8. `CombineView` - 32 edges
9. `FFMpegUtils` - 30 edges
10. `OptionsViewModel` - 29 edges

## Surprising Connections (you probably didn't know these)
- `CombineViewModel` --references--> `CombineFile`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/CombineFile.cs
- `CombineViewModel` --references--> `CombineFileSortMode`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/CombineFile.cs
- `CombineViewModel` --references--> `TimeRange`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/TimeRange.cs
- `ReplaceSegmentRange` --references--> `TimeRange`  [EXTRACTED]
  MP4Tools/ViewModels/ReplaceSegmentRange.cs → MP4ToolsLib/TimeRange.cs
- `ReplaceSegmentViewModel` --references--> `TimeRange`  [EXTRACTED]
  MP4Tools/ViewModels/ReplaceSegmentViewModel.cs → MP4ToolsLib/TimeRange.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **DaVinci Resolve crash mux/timing factors in 1.mp4** — mp4tools_notes_1_vs_1_working_analysis_1_mp4, mp4tools_notes_1_vs_1_working_analysis_ctts_box, mp4tools_notes_1_vs_1_working_analysis_edit_list, mp4tools_notes_1_vs_1_working_analysis_open_gop, mp4tools_notes_1_vs_1_working_analysis_broken_mp4_timeline, mp4tools_notes_1_vs_1_working_analysis_davinci_resolve [EXTRACTED 1.00]
- **Resolve-safe re-encode remediation path** — mp4tools_notes_1_vs_1_working_analysis_libx265_reencode, mp4tools_notes_1_vs_1_working_analysis_1_working_mp4, mp4tools_notes_1_vs_1_working_analysis_ffmpeg, mp4tools_notes_1_vs_1_working_analysis_hevc_main_8bit [EXTRACTED 1.00]
- **1.mp4 vs 1_working.mp4 container contrast** — mp4tools_notes_1_vs_1_working_analysis_1_mp4, mp4tools_notes_1_vs_1_working_analysis_1_working_mp4, mp4tools_notes_1_vs_1_working_analysis_ctts_box, mp4tools_notes_1_vs_1_working_analysis_colr_box, mp4tools_notes_1_vs_1_working_analysis_hevc_main_10, mp4tools_notes_1_vs_1_working_analysis_hevc_main_8bit [EXTRACTED 1.00]

## Communities (71 total, 10 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.06
Nodes (34): IProgress, CancellationToken, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments, FfmpegCommandLine, FfmpegOption (+26 more)

### Community 1 - "CombineFile"
Cohesion: 0.13
Nodes (9): IEnumerable, IReadOnlyList, List, CombineFile, Name, CombineFileSortMode, Creation, FileName (+1 more)

### Community 2 - ".HasFilePayload"
Cohesion: 0.16
Nodes (8): IDataTransfer, DragEventArgs, HashSet, IReadOnlyList, Task, FileDropHelper, DragEventArgs, DragEventArgs

### Community 3 - ".Log"
Cohesion: 0.22
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
Cohesion: 0.06
Nodes (30): AsyncRelayCommand, IReadOnlyList, LogOperationSource, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand (+22 more)

### Community 8 - "ReplaceSegmentViewModel"
Cohesion: 0.07
Nodes (27): AsyncRelayCommand, IReadOnlyList, List, LogOperationSource, ObservableCollection, RelayCommand, Task, TimeSpan (+19 more)

### Community 9 - "AppUserSettings"
Cohesion: 0.12
Nodes (17): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+9 more)

### Community 10 - ".CreateAsync"
Cohesion: 0.21
Nodes (8): Action, CancellationToken, JsonElement, Task, Uri, BoxScoreSessionCreator, CreateBoxScoreSessionRequest, CreateBoxScoreSessionResult

### Community 11 - "Resolve MP4 Compatibility"
Cohesion: 0.22
Nodes (16): 1.mp4, 1_working.mp4, B-frames, Broken MP4 timeline, colr box, ctts box, DaVinci Resolve, Edit list (+8 more)

### Community 12 - "DrawTextUtils"
Cohesion: 0.24
Nodes (7): IReadOnlyList, DrawTextPositionOption, DisplayName, Value, DrawTextUtils, DrawTextPositionOptions, DrawTextPositions

### Community 13 - "MP4Tools.csproj"
Cohesion: 0.11
Nodes (16): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.3), Avalonia.Desktop (12.1.3), Avalonia.Fonts.Inter (12.1.3), Avalonia.Themes.Simple (12.1.3) (+8 more)

### Community 14 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (15): AiViewModel, InningDetectorViewModel, LrfInningDetectorViewModel, EditViewModel, ReplaceSegmentViewModel, RewriteIntroViewModel, MainWindowViewModel, AiViewModel (+7 more)

### Community 15 - "GenerateIntroViewModel"
Cohesion: 0.05
Nodes (34): Bitmap, AsyncRelayCommand, CancellationToken, CancellationTokenSource, IReadOnlyList, LogOperationSource, RelayCommand, Task (+26 more)

### Community 17 - "TimeRange"
Cohesion: 0.18
Nodes (7): INotifyPropertyChanged, IReadOnlyList, TimeRange, Hours, Minutes, Range, TotalSeconds

### Community 18 - "ReplaceSegmentRange"
Cohesion: 0.12
Nodes (9): Guid, PropertyChangedEventArgs, ReplaceSegmentRange, EndSeconds, HasReplacement, Id, IsValid, ReplacementStatus (+1 more)

### Community 19 - ".EstimateReplaceSegment"
Cohesion: 0.11
Nodes (11): CheckResult, EndSeconds, Estimate, Task, DiskSpaceWarning, IEnumerable, CheckResult, DiskSpaceEstimator (+3 more)

### Community 20 - "BoxScoreUploader"
Cohesion: 0.09
Nodes (25): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult (+17 more)

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.08
Nodes (21): AsyncRelayCommand, DateTime, IReadOnlyList, LogOperationSource, RewriteIntroViewModel, CanReadIntro, CanRewrite, EventDate (+13 more)

### Community 22 - ".DetectAsync"
Cohesion: 0.06
Nodes (43): DenseTensor, FrameSignals, IDisposable, InferenceSession, Action, CancellationToken, IReadOnlyList, List (+35 more)

### Community 23 - "PendingIntroSpec"
Cohesion: 0.11
Nodes (15): PendingIntroSpec, BackgroundColor, Details, DetailsFontSize, DisplaySummary, DurationSeconds, HasContent, LineGap (+7 more)

### Community 24 - "CombineView"
Cohesion: 0.07
Nodes (18): DispatcherTimer, IPointer, KeyEventArgs, ListBoxItem, EventArgs, IReadOnlyList, List, PointerPressedEventArgs (+10 more)

### Community 25 - "OptionsViewModel"
Cohesion: 0.18
Nodes (5): CancellationTokenSource, OptionsViewModel, ApiKeyPasswordChar, OpenRouterApiKeyPasswordChar, SettingsFilePathDisplay

### Community 26 - "ExportReplaceSegmentState"
Cohesion: 0.11
Nodes (18): List, ExportReplaceSegmentRange, End, Label, PendingIntro, ReplaceWithText, Start, ExportReplaceSegmentState (+10 more)

### Community 27 - "GenerateIntroView"
Cohesion: 0.31
Nodes (4): DragEventArgs, List, RoutedEventArgs, GenerateIntroView

### Community 28 - "StartStopRange"
Cohesion: 0.11
Nodes (14): DrawTextPosition, BottomLeft, BottomRight, TopLeft, TopRight, StartStopRange, EndRange, EndRangeDisplay (+6 more)

### Community 29 - ".WriteSessionAndYoutubeAsync"
Cohesion: 0.19
Nodes (10): JsonPath, Action, CancellationToken, DateTime, IReadOnlyList, JsonObject, JsonSerializerOptions, Task (+2 more)

### Community 30 - "MP4ToolsLib"
Cohesion: 0.10
Nodes (6): MP4Tools.Services, MP4ToolsLib, FileUtils, CombineEditMapEmbedder, IntroTitleCardClassifier, IntroFrameExtractor

### Community 31 - "LogViewModel"
Cohesion: 0.06
Nodes (28): ConcurrentQueue, LogOperationSource, Combine, GenerateIntro, InningDetector, LrfInningDetector, None, ReplaceSegment (+20 more)

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
Cohesion: 0.40
Nodes (3): DragEventArgs, RoutedEventArgs, InningDetectorView

### Community 37 - "LrfInningDetectorViewModel"
Cohesion: 0.19
Nodes (9): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, LrfInningDetectorViewModel, DetectCommand, DetectedEventLines, MatchedLrfLines (+1 more)

### Community 38 - "ViewLocator"
Cohesion: 0.33
Nodes (3): Control, IDataTemplate, ViewLocator

### Community 39 - "CombineEditMap"
Cohesion: 0.14
Nodes (13): DateTimeOffset, CombineEditMap, Clips, CombineMapPath, CreatedUtc, EndKeepSeconds, IntroApplied, IntroDurationSeconds (+5 more)

### Community 40 - "IntroScreenReadResult"
Cohesion: 0.17
Nodes (11): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+3 more)

### Community 41 - "Options"
Cohesion: 0.06
Nodes (33): Options, AnalysisWidth, AssumeTopFirstAtGameStart, BatterApproachRoi, BatterBoxRoi, ConfidenceThreshold, EmptyFieldHoldSeconds, FieldRoi (+25 more)

### Community 42 - "UiBehaviorSettingsRuntime"
Cohesion: 0.29
Nodes (5): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, LrfInningSampleIntervalSeconds, OpenOutputFolderOnComplete, WarnOnInsufficientDiskSpace

### Community 43 - "MP4Tools"
Cohesion: 0.15
Nodes (8): AppBuilder, Application, MP4Tools, App, Logger, Program, STAThread, WaylandPlatformOptions

### Community 44 - "UserControl"
Cohesion: 0.29
Nodes (4): AiView, EditView, OptionsView, UserControl

### Community 45 - "InningDetectorViewModel"
Cohesion: 0.19
Nodes (9): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, InningDetectorViewModel, DetectCommand, DetectedEventLines, OperationLogSource (+1 more)

### Community 46 - "ExportPendingIntro"
Cohesion: 0.18
Nodes (11): ExportPendingIntro, BackgroundColor, Details, DetailsFontSize, DurationSeconds, LineGap, Subtitle, SubtitleFontSize (+3 more)

### Community 47 - ".Apply"
Cohesion: 0.15
Nodes (11): EncodingSettingsRuntime, Current, OpenRouterSettingsRuntime, ApiKey, Model, EncodingSettingsDto, AudioCodec, HardwareAcceleration (+3 more)

### Community 48 - ".Build"
Cohesion: 0.13
Nodes (15): Path, Action, CancellationToken, IReadOnlyList, JsonSerializerOptions, List, Task, CombineEditMapClip (+7 more)

### Community 49 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

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
Cohesion: 0.22
Nodes (5): List, ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 56 - ".InsertVideoAtFront"
Cohesion: 0.12
Nodes (5): CancellationTokenSource, IEnumerable, IReadOnlyList, SelectionChangedEventArgs, NotifyCollectionChangedEventArgs

### Community 59 - "LrfInningDetectorView"
Cohesion: 0.38
Nodes (3): DragEventArgs, RoutedEventArgs, LrfInningDetectorView

### Community 60 - "ReplaceSegmentView"
Cohesion: 0.48
Nodes (3): List, RoutedEventArgs, ReplaceSegmentView

### Community 61 - "YesNoConfirmWindow"
Cohesion: 0.25
Nodes (4): MainWindow, RoutedEventArgs, YesNoConfirmWindow, Window

### Community 62 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 63 - ".DetectCoreAsync"
Cohesion: 0.29
Nodes (8): Action, CancellationToken, IReadOnlyList, List, Task, LrfInningDetector, Options, DetectorOptions

### Community 65 - ".RunAndLogFFMpegAsync"
Cohesion: 0.33
Nodes (4): Action, CancellationToken, Process, Task

### Community 67 - "InningDetectionResult"
Cohesion: 0.20
Nodes (10): IReadOnlyList, InningDetectionEvent, ElapsedSeconds, Kind, Label, InningDetectionResult, Events, OutputJsonPath (+2 more)

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **360 isolated node(s):** `None`, `Combine`, `Trim`, `RewriteIntro`, `GenerateIntro` (+355 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 527 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `CombineFile`, `FFMpegUtils`, `RecordingJsonElapsedOffset`, `.CreateAsync`, `DrawTextUtils`, `GenerateIntroViewModel`, `TimeRange`, `.EstimateReplaceSegment`, `BoxScoreUploader`, `.DetectAsync`, `StartStopRange`, `.WriteSessionAndYoutubeAsync`, `StreamProbeInfo`, `ModifiedRecordingUploader`, `MP4Tools.ViewModels`, `IntroScreenReadResult`, `MP4Tools`, `.Apply`, `.Build`, `.EnsureDetectorModelAsync`, `.DetectCoreAsync`, `InningDetectionResult`, `FfmpegProgressParser`?**
  _High betweenness centrality (0.236) - this node is a cross-community bridge._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `CombineFile`, `.HasFilePayload`, `.Log`, `ReplaceSegmentViewModel`, `InningDetectorViewModel`, `MainWindowViewModel`, `GenerateIntroViewModel`, `.RebuildEndSkipRangesAndClamp`, `TimeRange`, `CombineView`, `.InsertVideoAtFront`, `.ApplyDefaultSkipRanges`, `MP4ToolsLib`?**
  _High betweenness centrality (0.229) - this node is a cross-community bridge._
- **Why does `ReplaceSegmentViewModel` connect `ReplaceSegmentViewModel` to `MP4ViewModelBase`, `.ImportReplaceFile`, `CombineViewModel`, `InningDetectorViewModel`, `GenerateIntroViewModel`, `TimeRange`, `ReplaceSegmentRange`, `PendingIntroSpec`, `ExportReplaceSegmentState`, `MP4ToolsLib`?**
  _High betweenness centrality (0.098) - this node is a cross-community bridge._
- **What connects `None`, `Combine`, `Trim` to the rest of the system?**
  _360 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06118811881188119 - nodes in this community are weakly interconnected._
- **Should `CombineFile` be split into smaller, more focused modules?**
  _Cohesion score 0.12554112554112554 - nodes in this community are weakly interconnected._