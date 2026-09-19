# Graph Report - MP4ToolsUniversal  (2026-09-18)

## Corpus Check
- 52 files · ~21,947 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 662 nodes · 1296 edges · 34 communities (29 shown, 4 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 39 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `47c64998`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- MP4ViewModelBase
- TrimView
- .CreateBoxScoreSessionIfNeededAsync
- FFMpegUtils
- CombineViewModel
- RecordingJsonElapsedOffset
- TrimViewModel
- BoxScoreUploader
- AppUserSettings
- CombineFile
- Resolve MP4 Compatibility
- DrawText Overlay Utils
- Project Dependencies
- LogViewModel
- MainWindowViewModel
- .OnPropertyChanged
- BaseballLoggerSettingsRuntime
- EncodingSettingsDto
- .MoveInputFilesToIndex
- DefaultOutputPathRuntime
- .BuildAvaloniaApp
- .Log
- UiBehaviorSettingsRuntime
- CombineView
- .SyncSelectedInputFiles
- ExportTrimState
- StartStopRange
- .BuildGameInfoOutputFileName
- MP4ToolsLib
- App
- ViewLocator
- FileUtils

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 91 edges
2. `TrimViewModel` - 51 edges
3. `CombineView` - 31 edges
4. `MP4ToolsLib` - 31 edges
5. `FFMpegUtils` - 27 edges
6. `MP4ViewModelBase` - 24 edges
7. `AppUserSettings` - 21 edges
8. `StartStopRange` - 21 edges
9. `CombineFile` - 19 edges
10. `FfmpegOption` - 19 edges

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

## Communities (34 total, 4 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.07
Nodes (21): IProgress, CancellationToken, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments, FfmpegCommandLine, FfmpegOption (+13 more)

### Community 1 - "MP4ViewModelBase"
Cohesion: 0.06
Nodes (27): ConcurrentQueue, CancellationToken, CancellationTokenSource, EventArgs, Process, Task, MP4ViewModelBase, CanClear (+19 more)

### Community 2 - "TrimView"
Cohesion: 0.09
Nodes (13): DataFormat, IDataTransfer, RelayCommand, DragEventArgs, HashSet, IReadOnlyList, Task, FileDropHelper (+5 more)

### Community 3 - ".CreateBoxScoreSessionIfNeededAsync"
Cohesion: 0.08
Nodes (18): CancellationToken, JsonNode, Task, Action, CancellationToken, JsonElement, Task, Uri (+10 more)

### Community 4 - "FFMpegUtils"
Cohesion: 0.14
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 5 - "CombineViewModel"
Cohesion: 0.06
Nodes (30): ConcurrentDictionary, AsyncRelayCommand, DateTime, List, ObservableCollection, RelayCommand, CombineViewModel, CanCombine (+22 more)

### Community 6 - "RecordingJsonElapsedOffset"
Cohesion: 0.10
Nodes (20): Elapsed, INotifyPropertyChanged, JsonObject, Action, CancellationToken, DateTime, HashSet, JsonNode (+12 more)

### Community 7 - "TrimViewModel"
Cohesion: 0.07
Nodes (28): AsyncRelayCommand, IReadOnlyList, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand, CanTrim (+20 more)

### Community 8 - "BoxScoreUploader"
Cohesion: 0.26
Nodes (8): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult

### Community 9 - "AppUserSettings"
Cohesion: 0.09
Nodes (19): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+11 more)

### Community 10 - "CombineFile"
Cohesion: 0.19
Nodes (9): IEnumerable, IReadOnlyList, List, CombineFile, Name, Path, CombineFileSortMode, Creation (+1 more)

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
Cohesion: 0.10
Nodes (14): RelayCommand, Task, TimeSpan, LogViewModel, AutoScrollToBottom, ClearLogCommand, LogText, EventArgs (+6 more)

### Community 15 - "MainWindowViewModel"
Cohesion: 0.22
Nodes (7): MainWindowViewModel, CombineViewModel, LogViewModel, OptionsViewModel, TrimViewModel, ViewModelBase, ObservableObject

### Community 17 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.50
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 18 - "EncodingSettingsDto"
Cohesion: 0.17
Nodes (8): EncodingSettingsRuntime, Current, EncodingSettingsDto, AudioCodec, HardwareAcceleration, TrimAudioCodec, UseResolveSafeEncoding, VideoCodec

### Community 19 - ".MoveInputFilesToIndex"
Cohesion: 0.24
Nodes (3): CancellationTokenSource, IReadOnlyList, NotifyCollectionChangedEventArgs

### Community 20 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 21 - ".BuildAvaloniaApp"
Cohesion: 0.38
Nodes (4): AppBuilder, Program, STAThread, WaylandPlatformOptions

### Community 22 - ".Log"
Cohesion: 0.24
Nodes (3): Exception, Logger, Task

### Community 23 - "UiBehaviorSettingsRuntime"
Cohesion: 0.50
Nodes (3): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, OpenOutputFolderOnComplete

### Community 24 - "CombineView"
Cohesion: 0.11
Nodes (14): DispatcherTimer, IPointer, ListBoxItem, EventArgs, IReadOnlyList, List, PointerPressedEventArgs, RoutedEventArgs (+6 more)

### Community 27 - "ExportTrimState"
Cohesion: 0.22
Nodes (5): List, ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 28 - "StartStopRange"
Cohesion: 0.18
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 30 - "MP4ToolsLib"
Cohesion: 0.18
Nodes (6): MP4Tools.ViewModels, MP4Tools.Services, MP4Tools.Views, MP4Tools, MP4ToolsLib, FolderOpener

### Community 31 - "App"
Cohesion: 0.29
Nodes (4): Application, App, MainWindow, Window

### Community 32 - "ViewLocator"
Cohesion: 0.33
Nodes (3): Control, IDataTemplate, ViewLocator

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **141 isolated node(s):** `net10.0`, `Avalonia (12.1.2)`, `Avalonia.Desktop (12.1.2)`, `Avalonia.Fonts.Inter (12.1.2)`, `Avalonia.Themes.Simple (12.1.2)` (+136 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 232 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `MP4ViewModelBase`, `TrimView`, `.CreateBoxScoreSessionIfNeededAsync`, `RecordingJsonElapsedOffset`, `CombineFile`, `LogViewModel`, `MainWindowViewModel`, `.OnPropertyChanged`, `.MoveInputFilesToIndex`, `CombineView`, `.SyncSelectedInputFiles`, `.RefreshCanCombineFromInputs`, `.BuildGameInfoOutputFileName`, `MP4ToolsLib`?**
  _High betweenness centrality (0.296) - this node is a cross-community bridge._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `FileUtils`, `MP4ViewModelBase`, `.CreateBoxScoreSessionIfNeededAsync`, `FFMpegUtils`, `RecordingJsonElapsedOffset`, `BoxScoreUploader`, `CombineFile`, `DrawText Overlay Utils`, `EncodingSettingsDto`?**
  _High betweenness centrality (0.220) - this node is a cross-community bridge._
- **Why does `TrimViewModel` connect `TrimViewModel` to `.PrependIntroAsync`, `TrimView`, `RecordingJsonElapsedOffset`, `LogViewModel`, `.OnPropertyChanged`, `.Log`, `ExportTrimState`, `StartStopRange`, `MP4ToolsLib`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **What connects `net10.0`, `Avalonia (12.1.2)`, `Avalonia.Desktop (12.1.2)` to the rest of the system?**
  _141 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.07204968944099378 - nodes in this community are weakly interconnected._
- **Should `MP4ViewModelBase` be split into smaller, more focused modules?**
  _Cohesion score 0.05574912891986063 - nodes in this community are weakly interconnected._