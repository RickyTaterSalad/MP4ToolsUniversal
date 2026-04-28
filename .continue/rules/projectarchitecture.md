---
description: A description of your rule
---

## Codebase Overview: MP4Tools

This is a C#/.NET 10 application built with Avalonia UI for Windows/Linux/macOS that provides MP4 video editing functionality. It's a desktop application designed for combining and trimming MP4 video files.

### Key Components

1. **MP4ToolsLib** - The core library with video processing functionality
   - FFmpeg integration for video operations
   - File processing utilities
   - Video information probing
   - CombineFile handling
2. **MP4Tools** - The main application project
   - Avalonia UI application with MVVM pattern
   - ViewModels for different operations (combine, trim)
   - Views for user interface

### Main Features

- **Video Combination**: Combine multiple MP4 files into one
- **Video Trimming**: Trim start and end of videos
- **Intro Generation**: Create intro clips with customizable text
- **Audio Codec Selection**: Choose audio codecs for output
- **FFmpeg Integration**: Uses FFmpeg for all video processing

### Key Files and Functionality

1. **CombineViewModel.cs**: Core functionality for combining videos
   - Supports intro clips with customizable titles/subtitles/details
   - Video trimming capabilities
   - Output path management
   - Temp file handling for processing
2. **FFMpegUtils.cs**: FFmpeg integration layer
   - Process management for FFmpeg/ffprobe
   - Duration detection
   - Video codec information retrieval
   - Hardware acceleration detection
3. **IntroVideoComposerAsync.cs**: Creates intro clips with text overlays
   - Customizable intro duration
   - Title, subtitle, and details text
   - Text rendering using FFmpeg's drawtext filter

### Architecture

- **MVVM Pattern**: ViewModels handle business logic, Views are UI components
- **Avalonia UI**: Cross-platform UI framework
- **FFmpeg Integration**: Core video processing powered by FFmpeg
- **Async Operations**: All long-running operations use async/await patterns

### Key Technical Details

1. **Cross-platform Support**: Works on Windows, Linux, and macOS
2. **Hardware Acceleration**: Detects and uses GPU acceleration (CUDA, VAAPI, D3D11VA)
3. **Robust Error Handling**: Process management with timeout and error recovery
4. **Temp File Management**: Proper cleanup of temporary files
5. **Configuration**: Uses standard .NET configuration patterns

### Usage

The application allows users to:

1. Select a folder of MP4 files to combine
2. Add customizable intro clips
3. Set start/end trimming parameters
4. Specify game information (scores, teams, etc.) for naming
5. Choose output location and audio codec