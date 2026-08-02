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
/// Reads a recording JSON (local path or http/https URL), subtracts a start-skip offset from all
/// <c>elapsed</c> and <c>stopElapsed</c> string fields, and writes a new file. Never modifies the source.
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
	/// Loads source JSON, applies the start-skip offset, writes the offset JSON, and returns the in-memory root.
	/// </summary>
	public static async Task<JsonNode> WriteOffsetCopyAsync(
		string sourcePathOrUrl,
		string destinationPath,
		double offsetSeconds,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (string.IsNullOrWhiteSpace(sourcePathOrUrl))
			throw new ArgumentException("Source path or URL is required.", nameof(sourcePathOrUrl));
		if (string.IsNullOrWhiteSpace(destinationPath))
			throw new ArgumentException("Destination path is required.", nameof(destinationPath));

		var root = await LoadOffsetRootAsync(sourcePathOrUrl.Trim(), offsetSeconds, ct, log).ConfigureAwait(false);

		var destDir = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
			Directory.CreateDirectory(destDir);

		await File.WriteAllTextAsync(destinationPath, root.ToJsonString(WriteOptions), ct).ConfigureAwait(false);
		log?.Invoke($"Wrote offset recording JSON ({Math.Max(0, offsetSeconds):0.###}s start skip): {destinationPath}");
		return root;
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
		Action<string> log = null)
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
			homeScore);

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
		int? homeScore)
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

			foreach (var bookmark in bookmarks)
				sb.AppendLine($"{bookmark.Elapsed} {bookmark.Text}");
		}

		return sb.ToString().TrimEnd() + Environment.NewLine;
	}

	private static async Task<JsonNode> LoadOffsetRootAsync(
		string sourcePathOrUrl,
		double offsetSeconds,
		CancellationToken ct,
		Action<string> log)
	{
		var json = await ReadSourceAsync(sourcePathOrUrl, ct, log).ConfigureAwait(false);
		var root = JsonNode.Parse(json)
			?? throw new InvalidOperationException("Recording JSON is empty or invalid.");

		OffsetElapsedFields(root, Math.Max(0, offsetSeconds));
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
				log?.Invoke($"Recording JSON from URL:{Environment.NewLine}{body}");
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

	private static void OffsetElapsedFields(JsonNode node, double offsetSeconds)
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
					obj[key] = OffsetTimeString(raw, offsetSeconds);
				}
				else
				{
					OffsetElapsedFields(child, offsetSeconds);
				}
			}
		}
		else if (node is JsonArray arr)
		{
			foreach (var item in arr)
			{
				if (item != null)
					OffsetElapsedFields(item, offsetSeconds);
			}
		}
	}

	private static string OffsetTimeString(string time, double offsetSeconds)
	{
		var range = TimeRange.FromString(time);
		if (range == null)
			return time ?? "00:00:00";

		var remaining = Math.Max(0, range.TotalSeconds - offsetSeconds);
		var ts = TimeSpan.FromSeconds(Math.Floor(remaining));
		return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
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

		if (obj["notes"] is JsonArray notes)
		{
			foreach (var item in notes.OfType<JsonObject>())
			{
				var text = item["text"]?.GetValue<string>()?.Trim();
				if (string.IsNullOrWhiteSpace(text))
					continue;

				AddBookmark(bookmarks, item["elapsed"]?.GetValue<string>(), text);
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
			? $"{range.Hours:00}:{range.Minutes:00}:{range.Seconds:00}"
			: (elapsed?.Trim() ?? "00:00:00");
		bookmarks.Add((seconds, formatted, text));
	}
}
