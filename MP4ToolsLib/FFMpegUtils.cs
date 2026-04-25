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

		private readonly object _processLock = new object();

		private Process _currentRunningProcess = null;

		public void KillCurrentRunningProcess()
		{
			if (_currentRunningProcess != null)
			{
				try
				{
					if (!_currentRunningProcess.HasExited)
					{
						_currentRunningProcess.Kill();
						_currentRunningProcess = null;
					}
				}
				catch
				{

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


		public bool IsFFmpegExe(string exe)
		{
			if (string.IsNullOrWhiteSpace(exe))
			{
				return false;
			}
			var fileName = Path.GetFileName(exe);
			return fileName.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase);
		}
		public bool IsFFmpegProcess(Process process)
		{
			if (process?.StartInfo?.FileName == null)
			{
				return false;
			}
			var fileName = Path.GetFileName(process.StartInfo.FileName);
			return fileName.Equals("ffmpegName", StringComparison.OrdinalIgnoreCase)
				|| fileName.Equals("ffmpeg", StringComparison.OrdinalIgnoreCase);
		}

		public string FFPROBE_EXE
		{get
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
							Console.WriteLine($"Using ffprobe at: {exe}");
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
							Console.WriteLine($"Using ffmpeg at: {exe}");
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
			if (!args.Contains("-nostdin", StringComparison.OrdinalIgnoreCase))
			{
				args = $"-nostdin {args}".Trim();
			}
			var isCopyOnly = args.Contains("-c copy", StringComparison.OrdinalIgnoreCase)
				|| args.Contains("-c:v copy", StringComparison.OrdinalIgnoreCase);
			if (!isCopyOnly)
			{
				if (!args.Contains("-hwaccel", StringComparison.OrdinalIgnoreCase))
				{
					args = $"-hwaccel cuda {args}".Trim();
				}
			}
			return args;
		}


		public async Task<(int maxMinutes, int maxSecondsAtMaxMinute)> GetDurationBoundsAsync(string filePath)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
				{
					return (59, 59);
				}
				var duration = await GetFileDurationAsync(filePath);
				var tr = TimeRange.FromString(duration ?? string.Empty);
				if (tr == null)
				{
					return (59, 59);
				}
				if (tr.Hours > 0)
				{
					return (59, 59);
				}
				var maxMinutes = Math.Clamp(tr.Minutes, 0, 59);
				var maxSeconds = Math.Clamp(tr.Seconds, 0, 59);
				return (maxMinutes, maxSeconds);
			}
			catch
			{
				return (59, 59);
			}
		}



		public async Task<string> GetFileDurationAsync(string file)
		{
			if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
			{
				return string.Empty;
			}
			try
			{
				var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 -sexagesimal \"{file}\"";
				var output = await RunCaptureAsync(FFPROBE_EXE, args, CancellationToken.None);
				return (output ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
					.FirstOrDefault()?.Trim() ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		public async Task<string> GetFirstVideoCodecNameAsync(string inputFile)
		{
			if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
			{
				return string.Empty;
			}
			try
			{
				var args = $"-v error -select_streams v:0 -show_entries stream=codec_name -of default=nk=1:nw=1 \"{inputFile}\"";
				var output = await RunCaptureAsync(FFPROBE_EXE, args, CancellationToken.None);
				return (output ?? string.Empty).Trim();
			}
			catch
			{
			}
			return string.Empty;

		}

		public Task<string> RunCaptureFFMpegAsync(string args, CancellationToken ct, Action<string> onErr = null)
		{
			return RunCaptureAsync(FFPMEG_EXE, args, ct, onErr);
		}

		public Task<string> RunCaptureFFProbeAsync(string args, CancellationToken ct, Action<string> onErr = null)
		{
			return RunCaptureAsync(FFPROBE_EXE, args, ct, onErr);
		}

		public async Task<string> RunCaptureAsync(string exe, string args, CancellationToken ct, Action<string> onErr = null)
		{
			var psi = new ProcessStartInfo(exe, args)
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
			p.ErrorDataReceived += (_, e) => { if (e.Data != null) onErr?.Invoke(e.Data); };

			if (!p.Start()) throw new InvalidOperationException($"Failed to start: {exe}");
			p.BeginErrorReadLine();

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeoutCts.CancelAfter(ProcessTimeout);

			try
			{
				var stdoutTask = p.StandardOutput.ReadToEndAsync(timeoutCts.Token);
				await p.WaitForExitAsync(timeoutCts.Token);
				string stdout = await stdoutTask;

				if (p.ExitCode != 0) throw new InvalidOperationException($"{exe} failed with exit code {p.ExitCode}");
				return stdout;
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				try { if (!p.HasExited) p.Kill(true); } catch { }
				throw new TimeoutException($"{exe} timed out after {ProcessTimeout.TotalMinutes:0} minutes");
			}
			catch
			{
				try { if (!p.HasExited) p.Kill(true); } catch { }
				throw;
			}
		}
		public void RunAndLogFFMpeg(string args, Action<Process, string, string> ProcessOutputAction, Action<string> Log, string workingDirectory = "")
		{
			Log ??= (s => Debug.WriteLine(s));
			var timedOut = false;
			args = ApplyForcedFfmpegArgs(args);

			var psi = new ProcessStartInfo
			{
				FileName = FFPMEG_EXE,
				Arguments = args,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = workingDirectory
			};

			DataReceivedEventHandler onErr = null;
			DataReceivedEventHandler onOut = null;
			using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			if (ProcessOutputAction != null)
			{
				onErr = (a, b) => ProcessOutputAction(process, "stderr", b?.Data);
				onOut = (a, b) => ProcessOutputAction(process, "stdout", b?.Data);
				process.ErrorDataReceived += onErr;
				process.OutputDataReceived += onOut;
			}

			try
			{
				lock (_processLock)
				{
					if (_currentRunningProcess != null)
					{
						try
						{
							if (!_currentRunningProcess.HasExited)
								_currentRunningProcess.Kill(true);
						}
						catch (Exception ex)
						{
							Debug.WriteLine($"[RunAndLogProcessEx][KillExistingError] {ex}");
						}
						finally
						{
							_currentRunningProcess = null;
						}
					}
					_currentRunningProcess = process;
				}

				Debug.WriteLine($"[RunAndLogProcessEx] FileName: {psi.FileName}");
				Debug.WriteLine($"[RunAndLogProcessEx] Arguments: {psi.Arguments}");
				Debug.WriteLine($"[RunAndLogProcessEx] WorkingDirectory: {psi.WorkingDirectory}");

				Log($"{psi.FileName} ({psi.WorkingDirectory}): {psi.Arguments}");

				if (!process.Start())
					throw new InvalidOperationException($"Failed to start: {FFPMEG_EXE}");

				process.BeginErrorReadLine();
				process.BeginOutputReadLine();

				var timeout = TimeSpan.FromMinutes(30);
				using var timeoutCts = new CancellationTokenSource(timeout);
				try
				{
					process.WaitForExitAsync(timeoutCts.Token).GetAwaiter().GetResult();
					process.WaitForExit();
				}
				catch (OperationCanceledException)
				{
					timedOut = true;
					Log($"** Timed Out After {timeout.TotalMinutes:0} Minutes **");
					try
					{
						if (!process.HasExited)
						{
							process.Kill(true);
							process.WaitForExit();
						}
					}
					catch { }
				}

				if (!timedOut)
				{
					if (process.ExitCode == 0)
						Log("** Complete **");
					else
						Log($"** Exit Code {process.ExitCode} **");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[RunAndLogProcessEx][Error] {ex}");
				Log($"** Error: {ex.Message} **");
			}
			finally
			{
				try
				{
					if (onErr != null)
					{
						process.ErrorDataReceived -= onErr;
					}
					if (onOut != null)
					{
						process.OutputDataReceived -= onOut;
					}
				}
				catch { }

				lock (_processLock)
				{
					if (ReferenceEquals(_currentRunningProcess, process))
						_currentRunningProcess = null;
				}

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


		public async Task<(StreamProbeInfo video, StreamProbeInfo audio)> ProbeMediaInfoAsync(string input, CancellationToken ct)
		{
			string args = "-v error -show_entries stream=index,codec_type,width,height,r_frame_rate,pix_fmt,codec_name,sample_rate,channels,channel_layout -of json " +
				$"\"{input}\"";
			string stdout = await FFMpegUtils.Instance.RunCaptureFFProbeAsync(args, ct);
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
						ChannelLayout = GetProbedString(s, "channel_layout")
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
	}
}
