namespace MP4Tools.ViewModels;

/// <summary>Intro text/style captured from Generate Intro; encoded later during Apply Edits.</summary>
public sealed class PendingIntroSpec
{
	public string Title { get; init; } = string.Empty;
	public string Subtitle { get; init; } = string.Empty;
	public string Details { get; init; } = string.Empty;
	public int DurationSeconds { get; init; } = 10;
	public int TitleFontSize { get; init; } = 128;
	public int SubtitleFontSize { get; init; } = 64;
	public int DetailsFontSize { get; init; } = 48;
	public int LineGap { get; init; } = 36;
	public string BackgroundColor { get; init; } = "0x1E1E1E";
	public string TextColor { get; init; } = "white";

	public bool HasContent =>
		!string.IsNullOrWhiteSpace(Title)
		|| !string.IsNullOrWhiteSpace(Subtitle)
		|| !string.IsNullOrWhiteSpace(Details);

	public string DisplaySummary
	{
		get
		{
			var line = FirstNonEmpty(Title, Subtitle, Details);
			if (string.IsNullOrWhiteSpace(line))
				return "Intro ready";
			line = line.Trim();
			if (line.Length > 40)
				line = line[..37] + "…";
			return $"{line} ({DurationSeconds}s)";
		}
	}

	private static string FirstNonEmpty(params string[] parts)
	{
		foreach (var p in parts)
		{
			if (!string.IsNullOrWhiteSpace(p))
				return p;
		}
		return string.Empty;
	}

	public void NormalizeTitleFields(out string title, out string subtitle, out string details)
	{
		title = Title?.Trim() ?? string.Empty;
		subtitle = Subtitle?.Trim() ?? string.Empty;
		details = Details?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(subtitle))
		{
			title = subtitle;
			subtitle = string.Empty;
		}
		if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(details) && string.IsNullOrWhiteSpace(subtitle))
		{
			title = details;
			details = string.Empty;
		}
	}
}
