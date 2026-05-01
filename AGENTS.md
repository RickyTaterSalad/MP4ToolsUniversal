# Repository Guidelines

Welcome to the MP4ToolsUniversal repository! This guide provides essential information for contributing to this MP4 editing tool.

## Project Structure & Module Organization

The repository follows a modular structure with two main components:

- **MP4ToolsLib/**: Core library containing video processing utilities and business logic
  - `FFMpegUtils.cs`: FFMPEG integration for video processing
  - `CombineFile.cs`: File combining functionality
  - `IntroVideoComposerAsync.cs`: Intro video composition
  - `FileUtils.cs`: File manipulation utilities

- **MP4Tools/**: Avalonia-based desktop application UI
  - `Program.cs`: Entry point
  - `Views/`: UI view files (e.g., TrimView.axaml.cs)
  - `ViewModels/`: View models for MVVM pattern
  - `Assets/`: Application resources

## Build, Test, and Development Commands

- **Build**: `dotnet build`
- **Run**: `dotnet run` (from MP4Tools directory)
- **Debug**: `dotnet build -c Debug`
- **Release**: `dotnet build -c Release`

## Coding Style & Naming Conventions

- C# code follows .NET naming conventions
- Class names use PascalCase (e.g., `FFMpegUtils`)
- Method names use PascalCase (e.g., `CombineFile`)
- Variable names use camelCase (e.g., `videoPath`)
- Indentation: 4 spaces (no tabs)

## Testing Guidelines

The project uses .NET's built-in testing capabilities:
- Tests are written in C# using xUnit or MSTest
- Test files follow the naming pattern `*Tests.cs`
- Run tests with: `dotnet test`

## Commit & Pull Request Guidelines

- Commit messages follow the format: `type(scope): description`
- Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`
- Example: `feat(video): add support for MP4 trimming`
- PRs should reference related issues
- Include screenshots for UI changes

## Architecture Overview

This is a .NET 10.0 desktop application using Avalonia UI framework for cross-platform compatibility. The application uses FFMPEG for video processing through the MP4ToolsLib library.

## FFmpegArgumentBuilder - Fluent Argument Construction

The project includes an `FFmpegArgumentBuilder` fluent API in `MP4ToolsLib/FFmpegArguments/FFmpegArgumentBuilder.cs` for constructing FFmpeg command-line arguments programmatically. This replaces raw string interpolation with type-safe, composable building blocks.

### Core Types

- **`FFmpegArgumentBuilder`**: Top-level builder. Methods return the builder for chaining.
  - `AddGlobalOption("-nostdin")` / `AddGlobalOption("-y")` — adds global flags
  - `WithHardwareAccel("vaapi")` / `WithVaapiDevice("/dev/dri/renderD128")` — hardware acceleration
  - `AddInput(path, config?)` / `AddOutput(path, config?)` — input/output with optional callbacks
  - `AddConcatInput(fileListPath)` / `AddConcatProtocol("file1.ts|file2.ts")` — concat modes
  - `AddRawArguments("...")` — raw passthrough for untyped arguments
  - `.Build()` — produces the final argument string

- **`InputGroup` / `OutputGroup` / `ConcatInputGroup` / `ConcatProtocolGroup`**: Each extends `ArgumentGroup` and provides domain-specific configuration methods:
  - `InputGroup`: `SeekTo()`, `Duration()`, `WithFormat()`
  - `OutputGroup`: `WithVideoCodec()`, `WithAudioCodec()`, `WithPixelFormat()`, `AsMpegTs()`, `AsMp4()`, `Shortest()`, `WithVideoPreset()`, `WithVideoTune()`, `WithMovFlags()`, `WithRateControl()`, `WithVideoProfile()`, `WithVideoBitrate()`, `WithMaxVideoBitrate()`
  - `ConcatProtocolGroup`: `WithFormat()`

- **Common methods on all groups** (via `ArgumentGroup` base class):
  - `AddFlag(string)` — adds a bare flag (e.g., `-shortest`)
  - `AddKeyValue(key, value)` — adds `-key value`
  - `AddVideoFilter(string)` / `AddAudioFilter(string)` / `AddComplexFilter(string)` — filter chains
  - `AddMap(string)` — stream mapping
  - `AddBitstreamFilter(streamSpecifier, filter)` — e.g., `-bsf:v h264_mp4toannexb`

- **Value types**: `FlagArgument`, `KeyValueArgument`, `FilterArgument`, `MapArgument`, `BitstreamFilter` (public structs)

### Usage Pattern

The builder uses **callback-based configuration** for inputs and outputs to avoid type resolution issues with fluent chaining across different group types:

```csharp
new FFmpegArgumentBuilder()
    .AddGlobalOption("-nostdin")
    .AddGlobalOption("-y")
    .AddInput("input.mp4")
    .AddOutput("output.mp4", o =>
    {
        o.WithVideoCodec("libx264");
        o.WithPixelFormat("yuv420p");
        o.WithAudioCodec("aac");
        o.Shortest();
        if (useBsf)
            o.AddBitstreamFilter("a", "aac_adtstoasc");
    })
    .Build();
```

### Extension Methods

`MP4ToolsLib/FFmpegArguments/FFmpegUtilsExtensions.cs` provides extension methods on `FFMpegUtils` to accept builders directly:

- `RunCaptureFFMpegAsync(builder, ct, log)` — async FFmpeg execution
- `RunAndLogFFMpeg(builder, processOutputAction, log, workingDir)` — sync execution
- `RunAndLogFFMpeg(builder, log, workingDir)` — simplified sync

These methods call `.Build()` internally and apply forced arguments (like `-nostdin`) before execution.

### File Locations

- `MP4ToolsLib/FFmpegArguments/FFmpegArgumentBuilder.cs` — main builder and group classes
- `MP4ToolsLib/FFmpegArguments/FFmpegUtilsExtensions.cs` — extension methods for FFMpegUtils
- `MP4ToolsLib/IntroVideoComposerAsync.cs` — refactored to use the builder API

## Video Bit Depth Feature

The application now supports setting the output video bit depth for both Trim and Combine operations.

### Bit Depth Options
- **8 bit**: Standard 8-bit video output (yuv420p pixel format)
- **10 bit**: High dynamic range 10-bit video output (yuv420p10le pixel format)

### Default Behavior
- The output bit depth defaults to **10 bit** for optimal quality
- When a video file is selected, the application reads the input video's bit depth and displays it as `InputVideoBitDepth` for reference
- The user can override the default bit depth setting to change output format

### Technical Details
- The bit depth setting controls the pixel format used during video encoding:
  - 8 bit: `-pix_fmt yuv420p`
  - 10 bit: `-pix_fmt yuv420p10le`
- When copying video streams (copy mode), the original bit depth is preserved
- When re-encoding video, the selected bit depth is applied

### Implementation
- **ViewModel**: `MP4ViewModelBase` contains `VideoBitDepth` property (defaults to "10 bit")
- **View**: Both `TrimView.axaml` and `CombineView.axaml` include a bit depth dropdown
- **FFmpeg**: Uses ffprobe to read input video bit depth and applies pixel format during encoding

### Usage
1. Select your input video file or folder
2. The input video's bit depth will be displayed automatically
3. Choose your desired output bit depth from the dropdown:
   - Select **8 bit** for compatibility or reduced file size
   - Select **10 bit** for better quality and HDR support
4. Proceed with Trim or Combine operation

## Performance & Threading Best Practices

### UI Freezing Issues

The UI can freeze when performing intensive operations like video processing or logging. Here's how to avoid common pitfalls:

#### Logging to the Log Pane

**Problem**: Each FFmpeg log line was triggering an individual UI update via `InvokeAsync`, overwhelming the UI thread when FFmpeg outputs many lines rapidly.

**Solution**: Implemented batching using a `ConcurrentQueue` with delayed batch processing in [`LogViewModel.cs`](/opt/Repos/MP4ToolsUniversal/MP4Tools/ViewModels/LogViewModel.cs):

- Log messages are queued in `_logQueue` instead of directly updating the UI
- A 50ms debounce delay allows multiple log messages to accumulate
- Batches are processed together on the UI thread
- Uses `DispatcherPriority.Background` to process during UI idle time

**Additional Optimization**: For repeated ffmpeg progress lines (e.g., `frame= 3036 fps=...`), the logger now:
- Detects ffmpeg progress output patterns
- Skips repeated lines (only logs every 20th occurrence)
- Resets the counter when the line format changes

#### Async Command Execution

**Problem**: `AsyncRelayCommand` handlers that call synchronous FFmpeg operations (`RunAndLogFFMpeg`) block the UI thread, causing the interface to freeze.

**Solution**: Wrap FFmpeg operations in `Task.Run()` to execute them on background threads:

- **[`CombineViewModel.cs`](/opt/Repos/MP4ToolsUniversal/MP4Tools/ViewModels/CombineViewModel.cs#L482)**:
  ```csharp
  // Run on background thread to avoid blocking UI
  await Task.Run(() => CombineInternal());
  ```

- **[`TrimViewModel.cs`](/opt/Repos/MP4ToolsUniversal/MP4Tools/ViewModels/TrimViewModel.cs#L262)**:
  ```csharp
  // Run on background thread to avoid blocking UI
  await Task.Run(() => TrimAndCombineAsyncInternal(outTrimmedFolder, combine));
  ```

**Key Principle**: Always use `Task.Run()` for CPU-bound or blocking operations that should not freeze the UI, while keeping UI updates (logging, property changes) on the UI thread.

## Audio/Video Bitstream Filter Issues

### aac_adtstoasc Error with Opus Audio

**Error**: 
```
Codec 'opus' (86076) is not supported by the bitstream filter 'aac_adtstoasc'. 
Supported codecs are: aac (86018)
```

**Cause**: The `aac_adtstoasc` bitstream filter only works with AAC audio streams, not Opus or other codecs. The code was applying this filter unconditionally to all audio when finalizing TS-to-MP4 conversion.

**Fix**: Make the bitstream filter conditional based on the audio codec:

- **[`CombineViewModel.cs`](/opt/Repos/MP4ToolsUniversal/MP4Tools/ViewModels/CombineViewModel.cs#L752)**:
  ```csharp
  // Only apply aac_adtstoasc for AAC audio
  var aacBsf = SelectedAudioCodec.Contains("aac", StringComparison.OrdinalIgnoreCase) 
      ? "-bsf:a aac_adtstoasc" 
      : string.Empty;
  ```

**Key Principle**: Bitstream filters are codec-specific. Always verify the codec before applying filters:
- `aac_adtstoasc` → only for AAC audio
- `h264_mp4toannexb` / `hevc_mp4toannexb` → only for H.264/H.265 video
- Don't apply any BSF when copying streams that are already in the correct format
