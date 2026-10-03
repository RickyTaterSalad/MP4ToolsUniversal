using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

public sealed class CombineEditMapClip
{
	public int Index { get; set; }
	public string SourcePath { get; set; }
	public string SourceFileName { get; set; }
	/// <summary>Filename without extension (used to match <c>.LRF</c> proxies).</summary>
	public string SourceStem { get; set; }
	public double SourceDurationSeconds { get; set; }
}

/// <summary>
/// Sidecar describing how Combine built an output MP4 so LRF inning detection can remap
/// proxy timelines onto the final video (intro + start/end trims + clip order).
/// </summary>
public sealed class CombineEditMap
{
	public const int CurrentVersion = 1;
	public const string FileSuffix = ".combine-map.json";

	public int Version { get; set; } = CurrentVersion;
	public string OutputVideoPath { get; set; }
	public string CombineMapPath { get; set; }
	public DateTimeOffset CreatedUtc { get; set; }
	public double IntroDurationSeconds { get; set; }
	public bool IntroApplied { get; set; }
	public bool TrimFirstVideo { get; set; }
	public double StartSkipSeconds { get; set; }
	public bool TrimLastVideo { get; set; }
	/// <summary>
	/// Matches Combine "End Video Duration": keep this many seconds from the start of the
	/// last clip's contribution after concat (Combine applies <c>-t</c> on the full output).
	/// </summary>
	public double EndKeepSeconds { get; set; }
	public List<CombineEditMapClip> Clips { get; set; } = new();

	public double ComputeGameContentDurationSeconds()
	{
		if (Clips == null || Clips.Count == 0)
			return 0;

		var total = Clips.Sum(c => Math.Max(0, c.SourceDurationSeconds));
		var startSkip = TrimFirstVideo ? Math.Max(0, StartSkipSeconds) : 0;
		var endCut = 0.0;
		if (TrimLastVideo)
		{
			var last = Math.Max(0, Clips[^1].SourceDurationSeconds);
			var keep = Math.Max(0, EndKeepSeconds);
			endCut = Math.Max(0, last - keep);
		}

		return Math.Max(0, total - startSkip - endCut);
	}

	public double ComputeExpectedOutputDurationSeconds() =>
		(IntroApplied ? Math.Max(0, IntroDurationSeconds) : 0) + ComputeGameContentDurationSeconds();
}

public static class CombineEditMapIO
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public static string GetMapPathForOutputVideo(string outputVideoPath)
	{
		if (string.IsNullOrWhiteSpace(outputVideoPath))
			throw new ArgumentException("Output video path is required.", nameof(outputVideoPath));

		var dir = Path.GetDirectoryName(outputVideoPath);
		if (string.IsNullOrWhiteSpace(dir))
			dir = Directory.GetCurrentDirectory();

		var stem = Path.GetFileNameWithoutExtension(outputVideoPath);
		if (string.IsNullOrWhiteSpace(stem))
			stem = "combine";

		return Path.Combine(dir, stem + CombineEditMap.FileSuffix);
	}

	public static CombineEditMap Build(
		string outputVideoPath,
		IReadOnlyList<(string Path, double DurationSeconds)> sourceClips,
		bool introApplied,
		double introDurationSeconds,
		bool trimFirstVideo,
		double startSkipSeconds,
		bool trimLastVideo,
		double endKeepSeconds)
	{
		if (sourceClips == null || sourceClips.Count == 0)
			throw new ArgumentException("At least one source clip is required.", nameof(sourceClips));

		var intro = introApplied ? Math.Max(0, introDurationSeconds) : 0;
		var startSkip = trimFirstVideo ? Math.Max(0, startSkipSeconds) : 0;
		var map = new CombineEditMap
		{
			Version = CombineEditMap.CurrentVersion,
			OutputVideoPath = Path.GetFullPath(outputVideoPath),
			CreatedUtc = DateTimeOffset.UtcNow,
			IntroApplied = introApplied && intro > 0,
			IntroDurationSeconds = intro,
			TrimFirstVideo = trimFirstVideo && startSkip > 0,
			StartSkipSeconds = startSkip,
			TrimLastVideo = trimLastVideo,
			EndKeepSeconds = trimLastVideo ? Math.Max(0, endKeepSeconds) : 0,
			Clips = new List<CombineEditMapClip>(sourceClips.Count),
		};
		map.CombineMapPath = GetMapPathForOutputVideo(map.OutputVideoPath);

		for (var i = 0; i < sourceClips.Count; i++)
		{
			var (path, duration) = sourceClips[i];
			var fileName = Path.GetFileName(path) ?? $"clip_{i}";
			map.Clips.Add(new CombineEditMapClip
			{
				Index = i,
				SourcePath = path,
				SourceFileName = fileName,
				SourceStem = Path.GetFileNameWithoutExtension(fileName),
				SourceDurationSeconds = Math.Max(0, duration),
			});
		}

		return map;
	}

	public static async Task WriteAsync(
		CombineEditMap map,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (map is null)
			throw new ArgumentNullException(nameof(map));

		var path = string.IsNullOrWhiteSpace(map.CombineMapPath)
			? GetMapPathForOutputVideo(map.OutputVideoPath)
			: map.CombineMapPath;
		map.CombineMapPath = path;

		var dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
			Directory.CreateDirectory(dir);

		var json = JsonSerializer.Serialize(map, JsonOptions);
		await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
		log?.Invoke($"Wrote combine edit map: {path}");
	}

	public static async Task<CombineEditMap> LoadAsync(string mapOrVideoPath, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(mapOrVideoPath))
			throw new ArgumentException("Map or video path is required.", nameof(mapOrVideoPath));

		var path = mapOrVideoPath.Trim();
		if (path.EndsWith(CombineEditMap.FileSuffix, StringComparison.OrdinalIgnoreCase))
		{
			if (!File.Exists(path))
				throw new FileNotFoundException("Combine edit map not found.", path);
		}
		else
		{
			var mapPath = GetMapPathForOutputVideo(path);
			if (!File.Exists(mapPath))
			{
				throw new FileNotFoundException(
					$"Combine edit map not found next to video. Expected: {mapPath}",
					mapPath);
			}

			path = mapPath;
		}

		var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
		var map = JsonSerializer.Deserialize<CombineEditMap>(json, JsonOptions)
			?? throw new InvalidOperationException("Combine edit map was empty or invalid.");
		if (string.IsNullOrWhiteSpace(map.CombineMapPath))
			map.CombineMapPath = path;
		return map;
	}

	/// <summary>
	/// Finds the combine edit map beside a combined output video.
	/// Canonical name matches Combine write: <c>{videoStem}.combine-map.json</c>
	/// (same folder as the MP4; no extra prefix — e.g. <c>game_combined.mp4</c> →
	/// <c>game_combined.combine-map.json</c>).
	/// </summary>
	public static bool TryFindMapPath(string outputVideoPath, out string mapPath)
	{
		mapPath = null;
		if (string.IsNullOrWhiteSpace(outputVideoPath))
			return false;

		var canonical = GetMapPathForOutputVideo(outputVideoPath);
		if (File.Exists(canonical))
		{
			mapPath = canonical;
			return true;
		}

		// Fallback: scan the same folder for a map that points at this video
		// (handles minor renames / CleanupPath _N siblings).
		var dir = Path.GetDirectoryName(outputVideoPath);
		if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
		{
			mapPath = canonical;
			return false;
		}

		string fullVideo = null;
		try
		{
			fullVideo = Path.GetFullPath(outputVideoPath);
		}
		catch
		{
			fullVideo = outputVideoPath;
		}

		var videoFileName = Path.GetFileName(outputVideoPath);
		foreach (var candidate in Directory.EnumerateFiles(dir, "*" + CombineEditMap.FileSuffix))
		{
			try
			{
				var json = File.ReadAllText(candidate);
				using var doc = JsonDocument.Parse(json);
				if (!doc.RootElement.TryGetProperty("outputVideoPath", out var outProp))
					continue;
				var mappedOutput = outProp.GetString();
				if (string.IsNullOrWhiteSpace(mappedOutput))
					continue;

				string mappedFull;
				try
				{
					mappedFull = Path.GetFullPath(mappedOutput);
				}
				catch
				{
					mappedFull = mappedOutput;
				}

				if (string.Equals(mappedFull, fullVideo, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(Path.GetFileName(mappedOutput), videoFileName, StringComparison.OrdinalIgnoreCase))
				{
					mapPath = candidate;
					return true;
				}
			}
			catch
			{
				// ignore unreadable / non-map files
			}
		}

		mapPath = canonical;
		return false;
	}

	/// <summary>Expected sidecar path for a combined video (whether or not it exists yet).</summary>
	public static string GetExpectedMapPath(string outputVideoPath) =>
		GetMapPathForOutputVideo(outputVideoPath);
}
