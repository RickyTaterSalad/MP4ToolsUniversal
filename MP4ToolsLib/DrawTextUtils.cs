using System;
using System.Collections.Generic;

namespace MP4ToolsLib
{
	public class DrawTextPositionOption
	{
		public DrawTextPosition Value { get; set; }
		public string DisplayName { get; set; } = string.Empty;
	}

	public static class DrawTextUtils
	{
		public static IReadOnlyList<DrawTextPositionOption> DrawTextPositionOptions { get; } = new List<DrawTextPositionOption>
		{
			new DrawTextPositionOption { Value = DrawTextPosition.TopLeft, DisplayName = "Top Left" },
			new DrawTextPositionOption { Value = DrawTextPosition.TopRight, DisplayName = "Top Right" },
			new DrawTextPositionOption { Value = DrawTextPosition.BottomLeft, DisplayName = "Bottom Left" },
			new DrawTextPositionOption { Value = DrawTextPosition.BottomRight, DisplayName = "Bottom Right" },
		};

		public static IReadOnlyList<DrawTextPosition> DrawTextPositions { get; } = Enum.GetValues<DrawTextPosition>();

		public static string EscapeDrawTextValue(string value)
		{
			return (value ?? string.Empty)
				.Replace("\\", "\\\\")
				.Replace(":", "\\:")
				.Replace("'", "\\'")
				.Replace("%", "\\%");
		}

		public static string CreateVideoOverlayText(string value, DrawTextPosition position)
		{
			var escapedValue = EscapeDrawTextValue(value);
			var positionPart = position switch
			{
				DrawTextPosition.TopLeft => "x=10:y=10",
				DrawTextPosition.TopRight => "x=w-text_w-10:y=10",
				DrawTextPosition.BottomLeft => "x=10:y=h-text_h-10",
				DrawTextPosition.BottomRight => "x=w-text_w-10:y=h-text_h-10",
				_ => "x=10:y=10"
			};
			var font = OperatingSystem.IsLinux() ? 
			"/usr/share/fonts/truetype/noto/NotoSansMono-Regular.ttf" : "C:/Windows/Fonts/calibri.ttf";
			return $"\"drawtext=fontfile='{font}':text='{escapedValue}':fontcolor=white:fontsize=64:box=1:boxcolor=black@0.75:boxborderw=5:{positionPart}\"";
		}
	}
}