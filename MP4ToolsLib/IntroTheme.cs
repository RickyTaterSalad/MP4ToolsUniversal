using System.Collections.Generic;

namespace MP4ToolsLib;

/// <summary>Visual preset for standalone intro generation.</summary>
public sealed class IntroTheme
{
	public string Name { get; init; } = "";
	/// <summary>lavfi color source value, e.g. <c>0x1E1E1E</c>.</summary>
	public string BackgroundColor { get; init; } = "0x1E1E1E";
	/// <summary>drawtext fontcolor, e.g. <c>white</c> or <c>0xFFFFFF</c>.</summary>
	public string TextColor { get; init; } = "white";
	public int TitleFontSize { get; init; } = 128;
	public int SubtitleFontSize { get; init; } = 64;
	public int DetailsFontSize { get; init; } = 48;
	/// <summary>Base vertical gap between title/subtitle before resolution scaling.</summary>
	public int LineGap { get; init; } = 36;

	public static IReadOnlyList<IntroTheme> Presets { get; } =
	[
		new IntroTheme
		{
			Name = "Classic Dark",
			BackgroundColor = "0x1E1E1E",
			TextColor = "white",
			TitleFontSize = 128,
			SubtitleFontSize = 64,
			DetailsFontSize = 48,
			LineGap = 36,
		},
		new IntroTheme
		{
			Name = "Pure Black",
			BackgroundColor = "0x000000",
			TextColor = "white",
			TitleFontSize = 128,
			SubtitleFontSize = 64,
			DetailsFontSize = 48,
			LineGap = 36,
		},
		new IntroTheme
		{
			Name = "Navy",
			BackgroundColor = "0x0B1F3A",
			TextColor = "white",
			TitleFontSize = 120,
			SubtitleFontSize = 56,
			DetailsFontSize = 44,
			LineGap = 32,
		},
		new IntroTheme
		{
			Name = "Forest",
			BackgroundColor = "0x0F2A1D",
			TextColor = "0xE8F5E9",
			TitleFontSize = 120,
			SubtitleFontSize = 56,
			DetailsFontSize = 44,
			LineGap = 32,
		},
		new IntroTheme
		{
			Name = "Light",
			BackgroundColor = "0xF5F5F5",
			TextColor = "0x1A1A1A",
			TitleFontSize = 128,
			SubtitleFontSize = 64,
			DetailsFontSize = 48,
			LineGap = 36,
		},
		new IntroTheme
		{
			Name = "High Contrast",
			BackgroundColor = "0x000000",
			TextColor = "0xFFFF00",
			TitleFontSize = 140,
			SubtitleFontSize = 72,
			DetailsFontSize = 56,
			LineGap = 40,
		},
	];
}
