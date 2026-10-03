# Graph Report - MP4ToolsUniversal  (2026-10-03)

## Corpus Check
- 71 files · ~30,586 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 972 nodes · 1899 edges · 48 communities (38 shown, 8 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 64 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c854e087`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- StreamProbeInfo
- TrimView
- ModifiedRecordingUploader
- FFMpegUtils
- CombineViewModel
- RecordingJsonElapsedOffset
- TrimViewModel
- ReplaceSegmentViewModel
- AppUserSettings
- IntroScreenReadResult
- Resolve MP4 Compatibility
- DrawTextPosition
- Project Dependencies
- LogViewModel
- GenerateIntroViewModel
- .RebuildEndSkipRangesAndClamp
- TimeRange
- MainWindowViewModel
- MP4ViewModelBase
- BoxScoreUploader
- RewriteIntroViewModel
- App
- PendingIntroSpec
- CombineView
- OptionsViewModel
- .CreateAsync
- ExportTrimState
- StartStopRange
- .CreateBoxScoreSessionIfNeededAsync
- MP4ToolsLib
- EventInfoDefaultsWindow
- MP4Tools
- ReplaceSegmentRange
- .Log
- MP4Tools.ViewModels
- .BuildAvaloniaApp
- .EncodePendingIntroAsync
- .ReadIntroAsync
- .ScheduleEdgeDurationRefresh
- .SetFile
- .ApplyAsync
- .BuildGameInfoOutputFileName
- Task
- .ApplyDefaultSkipRanges
- .InsertVideoAtFront

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 95 edges
2. `TrimViewModel` - 51 edges
3. `ReplaceSegmentViewModel` - 47 edges
4. `GenerateIntroViewModel` - 42 edges
5. `MP4ToolsLib` - 42 edges
6. `RewriteIntroViewModel` - 34 edges
7. `CombineView` - 32 edges
8. `OptionsViewModel` - 27 edges
9. `FFMpegUtils` - 27 edges
10. `AppUserSettings` - 24 edges

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

## Communities (48 total, 8 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.06
Nodes (30): IProgress, CancellationToken, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments, FfmpegCommandLine, FfmpegOption (+22 more)

### Community 1 - "StreamProbeInfo"
Cohesion: 0.14
Nodes (12): StreamProbeInfo, BitDepth, ChannelLayout, Channels, CodecName, CodecType, FrameRate, Height (+4 more)

### Community 2 - "TrimView"
Cohesion: 0.06
Nodes (27): DataFormat, IDataTransfer, DragEventArgs, EditView, HashSet, IReadOnlyList, Task, FileDropHelper (+19 more)

### Community 3 - "ModifiedRecordingUploader"
Cohesion: 0.20
Nodes (7): Action, CancellationToken, JsonElement, Task, Uri, ModifiedRecordingUploader, UploadRevisionResult

### Community 4 - "FFMpegUtils"
Cohesion: 0.15
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 5 - "CombineViewModel"
Cohesion: 0.06
Nodes (30): ConcurrentDictionary, AsyncRelayCommand, DateTime, ObservableCollection, RelayCommand, CombineViewModel, CanCombine, CombineCommand (+22 more)

### Community 6 - "RecordingJsonElapsedOffset"
Cohesion: 0.17
Nodes (13): Elapsed, JsonObject, Action, CancellationToken, DateTime, HashSet, JsonNode, JsonSerializerOptions (+5 more)

### Community 7 - "TrimViewModel"
Cohesion: 0.07
Nodes (28): AsyncRelayCommand, IReadOnlyList, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand, CanTrim (+20 more)

### Community 8 - "ReplaceSegmentViewModel"
Cohesion: 0.08
Nodes (23): AsyncRelayCommand, IReadOnlyList, List, ObservableCollection, RelayCommand, TimeSpan, ReplaceSegmentViewModel, AddSegmentCommand (+15 more)

### Community 9 - "AppUserSettings"
Cohesion: 0.05
Nodes (35): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+27 more)

### Community 10 - "IntroScreenReadResult"
Cohesion: 0.06
Nodes (28): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+20 more)

### Community 11 - "Resolve MP4 Compatibility"
Cohesion: 0.22
Nodes (16): 1.mp4, 1_working.mp4, B-frames, Broken MP4 timeline, colr box, ctts box, DaVinci Resolve, Edit list (+8 more)

### Community 12 - "DrawTextPosition"
Cohesion: 0.16
Nodes (12): IReadOnlyList, DrawTextPositionOption, DisplayName, Value, DrawTextUtils, DrawTextPositionOptions, DrawTextPositions, DrawTextPosition (+4 more)

### Community 13 - "Project Dependencies"
Cohesion: 0.14
Nodes (12): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.2), Avalonia.Desktop (12.1.2), Avalonia.Fonts.Inter (12.1.2), Avalonia.Themes.Simple (12.1.2) (+4 more)

### Community 14 - "LogViewModel"
Cohesion: 0.11
Nodes (13): ConcurrentQueue, RelayCommand, Task, TimeSpan, LogViewModel, AutoScrollToBottom, ClearLogCommand, LogText (+5 more)

### Community 15 - "GenerateIntroViewModel"
Cohesion: 0.05
Nodes (32): Bitmap, AsyncRelayCommand, CancellationToken, CancellationTokenSource, IReadOnlyList, RelayCommand, Task, GenerateIntroViewModel (+24 more)

### Community 17 - "TimeRange"
Cohesion: 0.18
Nodes (7): INotifyPropertyChanged, IReadOnlyList, TimeRange, Hours, Minutes, Range, TotalSeconds

### Community 18 - "MainWindowViewModel"
Cohesion: 0.14
Nodes (12): EditViewModel, GenerateIntroViewModel, ReplaceSegmentViewModel, RewriteIntroViewModel, MainWindowViewModel, CombineViewModel, EditViewModel, LogViewModel (+4 more)

### Community 19 - "MP4ViewModelBase"
Cohesion: 0.14
Nodes (10): EventArgs, MP4ViewModelBase, CanClear, CanStop, ClearCommand, InputPath, InputProbeVideoCodecName, IsInput10Bit (+2 more)

### Community 20 - "BoxScoreUploader"
Cohesion: 0.26
Nodes (8): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.09
Nodes (19): AsyncRelayCommand, DateTime, IReadOnlyList, RewriteIntroViewModel, CanReadIntro, CanRewrite, EventDate, EventInfo (+11 more)

### Community 22 - "App"
Cohesion: 0.18
Nodes (6): Application, App, MainWindow, RoutedEventArgs, YesNoConfirmWindow, Window

### Community 23 - "PendingIntroSpec"
Cohesion: 0.13
Nodes (13): PendingIntroSpec, BackgroundColor, Details, DetailsFontSize, DisplaySummary, DurationSeconds, HasContent, LineGap (+5 more)

### Community 24 - "CombineView"
Cohesion: 0.07
Nodes (23): DispatcherTimer, IPointer, ListBoxItem, EventArgs, IReadOnlyList, List, PointerPressedEventArgs, RoutedEventArgs (+15 more)

### Community 25 - "OptionsViewModel"
Cohesion: 0.12
Nodes (8): CancellationTokenSource, EventArgs, RelayCommand, Task, OptionsViewModel, ApiKeyPasswordChar, OpenRouterApiKeyPasswordChar, SettingsFilePathDisplay

### Community 26 - ".CreateAsync"
Cohesion: 0.21
Nodes (8): Action, CancellationToken, JsonElement, Task, Uri, BoxScoreSessionCreator, CreateBoxScoreSessionRequest, CreateBoxScoreSessionResult

### Community 27 - "ExportTrimState"
Cohesion: 0.29
Nodes (5): List, ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 28 - "StartStopRange"
Cohesion: 0.15
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 29 - ".CreateBoxScoreSessionIfNeededAsync"
Cohesion: 0.31
Nodes (3): CancellationToken, JsonNode, Task

### Community 30 - "MP4ToolsLib"
Cohesion: 0.12
Nodes (5): MP4Tools.Services, MP4ToolsLib, FolderOpener, FileUtils, IntroFrameExtractor

### Community 31 - "EventInfoDefaultsWindow"
Cohesion: 0.23
Nodes (5): KeyEventArgs, RoutedEventArgs, SelectionChangedEventArgs, EventInfoDefaultsWindow, TappedEventArgs

### Community 32 - "MP4Tools"
Cohesion: 0.20
Nodes (5): Control, MP4Tools, IDataTemplate, Logger, ViewLocator

### Community 33 - "ReplaceSegmentRange"
Cohesion: 0.15
Nodes (9): Guid, PropertyChangedEventArgs, ReplaceSegmentRange, EndSeconds, HasReplacement, Id, IsValid, ReplacementStatus (+1 more)

### Community 34 - ".Log"
Cohesion: 0.27
Nodes (3): Exception, Process, Task

### Community 36 - ".BuildAvaloniaApp"
Cohesion: 0.32
Nodes (4): AppBuilder, Program, STAThread, WaylandPlatformOptions

### Community 37 - ".EncodePendingIntroAsync"
Cohesion: 0.29
Nodes (3): Action, CancellationToken, Task

### Community 41 - ".ApplyAsync"
Cohesion: 0.28
Nodes (3): CancellationToken, CancellationTokenSource, RemovalRange

### Community 47 - ".InsertVideoAtFront"
Cohesion: 0.15
Nodes (3): IEnumerable, IReadOnlyList, IReadOnlyList

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **238 isolated node(s):** `net10.0`, `Avalonia (12.1.2)`, `Avalonia.Desktop (12.1.2)`, `Avalonia.Fonts.Inter (12.1.2)`, `Avalonia.Themes.Simple (12.1.2)` (+233 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 360 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `TrimView`, `.ScheduleEdgeDurationRefresh`, `ReplaceSegmentViewModel`, `.BuildGameInfoOutputFileName`, `.ApplyDefaultSkipRanges`, `LogViewModel`, `.InsertVideoAtFront`, `.RebuildEndSkipRangesAndClamp`, `TimeRange`, `GenerateIntroViewModel`, `MainWindowViewModel`, `CombineView`, `.CreateBoxScoreSessionIfNeededAsync`, `MP4ToolsLib`?**
  _High betweenness centrality (0.252) - this node is a cross-community bridge._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `MP4Tools`, `.PrependIntroAsync`, `StreamProbeInfo`, `MP4Tools.ViewModels`, `ModifiedRecordingUploader`, `RecordingJsonElapsedOffset`, `AppUserSettings`, `IntroScreenReadResult`, `DrawTextPosition`, `GenerateIntroViewModel`, `TimeRange`, `BoxScoreUploader`, `CombineView`, `.CreateAsync`?**
  _High betweenness centrality (0.219) - this node is a cross-community bridge._
- **Why does `ReplaceSegmentViewModel` connect `ReplaceSegmentViewModel` to `ReplaceSegmentRange`, `.Log`, `CombineViewModel`, `.EncodePendingIntroAsync`, `.SetFile`, `.ApplyAsync`, `.RefreshCanApply`, `.ExitGenerateIntroUi`, `LogViewModel`, `TimeRange`, `MainWindowViewModel`, `MP4ToolsLib`?**
  _High betweenness centrality (0.088) - this node is a cross-community bridge._
- **What connects `net10.0`, `Avalonia (12.1.2)`, `Avalonia.Desktop (12.1.2)` to the rest of the system?**
  _238 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.061366181410974247 - nodes in this community are weakly interconnected._
- **Should `StreamProbeInfo` be split into smaller, more focused modules?**
  _Cohesion score 0.14166666666666666 - nodes in this community are weakly interconnected._