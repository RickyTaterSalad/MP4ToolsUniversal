using System;

namespace MP4ToolsLib;

/// <summary>Structured fields extracted from an intro title card image.</summary>
public sealed class IntroScreenReadResult
{
	public string Title { get; set; } = "";
	public string Subtitle { get; set; } = "";
	public string Details { get; set; } = "";
	public string VisitorName { get; set; } = "";
	public string HomeName { get; set; } = "";
	public int? VisitorScore { get; set; }
	public int? HomeScore { get; set; }
	public DateTime? EventDate { get; set; }
	public string EventInfo { get; set; } = "";
}
