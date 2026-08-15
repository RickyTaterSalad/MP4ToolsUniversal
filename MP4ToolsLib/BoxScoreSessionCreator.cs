using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

public sealed record CreateBoxScoreSessionRequest(
	string VisitorName,
	string HomeName,
	int VisitorScore = 0,
	int HomeScore = 0,
	string StartDate = null,
	string Timezone = null,
	string SessionId = null,
	string EventInfo = null,
	string IntroTitle = null,
	string IntroSubtitle = null,
	string IntroDetails = null,
	int IntroDurationSeconds = 0,
	string ImagePath = null);

public sealed record CreateBoxScoreSessionResult(
	string RecordingId,
	string ViewUrl,
	bool ManualOnly,
	bool HasBoxScore,
	bool? Parsed = null,
	string ParseError = null,
	bool ParseSkipped = false,
	string ImageUrl = null);

/// <summary>
/// Creates a Baseball Logger source recording from Combine game info (no share JSON)
/// via <c>POST /api/boxscore-sessions</c> (API key required).
/// </summary>
public static class BoxScoreSessionCreator
{
	private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

	/// <summary>
	/// Returns true when server URL, API key, visitor name, and home name are set.
	/// Image is optional.
	/// </summary>
	public static bool CanCreate(
		string visitorName,
		string homeName,
		string configuredServerBaseUrl,
		string apiKey)
	{
		if (string.IsNullOrWhiteSpace(visitorName)
			|| string.IsNullOrWhiteSpace(homeName)
			|| string.IsNullOrWhiteSpace(configuredServerBaseUrl)
			|| string.IsNullOrWhiteSpace(apiKey))
		{
			return false;
		}

		return !string.IsNullOrWhiteSpace(
			ModifiedRecordingUploader.ResolveServerBaseUrl(configuredServerBaseUrl));
	}

	/// <summary>
	/// Posts multipart form fields to <c>/api/boxscore-sessions</c>.
	/// Does not send events, notes, or share-token headers.
	/// </summary>
	public static async Task<CreateBoxScoreSessionResult> CreateAsync(
		string serverBaseUrl,
		string apiKey,
		CreateBoxScoreSessionRequest request,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (request is null)
			throw new ArgumentNullException(nameof(request));
		if (string.IsNullOrWhiteSpace(serverBaseUrl))
			throw new ArgumentException("Server base URL is required.", nameof(serverBaseUrl));
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new ArgumentException("API key is required.", nameof(apiKey));
		if (string.IsNullOrWhiteSpace(request.VisitorName))
			throw new ArgumentException("Visitor name is required.", nameof(request));
		if (string.IsNullOrWhiteSpace(request.HomeName))
			throw new ArgumentException("Home name is required.", nameof(request));

		if (!string.IsNullOrWhiteSpace(request.ImagePath))
		{
			var image = request.ImagePath.Trim();
			if (!File.Exists(image))
				throw new FileNotFoundException("Box score image not found.", image);

			var ext = Path.GetExtension(image).ToLowerInvariant();
			if (Array.IndexOf(AllowedExtensions, ext) < 0)
			{
				throw new ArgumentException(
					"Box score image must be JPEG, PNG, WebP, or GIF.", nameof(request));
			}
		}

		var baseUri = NormalizeBaseUri(serverBaseUrl);
		log?.Invoke($"Creating box-score session at {baseUri}api/boxscore-sessions …");

		using var http = new HttpClient { BaseAddress = baseUri };
		using var form = new MultipartFormDataContent();

		void Add(string name, string value)
		{
			if (!string.IsNullOrWhiteSpace(value))
				form.Add(new StringContent(value.Trim()), name);
		}

		Add("visitorName", request.VisitorName);
		Add("homeName", request.HomeName);
		form.Add(new StringContent(Math.Max(0, request.VisitorScore).ToString()), "visitorScore");
		form.Add(new StringContent(Math.Max(0, request.HomeScore).ToString()), "homeScore");
		Add("startDate", request.StartDate);
		Add("timezone", request.Timezone);
		Add("sessionId", request.SessionId);
		Add("eventInfo", request.EventInfo);
		Add("introTitle", request.IntroTitle);
		Add("introSubtitle", request.IntroSubtitle);
		Add("introDetails", request.IntroDetails);
		if (request.IntroDurationSeconds > 0)
			form.Add(new StringContent(request.IntroDurationSeconds.ToString()), "introDurationSeconds");

		ByteArrayContent fileContent = null;
		if (!string.IsNullOrWhiteSpace(request.ImagePath))
		{
			var image = request.ImagePath.Trim();
			var bytes = await File.ReadAllBytesAsync(image, ct).ConfigureAwait(false);
			fileContent = new ByteArrayContent(bytes);
			fileContent.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeForExtension(image));
			form.Add(fileContent, "file", Path.GetFileName(image));
		}

		using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/boxscore-sessions")
		{
			Content = form,
		};
		httpRequest.Headers.TryAddWithoutValidation("apikey", apiKey.Trim());
		httpRequest.Headers.Accept.ParseAdd("application/json");

		using var response = await http.SendAsync(httpRequest, ct).ConfigureAwait(false);
		var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			var error = TryReadError(body) ?? body;
			throw new InvalidOperationException(
				$"Create box-score session failed {(int)response.StatusCode}: {error}");
		}

		var result = ParseCreateResult(body);
		if (string.IsNullOrWhiteSpace(result.RecordingId))
			throw new InvalidOperationException("Create box-score session response missing recording id.");

		log?.Invoke(
			result.HasBoxScore
				? $"Created box-score session {result.RecordingId} (image attached)."
				: $"Created box-score session {result.RecordingId} (manual stats).");
		return result;
	}

	private static CreateBoxScoreSessionResult ParseCreateResult(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
			return new CreateBoxScoreSessionResult(null, null, false, false);

		try
		{
			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;

			string recordingId = null;
			if (root.TryGetProperty("recording", out var rec)
				&& rec.ValueKind == JsonValueKind.Object
				&& rec.TryGetProperty("id", out var id)
				&& id.ValueKind == JsonValueKind.String)
			{
				recordingId = id.GetString();
			}

			if (string.IsNullOrWhiteSpace(recordingId))
				recordingId = GetString(root, "recordingId");

			bool? parsed = null;
			if (root.TryGetProperty("parsed", out var parsedProp)
				&& (parsedProp.ValueKind == JsonValueKind.True || parsedProp.ValueKind == JsonValueKind.False))
			{
				parsed = parsedProp.GetBoolean();
			}

			return new CreateBoxScoreSessionResult(
				recordingId,
				GetString(root, "viewUrl") ?? "",
				root.TryGetProperty("manualOnly", out var mo) && mo.ValueKind == JsonValueKind.True,
				root.TryGetProperty("hasBoxScore", out var hb) && hb.ValueKind == JsonValueKind.True,
				parsed,
				GetString(root, "parseError"),
				root.TryGetProperty("parseSkipped", out var ps) && ps.ValueKind == JsonValueKind.True,
				GetString(root, "imageUrl"));
		}
		catch
		{
			return new CreateBoxScoreSessionResult(null, null, false, false);
		}
	}

	private static Uri NormalizeBaseUri(string serverBaseUrl)
	{
		var trimmed = serverBaseUrl.Trim().TrimEnd('/');
		if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
			|| (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
		{
			throw new ArgumentException($"Invalid server base URL: {serverBaseUrl}", nameof(serverBaseUrl));
		}

		var builder = new UriBuilder(uri) { Path = "/", Query = null, Fragment = null };
		return builder.Uri;
	}

	private static string ContentTypeForExtension(string path)
	{
		return Path.GetExtension(path).ToLowerInvariant() switch
		{
			".png" => "image/png",
			".webp" => "image/webp",
			".gif" => "image/gif",
			_ => "image/jpeg",
		};
	}

	private static string TryReadError(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
			return null;
		try
		{
			using var doc = JsonDocument.Parse(body);
			if (doc.RootElement.TryGetProperty("error", out var err))
				return err.GetString();
		}
		catch
		{
			// ignored
		}

		return null;
	}

	private static string GetString(JsonElement root, string name)
	{
		return root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
			? prop.GetString()
			: null;
	}
}
