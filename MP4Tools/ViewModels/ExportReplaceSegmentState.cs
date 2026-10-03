using System.Collections.Generic;

namespace MP4Tools.ViewModels;

/// <summary>JSON payload for Replace Segment export/import.</summary>
internal sealed class ExportReplaceSegmentState
{
	public string InputFile { get; set; } = string.Empty;
	public string OutputPath { get; set; } = string.Empty;
	public bool DraftReplaceWithText { get; set; }
	public ExportTimeParts DraftStart { get; set; } = new();
	public ExportTimeParts DraftEnd { get; set; } = new();
	public List<ExportReplaceSegmentRange> Segments { get; set; } = new();
}

internal sealed class ExportTimeParts
{
	public int Hours { get; set; }
	public int Minutes { get; set; }
	public int Seconds { get; set; }
}

internal sealed class ExportReplaceSegmentRange
{
	public ExportTimeParts Start { get; set; } = new();
	public ExportTimeParts End { get; set; } = new();
	public bool ReplaceWithText { get; set; }
	public string Label { get; set; } = string.Empty;
	public ExportPendingIntro PendingIntro { get; set; }
}

internal sealed class ExportPendingIntro
{
	public string Title { get; set; } = string.Empty;
	public string Subtitle { get; set; } = string.Empty;
	public string Details { get; set; } = string.Empty;
	public int DurationSeconds { get; set; } = 10;
	public int TitleFontSize { get; set; } = 128;
	public int SubtitleFontSize { get; set; } = 64;
	public int DetailsFontSize { get; set; } = 48;
	public int LineGap { get; set; } = 36;
	public string BackgroundColor { get; set; } = "0x1E1E1E";
	public string TextColor { get; set; } = "white";
}
