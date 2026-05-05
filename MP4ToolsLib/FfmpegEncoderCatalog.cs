using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>Parses <c>ffmpeg -encoders</c> once and answers capability queries.</summary>
public static class FfmpegEncoderCatalog
{
	private static readonly object Gate = new();
	private static HashSet<string> _encoderIds;

	internal static void ParseEncoderListingInto(HashSet<string> dest, string text)
	{
		if (dest == null || string.IsNullOrEmpty(text))
			return;

		foreach (var raw in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
		{
			var line = raw.TrimEnd('\r', '\n');
			if (line.Length < 8)
				continue;

			var trimmedLeft = line.TrimStart();
			if (trimmedLeft.Length < 8)
				continue;

			// Capability flags column starts with V/A/S (video/audio/subtitle).
			var fc = trimmedLeft[0];
			if (fc is not ('V' or 'A' or 'S'))
				continue;

			var parts = Regex.Split(trimmedLeft, @"\s+", RegexOptions.None);
			if (parts.Length >= 2 && parts[1].Length > 1)
				dest.Add(parts[1]);
		}
	}

	public static bool HasEncoder(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return false;
		EnsureLoaded();
		return _encoderIds.Contains(id.Trim());
	}

	/// <summary>Clears cached <c>-encoders</c> output so the next query reloads (e.g. after toggling dry run).</summary>
	public static void InvalidateCache()
	{
		lock (Gate)
			_encoderIds = null;
	}

	private static void EnsureLoaded()
	{
		lock (Gate)
		{
			if (_encoderIds != null)
				return;
			_encoderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (FFMpegUtils.DryRunExternalCommands)
			{
				// Encoder probes are skipped; seed names so VideoEncodeSelector matches real VA-API/software paths in logged commands.
				foreach (var id in new[]
				         {
					         "hevc_vaapi","hevc_amf",
					         
				         })
					_encoderIds.Add(id);
				Debug.WriteLine("[DRY RUN] ffmpeg -encoders skipped — using assumed encoder list for planning.");
				return;
			}

			try
			{
				var ffmpeg = FFMpegUtils.Instance.FFPMEG_EXE;
				var psi = new ProcessStartInfo
				{
					FileName = ffmpeg,
					Arguments = "-hide_banner -encoders",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				};
				using var p = Process.Start(psi);
				if (p == null)
					return;
				var stdoutTask = p.StandardOutput.ReadToEndAsync();
				var stderrTask = p.StandardError.ReadToEndAsync();
				if (!p.WaitForExit(60000))
				{
					try { p.Kill(entireProcessTree: true); }
					catch { /* ignore */ }
				}

				Task.WhenAll(stdoutTask, stderrTask).GetAwaiter().GetResult();
				ParseEncoderListingInto(_encoderIds, stdoutTask.Result);
				ParseEncoderListingInto(_encoderIds, stderrTask.Result);
			}
			catch
			{
			}
		}
	}
}
