using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>
/// Removes and/or replaces timeline ranges in a single video via stream-copy extract + concat
/// (cut points may snap to nearby keyframes).
/// </summary>
public static class SegmentReplaceComposer
{
	public readonly record struct RemovalRange(double StartSeconds, double EndSeconds, string ReplacementPath);

	public static async Task ApplyAsync(
		string inputPath,
		string outputPath,
		IReadOnlyList<RemovalRange> removals,
		Action<string> log = null,
		CancellationToken ct = default,
		Action<string> operationStep = null)
	{
		log ??= _ => { };
		void Step(string message) => operationStep?.Invoke(message);

		if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
			throw new FileNotFoundException("Input video not found.", inputPath);
		if (string.IsNullOrWhiteSpace(outputPath))
			throw new ArgumentException("Output path is required.", nameof(outputPath));
		if (removals == null || removals.Count == 0)
			throw new ArgumentException("At least one removal range is required.", nameof(removals));

		var ordered = removals.OrderBy(r => r.StartSeconds).ToList();
		for (var i = 0; i < ordered.Count; i++)
		{
			var r = ordered[i];
			if (r.EndSeconds <= r.StartSeconds)
				throw new ArgumentException($"Invalid range: end must be after start ({r.StartSeconds}–{r.EndSeconds}).");
			if (i > 0 && r.StartSeconds < ordered[i - 1].EndSeconds)
				throw new ArgumentException("Overlapping ranges are not allowed.");
			if (!string.IsNullOrWhiteSpace(r.ReplacementPath) && !File.Exists(r.ReplacementPath))
				throw new FileNotFoundException("Replacement intro not found.", r.ReplacementPath);
		}

		var durationStr = await FFMpegUtils.Instance.GetFileDurationAsync(inputPath, ct, log).ConfigureAwait(false);
		var durationTr = string.IsNullOrWhiteSpace(durationStr) ? null : TimeRange.FromString(durationStr.Trim());
		var totalSeconds = durationTr?.TotalSeconds ?? 0;
		if (totalSeconds <= 0)
			throw new InvalidOperationException("Could not read input duration.");

		var outDir = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrWhiteSpace(outDir) && !Directory.Exists(outDir))
			Directory.CreateDirectory(outDir);

		var workDir = Path.Combine(TempPathHelper.GetTempPath(), $"replace_seg_{Guid.NewGuid():N}");
		Directory.CreateDirectory(workDir);
		var piecePaths = new List<string>();
		var tempKeeps = new List<string>();

		try
		{
			double cursor = 0;
			var pieceIndex = 0;
			var keepTotal = CountKeepSegments(ordered, totalSeconds);
			var keepDone = 0;

			foreach (var removal in ordered)
			{
				ct.ThrowIfCancellationRequested();
				if (removal.StartSeconds > cursor + 0.05)
				{
					keepDone++;
					Step($"Extracting keep segment {keepDone} of {keepTotal} ({FormatTs(cursor)}–{FormatTs(removal.StartSeconds)})…");
					var keepPath = Path.Combine(workDir, $"keep_{pieceIndex:D3}.mp4");
					await ExtractKeepAsync(inputPath, cursor, removal.StartSeconds - cursor, keepPath, log, ct)
						.ConfigureAwait(false);
					piecePaths.Add(keepPath);
					tempKeeps.Add(keepPath);
					pieceIndex++;
				}

				if (!string.IsNullOrWhiteSpace(removal.ReplacementPath))
				{
					Step($"Queuing replacement intro for removed {FormatTs(removal.StartSeconds)}–{FormatTs(removal.EndSeconds)}…");
					piecePaths.Add(removal.ReplacementPath);
				}
				else
				{
					log($"Removing {FormatTs(removal.StartSeconds)}–{FormatTs(removal.EndSeconds)} (no replacement).");
					Step($"Skipping removed range {FormatTs(removal.StartSeconds)}–{FormatTs(removal.EndSeconds)} (no replacement).");
				}

				cursor = Math.Max(cursor, removal.EndSeconds);
			}

			if (cursor < totalSeconds - 0.05)
			{
				keepDone++;
				Step($"Extracting keep segment {keepDone} of {keepTotal} ({FormatTs(cursor)}–{FormatTs(totalSeconds)})…");
				var keepPath = Path.Combine(workDir, $"keep_{pieceIndex:D3}.mp4");
				await ExtractKeepAsync(inputPath, cursor, totalSeconds - cursor, keepPath, log, ct)
					.ConfigureAwait(false);
				piecePaths.Add(keepPath);
				tempKeeps.Add(keepPath);
			}

			if (piecePaths.Count == 0)
				throw new InvalidOperationException("Nothing left to write after removals.");

			if (piecePaths.Count == 1)
			{
				Step("Writing output…");
				if (File.Exists(outputPath))
					File.Delete(outputPath);
				File.Copy(piecePaths[0], outputPath, overwrite: true);
				log($"Done: {outputPath}");
				return;
			}

			Step($"Combining {piecePaths.Count} pieces (stream copy)…");
			var listPath = Path.Combine(workDir, "concat.txt");
			static string Escape(string p) => (p ?? string.Empty).Replace("'", "'\\''");
			File.WriteAllLines(listPath, piecePaths.Select(p => $"file '{Escape(Path.GetFullPath(p))}'"));

			var concatOpts = new List<FfmpegOption?>
			{
				FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
				FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
				FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatConcatDemuxer),
				FfmpegOption.Pair(FfmpegArguments.ConcatDemuxerSafeFlag, FfmpegArguments.ConcatDemuxerAllowAnyPath),
				FfmpegCommandLine.DefaultInputThreadQueue(),
				FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(listPath)),
			};
			LegacyFastEncoding.AppendStreamCopyTail(concatOpts);
			concatOpts.Add(FfmpegOption.Pair("-avoid_negative_ts", "make_zero"));
			concatOpts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(outputPath)));

			await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(concatOpts), ct, log)
				.ConfigureAwait(false);

			if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
				throw new InvalidOperationException("Output was not created.");

			log($"Done: {outputPath}");
			Step("Combine finished.");
		}
		finally
		{
			foreach (var keep in tempKeeps)
				TempPathHelper.DeleteTemporaryFileUnlessRetained(keep);
			TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(workDir, recursive: true);
		}
	}

	private static int CountKeepSegments(IReadOnlyList<RemovalRange> ordered, double totalSeconds)
	{
		double cursor = 0;
		var count = 0;
		foreach (var removal in ordered)
		{
			if (removal.StartSeconds > cursor + 0.05)
				count++;
			cursor = Math.Max(cursor, removal.EndSeconds);
		}
		if (cursor < totalSeconds - 0.05)
			count++;
		return Math.Max(count, 1);
	}

	private static async Task ExtractKeepAsync(
		string inputPath,
		double startSeconds,
		double durationSeconds,
		string outputPath,
		Action<string> log,
		CancellationToken ct)
	{
		if (durationSeconds <= 0.05)
			throw new InvalidOperationException("Keep segment duration is too small.");

		var seek = FormatTs(startSeconds);
		var dur = FormatTs(durationSeconds);
		log($"Stream-copy extract {seek} +{dur} → {Path.GetFileName(outputPath)}");

		var parts = new List<FfmpegOption?>
		{
			FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
			FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
			FfmpegOption.Pair(FfmpegArguments.SeekInputTimestamp, seek),
			FfmpegCommandLine.DefaultInputThreadQueue(),
			FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(inputPath)),
			FfmpegOption.Pair(FfmpegArguments.LimitOutputDuration, dur),
		};
		LegacyFastEncoding.AppendStreamCopyTail(parts);
		parts.Add(FfmpegOption.Pair("-avoid_negative_ts", "make_zero"));
		parts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(outputPath)));

		await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(parts), ct, log)
			.ConfigureAwait(false);

		if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
			throw new InvalidOperationException($"Failed to extract keep segment at {seek}.");
	}

	private static string FormatTs(double totalSeconds)
	{
		if (totalSeconds < 0)
			totalSeconds = 0;
		var ts = TimeSpan.FromSeconds(totalSeconds);
		return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
	}
}
