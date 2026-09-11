# Graph Report - MP4ToolsUniversal  (2026-09-10)

## Corpus Check
- Corpus is ~20,206 words - fits in a single context window. You may not need a graph.

## Summary
- 616 nodes · 1177 edges · 33 communities (30 shown, 1 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 33 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- FFMPEG Combine Pipeline
- Combine Operation Control
- Combine View Drag Drop
- Box Score Session API
- FFMPEG Utilities Core
- Combine ViewModel State
- File and JSON Utils
- Trim ViewModel Commands
- Box Score Uploader
- App Settings Store
- Combine File Model
- Resolve MP4 Compatibility
- DrawText Overlay Utils
- Project Dependencies
- Log ViewModel
- Options ViewModel
- Skip Range Clamping
- Encoding Helper Modules
- Encoding Settings Runtime
- Edge Duration Refresh
- Time Range Model
- Log and Options Views
- App Shell Locators
- Services Path Helpers
- Main Window ViewModels
- App Avalonia Bootstrap
- Trim State Export
- Start Stop Ranges
- Program Entry Point
- Baseball Logger Settings
- UI Behavior Settings

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 79 edges
2. `TrimViewModel` - 46 edges
3. `MP4ToolsLib` - 31 edges
4. `FFMpegUtils` - 27 edges
5. `MP4ViewModelBase` - 24 edges
6. `AppUserSettings` - 21 edges
7. `FfmpegOption` - 19 edges
8. `RecordingJsonElapsedOffset` - 18 edges
9. `LogViewModel` - 17 edges
10. `TimeRange` - 17 edges

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

## Communities (33 total, 1 thin omitted)

### Community 0 - "FFMPEG Combine Pipeline"
Cohesion: 0.07
Nodes (24): Exception, IProgress, CancellationToken, List, Task, ICollection, DaVinciOutputEncoding, IEnumerable (+16 more)

### Community 1 - "Combine Operation Control"
Cohesion: 0.05
Nodes (28): ConcurrentQueue, CancellationToken, CancellationTokenSource, EventArgs, Process, RelayCommand, Task, MP4ViewModelBase (+20 more)

### Community 2 - "Combine View Drag Drop"
Cohesion: 0.08
Nodes (16): IDataTransfer, DataFormat, DragEventArgs, PointerPressedEventArgs, RoutedEventArgs, CombineView, HashSet, IReadOnlyList (+8 more)

### Community 3 - "Box Score Session API"
Cohesion: 0.09
Nodes (16): JsonNode, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreSessionCreator, CreateBoxScoreSessionRequest (+8 more)

### Community 4 - "FFMPEG Utilities Core"
Cohesion: 0.14
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 5 - "Combine ViewModel State"
Cohesion: 0.06
Nodes (28): AsyncRelayCommand, DateTime, IReadOnlyList, ObservableCollection, RelayCommand, CombineViewModel, CanCombine, CombineCommand (+20 more)

### Community 6 - "File and JSON Utils"
Cohesion: 0.14
Nodes (15): Elapsed, JsonObject, DateTime, FileUtils, Action, CancellationToken, DateTime, HashSet (+7 more)

### Community 7 - "Trim ViewModel Commands"
Cohesion: 0.07
Nodes (26): AsyncRelayCommand, IReadOnlyList, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand, CanTrim (+18 more)

### Community 8 - "Box Score Uploader"
Cohesion: 0.23
Nodes (8): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult

### Community 9 - "App Settings Store"
Cohesion: 0.16
Nodes (13): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+5 more)

### Community 10 - "Combine File Model"
Cohesion: 0.16
Nodes (9): IEnumerable, IReadOnlyList, List, CombineFile, Name, Path, CombineFileSortMode, Creation (+1 more)

### Community 11 - "Resolve MP4 Compatibility"
Cohesion: 0.22
Nodes (16): 1.mp4, 1_working.mp4, B-frames, Broken MP4 timeline, colr box, ctts box, DaVinci Resolve, Edit list (+8 more)

### Community 12 - "DrawText Overlay Utils"
Cohesion: 0.16
Nodes (12): IReadOnlyList, DrawTextPositionOption, DisplayName, Value, DrawTextUtils, DrawTextPositionOptions, DrawTextPositions, DrawTextPosition (+4 more)

### Community 13 - "Project Dependencies"
Cohesion: 0.14
Nodes (12): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.1), Avalonia.Desktop (12.1.1), Avalonia.Fonts.Inter (12.1.1), Avalonia.Themes.Simple (12.1.1) (+4 more)

### Community 14 - "Log ViewModel"
Cohesion: 0.19
Nodes (9): RelayCommand, Task, TimeSpan, LogViewModel, AutoScrollToBottom, ClearLogCommand, LogText, MP4ViewModelBase (+1 more)

### Community 15 - "Options ViewModel"
Cohesion: 0.22
Nodes (6): EventArgs, RelayCommand, Task, OptionsViewModel, ApiKeyPasswordChar, SettingsFilePathDisplay

### Community 17 - "Encoding Helper Modules"
Cohesion: 0.21
Nodes (3): MP4Tools.ViewModels, MP4Tools.Views, MP4ToolsLib

### Community 18 - "Encoding Settings Runtime"
Cohesion: 0.18
Nodes (8): EncodingSettingsRuntime, Current, EncodingSettingsDto, AudioCodec, HardwareAcceleration, TrimAudioCodec, UseResolveSafeEncoding, VideoCodec

### Community 19 - "Edge Duration Refresh"
Cohesion: 0.17
Nodes (5): CancellationToken, CancellationTokenSource, List, Task, NotifyCollectionChangedEventArgs

### Community 20 - "Time Range Model"
Cohesion: 0.20
Nodes (7): INotifyPropertyChanged, IReadOnlyList, TimeRange, Hours, Minutes, Range, TotalSeconds

### Community 21 - "Log and Options Views"
Cohesion: 0.18
Nodes (5): EventArgs, LogView, OptionsView, UserControl, VisualTreeAttachmentEventArgs

### Community 22 - "App Shell Locators"
Cohesion: 0.20
Nodes (5): Control, MP4Tools, IDataTemplate, Logger, ViewLocator

### Community 23 - "Services Path Helpers"
Cohesion: 0.20
Nodes (5): MP4Tools.Services, DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory, FolderOpener

### Community 24 - "Main Window ViewModels"
Cohesion: 0.22
Nodes (7): MainWindowViewModel, CombineViewModel, LogViewModel, OptionsViewModel, TrimViewModel, ViewModelBase, ObservableObject

### Community 25 - "App Avalonia Bootstrap"
Cohesion: 0.25
Nodes (4): Application, App, MainWindow, Window

### Community 27 - "Trim State Export"
Cohesion: 0.25
Nodes (4): ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 28 - "Start Stop Ranges"
Cohesion: 0.25
Nodes (5): StartStopRange, EndRange, Label, SelectedDrawTextPosition, StartRange

### Community 29 - "Program Entry Point"
Cohesion: 0.40
Nodes (3): AppBuilder, Program, STAThread

### Community 30 - "Baseball Logger Settings"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 31 - "UI Behavior Settings"
Cohesion: 0.50
Nodes (3): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, OpenOutputFolderOnComplete

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **134 isolated node(s):** `net10.0`, `Avalonia (12.1.1)`, `Avalonia.Desktop (12.1.1)`, `Avalonia.Fonts.Inter (12.1.1)`, `Avalonia.Themes.Simple (12.1.1)` (+129 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 220 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **1 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `CombineViewModel` connect `Combine ViewModel State` to `FFMPEG Combine Pipeline`, `Combine Operation Control`, `Combine View Drag Drop`, `Box Score Session API`, `File and JSON Utils`, `Box Score Uploader`, `Combine File Model`, `Log ViewModel`, `Skip Range Clamping`, `Encoding Helper Modules`, `Edge Duration Refresh`, `Time Range Model`, `Main Window ViewModels`, `Intro Combine Gates`?**
  _High betweenness centrality (0.265) - this node is a cross-community bridge._
- **Why does `MP4ToolsLib` connect `Encoding Helper Modules` to `FFMPEG Combine Pipeline`, `Combine Operation Control`, `Box Score Session API`, `FFMPEG Utilities Core`, `File and JSON Utils`, `Box Score Uploader`, `Combine File Model`, `DrawText Overlay Utils`, `Encoding Settings Runtime`, `Time Range Model`, `App Shell Locators`, `Services Path Helpers`, `App Avalonia Bootstrap`, `Trim State Export`?**
  _High betweenness centrality (0.232) - this node is a cross-community bridge._
- **Why does `TrimViewModel` connect `Trim ViewModel Commands` to `FFMPEG Combine Pipeline`, `Combine Operation Control`, `Input Path File Info`, `Log ViewModel`, `Skip Range Clamping`, `Time Range Model`, `Trim State Export`, `Start Stop Ranges`?**
  _High betweenness centrality (0.111) - this node is a cross-community bridge._
- **What connects `net10.0`, `Avalonia (12.1.1)`, `Avalonia.Desktop (12.1.1)` to the rest of the system?**
  _134 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FFMPEG Combine Pipeline` be split into smaller, more focused modules?**
  _Cohesion score 0.06701754385964913 - nodes in this community are weakly interconnected._
- **Should `Combine Operation Control` be split into smaller, more focused modules?**
  _Cohesion score 0.052525252525252523 - nodes in this community are weakly interconnected._