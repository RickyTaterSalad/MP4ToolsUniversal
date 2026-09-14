# Graph Report - MP4ToolsUniversal  (2026-09-13)

## Corpus Check
- 52 files · ~21,800 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 658 nodes · 1279 edges · 30 communities (24 shown, 4 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 39 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `cd1737d6`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- MP4ViewModelBase
- TrimView
- .Log
- FFMpegUtils
- CombineViewModel
- RecordingJsonElapsedOffset
- TrimViewModel
- BoxScoreUploader
- AppUserSettings
- OptionsViewModel
- Resolve MP4 Compatibility
- DrawText Overlay Utils
- Project Dependencies
- LogViewModel
- TimeRange
- .OnPropertyChanged
- BaseballLoggerSettingsRuntime
- EncodingSettingsDto
- .ScheduleEdgeDurationRefresh
- DefaultOutputPathRuntime
- UiBehaviorSettingsRuntime
- CombineView
- .SyncSelectedInputFiles
- ExportTrimState
- StartStopRange
- .OnInputPathSet
- MP4ToolsLib

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 87 edges
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

## Communities (30 total, 4 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.07
Nodes (23): IProgress, CancellationToken, List, Task, ICollection, DaVinciOutputEncoding, IEnumerable, FfmpegArguments (+15 more)

### Community 1 - "MP4ViewModelBase"
Cohesion: 0.05
Nodes (27): CancellationToken, CancellationTokenSource, EventArgs, Process, RelayCommand, Task, MP4ViewModelBase, CanClear (+19 more)

### Community 2 - "TrimView"
Cohesion: 0.12
Nodes (12): DataFormat, IDataTransfer, DragEventArgs, HashSet, IReadOnlyList, Task, FileDropHelper, DragEventArgs (+4 more)

### Community 3 - ".Log"
Cohesion: 0.08
Nodes (20): Exception, Logger, CancellationToken, JsonNode, Task, Action, CancellationToken, JsonElement (+12 more)

### Community 4 - "FFMpegUtils"
Cohesion: 0.15
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 5 - "CombineViewModel"
Cohesion: 0.06
Nodes (29): AsyncRelayCommand, DateTime, List, ObservableCollection, RelayCommand, CombineViewModel, CanCombine, CombineCommand (+21 more)

### Community 6 - "RecordingJsonElapsedOffset"
Cohesion: 0.17
Nodes (13): Elapsed, JsonObject, Action, CancellationToken, DateTime, HashSet, JsonNode, JsonSerializerOptions (+5 more)

### Community 7 - "TrimViewModel"
Cohesion: 0.07
Nodes (28): AsyncRelayCommand, IReadOnlyList, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand, CanTrim (+20 more)

### Community 8 - "BoxScoreUploader"
Cohesion: 0.26
Nodes (8): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult

### Community 9 - "AppUserSettings"
Cohesion: 0.16
Nodes (13): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+5 more)

### Community 10 - "OptionsViewModel"
Cohesion: 0.23
Nodes (6): EventArgs, RelayCommand, Task, OptionsViewModel, ApiKeyPasswordChar, SettingsFilePathDisplay

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
Cohesion: 0.09
Nodes (15): ConcurrentQueue, RelayCommand, Task, TimeSpan, LogViewModel, AutoScrollToBottom, ClearLogCommand, LogText (+7 more)

### Community 15 - "TimeRange"
Cohesion: 0.20
Nodes (7): INotifyPropertyChanged, IReadOnlyList, TimeRange, Hours, Minutes, Range, TotalSeconds

### Community 17 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 18 - "EncodingSettingsDto"
Cohesion: 0.22
Nodes (8): EncodingSettingsRuntime, Current, EncodingSettingsDto, AudioCodec, HardwareAcceleration, TrimAudioCodec, UseResolveSafeEncoding, VideoCodec

### Community 20 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 23 - "UiBehaviorSettingsRuntime"
Cohesion: 0.40
Nodes (3): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, OpenOutputFolderOnComplete

### Community 24 - "CombineView"
Cohesion: 0.07
Nodes (23): DispatcherTimer, IPointer, ListBoxItem, EventArgs, IReadOnlyList, List, PointerPressedEventArgs, RoutedEventArgs (+15 more)

### Community 27 - "ExportTrimState"
Cohesion: 0.33
Nodes (4): ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 28 - "StartStopRange"
Cohesion: 0.16
Nodes (9): StartStopRange, EndRange, EndRangeDisplay, HasEndBound, InputPath, Label, SelectedDrawTextPosition, SourceFileName (+1 more)

### Community 30 - "MP4ToolsLib"
Cohesion: 0.05
Nodes (25): AppBuilder, Application, Control, MP4Tools.ViewModels, MP4Tools.Services, MP4Tools.Views, MP4Tools, MP4ToolsLib (+17 more)

## Ambiguous Edges - Review These
- `DaVinci Resolve` → `Opus audio`  [AMBIGUOUS]
  MP4Tools/Notes/1_vs_1_working_analysis.md · relation: conceptually_related_to

## Knowledge Gaps
- **141 isolated node(s):** `net10.0`, `Avalonia (12.1.2)`, `Avalonia.Desktop (12.1.2)`, `Avalonia.Fonts.Inter (12.1.2)`, `Avalonia.Themes.Simple (12.1.2)` (+136 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 231 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `DaVinci Resolve` and `Opus audio`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `MP4ViewModelBase`, `.Log`, `LogViewModel`, `TimeRange`, `.OnPropertyChanged`, `.ScheduleEdgeDurationRefresh`, `CombineView`, `.SyncSelectedInputFiles`, `.RefreshCanCombineFromInputs`, `.OnInputPathSet`, `MP4ToolsLib`?**
  _High betweenness centrality (0.288) - this node is a cross-community bridge._
- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `MP4ViewModelBase`, `.Log`, `RecordingJsonElapsedOffset`, `BoxScoreUploader`, `DrawText Overlay Utils`, `TimeRange`, `EncodingSettingsDto`, `CombineView`?**
  _High betweenness centrality (0.222) - this node is a cross-community bridge._
- **Why does `TrimViewModel` connect `TrimViewModel` to `.PrependIntroAsync`, `MP4ViewModelBase`, `LogViewModel`, `TimeRange`, `.OnPropertyChanged`, `.ReadFileInfoAsync`, `ExportTrimState`, `StartStopRange`, `MP4ToolsLib`?**
  _High betweenness centrality (0.113) - this node is a cross-community bridge._
- **What connects `net10.0`, `Avalonia (12.1.2)`, `Avalonia.Desktop (12.1.2)` to the rest of the system?**
  _141 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06630630630630631 - nodes in this community are weakly interconnected._
- **Should `MP4ViewModelBase` be split into smaller, more focused modules?**
  _Cohesion score 0.052854122621564484 - nodes in this community are weakly interconnected._