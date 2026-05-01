using System.Diagnostics;

namespace MP4ToolsLib.Tests.Support;

public static class FfmpegCliRunner
{
	public static bool TryGetFfmpegExecutable(out string path)
	{
		path = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
		try
		{
			using var p = Process.Start(new ProcessStartInfo
			{
				FileName = path,
				Arguments = "-hide_banner -version",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			});
			if (p == null)
				return false;
			p.WaitForExit(15000);
			return p.ExitCode == 0;
		}
		catch
		{
			return false;
		}
	}

	public static async Task<double> ProbeDurationSecondsAsync(string videoPath, CancellationToken ct = default)
	{
		var ffprobe = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";
		var psi = new ProcessStartInfo
		{
			FileName = ffprobe,
			Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		using var p = Process.Start(psi);
		if (p == null)
			return 0;
		var stdout = await p.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
		await p.WaitForExitAsync(ct).ConfigureAwait(false);
		if (p.ExitCode != 0)
			return 0;
		var line = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		return double.TryParse(line?.Trim(), System.Globalization.NumberStyles.Float,
			System.Globalization.CultureInfo.InvariantCulture, out var s)
			? s
			: 0;
	}

	public static async Task<int> RunFfmpegAsync(string arguments, CancellationToken ct = default)
	{
		var ffmpeg = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
		var psi = new ProcessStartInfo
		{
			FileName = ffmpeg,
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		using var p = Process.Start(psi);
		if (p == null)
			return -1;
		await p.WaitForExitAsync(ct).ConfigureAwait(false);
		return p.ExitCode;
	}
}
