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
