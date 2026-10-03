using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>
/// Removes and/or replaces timeline ranges in a single video via stream-copy extract,
/// MPEG-TS remux, and concat (cut points may snap to nearby keyframes).
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
		var pieceMp4Paths = new List<string>();
		var tempKeeps = new List<string>();
		var tempTsFiles = new List<string>();

		try
		{
			double cursor = 0;
			var pieceIndex = 0;
			var keepTotal = CountKeepSegments(ordered, totalSeconds);
			var keepDone = 0;
			var replacementCount = ordered.Count(r => !string.IsNullOrWhiteSpace(r.ReplacementPath));
			var replacementDone = 0;

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
					pieceMp4Paths.Add(keepPath);
					tempKeeps.Add(keepPath);
					pieceIndex++;
				}

				if (!string.IsNullOrWhiteSpace(removal.ReplacementPath))
				{
					replacementDone++;
					Step($"Including replacement intro {replacementDone} of {replacementCount} for removed {FormatTs(removal.StartSeconds)}–{FormatTs(removal.EndSeconds)}…");
					log($"Replacement intro: {removal.ReplacementPath}");
					pieceMp4Paths.Add(removal.ReplacementPath);
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
				pieceMp4Paths.Add(keepPath);
				tempKeeps.Add(keepPath);
			}

			if (pieceMp4Paths.Count == 0)
				throw new InvalidOperationException("Nothing left to write after removals.");

			log($"Assembling {pieceMp4Paths.Count} piece(s) via MPEG-TS concat ({replacementCount} replacement intro(s)).");

			// Probe audio before remux so we can free obsolete MP4 pieces afterward.
			var (_, firstA) = await FFMpegUtils.Instance
				.GetFirstAvCodecNamesAsync(pieceMp4Paths[0], ct, log)
				.ConfigureAwait(false);
			var needAacAdtsBsf = MpegTsConcatAudio.IntermediateTsAudioIsAac(firstA)
				|| (MpegTsConcatAudio.ShouldTranscodeAudioMp4ToTs(firstA));

			// Remux every piece to MPEG-TS (same path as Combine) so intro + keep segments
			// stream-copy-concat reliably. Plain MP4 concat demuxer often drops the intro.
			for (var i = 0; i < pieceMp4Paths.Count; i++)
			{
				ct.ThrowIfCancellationRequested();
				var piece = pieceMp4Paths[i];
				Step($"Remuxing piece {i + 1} of {pieceMp4Paths.Count} to MPEG-TS…");
				var tsPath = Path.Combine(workDir, $"part_{i:D3}.ts");
				await RemuxMp4ToTsAsync(piece, tsPath, log, ct).ConfigureAwait(false);
				tempTsFiles.Add(tsPath);

				// Keep/replacement MP4 is obsolete once its .ts exists; free it when on the output volume.
				if (TempPathHelper.TryDeleteObsoleteTemporaryOnOutputVolume(piece, outputPath, log))
					tempKeeps.Remove(piece);
			}

			Step(tempTsFiles.Count == 1
				? "Writing output…"
				: $"Combining {tempTsFiles.Count} pieces…");

			var tsInput = tempTsFiles.Count == 1
				? tempTsFiles[0]
				: $"concat:{string.Join("|", tempTsFiles)}";

			var finalizeParts = new List<FfmpegOption?>
			{
				FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
				FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
				FfmpegCommandLine.DefaultInputThreadQueue(),
				FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(tsInput)),
			};
			LegacyFastEncoding.AppendStreamCopyTail(finalizeParts);
			if (needAacAdtsBsf)
				finalizeParts.Add(FfmpegOption.Pair(FfmpegArguments.AudioBitstreamFilter, FfmpegArguments.BitstreamFilterAacAdtsToAsc));
			finalizeParts.Add(FfmpegOption.Pair("-avoid_negative_ts", "make_zero"));
			finalizeParts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(outputPath)));

			await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(finalizeParts), ct, log)
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
			foreach (var ts in tempTsFiles)
				TempPathHelper.DeleteTemporaryFileUnlessRetained(ts);
			TempPathHelper.DeleteTemporaryDirectoryUnlessRetained(workDir, recursive: true);
		}
	}

	private static async Task RemuxMp4ToTsAsync(
		string inputMp4,
		string outputTs,
		Action<string> log,
		CancellationToken ct)
	{
		var (vProbe, aProbe) = await FFMpegUtils.Instance
			.GetFirstAvCodecNamesAsync(inputMp4, ct, log)
			.ConfigureAwait(false);

		if (MpegTsConcatAudio.ShouldTranscodeAudioMp4ToTs(aProbe))
			log($"Piece audio is Opus ({Path.GetFileName(inputMp4)}): using AAC in MPEG-TS intermediate.");

		var opts = new List<FfmpegOption?>
		{
			FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
			FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
			FfmpegCommandLine.DefaultInputThreadQueue(),
			FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(inputMp4)),
			FfmpegOption.Pair(FfmpegArguments.SelectVideoCodec, FfmpegArguments.StreamCopy),
			MpegTsVideoBitstream.GetMp4ToAnnexBOption(vProbe),
		};
		MpegTsConcatAudio.AppendMp4ToTsAudioOptions(opts, aProbe);
		opts.Add(FfmpegOption.Pair(FfmpegArguments.InputFormat, FfmpegArguments.InputFormatMpegTs));
		opts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(outputTs)));

		await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(opts), ct, log)
			.ConfigureAwait(false);

		if (!File.Exists(outputTs) || new FileInfo(outputTs).Length == 0)
			throw new InvalidOperationException($"MPEG-TS remux failed for {Path.GetFileName(inputMp4)}.");
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
