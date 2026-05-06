using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
					var i = 1;
					var filenameNoExt = Path.GetFileNameWithoutExtension(path);
					var ext = Path.GetExtension(path);
					var dir = Path.GetDirectoryName(path) ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(dir))
					{
						path = Path.Combine(dir, $"{filenameNoExt}_{i}{ext}");
						while (File.Exists(path))
						{
							if (i == 500)
							{
								break;
							}
							path = Path.Combine(dir, $"{filenameNoExt}_{i++}{ext}");
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
				var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 -sexagesimal \"{file}\"";
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
			if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
			{
				return string.Empty;
			}
			try
			{
				var args = $"-v error -select_streams v:0 -show_entries stream=codec_name -of default=nk=1:nw=1 \"{inputFile}\"";
				var output = await RunCaptureAsync(FFPROBE_EXE, args, cancellationToken, log).ConfigureAwait(false);
				return (output ?? string.Empty).Trim();
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
			}
			return string.Empty;

		}

		public async Task<string> GetFirstAudioCodecNameAsync(string inputFile, CancellationToken cancellationToken = default, Action<string> log = null)
		{
			if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
				return string.Empty;
			try
			{
				var args = $"-v error -select_streams a:0 -show_entries stream=codec_name -of default=nk=1:nw=1 \"{inputFile}\"";
				var output = await RunCaptureAsync(FFPROBE_EXE, args, cancellationToken, log).ConfigureAwait(false);
				return (output ?? string.Empty).Trim();
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
			}

			return string.Empty;
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
				onOut = (_, b) => processOutputAction(process,b?.Data);
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
					log($"** Exit Code {process.ExitCode} **");
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				log("** FFmpeg timed out after 2 hours **");
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
				$"\"{input}\"";
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
						CodecName = GetProbedString(s, "codec_name"),
						SampleRate = GetProbedRawString(s, "sample_rate"),
						Channels = GetProbedRawString(s, "channels"),
						ChannelLayout = GetProbedString(s, "channel_layout"),
						BitDepth = GetProbedString(s, "bit_depth")
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
					var renderDevices = Directory.GetFiles(drmPath, "renderD*");
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
