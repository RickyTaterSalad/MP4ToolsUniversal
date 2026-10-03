# Graph Report - MP4ToolsUniversal  (2026-10-03)

## Corpus Check
- 97 files · ~55,001 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1537 nodes · 3108 edges · 75 communities (66 shown, 7 thin omitted)
- Extraction: 96% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 108 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `45a1b173`
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
- BoxScoreUploader
- Resolve MP4 Compatibility
- DrawTextPosition
- MP4Tools.csproj
- MainWindowViewModel
- GenerateIntroViewModel
- .RebuildEndSkipRangesAndClamp
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
- ReplaceSegmentRange
- StartStopRange
- .WriteSessionAndYoutubeAsync
- MP4ToolsLib
- LogViewModel
- MP4ViewModelBase
- StreamProbeInfo
- ModifiedRecordingUploader
- InningDetectorView
- InningDetectionArtifactSession
- .DetectFromSamplesAsync
- MP4Tools.ViewModels
- .TrimAndCombineAsyncInternal
- LrfInningDetectorViewModel
- Options
- UiBehaviorSettingsRuntime
- MP4Tools
- UserControl
- .SetFile
- ExportPendingIntro
- .Apply
- InningDetectorViewModel
- BaseballLoggerSettingsRuntime
- InningDetectionArtifactManifest
- .BuildAvaloniaApp
- RewriteIntroView
- TrimView
- .EnsureDetectorModelAsync
- ExportTrimState
- .ScheduleEdgeDurationRefresh
- CombineEditMapIO
- LrfInningDetectorView
- ReplaceSegmentView
- GenerateIntroView
- RelayCommand
- .DetectCoreAsync
- HalfInningDetector
- YesNoConfirmWindow
- .ExtractAsync
- InningDetectionResult
- IReadOnlyList
- .RefineHalfInningStartAsync
- DefaultOutputPathRuntime
- .ApplyFrom
- Phase
- export_baseballcv_onnx.sh

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

## Communities (75 total, 7 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.06
Nodes (31): IProgress, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments, FfmpegCommandLine, FfmpegOption, IsSkipped (+23 more)

### Community 1 - "CombineFile"
Cohesion: 0.11
Nodes (11): IEnumerable, IReadOnlyList, SelectionChangedEventArgs, IEnumerable, IReadOnlyList, List, CombineFile, Name (+3 more)

### Community 2 - ".HasFilePayload"
Cohesion: 0.20
Nodes (7): IDataTransfer, HashSet, IReadOnlyList, Task, FileDropHelper, DragEventArgs, DragEventArgs

### Community 3 - ".Log"
Cohesion: 0.24
Nodes (5): Exception, IReadOnlyDictionary, CancellationToken, JsonNode, Task

### Community 4 - "FFMpegUtils"
Cohesion: 0.14
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 5 - "CombineViewModel"
Cohesion: 0.05
Nodes (33): ConcurrentDictionary, AsyncRelayCommand, DateTime, List, LogOperationSource, ObservableCollection, RelayCommand, CombineViewModel (+25 more)

### Community 6 - "RecordingJsonElapsedOffset"
Cohesion: 0.14
Nodes (15): Elapsed, DateTime, FileUtils, Action, CancellationToken, DateTime, HashSet, JsonNode (+7 more)

### Community 7 - "TrimViewModel"
Cohesion: 0.07
Nodes (30): AsyncRelayCommand, IReadOnlyList, LogOperationSource, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand (+22 more)

### Community 8 - "ReplaceSegmentViewModel"
Cohesion: 0.07
Nodes (26): AsyncRelayCommand, IReadOnlyList, List, LogOperationSource, ObservableCollection, RelayCommand, Task, TimeSpan (+18 more)

### Community 9 - "AppUserSettings"
Cohesion: 0.11
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

### Community 18 - "CombineEditMap"
Cohesion: 0.09
Nodes (23): Path, DateTimeOffset, IReadOnlyList, List, CombineEditMap, Clips, CombineMapPath, CreatedUtc (+15 more)

### Community 19 - ".GetTempPath"
Cohesion: 0.09
Nodes (14): CheckResult, EndSeconds, Estimate, Task, DiskSpaceWarning, IEnumerable, CheckResult, DiskSpaceEstimator (+6 more)

### Community 20 - "IntroScreenReadResult"
Cohesion: 0.06
Nodes (28): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+20 more)

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.08
Nodes (22): AsyncRelayCommand, DateTime, IReadOnlyList, LogOperationSource, Task, RewriteIntroViewModel, CanReadIntro, CanRewrite (+14 more)

### Community 22 - "YoloOnnxDetector"
Cohesion: 0.11
Nodes (21): DenseTensor, IDisposable, InferenceSession, HashSet, IReadOnlyList, List, Mat, Rect (+13 more)

### Community 23 - "PendingIntroSpec"
Cohesion: 0.11
Nodes (15): PendingIntroSpec, BackgroundColor, Details, DetailsFontSize, DisplaySummary, DurationSeconds, HasContent, LineGap (+7 more)

### Community 24 - "CombineView"
Cohesion: 0.07
Nodes (18): DispatcherTimer, IPointer, KeyEventArgs, ListBoxItem, EventArgs, IReadOnlyList, List, PointerPressedEventArgs (+10 more)

### Community 25 - "OptionsViewModel"
Cohesion: 0.17
Nodes (5): CancellationTokenSource, OptionsViewModel, ApiKeyPasswordChar, OpenRouterApiKeyPasswordChar, SettingsFilePathDisplay

### Community 26 - "ExportReplaceSegmentState"
Cohesion: 0.11
Nodes (18): List, ExportReplaceSegmentRange, End, Label, PendingIntro, ReplaceWithText, Start, ExportReplaceSegmentState (+10 more)

### Community 27 - "ReplaceSegmentRange"
Cohesion: 0.15
Nodes (9): Guid, PropertyChangedEventArgs, ReplaceSegmentRange, EndSeconds, HasReplacement, Id, IsValid, ReplacementStatus (+1 more)

### Community 28 - "StartStopRange"
Cohesion: 0.16
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 29 - ".WriteSessionAndYoutubeAsync"
Cohesion: 0.19
Nodes (10): JsonPath, YoutubeDescriptionPath, Action, CancellationToken, DateTime, IReadOnlyList, JsonObject, JsonSerializerOptions (+2 more)

### Community 30 - "MP4ToolsLib"
Cohesion: 0.14
Nodes (5): MP4Tools.Services, MP4ToolsLib, CombineEditMapEmbedder, IntroTitleCardClassifier, IntroFrameExtractor

### Community 31 - "LogViewModel"
Cohesion: 0.06
Nodes (27): ConcurrentQueue, LogOperationSource, Combine, GenerateIntro, InningDetector, LrfInningDetector, None, ReplaceSegment (+19 more)

### Community 32 - "MP4ViewModelBase"
Cohesion: 0.10
Nodes (16): Action, CancellationToken, CancellationTokenSource, EventArgs, Process, Task, MP4ViewModelBase, CanClear (+8 more)

### Community 33 - "StreamProbeInfo"
Cohesion: 0.14
Nodes (12): StreamProbeInfo, BitDepth, ChannelLayout, Channels, CodecName, CodecType, FrameRate, Height (+4 more)

### Community 34 - "ModifiedRecordingUploader"
Cohesion: 0.20
Nodes (7): Action, CancellationToken, JsonElement, Task, Uri, ModifiedRecordingUploader, UploadRevisionResult

### Community 35 - "InningDetectorView"
Cohesion: 0.38
Nodes (3): DragEventArgs, RoutedEventArgs, InningDetectorView

### Community 36 - "InningDetectionArtifactSession"
Cohesion: 0.09
Nodes (21): Action, CancellationToken, IReadOnlyList, JsonSerializerOptions, Rect2d, Task, InningDetectionArtifactSession, CombineMapCopyPath (+13 more)

### Community 37 - ".DetectFromSamplesAsync"
Cohesion: 0.21
Nodes (6): List, TimedSampleFrame, IReadOnlyList, InningHalfLabels, Mat, Vec3b

### Community 39 - ".TrimAndCombineAsyncInternal"
Cohesion: 0.19
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
Cohesion: 0.20
Nodes (5): Control, MP4Tools, IDataTemplate, Logger, ViewLocator

### Community 44 - "UserControl"
Cohesion: 0.29
Nodes (4): AiView, EditView, OptionsView, UserControl

### Community 46 - "ExportPendingIntro"
Cohesion: 0.18
Nodes (11): ExportPendingIntro, BackgroundColor, Details, DetailsFontSize, DurationSeconds, LineGap, Subtitle, SubtitleFontSize (+3 more)

### Community 47 - ".Apply"
Cohesion: 0.14
Nodes (11): EncodingSettingsRuntime, Current, OpenRouterSettingsRuntime, ApiKey, Model, EncodingSettingsDto, AudioCodec, HardwareAcceleration (+3 more)

### Community 48 - "InningDetectorViewModel"
Cohesion: 0.14
Nodes (10): FolderOpener, AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, InningDetectorViewModel, DetectCommand, DetectedEventLines (+2 more)

### Community 49 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 50 - "InningDetectionArtifactManifest"
Cohesion: 0.06
Nodes (36): DateTimeOffset, List, ArtifactSegmentInfo, ClipIndex, ContributionSeconds, GameStartSeconds, LocalStartSeconds, MediaPath (+28 more)

### Community 51 - ".BuildAvaloniaApp"
Cohesion: 0.32
Nodes (4): AppBuilder, Program, STAThread, WaylandPlatformOptions

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
Cohesion: 0.31
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

### Community 63 - ".DetectCoreAsync"
Cohesion: 0.25
Nodes (9): Options, Action, CancellationToken, IReadOnlyList, List, Task, LrfInningDetector, Options (+1 more)

### Community 64 - "HalfInningDetector"
Cohesion: 0.26
Nodes (6): Action, CancellationToken, Task, TimeSpan, HalfInningDetector, TimedSampleFrame

### Community 65 - "YesNoConfirmWindow"
Cohesion: 0.18
Nodes (6): Application, App, MainWindow, RoutedEventArgs, YesNoConfirmWindow, Window

### Community 66 - ".ExtractAsync"
Cohesion: 0.10
Nodes (18): Regex, FfmpegProgressParser, IReadOnlyList, List, GameContentRefineMediaSource, GameContentSegment, GameEndSeconds, GameContentTimeline (+10 more)

### Community 67 - "InningDetectionResult"
Cohesion: 0.17
Nodes (12): IReadOnlyList, InningDetectionEvent, ElapsedSeconds, Kind, Label, InningDetectionResult, ArtifactHandoffPath, ArtifactSessionDirectory (+4 more)

### Community 68 - "IReadOnlyList"
Cohesion: 0.31
Nodes (7): FrameSignals, IReadOnlyList, Rect, Rect2d, FrameSignals, DetectedObject, Point

### Community 70 - ".RefineHalfInningStartAsync"
Cohesion: 0.25
Nodes (5): Confidence, InBox, IRefineMediaSource, IRefineMediaSource, Time

### Community 71 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 74 - "Phase"
Cohesion: 0.50
Nodes (4): Phase, FieldClearing, Playing, Warmup

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **419 isolated node(s):** `None`, `Combine`, `Trim`, `RewriteIntro`, `GenerateIntro` (+414 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 598 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `CombineFile`, `FFMpegUtils`, `RecordingJsonElapsedOffset`, `BoxScoreUploader`, `DrawTextPosition`, `GenerateIntroViewModel`, `TimeRange`, `CombineEditMap`, `.GetTempPath`, `IntroScreenReadResult`, `YoloOnnxDetector`, `.WriteSessionAndYoutubeAsync`, `StreamProbeInfo`, `ModifiedRecordingUploader`, `.DetectFromSamplesAsync`, `MP4Tools.ViewModels`, `MP4Tools`, `.Apply`, `InningDetectionArtifactManifest`, `.EnsureDetectorModelAsync`, `.DetectCoreAsync`, `HalfInningDetector`, `.ExtractAsync`, `InningDetectionResult`?**
  _High betweenness centrality (0.254) - this node is a cross-community bridge._
- **Why does `CombineViewModel` connect `CombineViewModel` to `CombineFile`, `.Log`, `.SetCombineProgress`, `ReplaceSegmentViewModel`, `.SetFile`, `MainWindowViewModel`, `GenerateIntroViewModel`, `.RebuildEndSkipRangesAndClamp`, `TimeRange`, `InningDetectorViewModel`, `CombineView`, `.ScheduleEdgeDurationRefresh`, `MP4ToolsLib`?**
  _High betweenness centrality (0.206) - this node is a cross-community bridge._
- **Why does `ReplaceSegmentViewModel` connect `ReplaceSegmentViewModel` to `CombineViewModel`, `.SetFile`, `GenerateIntroViewModel`, `InningDetectorViewModel`, `TimeRange`, `.GetTempPath`, `PendingIntroSpec`, `.ImportReplaceFile`, `ExportReplaceSegmentState`, `ReplaceSegmentRange`, `MP4ToolsLib`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **What connects `None`, `Combine`, `Trim` to the rest of the system?**
  _419 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06091825307950728 - nodes in this community are weakly interconnected._
- **Should `CombineFile` be split into smaller, more focused modules?**
  _Cohesion score 0.11494252873563218 - nodes in this community are weakly interconnected._