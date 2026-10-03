using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MP4ToolsLib;

/// <summary>Parses ffmpeg stderr progress lines (<c>time=HH:MM:SS.xx</c>).</summary>
public static class FfmpegProgressParser
{
	private static readonly Regex FfmpegTimeRegex = new(
		@"time=\s*(-?\d{1,3}):(\d{2}):(\d{2}(?:\.\d+)?)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static bool TryParseTimeSeconds(string line, out double seconds)
	{
		seconds = 0;
		if (string.IsNullOrEmpty(line) || !line.Contains("time=", StringComparison.Ordinal))
			return false;

		var match = FfmpegTimeRegex.Match(line);
		if (!match.Success)
			return false;

		if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
			return false;
		if (!int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
			return false;
		if (!double.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var secs))
			return false;

		seconds = Math.Abs(hours) * 3600 + minutes * 60 + secs;
		return true;
	}
}
