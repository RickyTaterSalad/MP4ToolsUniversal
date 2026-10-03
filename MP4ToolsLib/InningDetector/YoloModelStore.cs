using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

public sealed record ResolvedYoloModel(string Path, YoloModelKind Kind);

/// <summary>
/// Locates or downloads ONNX models used by the inning detector.
/// Prefers BaseballCV pitcher/hitter/catcher when present; falls back to COCO YOLOv8n person.
/// </summary>
public static class YoloModelStore
{
	public const string PersonModelFileName = "yolov8n.onnx";
	public const string BaseballModelFileName = "pitcher_hitter_catcher.onnx";

	/// <summary>Legacy alias used by older call sites.</summary>
	public const string ModelFileName = PersonModelFileName;

	/// <summary>Public Hugging Face mirror of YOLOv8n ONNX (~12 MB).</summary>
	public const string DefaultDownloadUrl =
		"https://huggingface.co/cabelo/yolov8/resolve/main/yolov8n.onnx";

	/// <summary>
	/// BallDataLab / BaseballCV PHC weights (.pt). ONNX must be exported once with Ultralytics;
	/// place <see cref="BaseballModelFileName"/> in the model directory. Runtime itself is C#/ONNX only.
	/// </summary>
	public const string BaseballPtDownloadUrl =
		"https://data.balldatalab.com/index.php/s/KP5ZqJKEfjQ785X/download/pitcher_hitter_catcher_detector_v3.pt";

	public static string GetDefaultModelDirectory()
	{
		var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (!string.IsNullOrWhiteSpace(localApp))
			return Path.Combine(localApp, "MP4Tools", "models");

		return Path.Combine(TempPathHelper.GetTempPath(), "MP4Tools", "models");
	}

	public static string GetDefaultModelPath() =>
		Path.Combine(GetDefaultModelDirectory(), PersonModelFileName);

	public static string GetDefaultBaseballModelPath() =>
		Path.Combine(GetDefaultModelDirectory(), BaseballModelFileName);

	/// <summary>
	/// Prefer BaseballCV PHC ONNX for role classes; otherwise ensure COCO person ONNX.
	/// </summary>
	public static async Task<ResolvedYoloModel> EnsureDetectorModelAsync(
		string preferredPath = null,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		// Explicit path wins; infer kind from filename when possible.
		if (!string.IsNullOrWhiteSpace(preferredPath) && IsUsableModel(preferredPath.Trim()))
		{
			var path = preferredPath.Trim();
			var kind = LooksLikeBaseballModel(path) ? YoloModelKind.BaseballPhc : YoloModelKind.PersonCoco;
			log?.Invoke(
				kind == YoloModelKind.BaseballPhc
					? $"Using BaseballCV PHC model (hitter/pitcher/catcher): {path}"
					: $"Using YOLO person model: {path}");
			return new ResolvedYoloModel(path, kind);
		}

		foreach (var candidate in EnumerateCandidates(preferredPath: null, BaseballModelFileName))
		{
			if (IsUsableModel(candidate))
			{
				log?.Invoke($"Using BaseballCV PHC model (hitter/pitcher/catcher): {candidate}");
				return new ResolvedYoloModel(candidate, YoloModelKind.BaseballPhc);
			}
		}

		log?.Invoke(
			$"Baseball role model not found ({BaseballModelFileName}). " +
			$"Place the ONNX export in {GetDefaultModelDirectory()} for pitcher/hitter/catcher detection. " +
			$"Falling back to COCO person model.");

		var personPath = await EnsureModelAsync(preferredPath, ct, log).ConfigureAwait(false);
		return new ResolvedYoloModel(personPath, YoloModelKind.PersonCoco);
	}

	private static bool LooksLikeBaseballModel(string path)
	{
		var name = Path.GetFileName(path) ?? "";
		return name.Contains("pitcher_hitter", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("phc", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("baseball", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Returns a usable COCO person model path. Checks explicit path, app base Models/, then local cache;
	/// downloads into the local cache when missing.
	/// </summary>
	public static async Task<string> EnsureModelAsync(
		string preferredPath = null,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		foreach (var candidate in EnumerateCandidates(preferredPath, PersonModelFileName))
		{
			if (IsUsableModel(candidate))
			{
				log?.Invoke($"Using YOLO person model: {candidate}");
				return candidate;
			}
		}

		var dest = GetDefaultModelPath();
		var destDir = Path.GetDirectoryName(dest);
		if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
			Directory.CreateDirectory(destDir);

		log?.Invoke($"Downloading YOLO model ({PersonModelFileName})…");
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

	private static bool IsUsableModel(string path) =>
		!string.IsNullOrWhiteSpace(path)
		&& File.Exists(path)
		&& new FileInfo(path).Length > 1_000_000;

	private static IEnumerable<string> EnumerateCandidates(string preferredPath, string fileName)
	{
		if (!string.IsNullOrWhiteSpace(preferredPath))
		{
			var trimmed = preferredPath.Trim();
			yield return trimmed;
			// If caller passed a directory, look for the requested file inside it.
			if (Directory.Exists(trimmed))
				yield return Path.Combine(trimmed, fileName);
		}

		var baseDir = AppContext.BaseDirectory;
		if (!string.IsNullOrWhiteSpace(baseDir))
		{
			yield return Path.Combine(baseDir, "Models", fileName);
			yield return Path.Combine(baseDir, fileName);
		}

		yield return Path.Combine(GetDefaultModelDirectory(), fileName);

		var libModels = Path.GetFullPath(Path.Combine(baseDir ?? "", "..", "..", "..", "..", "MP4ToolsLib", "Models", fileName));
		yield return libModels;
	}
}
