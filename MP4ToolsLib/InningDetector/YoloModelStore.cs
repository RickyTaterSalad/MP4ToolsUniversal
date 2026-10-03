using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>
/// Locates or downloads the YOLOv8n ONNX person-detection model used by the inning detector.
/// </summary>
public static class YoloModelStore
{
	public const string ModelFileName = "yolov8n.onnx";

	/// <summary>Public Hugging Face mirror of YOLOv8n ONNX (~12 MB).</summary>
	public const string DefaultDownloadUrl =
		"https://huggingface.co/cabelo/yolov8/resolve/main/yolov8n.onnx";

	public static string GetDefaultModelDirectory()
	{
		var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (!string.IsNullOrWhiteSpace(localApp))
			return Path.Combine(localApp, "MP4Tools", "models");

		return Path.Combine(TempPathHelper.GetTempPath(), "MP4Tools", "models");
	}

	public static string GetDefaultModelPath() =>
		Path.Combine(GetDefaultModelDirectory(), ModelFileName);

	/// <summary>
	/// Returns a usable model path. Checks explicit path, app base Models/, then local cache;
	/// downloads into the local cache when missing.
	/// </summary>
	public static async Task<string> EnsureModelAsync(
		string preferredPath = null,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		foreach (var candidate in EnumerateCandidates(preferredPath))
		{
			if (File.Exists(candidate) && new FileInfo(candidate).Length > 1_000_000)
			{
				log?.Invoke($"Using YOLO model: {candidate}");
				return candidate;
			}
		}

		var dest = GetDefaultModelPath();
		var destDir = Path.GetDirectoryName(dest);
		if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
			Directory.CreateDirectory(destDir);

		log?.Invoke($"Downloading YOLO model ({ModelFileName})…");
		using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
		using var response = await http.GetAsync(DefaultDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		var tmp = dest + ".partial";
		await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
		{
			await response.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
		}

		if (File.Exists(dest))
			File.Delete(dest);
		File.Move(tmp, dest);

		log?.Invoke($"YOLO model ready: {dest}");
		return dest;
	}

	private static System.Collections.Generic.IEnumerable<string> EnumerateCandidates(string preferredPath)
	{
		if (!string.IsNullOrWhiteSpace(preferredPath))
			yield return preferredPath.Trim();

		var baseDir = AppContext.BaseDirectory;
		if (!string.IsNullOrWhiteSpace(baseDir))
		{
			yield return Path.Combine(baseDir, "Models", ModelFileName);
			yield return Path.Combine(baseDir, ModelFileName);
		}

		yield return GetDefaultModelPath();

		// Dev / source-tree convenience when running from bin/
		var libModels = Path.GetFullPath(Path.Combine(baseDir ?? "", "..", "..", "..", "..", "MP4ToolsLib", "Models", ModelFileName));
		yield return libModels;
	}
}
