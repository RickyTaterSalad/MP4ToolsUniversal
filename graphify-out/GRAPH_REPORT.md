# Graph Report - MP4ToolsUniversal  (2026-09-10)

## Corpus Check
- 50 files · ~18,098 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 562 nodes · 1046 edges · 29 communities (25 shown, 4 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 23 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e20c6235`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .PrependIntroAsync
- MP4ToolsLib
- MP4ViewModelBase
- FFMpegUtils
- .Log
- CombineViewModel
- TrimViewModel
- RecordingJsonElapsedOffset
- CombineView
- BoxScoreUploader
- AppUserSettings
- LogViewModel
- DrawTextPosition
- DaVinci Resolve crash: `1.mp4` vs `1_working.mp4`
- TrimView
- MP4Tools.csproj
- .OnPropertyChanged
- OptionsViewModel
- TimeRange
- EncodingSettingsDto
- CombineFile
- StartStopRange
- ExportTrimState
- BaseballLoggerSettingsRuntime
- DefaultOutputPathRuntime
- UiBehaviorSettingsRuntime
- .UpdateOutputPathFromGameInfo
- .ScheduleEdgeDurationRefresh
- .ApplyDefaultSkipRanges

## God Nodes (most connected - your core abstractions)
1. `CombineViewModel` - 62 edges
2. `TrimViewModel` - 45 edges
3. `MP4ToolsLib` - 30 edges
4. `FFMpegUtils` - 26 edges
5. `MP4ViewModelBase` - 24 edges
6. `AppUserSettings` - 21 edges
7. `FfmpegOption` - 18 edges
8. `RecordingJsonElapsedOffset` - 18 edges
9. `LogViewModel` - 17 edges
10. `TimeRange` - 17 edges

## Surprising Connections (you probably didn't know these)
- `CombineViewModel` --references--> `CombineFile`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/CombineFile.cs
- `CombineViewModel` --references--> `TimeRange`  [EXTRACTED]
  MP4Tools/ViewModels/CombineViewModel.cs → MP4ToolsLib/TimeRange.cs
- `ExportTrimState` --references--> `StartStopRange`  [EXTRACTED]
  MP4Tools/ViewModels/TrimViewModel.cs → MP4ToolsLib/Ranges.cs
- `TrimViewModel` --references--> `StartStopRange`  [EXTRACTED]
  MP4Tools/ViewModels/TrimViewModel.cs → MP4ToolsLib/Ranges.cs
- `TrimViewModel` --references--> `TimeRange`  [EXTRACTED]
  MP4Tools/ViewModels/TrimViewModel.cs → MP4ToolsLib/TimeRange.cs

## Import Cycles
- None detected.

## Communities (29 total, 4 thin omitted)

### Community 0 - ".PrependIntroAsync"
Cohesion: 0.07
Nodes (23): IEnumerable, IProgress, CancellationToken, List, Task, ICollection, DaVinciOutputEncoding, FfmpegArguments (+15 more)

### Community 1 - "MP4ToolsLib"
Cohesion: 0.05
Nodes (24): AppBuilder, Application, Control, MP4Tools.ViewModels, MP4Tools.Services, MP4Tools.Views, MP4Tools, MP4ToolsLib (+16 more)

### Community 2 - "MP4ViewModelBase"
Cohesion: 0.06
Nodes (27): CancellationToken, CancellationTokenSource, EventArgs, Process, RelayCommand, Task, MP4ViewModelBase, CanClear (+19 more)

### Community 3 - "FFMpegUtils"
Cohesion: 0.14
Nodes (17): audio, height, Lazy, Action, CancellationToken, HashSet, JsonElement, Process (+9 more)

### Community 4 - ".Log"
Cohesion: 0.11
Nodes (12): Exception, Logger, CancellationToken, JsonNode, Task, Action, CancellationToken, JsonElement (+4 more)

### Community 5 - "CombineViewModel"
Cohesion: 0.07
Nodes (24): AsyncRelayCommand, DateTime, IReadOnlyList, ObservableCollection, RelayCommand, CombineViewModel, CanCombine, CombineCommand (+16 more)

### Community 6 - "TrimViewModel"
Cohesion: 0.07
Nodes (26): AsyncRelayCommand, IReadOnlyList, ObservableCollection, RelayCommand, TimeSpan, TrimViewModel, AddTimeRangeCommand, CanTrim (+18 more)

### Community 7 - "RecordingJsonElapsedOffset"
Cohesion: 0.18
Nodes (13): Elapsed, JsonObject, Action, CancellationToken, DateTime, HashSet, JsonNode, JsonSerializerOptions (+5 more)

### Community 8 - "CombineView"
Cohesion: 0.11
Nodes (10): DataFormat, DragEventArgs, PointerPressedEventArgs, RoutedEventArgs, CombineView, EventArgs, LogView, OptionsView (+2 more)

### Community 9 - "BoxScoreUploader"
Cohesion: 0.26
Nodes (8): HttpClient, Action, CancellationToken, JsonElement, Task, Uri, BoxScoreUploader, UploadBoxScoreResult

### Community 10 - "AppUserSettings"
Cohesion: 0.16
Nodes (13): JsonSerializerOptions, AppSettingsStore, Current, SettingsFilePath, AppUserSettings, BaseballLoggerApiKey, BaseballLoggerServerUrl, DefaultOutputDirectory (+5 more)

### Community 11 - "LogViewModel"
Cohesion: 0.16
Nodes (10): ConcurrentQueue, RelayCommand, Task, TimeSpan, LogViewModel, AutoScrollToBottom, ClearLogCommand, LogText (+2 more)

### Community 12 - "DrawTextPosition"
Cohesion: 0.16
Nodes (12): IReadOnlyList, DrawTextPositionOption, DisplayName, Value, DrawTextUtils, DrawTextPositionOptions, DrawTextPositions, DrawTextPosition (+4 more)

### Community 13 - "DaVinci Resolve crash: `1.mp4` vs `1_working.mp4`"
Cohesion: 0.13
Nodes (14): 1. Missing `ctts` with B-frames present, 2. Bad trim timing / wrong sample count, 3. 10-bit HEVC vs 8-bit re-encode, 4. Missing color metadata in the container, Bottom line, DaVinci Resolve crash: `1.mp4` vs `1_working.mp4`, How each file was made, Key differences (+6 more)

### Community 14 - "TrimView"
Cohesion: 0.22
Nodes (6): DataFormat, DragEventArgs, List, PointerPressedEventArgs, RoutedEventArgs, TrimView

### Community 15 - "MP4Tools.csproj"
Cohesion: 0.15
Nodes (11): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, Avalonia (12.1.1), Avalonia.Desktop (12.1.1), Avalonia.Fonts.Inter (12.1.1), Avalonia.Themes.Simple (12.1.1) (+3 more)

### Community 17 - "OptionsViewModel"
Cohesion: 0.22
Nodes (6): EventArgs, RelayCommand, Task, OptionsViewModel, ApiKeyPasswordChar, SettingsFilePathDisplay

### Community 18 - "TimeRange"
Cohesion: 0.20
Nodes (7): INotifyPropertyChanged, IReadOnlyList, TimeRange, Hours, Minutes, Range, TotalSeconds

### Community 19 - "EncodingSettingsDto"
Cohesion: 0.22
Nodes (8): EncodingSettingsRuntime, Current, EncodingSettingsDto, AudioCodec, HardwareAcceleration, TrimAudioCodec, UseResolveSafeEncoding, VideoCodec

### Community 20 - "CombineFile"
Cohesion: 0.25
Nodes (4): IReadOnlyList, CombineFile, Name, Path

### Community 21 - "StartStopRange"
Cohesion: 0.25
Nodes (5): StartStopRange, EndRange, Label, SelectedDrawTextPosition, StartRange

### Community 22 - "ExportTrimState"
Cohesion: 0.29
Nodes (4): ExportTrimState, InputFile, OutputFolderName, StartStopRanges

### Community 23 - "BaseballLoggerSettingsRuntime"
Cohesion: 0.40
Nodes (3): BaseballLoggerSettingsRuntime, ApiKey, ServerUrl

### Community 24 - "DefaultOutputPathRuntime"
Cohesion: 0.40
Nodes (3): DefaultOutputPathRuntime, BuiltinFallbackDirectory, Directory

### Community 25 - "UiBehaviorSettingsRuntime"
Cohesion: 0.40
Nodes (3): UiBehaviorSettingsRuntime, DeleteTrimSegmentsAfterTrimAndCombine, OpenOutputFolderOnComplete

## Knowledge Gaps
- **136 isolated node(s):** `net10.0`, `Avalonia (12.1.1)`, `Avalonia.Desktop (12.1.1)`, `Avalonia.Fonts.Inter (12.1.1)`, `Avalonia.Themes.Simple (12.1.1)` (+131 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 216 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MP4ToolsLib` connect `MP4ToolsLib` to `.PrependIntroAsync`, `MP4ViewModelBase`, `FFMpegUtils`, `.Log`, `BoxScoreUploader`, `DrawTextPosition`, `TimeRange`, `EncodingSettingsDto`, `CombineFile`?**
  _High betweenness centrality (0.242) - this node is a cross-community bridge._
- **Why does `CombineViewModel` connect `CombineViewModel` to `.PrependIntroAsync`, `MP4ToolsLib`, `.Log`, `LogViewModel`, `.OnPropertyChanged`, `TimeRange`, `CombineFile`, `.UpdateOutputPathFromGameInfo`, `.ScheduleEdgeDurationRefresh`, `.ApplyDefaultSkipRanges`?**
  _High betweenness centrality (0.226) - this node is a cross-community bridge._
- **Why does `TrimViewModel` connect `TrimViewModel` to `.PrependIntroAsync`, `MP4ToolsLib`, `.Log`, `LogViewModel`, `.OnPropertyChanged`, `TimeRange`, `StartStopRange`, `ExportTrimState`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **What connects `net10.0`, `Avalonia (12.1.1)`, `Avalonia.Desktop (12.1.1)` to the rest of the system?**
  _136 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.PrependIntroAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06807511737089202 - nodes in this community are weakly interconnected._
- **Should `MP4ToolsLib` be split into smaller, more focused modules?**
  _Cohesion score 0.054426705370101594 - nodes in this community are weakly interconnected._
- **Should `MP4ViewModelBase` be split into smaller, more focused modules?**
  _Cohesion score 0.05574912891986063 - nodes in this community are weakly interconnected._