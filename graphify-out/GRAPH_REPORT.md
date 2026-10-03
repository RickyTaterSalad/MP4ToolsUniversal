# Graph Report - MP4ToolsUniversal  (2026-10-03)

## Corpus Check
- 97 files · ~52,011 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1520 nodes · 3053 edges · 71 communities (62 shown, 8 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 107 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `2f3efc7d`
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
- .OnPropertyChanged
- TimeRange
- .WriteSamplesCsvAsync
- .ApplyAsync
- IntroScreenReadResult
- RewriteIntroViewModel
- .DetectFromSamplesAsync
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
- LrfInningDetectorViewModel
- ViewLocator
- CombineEditMap
- ArtifactSegmentInfo
- Options
- UiBehaviorSettingsRuntime
- MP4Tools
- UserControl
- RelayCommand
- ExportPendingIntro
- .Apply
- InningDetectorViewModel
- BaseballLoggerSettingsRuntime
- InningDetectionArtifactManifest
- MP4Tools.ViewModels
- RewriteIntroView
- TrimView
- .EnsureDetectorModelAsync
- ExportTrimState
- .ScheduleEdgeDurationRefresh
- .SetFile
- LrfInningDetectorView
- ReplaceSegmentView
- GenerateIntroView
- DefaultOutputPathRuntime
- .DetectCoreAsync
- .RunAndLogFFMpegAsync
- YesNoConfirmWindow
- .ExtractAsync
- InningDetectionResult
- .ApplyDefaultSkipRanges
- .ApplyFrom
- export_baseballcv_onnx.sh

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 112 edges
2. `MP4ToolsLib` - 62 edges
3. `ReplaceSegmentViewModel` - 56 edges
4. `TrimViewModel` - 53 edges
5. `GenerateIntroViewModel` - 45 edges
6. `Options` - 40 edges
7. `RewriteIntroViewModel` - 36 edges
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

## Communities (71 total, 8 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.05
Nodes (36): IProgress, CancellationToken, List, Task, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments (+28 more)

### Community 1 - "CombineFile"
Cohesion: 0.12
Nodes (10): IEnumerable, SelectionChangedEventArgs, IEnumerable, IReadOnlyList, List, CombineFile, Name, CombineFileSortMode (+2 more)

### Community 2 - ".HasFilePayload"
Cohesion: 0.20
Nodes (7): IDataTransfer, HashSet, IReadOnlyList, Task, FileDropHelper, DragEventArgs, DragEventArgs

### Community 3 - ".Log"
Cohesion: 0.18
Nodes (6): Exception, IReadOnlyDictionary, CancellationToken, IReadOnlyList, JsonNode, Task

### Community 4 - "FFMpegUtils"
Cohesion: 0.14
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 5 - "CombineViewModel"
Cohesion: 0.05
Nodes (32): ConcurrentDictionary, AsyncRelayCommand, DateTime, LogOperationSource, ObservableCollection, RelayCommand, CombineViewModel, CanCombine (+24 more)

### Community 6 - "RecordingJsonElapsedOffset"
Cohesion: 0.14
Nodes (15): Elapsed, DateTime, FileUtils, Action, CancellationToken, DateTime, HashSet, JsonNode (+7 more)

### Community 7 - "TrimViewModel"
Cohesion: 0.06
Nodes (30): AsyncRelayCommand, IReadOnlyList, LogOperationSource, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand (+22 more)

### Community 8 - "ReplaceSegmentViewModel"
Cohesion: 0.08
Nodes (25): AsyncRelayCommand, IReadOnlyList, LogOperationSource, ObservableCollection, RelayCommand, Task, TimeSpan, ReplaceSegmentViewModel (+17 more)

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
Cohesion: 0.17
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

### Community 18 - ".WriteSamplesCsvAsync"
Cohesion: 0.18
Nodes (6): Action, CancellationToken, IReadOnlyList, Rect2d, Task, StringBuilder

### Community 19 - ".ApplyAsync"
Cohesion: 0.10
Nodes (14): CheckResult, EndSeconds, Estimate, Task, DiskSpaceWarning, List, IEnumerable, CheckResult (+6 more)

### Community 20 - "IntroScreenReadResult"
Cohesion: 0.06
Nodes (28): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+20 more)

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.08
Nodes (22): AsyncRelayCommand, DateTime, IReadOnlyList, LogOperationSource, Task, RewriteIntroViewModel, CanReadIntro, CanRewrite (+14 more)

### Community 22 - ".DetectFromSamplesAsync"
Cohesion: 0.06
Nodes (49): Confidence, DenseTensor, FrameSignals, IDisposable, InBox, InferenceSession, IRefineMediaSource, Action (+41 more)

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
Cohesion: 0.15
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 29 - ".WriteSessionAndYoutubeAsync"
Cohesion: 0.19
Nodes (10): JsonPath, YoutubeDescriptionPath, Action, CancellationToken, DateTime, IReadOnlyList, JsonObject, JsonSerializerOptions (+2 more)

### Community 30 - "MP4ToolsLib"
Cohesion: 0.10
Nodes (6): MP4Tools.Services, MP4ToolsLib, FolderOpener, CombineEditMapEmbedder, IntroTitleCardClassifier, IntroFrameExtractor

### Community 31 - "LogViewModel"
Cohesion: 0.06
Nodes (27): ConcurrentQueue, LogOperationSource, Combine, GenerateIntro, InningDetector, LrfInningDetector, None, ReplaceSegment (+19 more)

### Community 32 - "MP4ViewModelBase"
Cohesion: 0.12
Nodes (12): CancellationTokenSource, EventArgs, MP4ViewModelBase, CanClear, CanStop, ClearCommand, InputPath, InputProbeVideoCodecName (+4 more)

### Community 33 - "StreamProbeInfo"
Cohesion: 0.13
Nodes (12): StreamProbeInfo, BitDepth, ChannelLayout, Channels, CodecName, CodecType, FrameRate, Height (+4 more)

### Community 34 - "ModifiedRecordingUploader"
Cohesion: 0.20
Nodes (7): Action, CancellationToken, JsonElement, Task, Uri, ModifiedRecordingUploader, UploadRevisionResult

### Community 35 - "InningDetectorView"
Cohesion: 0.40
Nodes (3): DragEventArgs, RoutedEventArgs, InningDetectorView

### Community 36 - "InningDetectionArtifactSession"
Cohesion: 0.11
Nodes (16): JsonSerializerOptions, List, InningDetectionArtifactSession, CombineMapCopyPath, DetectorOptionsPath, EventsCombinedPath, EventsGamePath, FramesDirectory (+8 more)

### Community 37 - "LrfInningDetectorViewModel"
Cohesion: 0.13
Nodes (14): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, LrfInningDetectorViewModel, DetectCommand, DetectedEventLines, MatchedLrfLines (+6 more)

### Community 38 - "ViewLocator"
Cohesion: 0.33
Nodes (3): Control, IDataTemplate, ViewLocator

### Community 39 - "CombineEditMap"
Cohesion: 0.09
Nodes (22): Path, DateTimeOffset, IReadOnlyList, List, CombineEditMap, Clips, CombineMapPath, CreatedUtc (+14 more)

### Community 40 - "ArtifactSegmentInfo"
Cohesion: 0.22
Nodes (8): ArtifactSegmentInfo, ClipIndex, ContributionSeconds, GameStartSeconds, LocalStartSeconds, MediaPath, InningDetectionArtifactRuntime, Enabled

### Community 41 - "Options"
Cohesion: 0.05
Nodes (37): Options, AnalysisWidth, AssumeTopFirstAtGameStart, BatterApproachRoi, BatterBoxRoi, ConfidenceThreshold, EmptyFieldHoldSeconds, FieldRoi (+29 more)

### Community 42 - "UiBehaviorSettingsRuntime"
Cohesion: 0.29
Nodes (6): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, LrfInningSampleIntervalSeconds, OpenOutputFolderOnComplete, SaveInningDetectionArtifacts, WarnOnInsufficientDiskSpace

### Community 43 - "MP4Tools"
Cohesion: 0.15
Nodes (8): AppBuilder, Application, MP4Tools, App, Logger, Program, STAThread, WaylandPlatformOptions

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
Cohesion: 0.19
Nodes (9): AsyncRelayCommand, LogOperationSource, ObservableCollection, Task, InningDetectorViewModel, DetectCommand, DetectedEventLines, OperationLogSource (+1 more)

### Community 49 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 50 - "InningDetectionArtifactManifest"
Cohesion: 0.07
Nodes (27): DateTimeOffset, InningDetectionArtifactManifest, CombineMapCopyPath, CombineMapPath, CreatedLocal, DetectorOptionsPath, EventsCombinedPath, EventsGamePath (+19 more)

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
Cohesion: 0.29
Nodes (4): ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 59 - "LrfInningDetectorView"
Cohesion: 0.38
Nodes (3): DragEventArgs, RoutedEventArgs, LrfInningDetectorView

### Community 60 - "ReplaceSegmentView"
Cohesion: 0.48
Nodes (3): List, RoutedEventArgs, ReplaceSegmentView

### Community 61 - "GenerateIntroView"
Cohesion: 0.31
Nodes (4): DragEventArgs, List, RoutedEventArgs, GenerateIntroView

### Community 62 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 63 - ".DetectCoreAsync"
Cohesion: 0.27
Nodes (8): Action, CancellationToken, IReadOnlyList, List, Task, LrfInningDetector, Options, DetectorOptions

### Community 64 - ".RunAndLogFFMpegAsync"
Cohesion: 0.33
Nodes (4): Action, CancellationToken, Process, Task

### Community 65 - "YesNoConfirmWindow"
Cohesion: 0.25
Nodes (4): MainWindow, RoutedEventArgs, YesNoConfirmWindow, Window

### Community 66 - ".ExtractAsync"
Cohesion: 0.10
Nodes (19): Regex, FfmpegProgressParser, IReadOnlyList, List, GameContentRefineMediaSource, GameContentSegment, GameEndSeconds, GameContentTimeline (+11 more)

### Community 67 - "InningDetectionResult"
Cohesion: 0.17
Nodes (12): IReadOnlyList, InningDetectionEvent, ElapsedSeconds, Kind, Label, InningDetectionResult, ArtifactHandoffPath, ArtifactSessionDirectory (+4 more)

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **413 isolated node(s):** `None`, `Combine`, `Trim`, `RewriteIntro`, `GenerateIntro` (+408 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 593 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `CombineFile`, `FFMpegUtils`, `RecordingJsonElapsedOffset`, `BoxScoreUploader`, `DrawTextPosition`, `GenerateIntroViewModel`, `TimeRange`, `.ApplyAsync`, `IntroScreenReadResult`, `.DetectFromSamplesAsync`, `StartStopRange`, `.WriteSessionAndYoutubeAsync`, `StreamProbeInfo`, `ModifiedRecordingUploader`, `CombineEditMap`, `ArtifactSegmentInfo`, `MP4Tools`, `.Apply`, `MP4Tools.ViewModels`, `.EnsureDetectorModelAsync`, `.DetectCoreAsync`, `.ExtractAsync`, `InningDetectionResult`?**
  _High betweenness centrality (0.252) - this node is a cross-community bridge._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `CombineFile`, `.Log`, `.ApplyDefaultSkipRanges`, `ReplaceSegmentViewModel`, `MainWindowViewModel`, `GenerateIntroViewModel`, `.OnPropertyChanged`, `TimeRange`, `InningDetectorViewModel`, `CombineView`, `.ScheduleEdgeDurationRefresh`, `.SetFile`, `MP4ToolsLib`?**
  _High betweenness centrality (0.203) - this node is a cross-community bridge._
- **Why does `ReplaceSegmentViewModel` connect `ReplaceSegmentViewModel` to `ExportReplaceSegmentState`, `CombineViewModel`, `GenerateIntroViewModel`, `.OnPropertyChanged`, `TimeRange`, `InningDetectorViewModel`, `.ApplyAsync`, `PendingIntroSpec`, `.ImportReplaceFile`, `.SetFile`, `ReplaceSegmentRange`, `MP4ToolsLib`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._
- **What connects `None`, `Combine`, `Trim` to the rest of the system?**
  _413 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.05371836671324329 - nodes in this community are weakly interconnected._
- **Should `CombineFile` be split into smaller, more focused modules?**
  _Cohesion score 0.1206896551724138 - nodes in this community are weakly interconnected._