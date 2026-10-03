# Graph Report - MP4ToolsUniversal  (2026-10-03)

## Corpus Check
- 93 files · ~45,864 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1370 nodes · 2749 edges · 69 communities (59 shown, 10 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 85 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e87ec4cc`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- CombineFile
- .HasFilePayload
- .WriteCombineEditMapIfNeededAsync
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
- .InsertVideoAtFront
- .EstimateReplaceSegment
- IntroScreenReadResult
- RewriteIntroViewModel
- YoloOnnxDetector
- PendingIntroSpec
- CombineView
- OptionsViewModel
- ExportReplaceSegmentState
- ExportTrimState
- StartStopRange
- .WriteSessionAndYoutubeAsync
- MP4ToolsLib
- LogViewModel
- MP4ViewModelBase
- VolumePathHelper
- ModifiedRecordingUploader
- InningDetectorView
- .DetectAsync
- LrfInningDetectorViewModel
- ViewLocator
- CombineEditMap
- YesNoConfirmWindow
- Options
- UiBehaviorSettingsRuntime
- .BuildAvaloniaApp
- UserControl
- InningDetectorViewModel
- ExportPendingIntro
- EncodingSettingsDto
- CombineEditMapIO
- DefaultOutputPathRuntime
- OpenRouterSettingsRuntime
- .FormatHalfInning
- RewriteIntroView
- TrimView
- .EnsureDetectorModelAsync
- Phase
- .ScheduleEdgeDurationRefresh
- .Log
- .ImportReplaceFile
- LrfInningDetectorView
- ReplaceSegmentView
- FolderOpener.cs
- FileUtils
- .DetectCoreAsync
- IntroTitleCardClassifier.cs
- IntroFrameExtractor.cs
- .BuildFrameSignals
- InningDetectionResult
- export_baseballcv_onnx.sh

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 100 edges
2. `MP4ToolsLib` - 58 edges
3. `ReplaceSegmentViewModel` - 56 edges
4. `TrimViewModel` - 53 edges
5. `GenerateIntroViewModel` - 44 edges
6. `RewriteIntroViewModel` - 36 edges
7. `CombineView` - 32 edges
8. `Options` - 32 edges
9. `FFMpegUtils` - 30 edges
10. `OptionsViewModel` - 28 edges

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

## Communities (69 total, 10 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.06
Nodes (36): IProgress, CancellationToken, List, Task, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments (+28 more)

### Community 1 - "CombineFile"
Cohesion: 0.12
Nodes (10): PointerPressedEventArgs, IEnumerable, IReadOnlyList, List, CombineFile, Name, CombineFileSortMode, Creation (+2 more)

### Community 2 - ".HasFilePayload"
Cohesion: 0.14
Nodes (9): IDataTransfer, DragEventArgs, HashSet, IReadOnlyList, Task, FileDropHelper, DragEventArgs, DragEventArgs (+1 more)

### Community 3 - ".WriteCombineEditMapIfNeededAsync"
Cohesion: 0.21
Nodes (5): IReadOnlyDictionary, CancellationToken, JsonNode, List, Task

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
Cohesion: 0.10
Nodes (19): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+11 more)

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
Nodes (16): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.2), Avalonia.Desktop (12.1.2), Avalonia.Fonts.Inter (12.1.2), Avalonia.Themes.Simple (12.1.2) (+8 more)

### Community 14 - "MainWindowViewModel"
Cohesion: 0.10
Nodes (16): AiViewModel, InningDetectorViewModel, LrfInningDetectorViewModel, EditViewModel, GenerateIntroViewModel, ReplaceSegmentViewModel, RewriteIntroViewModel, MainWindowViewModel (+8 more)

### Community 15 - "GenerateIntroViewModel"
Cohesion: 0.05
Nodes (34): Bitmap, AsyncRelayCommand, CancellationToken, CancellationTokenSource, IReadOnlyList, LogOperationSource, RelayCommand, Task (+26 more)

### Community 17 - "TimeRange"
Cohesion: 0.10
Nodes (16): Guid, INotifyPropertyChanged, PropertyChangedEventArgs, ReplaceSegmentRange, EndSeconds, HasReplacement, Id, IsValid (+8 more)

### Community 18 - ".InsertVideoAtFront"
Cohesion: 0.19
Nodes (3): IEnumerable, IReadOnlyList, SelectionChangedEventArgs

### Community 19 - ".EstimateReplaceSegment"
Cohesion: 0.24
Nodes (8): EndSeconds, Estimate, IEnumerable, CheckResult, DiskSpaceEstimator, Estimate, IntroDurationSeconds, StartSeconds

### Community 20 - "IntroScreenReadResult"
Cohesion: 0.06
Nodes (28): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+20 more)

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.07
Nodes (22): AsyncRelayCommand, DateTime, IReadOnlyList, LogOperationSource, Task, RewriteIntroViewModel, CanReadIntro, CanRewrite (+14 more)

### Community 22 - "YoloOnnxDetector"
Cohesion: 0.11
Nodes (21): DenseTensor, IDisposable, InferenceSession, HashSet, IReadOnlyList, List, Mat, Rect (+13 more)

### Community 23 - "PendingIntroSpec"
Cohesion: 0.11
Nodes (15): PendingIntroSpec, BackgroundColor, Details, DetailsFontSize, DisplaySummary, DurationSeconds, HasContent, LineGap (+7 more)

### Community 24 - "CombineView"
Cohesion: 0.08
Nodes (18): DispatcherTimer, IPointer, KeyEventArgs, ListBoxItem, EventArgs, IReadOnlyList, List, RoutedEventArgs (+10 more)

### Community 25 - "OptionsViewModel"
Cohesion: 0.11
Nodes (8): CancellationTokenSource, EventArgs, RelayCommand, Task, OptionsViewModel, ApiKeyPasswordChar, OpenRouterApiKeyPasswordChar, SettingsFilePathDisplay

### Community 26 - "ExportReplaceSegmentState"
Cohesion: 0.11
Nodes (18): List, ExportReplaceSegmentRange, End, Label, PendingIntro, ReplaceWithText, Start, ExportReplaceSegmentState (+10 more)

### Community 27 - "ExportTrimState"
Cohesion: 0.29
Nodes (4): ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 28 - "StartStopRange"
Cohesion: 0.16
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 29 - ".WriteSessionAndYoutubeAsync"
Cohesion: 0.19
Nodes (10): JsonPath, Action, CancellationToken, DateTime, IReadOnlyList, JsonObject, JsonSerializerOptions, Task (+2 more)

### Community 30 - "MP4ToolsLib"
Cohesion: 0.13
Nodes (6): MP4Tools.ViewModels, MP4Tools.Services, MP4Tools.Views, MP4Tools, MP4ToolsLib, CombineEditMapEmbedder

### Community 31 - "LogViewModel"
Cohesion: 0.06
Nodes (28): ConcurrentQueue, LogOperationSource, Combine, GenerateIntro, InningDetector, LrfInningDetector, None, ReplaceSegment (+20 more)

### Community 32 - "MP4ViewModelBase"
Cohesion: 0.05
Nodes (27): CancellationTokenSource, EventArgs, Process, RelayCommand, Task, MP4ViewModelBase, CanClear, CanStop (+19 more)

### Community 34 - "ModifiedRecordingUploader"
Cohesion: 0.20
Nodes (7): Action, CancellationToken, JsonElement, Task, Uri, ModifiedRecordingUploader, UploadRevisionResult

### Community 35 - "InningDetectorView"
Cohesion: 0.40
Nodes (3): DragEventArgs, RoutedEventArgs, InningDetectorView

### Community 36 - ".DetectAsync"
Cohesion: 0.19
Nodes (10): Action, CancellationToken, List, Regex, Task, TimeSpan, FrameSignals, HalfInningDetector (+2 more)

### Community 37 - "LrfInningDetectorViewModel"
Cohesion: 0.19
Nodes (9): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, LrfInningDetectorViewModel, DetectCommand, DetectedEventLines, MatchedLrfLines (+1 more)

### Community 38 - "ViewLocator"
Cohesion: 0.33
Nodes (3): Control, IDataTemplate, ViewLocator

### Community 39 - "CombineEditMap"
Cohesion: 0.09
Nodes (22): DateTimeOffset, Path, IReadOnlyList, List, CombineEditMap, Clips, CombineMapPath, CreatedUtc (+14 more)

### Community 40 - "YesNoConfirmWindow"
Cohesion: 0.18
Nodes (6): Application, App, MainWindow, RoutedEventArgs, YesNoConfirmWindow, Window

### Community 41 - "Options"
Cohesion: 0.07
Nodes (30): Options, AnalysisWidth, AssumeTopFirstAtGameStart, BatterApproachRoi, BatterBoxRoi, ConfidenceThreshold, EmptyFieldHoldSeconds, FieldRoi (+22 more)

### Community 42 - "UiBehaviorSettingsRuntime"
Cohesion: 0.33
Nodes (4): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, OpenOutputFolderOnComplete, WarnOnInsufficientDiskSpace

### Community 43 - ".BuildAvaloniaApp"
Cohesion: 0.32
Nodes (4): AppBuilder, Program, STAThread, WaylandPlatformOptions

### Community 44 - "UserControl"
Cohesion: 0.20
Nodes (7): AiView, EditView, List, RoutedEventArgs, GenerateIntroView, OptionsView, UserControl

### Community 45 - "InningDetectorViewModel"
Cohesion: 0.19
Nodes (9): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, InningDetectorViewModel, DetectCommand, DetectedEventLines, OperationLogSource (+1 more)

### Community 46 - "ExportPendingIntro"
Cohesion: 0.18
Nodes (11): ExportPendingIntro, BackgroundColor, Details, DetailsFontSize, DurationSeconds, LineGap, Subtitle, SubtitleFontSize (+3 more)

### Community 47 - "EncodingSettingsDto"
Cohesion: 0.20
Nodes (8): EncodingSettingsRuntime, Current, EncodingSettingsDto, AudioCodec, HardwareAcceleration, TrimAudioCodec, UseResolveSafeEncoding, VideoCodec

### Community 48 - "CombineEditMapIO"
Cohesion: 0.31
Nodes (5): Action, CancellationToken, JsonSerializerOptions, Task, CombineEditMapIO

### Community 49 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 50 - "OpenRouterSettingsRuntime"
Cohesion: 0.40
Nodes (3): OpenRouterSettingsRuntime, ApiKey, Model

### Community 52 - "RewriteIntroView"
Cohesion: 0.53
Nodes (3): List, RoutedEventArgs, RewriteIntroView

### Community 53 - "TrimView"
Cohesion: 0.22
Nodes (6): DataFormat, DragEventArgs, List, PointerPressedEventArgs, RoutedEventArgs, TrimView

### Community 54 - ".EnsureDetectorModelAsync"
Cohesion: 0.28
Nodes (6): Action, CancellationToken, IEnumerable, Task, ResolvedYoloModel, YoloModelStore

### Community 55 - "Phase"
Cohesion: 0.50
Nodes (4): Phase, FieldClearing, Playing, Warmup

### Community 57 - ".Log"
Cohesion: 0.18
Nodes (7): CheckResult, Exception, Logger, Task, DiskSpaceWarning, CancellationToken, RemovalRange

### Community 59 - "LrfInningDetectorView"
Cohesion: 0.38
Nodes (3): DragEventArgs, RoutedEventArgs, LrfInningDetectorView

### Community 60 - "ReplaceSegmentView"
Cohesion: 0.48
Nodes (3): List, RoutedEventArgs, ReplaceSegmentView

### Community 63 - ".DetectCoreAsync"
Cohesion: 0.29
Nodes (9): Action, CancellationToken, IReadOnlyList, List, Task, LrfInningDetector, Options, DetectorOptions (+1 more)

### Community 66 - ".BuildFrameSignals"
Cohesion: 0.35
Nodes (5): FrameSignals, IReadOnlyList, Rect, DetectedObject, Rect2d

### Community 67 - "InningDetectionResult"
Cohesion: 0.20
Nodes (10): IReadOnlyList, InningDetectionEvent, ElapsedSeconds, Kind, Label, InningDetectionResult, Events, OutputJsonPath (+2 more)

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **355 isolated node(s):** `None`, `Combine`, `Trim`, `RewriteIntro`, `GenerateIntro` (+350 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 522 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `CombineFile`, `FFMpegUtils`, `RecordingJsonElapsedOffset`, `AppUserSettings`, `BoxScoreUploader`, `DrawTextPosition`, `GenerateIntroViewModel`, `TimeRange`, `.EstimateReplaceSegment`, `IntroScreenReadResult`, `YoloOnnxDetector`, `.WriteSessionAndYoutubeAsync`, `MP4ViewModelBase`, `VolumePathHelper`, `ModifiedRecordingUploader`, `.DetectAsync`, `CombineEditMap`, `EncodingSettingsDto`, `OpenRouterSettingsRuntime`, `.FormatHalfInning`, `.EnsureDetectorModelAsync`, `FileUtils`, `.DetectCoreAsync`, `IntroTitleCardClassifier.cs`, `IntroFrameExtractor.cs`, `InningDetectionResult`?**
  _High betweenness centrality (0.225) - this node is a cross-community bridge._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `CombineFile`, `.HasFilePayload`, `.WriteCombineEditMapIfNeededAsync`, `RecordingJsonElapsedOffset`, `ReplaceSegmentViewModel`, `InningDetectorViewModel`, `MainWindowViewModel`, `GenerateIntroViewModel`, `.OnPropertyChanged`, `TimeRange`, `.InsertVideoAtFront`, `CombineView`, `.ScheduleEdgeDurationRefresh`, `.Log`, `MP4ToolsLib`?**
  _High betweenness centrality (0.201) - this node is a cross-community bridge._
- **Why does `ReplaceSegmentViewModel` connect `ReplaceSegmentViewModel` to `MP4ViewModelBase`, `.ImportReplaceFile`, `CombineViewModel`, `InningDetectorViewModel`, `MainWindowViewModel`, `.OnPropertyChanged`, `TimeRange`, `PendingIntroSpec`, `.Log`, `ExportReplaceSegmentState`, `MP4ToolsLib`?**
  _High betweenness centrality (0.101) - this node is a cross-community bridge._
- **What connects `None`, `Combine`, `Trim` to the rest of the system?**
  _355 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.055713094245204334 - nodes in this community are weakly interconnected._
- **Should `CombineFile` be split into smaller, more focused modules?**
  _Cohesion score 0.11857707509881422 - nodes in this community are weakly interconnected._