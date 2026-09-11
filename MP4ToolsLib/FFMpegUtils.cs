using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib
{

	public class FFMpegUtils
	{
		private static readonly Lazy<FFMpegUtils> _instance = new Lazy<FFMpegUtils>(() => new FFMpegUtils());
		public static FFMpegUtils Instance => _instance.Value;
		private FFMpegUtils()
		{

		}

		private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(30);
		private static string _ffprobe_exe = string.Empty;

		private static readonly object TrackedProcessesLock = new object();
		private static readonly HashSet<int> TrackedMediaProcessIds = [];

		private static void RegisterTrackedMediaProcess(Process process)
		{
			if (process?.HasExited == false)
			{
				lock (TrackedProcessesLock)
					TrackedMediaProcessIds.Add(process.Id);
			}
		}

		private static void UnregisterTrackedMediaProcess(Process process)
		{
			if (process == null)
				return;
			lock (TrackedProcessesLock)
				TrackedMediaProcessIds.Remove(process.Id);
		}

		public void KillAllTrackedMediaProcesses()
		{
			int[] snapshot;
			lock (TrackedProcessesLock)
			{
				snapshot = TrackedMediaProcessIds.ToArray();
				TrackedMediaProcessIds.Clear();
			}

			foreach (var id in snapshot)
			{
				try
				{
					using var p = Process.GetProcessById(id);
					if (!p.HasExited)
						p.Kill(entireProcessTree: true);
				}
				catch
				{
					// already exited or inaccessible
				}
			}
		}
		public string CleanupPath(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					var filenameNoExt = Path.GetFileNameWithoutExtension(path);
					var ext = Path.GetExtension(path);
					var dir = Path.GetDirectoryName(path) ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(dir))
					{
						for (var i = 1; i <= 500; i++)
						{
							var candidate = Path.Combine(dir, $"{filenameNoExt}_{i}{ext}");
							if (!File.Exists(candidate))
								return candidate;
						}
					}
				}
			}
			catch
			{
				//
			}
			return path;
		}

		private static string QuoteArgument(string value)
		{
			if (value == null) return "\"\"";
			// Minimal, cross-platform escaping for ProcessStartInfo string-args.
			// FFmpeg/ffprobe accept \" inside a quoted argument on both Windows and *nix shells.
			var escaped = value.Replace("\"", "\\\"");
			return $"\"{escaped}\"";
		}


		public string FFPROBE_EXE
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_ffprobe_exe))
				{
					var testPaths = new List<string>()
				{
					AppDomain.CurrentDomain.BaseDirectory
				};
					var fileAppend = OperatingSystem.IsLinux() ? "" : ".exe";
					var ffprobeName = $"ffprobe{fileAppend}";
					foreach (var path in testPaths)
					{

						var exe = Path.Combine(path, ffprobeName);
						if (File.Exists(exe))
						{
							_ffprobe_exe = exe;
							break;
						}
					}
					if (string.IsNullOrWhiteSpace(_ffprobe_exe))
					{
						_ffprobe_exe = ffprobeName;
					}
				}
				return _ffprobe_exe;
			}
		}

		private string _ffmpeg_exe = string.Empty;
		public string FFPMEG_EXE
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_ffmpeg_exe))
				{


					var testPaths = new List<string>()
				{
					AppDomain.CurrentDomain.BaseDirectory
				};
					var fileAppend = OperatingSystem.IsLinux() ? "" : ".exe";
					var ffmpegName = $"ffmpeg{fileAppend}";
					foreach (var path in testPaths)
					{

						var exe = Path.Combine(path, ffmpegName);
						if (File.Exists(exe))
						{
							_ffmpeg_exe = exe;
							break;
						}
					}
					if (string.IsNullOrWhiteSpace(_ffmpeg_exe))
					{
						_ffmpeg_exe = ffmpegName;
					}
				}
				return _ffmpeg_exe;
			}
		}

		public string ApplyForcedFfmpegArgs(string args)
		{
			args = args ?? string.Empty;
			if (!args.Contains(FfmpegArguments.DisableInteractiveStdin, StringComparison.OrdinalIgnoreCase))
			{
				args = $"{FfmpegArguments.DisableInteractiveStdin} {args}".Trim();
			}
			return args;
		}


		public async Task<string> GetFileDurationAsync(string file, CancellationToken cancellationToken = default, Action<string> log = null)
		{
			if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
			{
				return string.Empty;
			}
			try
			{
				var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 -sexagesimal {QuoteArgument(file)}";
				var output = await RunCaptureAsync(FFPROBE_EXE, args, cancellationToken, log).ConfigureAwait(false);
				return (output ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
					.FirstOrDefault()?.Trim() ?? string.Empty;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				return string.Empty;
			}
		}

		public async Task<string> GetFirstVideoCodecNameAsync(string inputFile, CancellationToken cancellationToken = default, Action<string> log = null)
		{
			var (video, _) = await GetFirstAvCodecNamesAsync(inputFile, cancellationToken, log).ConfigureAwait(false);
			return video;
		}

		public async Task<string> GetFirstAudioCodecNameAsync(string inputFile, CancellationToken cancellationToken = default, Action<string> log = null)
		{
			var (_, audio) = await GetFirstAvCodecNamesAsync(inputFile, cancellationToken, log).ConfigureAwait(false);
			return audio;
		}

		/// <summary>
		/// One ffprobe pass for the first video and audio <c>codec_name</c> values.
		/// </summary>
		public async Task<(string video, string audio)> GetFirstAvCodecNamesAsync(
			string inputFile,
			CancellationToken cancellationToken = default,
			Action<string> log = null)
		{
			if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
				return (string.Empty, string.Empty);

			try
			{
				var args =
					"-v error -show_entries stream=codec_type,codec_name -of csv=p=0 " +
					QuoteArgument(inputFile);
				var output = await RunCaptureAsync(FFPROBE_EXE, args, cancellationToken, log).ConfigureAwait(false);
				var video = string.Empty;
				var audio = string.Empty;
				foreach (var line in (output ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
				{
					var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
					if (parts.Length < 2)
						continue;
					if (parts[0].Equals("video", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(video))
						video = parts[1];
					else if (parts[0].Equals("audio", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(audio))
						audio = parts[1];
				}

				return (video, audio);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
			}

			return (string.Empty, string.Empty);
		}

		private static readonly Regex FfmpegVideoResolutionRegex = new(
			@"Stream\s+#\d+:\d+.*?\bVideo:\s*.*?\b(\d{2,5})x(\d{2,5})\b",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

		/// <summary>Reads the first video stream dimensions from ffmpeg probe output (stderr).</summary>
		public async Task<(int width, int height)?> GetVideoResolutionViaFfmpegAsync(
			string inputFile,
			CancellationToken cancellationToken = default,
			Action<string> log = null)
		{
			if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
				return null;

			log ??= _ => { };
			var args = $"-hide_banner -i {QuoteArgument(inputFile)}";
			var stderr = new StringBuilder();
			var psi = new ProcessStartInfo(FFPMEG_EXE, args)
			{
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data == null)
					return;
				stderr.AppendLine(e.Data);
				log(e.Data);
			};

			try
			{
				if (!process.Start())
					return null;

				process.BeginErrorReadLine();
				RegisterTrackedMediaProcess(process);

				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				linkedCts.CancelAfter(ProcessTimeout);

				log($"{psi.FileName}: {psi.Arguments}");

				var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
				await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
				await stdoutTask.ConfigureAwait(false);

				var output = stderr.ToString();
				var match = FfmpegVideoResolutionRegex.Match(output);
				if (!match.Success)
					return null;

				if (!int.TryParse(match.Groups[1].Value, out var width) ||
				    !int.TryParse(match.Groups[2].Value, out var height))
					return null;

				return (width, height);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				log($"** Error reading video resolution: {ex.Message} **");
				return null;
			}
			finally
			{
				UnregisterTrackedMediaProcess(process);
				try
				{
					process.CancelErrorRead();
				}
				catch
				{
					// ignored
				}

				process.Dispose();
			}
		}

		public Task<string> RunCaptureFFMpegAsync(string args, CancellationToken ct, Action<string> Log = null)
		{
			args = ApplyForcedFfmpegArgs(args ?? string.Empty);
			return RunCaptureAsync(FFPMEG_EXE, args, ct, Log);
		}

		public Task<string> RunCaptureFFProbeAsync(string args, CancellationToken ct, Action<string> Log = null)
		{
			return RunCaptureAsync(FFPROBE_EXE, args, ct, Log);
		}

		public async Task<string> RunCaptureAsync(string exe, string args, CancellationToken ct, Action<string> Log = null)
		{
			Log ??= _ => { };
			var psi = new ProcessStartInfo(exe, args)
			{
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
					Log?.Invoke($"{e.Data}");
			};

			try
			{
				if (!process.Start())
					throw new InvalidOperationException($"Failed to start: {exe}");

				process.BeginErrorReadLine();
				RegisterTrackedMediaProcess(process);

				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
				linkedCts.CancelAfter(ProcessTimeout);

				Debug.WriteLine($"[RunCaptureAsync] FileName: {psi.FileName}");
				Debug.WriteLine($"[RunCaptureAsync] Arguments: {psi.Arguments}");

				Log($"{psi.FileName}: {psi.Arguments}");

				using (ct.Register(() =>
				{
					try
					{
						if (!process.HasExited)
							process.Kill(entireProcessTree: true);
					}
					catch
					{
						// ignored
					}
				}))
				{
					var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
					await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
					var stdout = await stdoutTask.ConfigureAwait(false);

					if (!string.IsNullOrWhiteSpace(stdout))
					{
						foreach (var line in stdout.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
						{
							if (!string.IsNullOrWhiteSpace(line))
								Log($"[stdout] {line}");
						}
					}

					if (process.ExitCode != 0)
						throw new InvalidOperationException($"{exe} failed with exit code {process.ExitCode}");
					return stdout;
				}
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				Log($"** Timeout after {ProcessTimeout.TotalMinutes:0} minutes **");
				try
				{
					if (!process.HasExited)
						process.Kill(entireProcessTree: true);
				}
				catch
				{
					// ignored
				}

				throw new TimeoutException($"{exe} timed out after {ProcessTimeout.TotalMinutes:0} minutes");
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Log($"** Error: {ex.Message} **");
				try
				{
					if (!process.HasExited)
						process.Kill(entireProcessTree: true);
				}
				catch
				{
					// ignored
				}

				throw;
			}
			finally
			{
				UnregisterTrackedMediaProcess(process);
				try
				{
					process.CancelErrorRead();
				}
				catch
				{
					// ignored
				}

				process.Dispose();
			}
		}

		public async Task RunAndLogFFMpegAsync(string args, Action<Process, string> processOutputAction, Action<string> log, CancellationToken cancellationToken, string workingDirectory = "")
		{
			log ??= (s => Debug.WriteLine(s));
			args = ApplyForcedFfmpegArgs(args);

			log($"Prepared ffmpeg args: {args}");

			var psi = new ProcessStartInfo
			{
				FileName = FFPMEG_EXE,
				Arguments = args,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = workingDirectory ?? string.Empty
			};

			DataReceivedEventHandler onErr = null;
			DataReceivedEventHandler onOut = null;
			var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			if (processOutputAction != null)
			{
				onErr = (_, b) => processOutputAction(process, b?.Data);
				onOut = (_, b) => processOutputAction(process, b?.Data);
				process.ErrorDataReceived += onErr;
				process.OutputDataReceived += onOut;
			}

			try
			{
				log($"{psi.FileName} ({psi.WorkingDirectory}): {psi.Arguments}");

				if (!process.Start())
					throw new InvalidOperationException($"Failed to start: {FFPMEG_EXE}");

				process.BeginErrorReadLine();
				process.BeginOutputReadLine();
				RegisterTrackedMediaProcess(process);

				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				linkedCts.CancelAfter(TimeSpan.FromHours(2));

				using (cancellationToken.Register(() =>
				{
					try
					{
						if (!process.HasExited)
							process.Kill(entireProcessTree: true);
					}
					catch
					{
						// ignored
					}
				}))
				{
					await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
				}

				if (process.ExitCode == 0)
					log("** Complete **");
				else
				{
					log($"** Exit Code {process.ExitCode} **");
					throw new InvalidOperationException($"ffmpeg failed with exit code {process.ExitCode}");
				}
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				log("** FFmpeg timed out after 2 hours **");
				try
				{
					if (!process.HasExited)
						process.Kill(entireProcessTree: true);
				}
				catch
				{
					// ignored
				}

				throw new TimeoutException("ffmpeg timed out after 2 hours");
			}
			catch (OperationCanceledException)
			{
				log("** Cancelled **");
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[RunAndLogFFMpegAsync][Error] {ex}");
				log($"** Error: {ex.Message} **");
				try
				{
					if (!process.HasExited)
						process.Kill(entireProcessTree: true);
				}
				catch
				{
					// ignored
				}

				throw;
			}
			finally
			{
				try
				{
					if (onErr != null)
						process.ErrorDataReceived -= onErr;
					if (onOut != null)
						process.OutputDataReceived -= onOut;
				}
				catch
				{
					// ignored
				}

				UnregisterTrackedMediaProcess(process);
				try
				{
					process.CancelErrorRead();
					process.CancelOutputRead();
				}
				catch
				{
					// ignored
				}

				process.Dispose();
			}
		}

		private static string GetProbedString(JsonElement element, string propertyName)
		{
			if (!element.TryGetProperty(propertyName, out var prop)) return string.Empty;
			return prop.ValueKind == JsonValueKind.String ? (prop.GetString() ?? string.Empty) : string.Empty;
		}

		private static string GetProbedRawString(JsonElement element, string propertyName)
		{
			if (!element.TryGetProperty(propertyName, out var prop)) return string.Empty;
			return prop.ValueKind switch
			{
				JsonValueKind.String => prop.GetString() ?? string.Empty,
				JsonValueKind.Number => prop.GetRawText(),
				_ => string.Empty
			};
		}


		public async Task<(StreamProbeInfo video, StreamProbeInfo audio)> ProbeMediaInfoAsync(string input, CancellationToken ct, Action<string> Log = null)
		{
			Log ??= _ => { };
			string args = "-v error -show_entries stream=index,codec_type,width,height,r_frame_rate,pix_fmt,codec_name,sample_rate,channels,channel_layout,bit_depth -of json " +
				$"{QuoteArgument(input)}";
			string stdout = await FFMpegUtils.Instance.RunCaptureFFProbeAsync(args, ct, Log);
			if (string.IsNullOrWhiteSpace(stdout))
			{
				return (null, null);
			}

			try
			{
				using var doc = JsonDocument.Parse(stdout);
				if (!doc.RootElement.TryGetProperty("streams", out var streams) || streams.ValueKind != JsonValueKind.Array)
				{
					return (null, null);
				}

				StreamProbeInfo video = null;
				StreamProbeInfo audio = null;
				foreach (var s in streams.EnumerateArray())
				{
					var info = new StreamProbeInfo
					{
						CodecType = GetProbedString(s, "codec_type"),
						Width = GetProbedRawString(s, "width"),
						Height = GetProbedRawString(s, "height"),
						FrameRate = GetProbedString(s, "r_frame_rate"),
						PixelFormat = GetProbedString(s, "pix_fmt"),
						BitDepth = GetProbedRawString(s, "bit_depth"),
						CodecName = GetProbedString(s, "codec_name"),
						SampleRate = GetProbedRawString(s, "sample_rate"),
						Channels = GetProbedRawString(s, "channels"),
						ChannelLayout = GetProbedString(s, "channel_layout"),
					};

					if (video == null && info.CodecType.Equals("video", StringComparison.OrdinalIgnoreCase))
					{
						video = info;
					}
					else if (audio == null && info.CodecType.Equals("audio", StringComparison.OrdinalIgnoreCase))
					{
						audio = info;
					}

					if (video != null && audio != null)
					{
						break;
					}
				}

				return (video, audio);
			}
			catch
			{
				return (null, null);
			}
		}

		public static string GetPreferredRenderDevice()
		{
			try
			{
				// Look for AMD 9070 XT (Navi 48) - PCI device ID 0x7550
				// Check render nodes in /sys/class/drm/
				var drmPath = "/sys/class/drm";
				if (Directory.Exists(drmPath))
				{
					// Look for renderD* devices
					var renderDevices = Directory.GetDirectories(drmPath, "renderD*");
					foreach (var renderDev in renderDevices)
					{
						// Get the device symlink to find PCI address
						var devPath = Path.Combine(renderDev, "device");
						if (Directory.Exists(devPath))
						{
							// Check vendor and device ID
							var vendorPath = Path.Combine(devPath, "vendor");
							var devicePath = Path.Combine(devPath, "device");
							if (File.Exists(vendorPath) && File.Exists(devicePath))
							{
								var vendor = File.ReadAllText(vendorPath).Trim();
								var device = File.ReadAllText(devicePath).Trim();
								// AMD vendor = 0x1002, Navi 48 (9070 XT) = 0x7550
								if (vendor == "0x1002" && device == "0x7550")
								{
									// Return the /dev/dri/renderD* path
									var devName = Path.GetFileName(renderDev);
									return $"/dev/dri/{devName}";
								}
							}
						}
					}
				}
			}
			catch
			{
				// Ignore errors
			}
			// Fallback to default renderD128 (first GPU)
			return "/dev/dri/renderD128";
		}
	}
}
