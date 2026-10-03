using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MP4ToolsLib;

/// <summary>
/// Estimates peak disk usage for Combine / Replace Segment (final output + temporary intermediates)
/// and compares against free space on the temp and output volumes.
/// </summary>
public static class DiskSpaceEstimator
{
	/// <summary>Extra headroom on top of the calculated peak (container overhead, probes, logs).</summary>
	public const double SafetyMargin = 1.15;

	/// <summary>Rough encoded intro bitrate used when a source duration is unavailable (~20 Mbps).</summary>
	private const long IntroBytesPerSecond = 2_500_000L;

	public readonly record struct Estimate(
		long FinalBytes,
		long TempBytes,
		string TempDirectory,
		string OutputDirectory);

	public readonly record struct CheckResult(
		bool IsSufficient,
		Estimate Estimate,
		long FreeTempBytes,
		long FreeOutputBytes,
		bool SameVolume,
		long RequiredTempBytes,
		long RequiredOutputBytes);

	public static Estimate EstimateCombine(
		IEnumerable<string> inputPaths,
		string outputPath,
		bool includeIntro,
		int introDurationSeconds)
	{
		var paths = (inputPaths ?? Enumerable.Empty<string>())
			.Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
			.ToList();
		long inputBytes = paths.Sum(SafeFileLength);
		long introBytes = includeIntro
			? EstimateIntroBytes(introDurationSeconds)
			: 0;

		// Stream-copy final ≈ inputs (+ short intro). Trims only shrink this; ignore them (conservative).
		long finalBytes = ApplyMargin(inputBytes + introBytes);

		var tempDir = TempPathHelper.GetTempPath();
		var outputDir = ResolveDirectory(outputPath);
		// No intro: concat demuxer writes a tiny file list, then the final directly.
		// With intro: PrependIntro + MPEG-TS remux. When temp shares the output volume,
		// obsolete merged MP4s are deleted after remux, so peak temp ≈ 1× rather than 2×.
		long tempBytes;
		if (!includeIntro)
			tempBytes = ApplyMargin(64 * 1024);
		else if (VolumePathHelper.AreSameVolume(tempDir, outputDir))
			tempBytes = ApplyMargin(inputBytes + introBytes);
		else
			tempBytes = ApplyMargin(2 * (inputBytes + introBytes));

		return new Estimate(finalBytes, tempBytes, tempDir, outputDir);
	}

	public static Estimate EstimateReplaceSegment(
		string inputPath,
		string outputPath,
		double inputDurationSeconds,
		IEnumerable<(double StartSeconds, double EndSeconds, int IntroDurationSeconds)> segments)
	{
		long inputBytes = SafeFileLength(inputPath);
		double totalSeconds = inputDurationSeconds > 0.05
			? inputDurationSeconds
			: 0;

		double removedSeconds = 0;
		long replacementBytes = 0;
		foreach (var segment in segments ?? Enumerable.Empty<(double, double, int)>())
		{
			var start = Math.Max(0, segment.StartSeconds);
			var end = Math.Max(start, segment.EndSeconds);
			removedSeconds += end - start;
			if (segment.IntroDurationSeconds > 0)
				replacementBytes += EstimateIntroBytes(segment.IntroDurationSeconds);
		}

		double keptFraction = 1.0;
		if (totalSeconds > 0.05)
		{
			removedSeconds = Math.Min(removedSeconds, totalSeconds);
			keptFraction = Math.Max(0, (totalSeconds - removedSeconds) / totalSeconds);
		}

		long keepBytes = (long)(inputBytes * keptFraction);
		long finalBytes = ApplyMargin(keepBytes + replacementBytes);

		var tempDir = TempPathHelper.GetTempPath();
		var outputDir = ResolveDirectory(outputPath);
		// Keeps/intros are extracted first; when temp shares the output volume they are
		// deleted after each remux, so peak ≈ 1× final. Otherwise keeps + TS coexist (~2×).
		long pieceBytes = keepBytes + replacementBytes;
		long tempBytes = ApplyMargin(
			VolumePathHelper.AreSameVolume(tempDir, outputDir) ? pieceBytes : 2 * pieceBytes);

		return new Estimate(finalBytes, tempBytes, tempDir, outputDir);
	}

	public static CheckResult Evaluate(Estimate estimate)
	{
		long freeTemp = VolumePathHelper.TryGetAvailableFreeSpace(estimate.TempDirectory);
		long freeOutput = VolumePathHelper.TryGetAvailableFreeSpace(estimate.OutputDirectory);
		bool sameVolume = VolumePathHelper.AreSameVolume(estimate.TempDirectory, estimate.OutputDirectory);

		long requiredTemp;
		long requiredOutput;
		if (sameVolume)
		{
			requiredTemp = estimate.TempBytes + estimate.FinalBytes;
			requiredOutput = requiredTemp;
		}
		else
		{
			requiredTemp = estimate.TempBytes;
			requiredOutput = estimate.FinalBytes;
		}

		bool tempOk = freeTemp < 0 || freeTemp >= requiredTemp;
		bool outputOk = freeOutput < 0 || freeOutput >= requiredOutput;
		// If free space could not be read, do not block (avoid false positives).
		bool sufficient = (freeTemp < 0 && freeOutput < 0) || (tempOk && outputOk);

		return new CheckResult(
			sufficient,
			estimate,
			freeTemp,
			freeOutput,
			sameVolume,
			requiredTemp,
			requiredOutput);
	}

	public static string FormatBytes(long bytes)
	{
		if (bytes < 0)
			return "unknown";

		string[] units = ["B", "KB", "MB", "GB", "TB"];
		double value = bytes;
		var unit = 0;
		while (value >= 1024 && unit < units.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		var decimals = unit == 0 ? 0 : (value >= 100 ? 0 : (value >= 10 ? 1 : 2));
		return string.Format(CultureInfo.CurrentCulture, "{0:F" + decimals + "} {1}", value, units[unit]);
	}

	public static string FormatWarningMessage(CheckResult check)
	{
		var e = check.Estimate;
		var sb = new StringBuilder();
		sb.AppendLine("Estimated disk space may not be enough for this operation.");
		sb.AppendLine();
		sb.AppendLine($"Estimated final output: {FormatBytes(e.FinalBytes)}");
		sb.AppendLine($"Estimated temporary files: {FormatBytes(e.TempBytes)}");

		if (check.SameVolume)
		{
			sb.AppendLine($"Estimated peak needed: {FormatBytes(check.RequiredTempBytes)}");
			sb.AppendLine($"Free space ({e.TempDirectory}): {FormatBytes(check.FreeTempBytes)}");
		}
		else
		{
			sb.AppendLine($"Needed on temp volume ({e.TempDirectory}): {FormatBytes(check.RequiredTempBytes)} — free {FormatBytes(check.FreeTempBytes)}");
			sb.AppendLine($"Needed on output volume ({e.OutputDirectory}): {FormatBytes(check.RequiredOutputBytes)} — free {FormatBytes(check.FreeOutputBytes)}");
		}

		sb.AppendLine();
		sb.Append("Continue anyway?");
		return sb.ToString();
	}

	public static long EstimateIntroBytes(int durationSeconds)
	{
		var seconds = Math.Max(1, durationSeconds);
		return Math.Max(1_000_000L, seconds * IntroBytesPerSecond);
	}

	private static long ApplyMargin(long bytes)
	{
		if (bytes <= 0)
			return 0;
		return (long)Math.Ceiling(bytes * SafetyMargin);
	}

	private static long SafeFileLength(string path)
	{
		try
		{
			return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
				? new FileInfo(path).Length
				: 0;
		}
		catch
		{
			return 0;
		}
	}

	private static string ResolveDirectory(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return TempPathHelper.GetTempPath();

		try
		{
			var full = Path.GetFullPath(path);
			if (Directory.Exists(full))
				return full;
			var dir = Path.GetDirectoryName(full);
			if (!string.IsNullOrWhiteSpace(dir))
				return dir;
		}
		catch
		{
			// ignored
		}

		return TempPathHelper.GetTempPath();
	}
}
