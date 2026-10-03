using System;
using System.Collections.Generic;

namespace MP4ToolsLib;

public static class InningHalfLabels
{
	public const string RecordingStart = "Recording Start";
	public const string RecordingEnd = "Recording End";

	public static string FormatHalfInning(int halfIndexZeroBased)
	{
		var inning = halfIndexZeroBased / 2 + 1;
		var isTop = halfIndexZeroBased % 2 == 0;
		return $"{(isTop ? "Top" : "Bottom")} {Ordinal(inning)}";
	}

	public static string Ordinal(int n)
	{
		var mod100 = n % 100;
		if (mod100 is >= 11 and <= 13)
			return $"{n}th";
		return (n % 10) switch
		{
			1 => $"{n}st",
			2 => $"{n}nd",
			3 => $"{n}rd",
			_ => $"{n}th",
		};
	}

	public static IReadOnlyList<string> BuildSequence(int halfInningCount)
	{
		var count = Math.Max(0, halfInningCount);
		var list = new List<string>(count);
		for (var i = 0; i < count; i++)
			list.Add(FormatHalfInning(i));
		return list;
	}
}
