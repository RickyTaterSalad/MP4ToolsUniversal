using System;

namespace MP4Tools;

public enum LogOperationSource
{
	None = 0,
	Combine,
	Trim,
	RewriteIntro,
	GenerateIntro,
	ReplaceSegment,
	InningDetector,
	LrfInningDetector,
}

/// <summary>Tracks which feature last started a logged ffmpeg/ffprobe operation.</summary>
public static class LogOperationTracker
{
	private static LogOperationSource _current = LogOperationSource.None;

	public static LogOperationSource Current => _current;

	public static event EventHandler Changed;

	public static void Set(LogOperationSource source)
	{
		if (source == LogOperationSource.None || _current == source)
			return;

		_current = source;
		Changed?.Invoke(null, EventArgs.Empty);
	}

	public static string GetDisplayName(LogOperationSource source) => source switch
	{
		LogOperationSource.Combine => "Combine",
		LogOperationSource.Trim => "Trim",
		LogOperationSource.RewriteIntro => "Rewrite Intro",
		LogOperationSource.GenerateIntro => "Generate Intro",
		LogOperationSource.ReplaceSegment => "Replace Segment",
		LogOperationSource.InningDetector => "Inning Detector",
		LogOperationSource.LrfInningDetector => "LRF Inning Detect",
		_ => string.Empty,
	};
}
