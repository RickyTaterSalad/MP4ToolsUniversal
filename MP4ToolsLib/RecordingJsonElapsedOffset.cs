using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>
/// Reads a recording JSON (local path or http/https URL), adjusts all <c>elapsed</c> and
/// <c>stopElapsed</c> string fields (subtract start skip, optionally add intro duration),
/// and writes a new file. Never modifies the source. When an intro is present, elapsed
/// times are floored at the intro duration so early events land after the intro screen.
/// </summary>
public static class RecordingJsonElapsedOffset
{
	private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };
	private static readonly HashSet<string> ExcludedBookmarkLabels = new(StringComparer.OrdinalIgnoreCase)
	{
		"Recording Start",
		"Recording End",
	};

	/// <summary>
	/// Loads source JSON, applies start-skip (subtract) and optional intro (add) offsets,
	/// writes the adjusted JSON, and returns the in-memory root.
	/// </summary>
	public static async Task<JsonNode> WriteOffsetCopyAsync(
		string sourcePathOrUrl,
		string destinationPath,
		double startSkipSeconds,
		CancellationToken ct = default,
		Action<string> log = null,
		double introAddSeconds = 0)
	{
		if (string.IsNullOrWhiteSpace(sourcePathOrUrl))
			throw new ArgumentException("Source path or URL is required.", nameof(sourcePathOrUrl));
		if (string.IsNullOrWhiteSpace(destinationPath))
			throw new ArgumentException("Destination path is required.", nameof(destinationPath));

		var skip = Math.Max(0, startSkipSeconds);
		var intro = Math.Max(0, introAddSeconds);
		// Net shift relative to original elapsed: remove skipped lead-in, then push forward by intro.
		// When an intro is present, floor elapsed at intro length so early events land after the intro.
		var netAddSeconds = intro - skip;
		var root = await LoadOffsetRootAsync(sourcePathOrUrl.Trim(), netAddSeconds, intro, ct, log)
			.ConfigureAwait(false);

		var destDir = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
			Directory.CreateDirectory(destDir);

		await File.WriteAllTextAsync(destinationPath, root.ToJsonString(WriteOptions), ct).ConfigureAwait(false);
		log?.Invoke(
			$"Wrote offset recording JSON (start skip -{skip:0.###}s, intro +{intro:0.###}s): {destinationPath}");
		return root;
	}

	/// <summary>
	/// Merges Combine intro-screen / game-info fields into the session JSON root
	/// (used for the local offset file and Baseball Logger revision upload).
	/// When game info is present, sets <c>id</c> to the same display name used for the combine MP4 file.
	/// </summary>
	public static void ApplyCombineMetadata(
		JsonNode root,
		string introTitle,
		string introSubtitle,
		string introDetails,
		int introDurationSeconds,
		DateTime? eventDate,
		string eventInfo,
		string visitorName,
		int? visitorScore,
		string homeName,
		int? homeScore)
	{
		if (root is not JsonObject obj)
			throw new ArgumentException("Recording JSON root must be an object.", nameof(root));

		var title = introTitle?.Trim() ?? "";
		var subtitle = introSubtitle?.Trim() ?? "";
		var details = introDetails?.Trim() ?? "";
		var duration = Math.Max(0, introDurationSeconds);

		var metadata = new JsonObject
		{
			["durationSeconds"] = duration,
		};
		if (!string.IsNullOrWhiteSpace(title))
			metadata["title"] = title;
		if (!string.IsNullOrWhiteSpace(subtitle))
			metadata["subtitle"] = subtitle;
		if (!string.IsNullOrWhiteSpace(details))
			metadata["details"] = details;
		obj["metadata"] = metadata;

		// Keep flat intro fields for older server builds / tooling.
		SetOptionalString(obj, "introTitle", title);
		SetOptionalString(obj, "introSubtitle", subtitle);
		SetOptionalString(obj, "introDetails", details);
		obj["introDurationSeconds"] = duration;

		SetOptionalString(obj, "eventInfo", eventInfo);
		SetOptionalString(obj, "visitorName", visitorName);
		SetOptionalString(obj, "homeName", homeName);

		if (eventDate.HasValue)
			obj["startDate"] = eventDate.Value.ToString("yyyy-MM-dd");

		if (visitorScore.HasValue)
			obj["visitorScore"] = Math.Max(0, visitorScore.Value);

		if (homeScore.HasValue)
			obj["homeScore"] = Math.Max(0, homeScore.Value);

		var displayName = FileUtils.BuildGameInfoOutputFileName(
			eventDate, visitorName, homeName, visitorScore, homeScore);
		if (!string.IsNullOrWhiteSpace(displayName))
			obj["id"] = displayName;
	}

	public static async Task WriteJsonNodeAsync(
		JsonNode root,
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

	private static void SetOptionalString(JsonObject obj, string key, string value)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
			obj.Remove(key);
		else
			obj[key] = trimmed;
	}

	public static async Task WriteYoutubeDescriptionAsync(
		JsonNode offsetRoot,
		string destinationPath,
		DateTime? eventDate,
		string eventInfo,
		string visitorName,
		int? visitorScore,
		string homeName,
		int? homeScore,
		CancellationToken ct = default,
		Action<string> log = null,
		double bookmarkTimeOffsetSeconds = 0)
	{
		if (offsetRoot is null)
			throw new ArgumentNullException(nameof(offsetRoot));
		if (string.IsNullOrWhiteSpace(destinationPath))
			throw new ArgumentException("Destination path is required.", nameof(destinationPath));

		var text = BuildYoutubeDescription(
			offsetRoot,
			eventDate,
			eventInfo,
			visitorName,
			visitorScore,
			homeName,
			homeScore,
			bookmarkTimeOffsetSeconds);

		var destDir = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
			Directory.CreateDirectory(destDir);

		await File.WriteAllTextAsync(destinationPath, text, ct).ConfigureAwait(false);
		log?.Invoke($"Wrote YouTube description: {destinationPath}");
	}

	public static string BuildYoutubeDescription(
		JsonNode offsetRoot,
		DateTime? eventDate,
		string eventInfo,
		string visitorName,
		int? visitorScore,
		string homeName,
		int? homeScore,
		double bookmarkTimeOffsetSeconds = 0)
	{
		var sb = new StringBuilder();

		var visitor = visitorName?.Trim() ?? string.Empty;
		var home = homeName?.Trim() ?? string.Empty;

		if (TryGetWinnerLine(visitor, visitorScore, home, homeScore, out var winnerLine))
			sb.AppendLine(winnerLine);

		if (eventDate.HasValue)
			sb.AppendLine($"Event Date: {eventDate.Value:MM/dd/yyyy}");

		if (!string.IsNullOrWhiteSpace(eventInfo))
			sb.AppendLine($"Event Info: {eventInfo.Trim()}");

		if (!string.IsNullOrWhiteSpace(visitor) || !string.IsNullOrWhiteSpace(home) || visitorScore.HasValue || homeScore.HasValue)
		{
			var visitorPart = FormatTeamLine(visitor, visitorScore);
			var homePart = FormatTeamLine(home, homeScore);
			sb.AppendLine($"Visitor: {visitorPart}");
			sb.AppendLine($"Home: {homePart}");
		}

		var bookmarks = CollectBookmarks(offsetRoot);
		if (bookmarks.Count > 0)
		{
			if (sb.Length > 0)
				sb.AppendLine();

			var introOffset = Math.Max(0, bookmarkTimeOffsetSeconds);
			foreach (var bookmark in bookmarks)
			{
				var elapsed = introOffset > 0
					? FormatElapsedTimestamp(bookmark.Seconds + introOffset)
					: bookmark.Elapsed;
				sb.AppendLine($"{elapsed} {bookmark.Text}");
			}
		}

		return sb.ToString().TrimEnd() + Environment.NewLine;
	}

	private static async Task<JsonNode> LoadOffsetRootAsync(
		string sourcePathOrUrl,
		double netAddSeconds,
		double minElapsedSeconds,
		CancellationToken ct,
		Action<string> log)
	{
		var json = await ReadSourceAsync(sourcePathOrUrl, ct, log).ConfigureAwait(false);
		var root = JsonNode.Parse(json)
			?? throw new InvalidOperationException("Recording JSON is empty or invalid.");

		AdjustElapsedFields(root, netAddSeconds, minElapsedSeconds);
		return root;
	}

	private static async Task<string> ReadSourceAsync(string sourcePathOrUrl, CancellationToken ct, Action<string> log)
	{
		if (Uri.TryCreate(sourcePathOrUrl, UriKind.Absolute, out var uri)
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			log?.Invoke($"Fetching recording JSON from URL: {uri}");
			try
			{
				using var client = new HttpClient();
				using var response = await client.GetAsync(uri, ct).ConfigureAwait(false);
				if (!response.IsSuccessStatusCode)
				{
					throw new HttpRequestException(
						$"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
				}

				var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
				log?.Invoke($"Fetched recording JSON ({body.Length} chars).");
				return body;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				log?.Invoke($"Cannot access recording JSON URL: {uri} ({ex.Message})");
				throw;
			}
		}

		if (!File.Exists(sourcePathOrUrl))
			throw new FileNotFoundException("Recording JSON file not found.", sourcePathOrUrl);

		return await File.ReadAllTextAsync(sourcePathOrUrl, ct).ConfigureAwait(false);
	}

	private static void AdjustElapsedFields(JsonNode node, double netAddSeconds, double minElapsedSeconds)
	{
		if (node is JsonObject obj)
		{
			foreach (var key in obj.Select(p => p.Key).ToList())
			{
				var child = obj[key];
				if (child is null)
					continue;

				if ((key is "elapsed" or "stopElapsed") && child is JsonValue value)
				{
					var raw = value.GetValue<string>();
					obj[key] = AdjustTimeString(raw, netAddSeconds, minElapsedSeconds);
				}
				else
				{
					AdjustElapsedFields(child, netAddSeconds, minElapsedSeconds);
				}
			}
		}
		else if (node is JsonArray arr)
		{
			foreach (var item in arr)
			{
				if (item != null)
					AdjustElapsedFields(item, netAddSeconds, minElapsedSeconds);
			}
		}
	}

	/// <summary>
	/// Applies a signed net add to an HH:MM:SS elapsed string.
	/// Floors at <paramref name="minElapsedSeconds"/> (intro length when an intro is present, otherwise 0).
	/// </summary>
	private static string AdjustTimeString(string time, double netAddSeconds, double minElapsedSeconds)
	{
		var range = TimeRange.FromString(time);
		if (range == null)
			return time ?? "00:00:00";

		var floor = Math.Max(0, minElapsedSeconds);
		return FormatElapsedTimestamp(Math.Max(floor, range.TotalSeconds + netAddSeconds));
	}

	private static string FormatTeamLine(string name, int? score)
	{
		if (string.IsNullOrWhiteSpace(name) && !score.HasValue)
			return string.Empty;
		if (string.IsNullOrWhiteSpace(name))
			return score.HasValue ? score.Value.ToString() : string.Empty;
		return score.HasValue ? $"{name} ({score.Value})" : name;
	}

	private static bool TryGetWinnerLine(
		string visitorName,
		int? visitorScore,
		string homeName,
		int? homeScore,
		out string winnerLine)
	{
		winnerLine = null;
		if (!visitorScore.HasValue || !homeScore.HasValue)
			return false;

		var visScore = visitorScore.Value;
		var hScore = homeScore.Value;
		if (visScore > hScore)
		{
			var winner = !string.IsNullOrWhiteSpace(visitorName) ? visitorName : "Visitor";
			winnerLine = $"Winner: {winner} ({visScore}-{hScore})";
			return true;
		}
		if (hScore > visScore)
		{
			var winner = !string.IsNullOrWhiteSpace(homeName) ? homeName : "Home";
			winnerLine = $"Winner: {winner} ({hScore}-{visScore})";
			return true;
		}

		winnerLine = $"Winner: TIE ({visScore}-{hScore})";
		return true;
	}

	private static List<(double Seconds, string Elapsed, string Text)> CollectBookmarks(JsonNode root)
	{
		var bookmarks = new List<(double Seconds, string Elapsed, string Text)>();
		if (root is not JsonObject obj)
			return bookmarks;

		if (obj["events"] is JsonArray events)
		{
			foreach (var item in events.OfType<JsonObject>())
			{
				var label = item["label"]?.GetValue<string>()?.Trim();
				if (string.IsNullOrWhiteSpace(label) || ExcludedBookmarkLabels.Contains(label))
					continue;

				AddBookmark(bookmarks, item["elapsed"]?.GetValue<string>(), label);
			}
		}

		return bookmarks
			.OrderBy(b => b.Seconds)
			.ThenBy(b => b.Text, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static void AddBookmark(
		List<(double Seconds, string Elapsed, string Text)> bookmarks,
		string elapsed,
		string text)
	{
		var range = TimeRange.FromString(elapsed);
		var seconds = range?.TotalSeconds ?? 0;
		var formatted = range != null
			? FormatElapsedTimestamp(seconds)
			: (elapsed?.Trim() ?? "00:00:00");
		bookmarks.Add((seconds, formatted, text));
	}

	private static string FormatElapsedTimestamp(double totalSeconds)
	{
		var remaining = Math.Max(0, totalSeconds);
		var ts = TimeSpan.FromSeconds(Math.Floor(remaining));
		return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
	}
}
