using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MP4ToolsLib;

/// <summary>Parses <c>ffmpeg -encoders</c> once and answers capability queries.</summary>
public static class FfmpegEncoderCatalog
{
	private static readonly object Gate = new();
	private static HashSet<string> _encoderIds;

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
				foreach (var id in new[] { "hevc_vaapi", "h264_vaapi", "libx264", "libx265" })
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
				var stdout = p.StandardOutput.ReadToEnd();
				p.WaitForExit(60000);
				foreach (var raw in stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
				{
					var line = raw.Trim();
					if (line.Length < 8 || line[0] != ' ')
						continue;
					var parts = Regex.Split(line.TrimStart(), @"\s+");
					if (parts.Length >= 2 && parts[1].Length > 1)
						_encoderIds.Add(parts[1]);
				}
			}
			catch
			{
				// leave empty — selector falls back to libx264/libx265
			}
		}
	}
}
