using System.Collections.Generic;

namespace MP4ToolsLib;

public sealed class InningDetectionEvent
{
	public double ElapsedSeconds { get; init; }
	public string Label { get; init; }
	public string Kind { get; init; }
}

public sealed class InningDetectionResult
{
	public string VideoPath { get; init; }
	public string OutputJsonPath { get; init; }
	public string OutputYoutubeDescriptionPath { get; init; }
	public double DurationSeconds { get; init; }
	public IReadOnlyList<InningDetectionEvent> Events { get; init; }
}
