using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>
/// Builds Baseball Logger / Combine-compatible recording session JSON from detected half-innings.
/// </summary>
public static class InningDetectionRecordingWriter
{
	private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

	public static string BuildUniqueOutputPath(string videoPath)
	{
		if (string.IsNullOrWhiteSpace(videoPath))
			throw new ArgumentException("Video path is required.", nameof(videoPath));

		var dir = Path.GetDirectoryName(videoPath);
		if (string.IsNullOrWhiteSpace(dir))
			dir = Directory.GetCurrentDirectory();

		var stem = Path.GetFileNameWithoutExtension(videoPath);
		if (string.IsNullOrWhiteSpace(stem))
			stem = "recording";

		var stamp = DateTime.Now.ToString("yyyyMMddTHHmmss");
		var candidate = Path.Combine(dir, $"{stem}_innings_{stamp}.json");
		return FFMpegUtils.Instance.CleanupPath(candidate);
	}

	public static string BuildYoutubeDescriptionPath(string jsonPath)
	{
		if (string.IsNullOrWhiteSpace(jsonPath))
			throw new ArgumentException("JSON path is required.", nameof(jsonPath));
		return Path.ChangeExtension(jsonPath, ".txt");
	}

	public static (string JsonPath, string YoutubeDescriptionPath) BuildOutputPaths(string videoPath)
	{
		var jsonPath = BuildUniqueOutputPath(videoPath);
		return (jsonPath, BuildYoutubeDescriptionPath(jsonPath));
	}

	public static JsonObject BuildSessionJson(
		string videoPath,
		double durationSeconds,
		IReadOnlyList<InningDetectionEvent> events,
		DateTime? recordingStartLocal = null)
	{
		var start = recordingStartLocal ?? InferRecordingStart(videoPath, durationSeconds);
		var tz = TimeZoneInfo.Local.Id;
		var duration = Math.Max(0, durationSeconds);

		var jsonEvents = new JsonArray();
		foreach (var ev in events.OrderBy(e => e.ElapsedSeconds).ThenBy(e => e.Label, StringComparer.OrdinalIgnoreCase))
		{
			var elapsedSec = Math.Max(0, ev.ElapsedSeconds);
			var clock = start.AddSeconds(elapsedSec);
			jsonEvents.Add(new JsonObject
			{
				["elapsed"] = FormatElapsed(elapsedSec),
				["timestamp"] = clock.ToString("HH:mm:ss"),
				["label"] = ev.Label ?? string.Empty,
			});
		}

		var idStem = Path.GetFileNameWithoutExtension(videoPath);
		if (string.IsNullOrWhiteSpace(idStem))
			idStem = "InningDetector";

		return new JsonObject
		{
			["id"] = idStem,
			["timezone"] = tz,
			["startDate"] = start.ToString("yyyy-MM-dd"),
			["startTimestamp"] = start.ToString("HH:mm:ss"),
			["stopElapsed"] = FormatElapsed(duration),
			["events"] = jsonEvents,
			["notes"] = new JsonArray(),
			["visitorScore"] = 0,
			["homeScore"] = 0,
		};
	}

	public static async Task WriteAsync(
		JsonObject root,
		string destinationPath,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (root is null)
			throw new ArgumentNullException(nameof(root));
		if (string.IsNullOrWhiteSpace(destinationPath))
			throw new ArgumentException("Destination path is required.", nameof(destinationPath));

		var destDir = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
			Directory.CreateDirectory(destDir);

		await File.WriteAllTextAsync(destinationPath, root.ToJsonString(WriteOptions), ct).ConfigureAwait(false);
		log?.Invoke($"Wrote recording JSON: {destinationPath}");
	}

	/// <summary>
	/// Writes recording JSON plus the Combine-compatible YouTube chapter/bookmark <c>.txt</c>
	/// next to it (same stem, <c>.txt</c> extension).
	/// </summary>
	public static async Task<(string JsonPath, string YoutubeDescriptionPath)> WriteSessionAndYoutubeAsync(
		JsonObject root,
		string jsonDestinationPath,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		await WriteAsync(root, jsonDestinationPath, ct, log).ConfigureAwait(false);

		// Same stem as Combine: paired .json / .txt next to the source video.
		var txtPath = BuildYoutubeDescriptionPath(jsonDestinationPath);

		await RecordingJsonElapsedOffset.WriteYoutubeDescriptionAsync(
			root,
			txtPath,
			eventDate: null,
			eventInfo: null,
			visitorName: null,
			visitorScore: null,
			homeName: null,
			homeScore: null,
			ct,
			log).ConfigureAwait(false);

		return (jsonDestinationPath, txtPath);
	}

	private static DateTime InferRecordingStart(string videoPath, double durationSeconds)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(videoPath) && File.Exists(videoPath))
			{
				var write = File.GetLastWriteTime(videoPath);
				var start = write.AddSeconds(-Math.Max(0, durationSeconds));
				return start;
			}
		}
		catch
		{
			// fall through
		}

		return DateTime.Now.AddSeconds(-Math.Max(0, durationSeconds));
	}

	private static string FormatElapsed(double totalSeconds)
	{
		var remaining = Math.Max(0, totalSeconds);
		var ts = TimeSpan.FromSeconds(Math.Floor(remaining));
		return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
	}
}
