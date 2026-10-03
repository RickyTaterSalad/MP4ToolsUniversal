using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

/// <summary>Reads intro title-card text via OpenRouter vision chat completions.</summary>
public static class OpenRouterIntroReader
{
	public const string DefaultModel = "google/gemini-2.5-flash";
	private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
	};

	public static async Task<IntroScreenReadResult> ReadIntroFromImageAsync(
		string imagePath,
		string apiKey,
		string model = null,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
			throw new FileNotFoundException("Intro frame image not found.", imagePath);
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new ArgumentException("OpenRouter API key is required.", nameof(apiKey));

		var resolvedModel = string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
		var bytes = await File.ReadAllBytesAsync(imagePath, ct).ConfigureAwait(false);
		var b64 = Convert.ToBase64String(bytes);
		var mime = imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
		var dataUrl = $"data:{mime};base64,{b64}";

		const string prompt =
			"""
			This image is a sports video intro title card (dark background, white centered text).
			Read the on-screen text carefully, preserving any typos exactly as written.

			Return ONLY a JSON object with these keys (use empty string or null when unknown):
			{
			  "title": "largest/main line, often TEAM vs. TEAM",
			  "subtitle": "date and/or event line under the title",
			  "details": "score / winner line if present",
			  "visitorName": "visitor/away team if parseable from title",
			  "homeName": "home team if parseable from title",
			  "visitorScore": number or null,
			  "homeScore": number or null,
			  "eventDate": "MM/dd/yyyy if a date appears, else null",
			  "eventInfo": "event name text without the date, else empty"
			}

			If the title is like "A vs. B" or "A vs B", visitorName is A and homeName is B.
			If details is like "Final Score 5-3 WINNER", set scores high/low into the matching teams when possible; otherwise leave scores null.
			Do not invent text that is not visible.
			""";

		var payload = new Dictionary<string, object>
		{
			["model"] = resolvedModel,
			["temperature"] = 0,
			["messages"] = new object[]
			{
				new Dictionary<string, object>
				{
					["role"] = "user",
					["content"] = new object[]
					{
						new Dictionary<string, object>
						{
							["type"] = "text",
							["text"] = prompt,
						},
						new Dictionary<string, object>
						{
							["type"] = "image_url",
							["image_url"] = new Dictionary<string, object>
							{
								["url"] = dataUrl,
							},
						},
					},
				},
			},
		};

		log?.Invoke($"OpenRouter: reading intro with model {resolvedModel}…");

		using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
		using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
		request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://github.com/MP4Tools");
		request.Headers.TryAddWithoutValidation("X-Title", "MP4Tools Rewrite Intro");
		request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

		using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
		var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			var snippet = body.Length > 400 ? body[..400] + "…" : body;
			throw new InvalidOperationException($"OpenRouter request failed ({(int)response.StatusCode}): {snippet}");
		}

		using var respDoc = JsonDocument.Parse(body);
		if (!respDoc.RootElement.TryGetProperty("choices", out var choices)
			|| choices.ValueKind != JsonValueKind.Array
			|| choices.GetArrayLength() == 0)
		{
			throw new InvalidOperationException("OpenRouter response had no choices.");
		}

		var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
		var json = ExtractJsonObject(content);
		if (string.IsNullOrWhiteSpace(json))
			throw new InvalidOperationException("OpenRouter response did not contain JSON.");

		var parsed = JsonSerializer.Deserialize<OpenRouterIntroDto>(json, JsonOptions)
			?? throw new InvalidOperationException("Failed to parse intro JSON.");

		return new IntroScreenReadResult
		{
			Title = parsed.Title?.Trim() ?? "",
			Subtitle = parsed.Subtitle?.Trim() ?? "",
			Details = parsed.Details?.Trim() ?? "",
			VisitorName = parsed.VisitorName?.Trim() ?? "",
			HomeName = parsed.HomeName?.Trim() ?? "",
			VisitorScore = parsed.VisitorScore,
			HomeScore = parsed.HomeScore,
			EventDate = TryParseDate(parsed.EventDate),
			EventInfo = parsed.EventInfo?.Trim() ?? "",
		};
	}

	private static DateTime? TryParseDate(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;
		if (DateTime.TryParse(value, out var dt))
			return dt.Date;
		return null;
	}

	private static string ExtractJsonObject(string content)
	{
		if (string.IsNullOrWhiteSpace(content))
			return null;

		var trimmed = content.Trim();
		if (trimmed.StartsWith("```", StringComparison.Ordinal))
		{
			var match = Regex.Match(trimmed, @"```(?:json)?\s*(\{[\s\S]*\})\s*```", RegexOptions.IgnoreCase);
			if (match.Success)
				return match.Groups[1].Value;
		}

		var start = trimmed.IndexOf('{');
		var end = trimmed.LastIndexOf('}');
		if (start >= 0 && end > start)
			return trimmed[start..(end + 1)];

		return null;
	}

	private sealed class OpenRouterIntroDto
	{
		public string Title { get; set; }
		public string Subtitle { get; set; }
		public string Details { get; set; }
		public string VisitorName { get; set; }
		public string HomeName { get; set; }
		public int? VisitorScore { get; set; }
		public int? HomeScore { get; set; }
		public string EventDate { get; set; }
		public string EventInfo { get; set; }
	}
}
