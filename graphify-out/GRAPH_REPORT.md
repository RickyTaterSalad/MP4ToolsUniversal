# Graph Report - MP4ToolsUniversal  (2026-10-03)

## Corpus Check
- 87 files · ~38,916 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1231 nodes · 2396 edges · 61 communities (50 shown, 11 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 80 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `0c743532`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- StreamProbeInfo
- .HasFilePayload
- .CreateBoxScoreSessionIfNeededAsync
- FFMpegUtils
- CombineViewModel
- RecordingJsonElapsedOffset
- TrimViewModel
- ReplaceSegmentViewModel
- AppUserSettings
- IntroScreenReadResult
- Resolve MP4 Compatibility
- DrawTextUtils
- MP4Tools.csproj
- MainWindowViewModel
- GenerateIntroViewModel
- .OnPropertyChanged
- TimeRange
- MP4Tools
- MP4ViewModelBase
- BoxScoreUploader
- RewriteIntroViewModel
- YoloPersonDetector
- PendingIntroSpec
- CombineView
- OptionsViewModel
- ExportReplaceSegmentState
- ExportTrimState
- StartStopRange
- .WriteSessionAndYoutubeAsync
- MP4ToolsLib
- LogViewModel
- EventInfoDefaultsWindow
- MP4Tools.ViewModels
- .Log
- CombineFile
- .DetectAsync
- .ImportReplaceFile
- RelayCommand
- InningDetectionResult
- InningDetectorViewModel
- Options
- UiBehaviorSettingsRuntime
- ViewLocator
- UserControl
- ModifiedRecordingUploader
- ExportPendingIntro
- .Apply
- .ApplyAsync
- BaseballLoggerSettingsRuntime
- DefaultOutputPathRuntime
- .FormatHalfInning
- YesNoConfirmWindow
- .IsSolidTextCard
- .BuildGameInfoOutputFileName
- Phase
- .ApplyFrom
- .SetFile
- .CreateAsync
- .ReadInputVideoBitDepthAsync
- .ScheduleEdgeDurationRefresh

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 98 edges
2. `ReplaceSegmentViewModel` - 56 edges
3. `TrimViewModel` - 53 edges
4. `MP4ToolsLib` - 53 edges
5. `GenerateIntroViewModel` - 44 edges
6. `RewriteIntroViewModel` - 36 edges
7. `CombineView` - 32 edges
8. `FFMpegUtils` - 30 edges
9. `OptionsViewModel` - 28 edges
10. `MP4ViewModelBase` - 27 edges

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

## Communities (61 total, 11 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.07
Nodes (31): IProgress, CancellationToken, List, Task, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments (+23 more)

### Community 1 - "StreamProbeInfo"
Cohesion: 0.17
Nodes (11): StreamProbeInfo, BitDepth, ChannelLayout, Channels, CodecName, CodecType, FrameRate, Height (+3 more)

### Community 2 - ".HasFilePayload"
Cohesion: 0.05
Nodes (27): DataFormat, IDataTransfer, DragEventArgs, HashSet, IReadOnlyList, Task, FileDropHelper, DragEventArgs (+19 more)

### Community 3 - ".CreateBoxScoreSessionIfNeededAsync"
Cohesion: 0.23
Nodes (4): CancellationToken, JsonNode, List, Task

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
Nodes (25): AsyncRelayCommand, IReadOnlyList, List, LogOperationSource, ObservableCollection, RelayCommand, TimeSpan, ReplaceSegmentViewModel (+17 more)

### Community 9 - "AppUserSettings"
Cohesion: 0.13
Nodes (16): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+8 more)

### Community 10 - "IntroScreenReadResult"
Cohesion: 0.06
Nodes (28): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+20 more)

### Community 11 - "Resolve MP4 Compatibility"
Cohesion: 0.22
Nodes (16): 1.mp4, 1_working.mp4, B-frames, Broken MP4 timeline, colr box, ctts box, DaVinci Resolve, Edit list (+8 more)

### Community 12 - "DrawTextUtils"
Cohesion: 0.24
Nodes (7): IReadOnlyList, DrawTextPositionOption, DisplayName, Value, DrawTextUtils, DrawTextPositionOptions, DrawTextPositions

### Community 13 - "MP4Tools.csproj"
Cohesion: 0.11
Nodes (16): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.2), Avalonia.Desktop (12.1.2), Avalonia.Fonts.Inter (12.1.2), Avalonia.Themes.Simple (12.1.2) (+8 more)

### Community 14 - "MainWindowViewModel"
Cohesion: 0.11
Nodes (15): AiViewModel, InningDetectorViewModel, EditViewModel, GenerateIntroViewModel, ReplaceSegmentViewModel, RewriteIntroViewModel, MainWindowViewModel, AiViewModel (+7 more)

### Community 15 - "GenerateIntroViewModel"
Cohesion: 0.05
Nodes (34): Bitmap, AsyncRelayCommand, CancellationToken, CancellationTokenSource, IReadOnlyList, LogOperationSource, RelayCommand, Task (+26 more)

### Community 17 - "TimeRange"
Cohesion: 0.10
Nodes (16): Guid, INotifyPropertyChanged, PropertyChangedEventArgs, ReplaceSegmentRange, EndSeconds, HasReplacement, Id, IsValid (+8 more)

### Community 18 - "MP4Tools"
Cohesion: 0.19
Nodes (7): AppBuilder, Application, MP4Tools, App, Program, STAThread, WaylandPlatformOptions

### Community 19 - "MP4ViewModelBase"
Cohesion: 0.10
Nodes (14): CancellationToken, CancellationTokenSource, EventArgs, Process, MP4ViewModelBase, CanClear, CanStop, ClearCommand (+6 more)

### Community 20 - "BoxScoreUploader"
Cohesion: 0.14
Nodes (13): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult (+5 more)

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.08
Nodes (22): AsyncRelayCommand, DateTime, IReadOnlyList, LogOperationSource, Task, RewriteIntroViewModel, CanReadIntro, CanRewrite (+14 more)

### Community 22 - "YoloPersonDetector"
Cohesion: 0.16
Nodes (14): DenseTensor, IDisposable, InferenceSession, IReadOnlyList, List, Mat, Vec3b, DetectedPerson (+6 more)

### Community 23 - "PendingIntroSpec"
Cohesion: 0.11
Nodes (15): PendingIntroSpec, BackgroundColor, Details, DetailsFontSize, DisplaySummary, DurationSeconds, HasContent, LineGap (+7 more)

### Community 24 - "CombineView"
Cohesion: 0.11
Nodes (14): DispatcherTimer, IPointer, ListBoxItem, EventArgs, IReadOnlyList, List, PointerPressedEventArgs, RoutedEventArgs (+6 more)

### Community 25 - "OptionsViewModel"
Cohesion: 0.19
Nodes (5): CancellationTokenSource, OptionsViewModel, ApiKeyPasswordChar, OpenRouterApiKeyPasswordChar, SettingsFilePathDisplay

### Community 26 - "ExportReplaceSegmentState"
Cohesion: 0.11
Nodes (18): List, ExportReplaceSegmentRange, End, Label, PendingIntro, ReplaceWithText, Start, ExportReplaceSegmentState (+10 more)

### Community 27 - "ExportTrimState"
Cohesion: 0.33
Nodes (4): ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 28 - "StartStopRange"
Cohesion: 0.12
Nodes (14): DrawTextPosition, BottomLeft, BottomRight, TopLeft, TopRight, StartStopRange, EndRange, EndRangeDisplay (+6 more)

### Community 29 - ".WriteSessionAndYoutubeAsync"
Cohesion: 0.19
Nodes (10): JsonPath, Action, CancellationToken, DateTime, IReadOnlyList, JsonObject, JsonSerializerOptions, Task (+2 more)

### Community 30 - "MP4ToolsLib"
Cohesion: 0.12
Nodes (5): MP4Tools.Services, MP4ToolsLib, FolderOpener, FileUtils, IntroFrameExtractor

### Community 31 - "LogViewModel"
Cohesion: 0.07
Nodes (23): LogOperationSource, Combine, GenerateIntro, InningDetector, None, ReplaceSegment, RewriteIntro, Trim (+15 more)

### Community 32 - "EventInfoDefaultsWindow"
Cohesion: 0.23
Nodes (5): KeyEventArgs, RoutedEventArgs, SelectionChangedEventArgs, EventInfoDefaultsWindow, TappedEventArgs

### Community 35 - "CombineFile"
Cohesion: 0.11
Nodes (12): IEnumerable, IReadOnlyList, SelectionChangedEventArgs, IEnumerable, IReadOnlyList, List, CombineFile, Name (+4 more)

### Community 36 - ".DetectAsync"
Cohesion: 0.24
Nodes (7): Action, CancellationToken, IReadOnlyList, List, Task, HalfInningDetector, Options

### Community 39 - "InningDetectionResult"
Cohesion: 0.18
Nodes (11): IReadOnlyList, InningDetectionEvent, ElapsedSeconds, Kind, Label, InningDetectionResult, DurationSeconds, Events (+3 more)

### Community 40 - "InningDetectorViewModel"
Cohesion: 0.19
Nodes (9): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, InningDetectorViewModel, DetectCommand, DetectedEventLines, OperationLogSource (+1 more)

### Community 41 - "Options"
Cohesion: 0.13
Nodes (15): Options, AnalysisWidth, BatterApproachRoi, BatterBoxRoi, ConfidenceThreshold, EmptyFieldHoldSeconds, FieldRoi, IntroConfirmSamples (+7 more)

### Community 42 - "UiBehaviorSettingsRuntime"
Cohesion: 0.33
Nodes (4): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, OpenOutputFolderOnComplete, WarnOnInsufficientDiskSpace

### Community 43 - "ViewLocator"
Cohesion: 0.33
Nodes (3): Control, IDataTemplate, ViewLocator

### Community 44 - "UserControl"
Cohesion: 0.14
Nodes (7): AiView, EditView, EventArgs, LogView, OptionsView, UserControl, VisualTreeAttachmentEventArgs

### Community 45 - "ModifiedRecordingUploader"
Cohesion: 0.20
Nodes (7): Action, CancellationToken, JsonElement, Task, Uri, ModifiedRecordingUploader, UploadRevisionResult

### Community 46 - "ExportPendingIntro"
Cohesion: 0.18
Nodes (11): ExportPendingIntro, BackgroundColor, Details, DetailsFontSize, DurationSeconds, LineGap, Subtitle, SubtitleFontSize (+3 more)

### Community 47 - ".Apply"
Cohesion: 0.15
Nodes (11): EncodingSettingsRuntime, Current, OpenRouterSettingsRuntime, ApiKey, Model, EncodingSettingsDto, AudioCodec, HardwareAcceleration (+3 more)

### Community 48 - ".ApplyAsync"
Cohesion: 0.08
Nodes (16): CheckResult, ConcurrentQueue, EndSeconds, Estimate, IntroDurationSeconds, Task, DiskSpaceWarning, IEnumerable (+8 more)

### Community 49 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 50 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 52 - "YesNoConfirmWindow"
Cohesion: 0.25
Nodes (4): MainWindow, RoutedEventArgs, YesNoConfirmWindow, Window

### Community 53 - ".IsSolidTextCard"
Cohesion: 0.40
Nodes (3): Mat, Vec3b, IntroTitleCardClassifier

### Community 55 - "Phase"
Cohesion: 0.50
Nodes (4): Phase, FieldClearing, Playing, Warmup

### Community 59 - ".CreateAsync"
Cohesion: 0.21
Nodes (8): Action, CancellationToken, JsonElement, Task, Uri, BoxScoreSessionCreator, CreateBoxScoreSessionRequest, CreateBoxScoreSessionResult

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **314 isolated node(s):** `None`, `Combine`, `Trim`, `RewriteIntro`, `GenerateIntro` (+309 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 472 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `.HasFilePayload`, `.CreateBoxScoreSessionIfNeededAsync`, `CombineFile`, `RecordingJsonElapsedOffset`, `InningDetectorViewModel`, `ReplaceSegmentViewModel`, `MainWindowViewModel`, `GenerateIntroViewModel`, `.OnPropertyChanged`, `.ApplyAsync`, `TimeRange`, `BoxScoreUploader`, `.BuildGameInfoOutputFileName`, `CombineView`, `.ScheduleEdgeDurationRefresh`, `MP4ToolsLib`?**
  _High betweenness centrality (0.237) - this node is a cross-community bridge._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `StreamProbeInfo`, `FFMpegUtils`, `RecordingJsonElapsedOffset`, `IntroScreenReadResult`, `DrawTextUtils`, `GenerateIntroViewModel`, `TimeRange`, `MP4Tools`, `BoxScoreUploader`, `YoloPersonDetector`, `StartStopRange`, `.WriteSessionAndYoutubeAsync`, `MP4Tools.ViewModels`, `CombineFile`, `.DetectAsync`, `InningDetectionResult`, `ModifiedRecordingUploader`, `.Apply`, `.ApplyAsync`, `.FormatHalfInning`, `.IsSolidTextCard`, `.CreateAsync`, `.ReadInputVideoBitDepthAsync`?**
  _High betweenness centrality (0.215) - this node is a cross-community bridge._
- **Why does `ReplaceSegmentViewModel` connect `ReplaceSegmentViewModel` to `CombineViewModel`, `.ImportReplaceFile`, `InningDetectorViewModel`, `MainWindowViewModel`, `.ApplyAsync`, `TimeRange`, `.OnPropertyChanged`, `PendingIntroSpec`, `.SetFile`, `ExportReplaceSegmentState`, `MP4ToolsLib`?**
  _High betweenness centrality (0.136) - this node is a cross-community bridge._
- **What connects `None`, `Combine`, `Trim` to the rest of the system?**
  _314 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06841448189762797 - nodes in this community are weakly interconnected._
- **Should `.HasFilePayload` be split into smaller, more focused modules?**
  _Cohesion score 0.053551912568306013 - nodes in this community are weakly interconnected._