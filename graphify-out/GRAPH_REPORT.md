# Graph Report - MP4ToolsUniversal  (2026-10-02)

## Corpus Check
- 60 files · ~25,290 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 792 nodes · 1518 edges · 41 communities (34 shown, 6 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 49 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `fe2ad905`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- MP4ViewModelBase
- TrimView
- ModifiedRecordingUploader
- FFMpegUtils
- CombineViewModel
- RecordingJsonElapsedOffset
- TrimViewModel
- BoxScoreUploader
- AppUserSettings
- IntroScreenReadResult
- Resolve MP4 Compatibility
- DrawText Overlay Utils
- Project Dependencies
- LogViewModel
- .CreateAsync
- .OnPropertyChanged
- TimeRange
- EncodingSettingsDto
- .MoveInputFilesToIndex
- BaseballLoggerSettingsRuntime
- RewriteIntroViewModel
- EventInfoDefaultsWindow
- .Log
- CombineView
- OptionsViewModel
- MP4Tools.Views
- ExportTrimState
- StartStopRange
- .Apply
- MP4Tools.ViewModels
- MainWindowViewModel
- ViewLocator
- MP4ToolsLib
- .BuildAvaloniaApp
- App.axaml.cs
- RelayCommand
- .ApplyFrom
- .ScheduleEdgeDurationRefresh
- .ApplyDefaultSkipRanges

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 92 edges
2. `TrimViewModel` - 51 edges
3. `MP4ToolsLib` - 36 edges
4. `RewriteIntroViewModel` - 34 edges
5. `CombineView` - 32 edges
6. `OptionsViewModel` - 27 edges
7. `FFMpegUtils` - 27 edges
8. `AppUserSettings` - 24 edges
9. `MP4ViewModelBase` - 24 edges
10. `StartStopRange` - 21 edges

## Surprising Connections (you probably didn't know these)
- `CombineViewModel` --references--> `CombineFile`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/CombineFile.cs
- `CombineViewModel` --references--> `CombineFileSortMode`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/CombineFile.cs
- `CombineViewModel` --references--> `TimeRange`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/TimeRange.cs
- `ExportTrimState` --references--> `StartStopRange`  [EXTRACTED]
  MP4Tools/ViewModels/TrimViewModel.cs → MP4ToolsLib/Ranges.cs
- `TrimViewModel` --references--> `StartStopRange`  [EXTRACTED]
  MP4Tools/ViewModels/TrimViewModel.cs → MP4ToolsLib/Ranges.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **DaVinci Resolve crash mux/timing factors in 1.mp4** — mp4tools_notes_1_vs_1_working_analysis_1_mp4, mp4tools_notes_1_vs_1_working_analysis_ctts_box, mp4tools_notes_1_vs_1_working_analysis_edit_list, mp4tools_notes_1_vs_1_working_analysis_open_gop, mp4tools_notes_1_vs_1_working_analysis_broken_mp4_timeline, mp4tools_notes_1_vs_1_working_analysis_davinci_resolve [EXTRACTED 1.00]
- **Resolve-safe re-encode remediation path** — mp4tools_notes_1_vs_1_working_analysis_libx265_reencode, mp4tools_notes_1_vs_1_working_analysis_1_working_mp4, mp4tools_notes_1_vs_1_working_analysis_ffmpeg, mp4tools_notes_1_vs_1_working_analysis_hevc_main_8bit [EXTRACTED 1.00]
- **1.mp4 vs 1_working.mp4 container contrast** — mp4tools_notes_1_vs_1_working_analysis_1_mp4, mp4tools_notes_1_vs_1_working_analysis_1_working_mp4, mp4tools_notes_1_vs_1_working_analysis_ctts_box, mp4tools_notes_1_vs_1_working_analysis_colr_box, mp4tools_notes_1_vs_1_working_analysis_hevc_main_10, mp4tools_notes_1_vs_1_working_analysis_hevc_main_8bit [EXTRACTED 1.00]

## Communities (41 total, 6 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.06
Nodes (26): IProgress, CancellationToken, List, Task, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments (+18 more)

### Community 1 - "MP4ViewModelBase"
Cohesion: 0.06
Nodes (26): CancellationToken, CancellationTokenSource, EventArgs, Process, Task, MP4ViewModelBase, CanClear, CanStop (+18 more)

### Community 2 - "TrimView"
Cohesion: 0.07
Nodes (17): DataFormat, IDataTransfer, RelayCommand, DragEventArgs, HashSet, IReadOnlyList, Task, FileDropHelper (+9 more)

### Community 3 - "ModifiedRecordingUploader"
Cohesion: 0.20
Nodes (7): Action, CancellationToken, JsonElement, Task, Uri, ModifiedRecordingUploader, UploadRevisionResult

### Community 4 - "FFMpegUtils"
Cohesion: 0.14
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 5 - "CombineViewModel"
Cohesion: 0.05
Nodes (30): ConcurrentDictionary, AsyncRelayCommand, DateTime, ObservableCollection, RelayCommand, CombineViewModel, CanCombine, CombineCommand (+22 more)

### Community 6 - "RecordingJsonElapsedOffset"
Cohesion: 0.18
Nodes (13): Elapsed, JsonObject, Action, CancellationToken, DateTime, HashSet, JsonNode, JsonSerializerOptions (+5 more)

### Community 7 - "TrimViewModel"
Cohesion: 0.07
Nodes (28): AsyncRelayCommand, IReadOnlyList, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand, CanTrim (+20 more)

### Community 8 - "BoxScoreUploader"
Cohesion: 0.26
Nodes (8): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult

### Community 9 - "AppUserSettings"
Cohesion: 0.12
Nodes (15): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+7 more)

### Community 10 - "IntroScreenReadResult"
Cohesion: 0.06
Nodes (28): DateTime, IntroScreenReadResult, Details, EventDate, EventInfo, HomeName, HomeScore, Subtitle (+20 more)

### Community 11 - "Resolve MP4 Compatibility"
Cohesion: 0.22
Nodes (16): 1.mp4, 1_working.mp4, B-frames, Broken MP4 timeline, colr box, ctts box, DaVinci Resolve, Edit list (+8 more)

### Community 12 - "DrawText Overlay Utils"
Cohesion: 0.16
Nodes (12): IReadOnlyList, DrawTextPositionOption, DisplayName, Value, DrawTextUtils, DrawTextPositionOptions, DrawTextPositions, DrawTextPosition (+4 more)

### Community 13 - "Project Dependencies"
Cohesion: 0.14
Nodes (12): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.2), Avalonia.Desktop (12.1.2), Avalonia.Fonts.Inter (12.1.2), Avalonia.Themes.Simple (12.1.2) (+4 more)

### Community 14 - "LogViewModel"
Cohesion: 0.17
Nodes (10): ConcurrentQueue, RelayCommand, Task, TimeSpan, LogViewModel, AutoScrollToBottom, ClearLogCommand, LogText (+2 more)

### Community 15 - ".CreateAsync"
Cohesion: 0.21
Nodes (8): Action, CancellationToken, JsonElement, Task, Uri, BoxScoreSessionCreator, CreateBoxScoreSessionRequest, CreateBoxScoreSessionResult

### Community 17 - "TimeRange"
Cohesion: 0.20
Nodes (7): INotifyPropertyChanged, IReadOnlyList, TimeRange, Hours, Minutes, Range, TotalSeconds

### Community 18 - "EncodingSettingsDto"
Cohesion: 0.20
Nodes (8): EncodingSettingsRuntime, Current, EncodingSettingsDto, AudioCodec, HardwareAcceleration, TrimAudioCodec, UseResolveSafeEncoding, VideoCodec

### Community 20 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 21 - "RewriteIntroViewModel"
Cohesion: 0.07
Nodes (22): AsyncRelayCommand, DateTime, IReadOnlyList, Task, RewriteIntroViewModel, CanReadIntro, CanRewrite, EventDate (+14 more)

### Community 22 - "EventInfoDefaultsWindow"
Cohesion: 0.13
Nodes (9): KeyEventArgs, RoutedEventArgs, SelectionChangedEventArgs, EventInfoDefaultsWindow, MainWindow, RoutedEventArgs, YesNoConfirmWindow, TappedEventArgs (+1 more)

### Community 23 - ".Log"
Cohesion: 0.23
Nodes (4): Exception, CancellationToken, JsonNode, Task

### Community 24 - "CombineView"
Cohesion: 0.07
Nodes (25): DispatcherTimer, IPointer, ListBoxItem, IEnumerable, EventArgs, IReadOnlyList, List, PointerPressedEventArgs (+17 more)

### Community 25 - "OptionsViewModel"
Cohesion: 0.20
Nodes (5): CancellationTokenSource, OptionsViewModel, ApiKeyPasswordChar, OpenRouterApiKeyPasswordChar, SettingsFilePathDisplay

### Community 26 - "MP4Tools.Views"
Cohesion: 0.14
Nodes (6): MP4Tools.Views, EventArgs, LogView, OptionsView, UserControl, VisualTreeAttachmentEventArgs

### Community 27 - "ExportTrimState"
Cohesion: 0.29
Nodes (4): ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 28 - "StartStopRange"
Cohesion: 0.16
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 29 - ".Apply"
Cohesion: 0.17
Nodes (6): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory, UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, OpenOutputFolderOnComplete

### Community 30 - "MP4Tools.ViewModels"
Cohesion: 0.32
Nodes (3): MP4Tools.ViewModels, MP4Tools.Services, FolderOpener

### Community 31 - "MainWindowViewModel"
Cohesion: 0.20
Nodes (8): MainWindowViewModel, CombineViewModel, LogViewModel, OptionsViewModel, RewriteIntroViewModel, TrimViewModel, ViewModelBase, ObservableObject

### Community 32 - "ViewLocator"
Cohesion: 0.33
Nodes (3): Control, IDataTemplate, ViewLocator

### Community 33 - "MP4ToolsLib"
Cohesion: 0.17
Nodes (5): MP4ToolsLib, OpenRouterSettingsRuntime, ApiKey, Model, IntroFrameExtractor

### Community 35 - ".BuildAvaloniaApp"
Cohesion: 0.32
Nodes (4): AppBuilder, Program, STAThread, WaylandPlatformOptions

### Community 36 - "App.axaml.cs"
Cohesion: 0.25
Nodes (4): Application, MP4Tools, App, Logger

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **181 isolated node(s):** `net10.0`, `Avalonia (12.1.2)`, `Avalonia.Desktop (12.1.2)`, `Avalonia.Fonts.Inter (12.1.2)`, `Avalonia.Themes.Simple (12.1.2)` (+176 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 286 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `TrimView`, `.ScheduleEdgeDurationRefresh`, `.ApplyDefaultSkipRanges`, `LogViewModel`, `.OnPropertyChanged`, `TimeRange`, `.MoveInputFilesToIndex`, `.Log`, `CombineView`, `MP4Tools.ViewModels`, `MainWindowViewModel`?**
  _High betweenness centrality (0.256) - this node is a cross-community bridge._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `MP4ViewModelBase`, `ModifiedRecordingUploader`, `App.axaml.cs`, `FFMpegUtils`, `BoxScoreUploader`, `AppUserSettings`, `IntroScreenReadResult`, `DrawText Overlay Utils`, `.CreateAsync`, `TimeRange`, `EncodingSettingsDto`, `RewriteIntroViewModel`, `CombineView`, `MP4Tools.Views`, `ExportTrimState`, `MP4Tools.ViewModels`?**
  _High betweenness centrality (0.249) - this node is a cross-community bridge._
- **Why does `TrimViewModel` connect `TrimViewModel` to `.PrependIntroAsync`, `TrimView`, `.ClearTimeRanges`, `LogViewModel`, `.OnPropertyChanged`, `TimeRange`, `.Log`, `ExportTrimState`, `StartStopRange`?**
  _High betweenness centrality (0.096) - this node is a cross-community bridge._
- **What connects `net10.0`, `Avalonia (12.1.2)`, `Avalonia.Desktop (12.1.2)` to the rest of the system?**
  _181 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06450617283950617 - nodes in this community are weakly interconnected._
- **Should `MP4ViewModelBase` be split into smaller, more focused modules?**
  _Cohesion score 0.059379217273954114 - nodes in this community are weakly interconnected._