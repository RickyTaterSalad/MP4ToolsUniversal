using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>
/// Best-effort MP4 tag embed for combine edit-map pointers.
/// Skips remux when the output is very large (sidecar remains the source of truth).
/// </summary>
public static class CombineEditMapEmbedder
{
	/// <summary>Above this size, skip ffmpeg remux embed (sidecar only).</summary>
	public const long MaxRemuxBytes = 3L * 1024 * 1024 * 1024; // 3 GiB

	public static async Task TryEmbedAsync(
		CombineEditMap map,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (map is null || string.IsNullOrWhiteSpace(map.OutputVideoPath) || !File.Exists(map.OutputVideoPath))
			return;

		long length;
		try
		{
			length = new FileInfo(map.OutputVideoPath).Length;
		}
		catch
		{
			return;
		}

		if (length <= 0 || length > MaxRemuxBytes)
		{
			log?.Invoke(
				length > MaxRemuxBytes
					? $"Skipping MP4 metadata embed (file is {length / (1024 * 1024)} MB; using sidecar only)."
					: "Skipping MP4 metadata embed (empty output).");
			return;
		}

		var mapFileName = Path.GetFileName(map.CombineMapPath);
		var tmp = map.OutputVideoPath + ".mp4tools_meta.tmp.mp4";
		try
		{
			var opts = new List<FfmpegOption?>
			{
				FfmpegOption.Unary(FfmpegArguments.DisableInteractiveStdin),
				FfmpegOption.Unary(FfmpegArguments.OverwriteOutputFile),
				FfmpegCommandLine.DefaultInputThreadQueue(),
				FfmpegOption.Pair(FfmpegArguments.Input, FfmpegCommandLine.Quoted(map.OutputVideoPath)),
				FfmpegOption.Pair(FfmpegArguments.MapMetadata, "0"),
				FfmpegOption.Pair("-metadata", $"mp4tools_combine_map={mapFileName}"),
				FfmpegOption.Pair(
					"-metadata",
					$"mp4tools_intro_duration_seconds={map.IntroDurationSeconds.ToString("0.###", CultureInfo.InvariantCulture)}"),
				FfmpegOption.Pair(
					"-metadata",
					$"mp4tools_start_skip_seconds={map.StartSkipSeconds.ToString("0.###", CultureInfo.InvariantCulture)}"),
				FfmpegOption.Pair(
					"-metadata",
					$"mp4tools_end_keep_seconds={map.EndKeepSeconds.ToString("0.###", CultureInfo.InvariantCulture)}"),
			};
			LegacyFastEncoding.AppendStreamCopyTail(opts);
			opts.Add(FfmpegOption.Positional(FfmpegCommandLine.Quoted(tmp)));

			log?.Invoke("Embedding combine edit-map metadata into MP4…");
			await FFMpegUtils.Instance.RunCaptureFFMpegAsync(FfmpegCommandLine.Build(opts), ct, log)
				.ConfigureAwait(false);

			if (!File.Exists(tmp) || new FileInfo(tmp).Length <= 0)
			{
				log?.Invoke("MP4 metadata embed produced no output; keeping original file + sidecar.");
				FileUtils.TryDeleteFile(tmp);
				return;
			}

			var backup = map.OutputVideoPath + ".mp4tools_premeta.bak";
			FileUtils.TryDeleteFile(backup);
			File.Move(map.OutputVideoPath, backup);
			File.Move(tmp, map.OutputVideoPath);
			FileUtils.TryDeleteFile(backup);
			log?.Invoke("Embedded combine edit-map metadata into MP4.");
		}
		catch (OperationCanceledException)
		{
			FileUtils.TryDeleteFile(tmp);
			throw;
		}
		catch (Exception ex)
		{
			FileUtils.TryDeleteFile(tmp);
			log?.Invoke($"MP4 metadata embed skipped ({ex.Message}); sidecar remains authoritative.");
		}
	}
}
