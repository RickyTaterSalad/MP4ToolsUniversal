using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP4ToolsLib.Tests.Configuration;

/// <summary>
/// Media roots for integration tests (optional — tests skip when unset).
/// Environment variables:
/// <list type="bullet">
/// <item><description><c>MP4TOOLS_TEST_TRIM_INPUT_DIR</c> — folder containing long clips for trim-style HW encode tests.</description></item>
/// <item><description><c>MP4TOOLS_TEST_COMBINE_INPUT_DIR</c> — folder with multiple MP4s for concat-style HW encode tests.</description></item>
/// <item><description><c>MP4TOOLS_TEST_OUTPUT_DIR</c> — optional; defaults to <c>%TEMP%/MP4ToolsTests</c>.</description></item>
/// <item><description><c>MP4TOOLS_TEST_MIN_DURATION_SECONDS</c> — optional; minimum duration (default 600 = 10 minutes) required before inner-bound trim tests run.</description></item>
/// </list>
/// </summary>
public static class TestMediaConfiguration
{
	public static string TrimInputDirectory =>
		(Environment.GetEnvironmentVariable("MP4TOOLS_TEST_TRIM_INPUT_DIR") ?? string.Empty).Trim();

	public static string CombineInputDirectory =>
		(Environment.GetEnvironmentVariable("MP4TOOLS_TEST_COMBINE_INPUT_DIR") ?? string.Empty).Trim();

	public static string OutputDirectory
	{
		get
		{
			var o = (Environment.GetEnvironmentVariable("MP4TOOLS_TEST_OUTPUT_DIR") ?? string.Empty).Trim();
			return string.IsNullOrEmpty(o)
				? Path.Combine(Path.GetTempPath(), "MP4ToolsTests")
				: Path.GetFullPath(o);
		}
	}

	public static int MinimumDurationSecondsForBoundsTests
	{
		get
		{
			var raw = Environment.GetEnvironmentVariable("MP4TOOLS_TEST_MIN_DURATION_SECONDS");
			if (int.TryParse(raw, out var n) && n > 0)
				return n;
			return 600;
		}
	}

	public static bool HasTrimMedia =>
		!string.IsNullOrEmpty(TrimInputDirectory) && Directory.Exists(TrimInputDirectory);

	public static bool HasCombineMedia =>
		!string.IsNullOrEmpty(CombineInputDirectory) && Directory.Exists(CombineInputDirectory);

	public static string FirstTrimVideoOrEmpty()
	{
		if (!HasTrimMedia)
			return string.Empty;
		return Directory.EnumerateFiles(TrimInputDirectory, "*.mp4", SearchOption.TopDirectoryOnly)
			.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault() ?? string.Empty;
	}

	public static IReadOnlyList<string> CombineVideosTopLevel(int maxFiles = 8)
	{
		if (!HasCombineMedia)
			return Array.Empty<string>();
		return Directory.EnumerateFiles(CombineInputDirectory, "*.mp4", SearchOption.TopDirectoryOnly)
			.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
			.Take(maxFiles)
			.ToArray();
	}
}
