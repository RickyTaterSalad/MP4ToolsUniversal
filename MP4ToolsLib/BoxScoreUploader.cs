using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

public sealed record UploadBoxScoreResult(
	string RecordingId,
	string FileName,
	long SizeBytes,
	bool Parsed,
	string ParseError,
	bool ParseSkipped,
	string ImageUrl);

/// <summary>
/// Uploads a box-score image to Baseball Logger via
/// <c>POST /api/recordings/{id}/boxscore</c> (API key required).
/// The recording is resolved from the session JSON root <c>id</c> (session id / file name / UUID).
/// </summary>
public static class BoxScoreUploader
{
	private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

	/// <summary>
	/// Returns true when server URL and API key are set and the image exists on disk
	/// with an allowed extension. Recording identity is resolved at upload time.
	/// </summary>
	public static bool CanUpload(
		string recordingJsonPathOrUrl,
		string imagePath,
		string configuredServerBaseUrl,
		string apiKey)
	{
		if (string.IsNullOrWhiteSpace(configuredServerBaseUrl)
			|| string.IsNullOrWhiteSpace(apiKey)
			|| string.IsNullOrWhiteSpace(recordingJsonPathOrUrl)
			|| string.IsNullOrWhiteSpace(imagePath))
		{
			return false;
		}

		if (!File.Exists(imagePath.Trim()))
			return false;

		var ext = Path.GetExtension(imagePath.Trim());
		if (string.IsNullOrEmpty(ext)
			|| Array.IndexOf(AllowedExtensions, ext.ToLowerInvariant()) < 0)
		{
			return false;
		}

		return !string.IsNullOrWhiteSpace(
			ModifiedRecordingUploader.ResolveServerBaseUrl(configuredServerBaseUrl));
	}

	/// <summary>
	/// Reads the session JSON (local path or http(s) URL), takes root <c>id</c> as the
	/// recording resolve key, and uploads <paramref name="imagePath"/> as multipart field <c>file</c>.
	/// If a box score already exists (conflict), deletes it and uploads again.
	/// </summary>
	public static async Task<UploadBoxScoreResult> UploadAsync(
		string serverBaseUrl,
		string recordingJsonPathOrUrl,
		string imagePath,
		string apiKey,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		var recordingId = await ResolveRecordingIdAsync(recordingJsonPathOrUrl?.Trim(), ct, log)
			.ConfigureAwait(false);
		if (string.IsNullOrWhiteSpace(recordingId))
			throw new InvalidOperationException(
				"Recording JSON has no root \"id\" to resolve the Baseball Logger entry.");

		return await UploadToRecordingAsync(serverBaseUrl, recordingId, imagePath, apiKey, ct, log)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Uploads <paramref name="imagePath"/> to <c>POST /api/recordings/{recordingId}/boxscore</c>.
	/// If a box score already exists (conflict), deletes it and uploads again.
	/// </summary>
	public static async Task<UploadBoxScoreResult> UploadToRecordingAsync(
		string serverBaseUrl,
		string recordingId,
		string imagePath,
		string apiKey,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (string.IsNullOrWhiteSpace(serverBaseUrl))
			throw new ArgumentException("Server base URL is required.", nameof(serverBaseUrl));
		if (string.IsNullOrWhiteSpace(recordingId))
			throw new ArgumentException("Recording id is required.", nameof(recordingId));
		if (string.IsNullOrWhiteSpace(imagePath))
			throw new ArgumentException("Image path is required.", nameof(imagePath));
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new ArgumentException("API key is required for box-score upload.", nameof(apiKey));

		var image = imagePath.Trim();
		if (!File.Exists(image))
			throw new FileNotFoundException("Box score image not found.", image);

		var ext = Path.GetExtension(image).ToLowerInvariant();
		if (Array.IndexOf(AllowedExtensions, ext) < 0)
			throw new ArgumentException(
				"Box score image must be JPEG, PNG, WebP, or GIF.", nameof(imagePath));

		var id = recordingId.Trim();
		var baseUri = NormalizeBaseUri(serverBaseUrl);
		var relativePath = $"api/recordings/{Uri.EscapeDataString(id)}/boxscore";
		log?.Invoke($"Uploading box score to {baseUri}{relativePath} …");

		using var http = new HttpClient { BaseAddress = baseUri };
		var result = await PostBoxScoreAsync(http, relativePath, image, apiKey, ct).ConfigureAwait(false);
		if (result != null)
		{
			log?.Invoke($"Uploaded box score for recording {result.RecordingId ?? id}: {result.FileName}");
			return result;
		}

		// Conflict: one image per recording — remove then replace (matches server docs).
		log?.Invoke("Box score already exists; replacing…");
		await DeleteBoxScoreAsync(http, relativePath, apiKey, ct).ConfigureAwait(false);
		result = await PostBoxScoreAsync(http, relativePath, image, apiKey, ct).ConfigureAwait(false);
		if (result == null)
		{
			throw new InvalidOperationException(
				"Box score upload failed after replace (unexpected conflict).");
		}

		log?.Invoke($"Replaced box score for recording {result.RecordingId ?? id}: {result.FileName}");
		return result;
	}

	/// <summary>
	/// Loads session JSON and returns the root <c>id</c> string used by Baseball Logger Resolve.
	/// </summary>
	public static async Task<string> ResolveRecordingIdAsync(
		string recordingJsonPathOrUrl,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		var json = await ReadSourceAsync(recordingJsonPathOrUrl, ct, log).ConfigureAwait(false);
		using var doc = JsonDocument.Parse(json);
		if (doc.RootElement.ValueKind != JsonValueKind.Object)
			throw new InvalidOperationException("Recording JSON root must be an object.");

		if (!doc.RootElement.TryGetProperty("id", out var idProp)
			|| idProp.ValueKind != JsonValueKind.String)
		{
			return null;
		}

		var id = idProp.GetString()?.Trim();
		return string.IsNullOrWhiteSpace(id) ? null : id;
	}

	private static async Task<UploadBoxScoreResult> PostBoxScoreAsync(
		HttpClient http,
		string relativePath,
		string imagePath,
		string apiKey,
		CancellationToken ct)
	{
		var bytes = await File.ReadAllBytesAsync(imagePath, ct).ConfigureAwait(false);
		var fileName = Path.GetFileName(imagePath);
		var form = new MultipartFormDataContent();
		var fileContent = new ByteArrayContent(bytes);
		fileContent.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeForExtension(imagePath));
		form.Add(fileContent, "file", fileName);

		using var request = new HttpRequestMessage(HttpMethod.Post, relativePath);
		request.Headers.TryAddWithoutValidation("apikey", apiKey.Trim());
		request.Headers.Accept.ParseAdd("application/json");
		request.Content = form;

		using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
		var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

		if (response.StatusCode == HttpStatusCode.Conflict)
			return null;

		if (!response.IsSuccessStatusCode)
		{
			var error = TryReadError(body) ?? body;
			throw new InvalidOperationException(
				$"Box score upload failed {(int)response.StatusCode}: {error}");
		}

		return ParseUploadResult(body);
	}

	private static async Task DeleteBoxScoreAsync(
		HttpClient http,
		string relativePath,
		string apiKey,
		CancellationToken ct)
	{
		using var request = new HttpRequestMessage(HttpMethod.Delete, relativePath);
		request.Headers.TryAddWithoutValidation("apikey", apiKey.Trim());
		request.Headers.Accept.ParseAdd("application/json");

		using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
		if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
			return;

		var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
		var error = TryReadError(body) ?? body;
		throw new InvalidOperationException(
			$"Box score delete failed {(int)response.StatusCode}: {error}");
	}

	private static async Task<string> ReadSourceAsync(
		string sourcePathOrUrl,
		CancellationToken ct,
		Action<string> log)
	{
		if (Uri.TryCreate(sourcePathOrUrl, UriKind.Absolute, out var uri)
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			log?.Invoke($"Fetching recording JSON from URL: {uri}");
			using var client = new HttpClient();
			using var response = await client.GetAsync(uri, ct).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				throw new HttpRequestException(
					$"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
			}

			return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
		}

		if (!File.Exists(sourcePathOrUrl))
			throw new FileNotFoundException("Recording JSON file not found.", sourcePathOrUrl);

		return await File.ReadAllTextAsync(sourcePathOrUrl, ct).ConfigureAwait(false);
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

	private static UploadBoxScoreResult ParseUploadResult(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
			return new UploadBoxScoreResult(null, null, 0, false, null, false, null);

		try
		{
			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			long size = 0;
			if (root.TryGetProperty("sizeBytes", out var sizeProp)
				&& sizeProp.TryGetInt64(out var parsedSize))
			{
				size = parsedSize;
			}

			return new UploadBoxScoreResult(
				GetString(root, "recordingId"),
				GetString(root, "fileName"),
				size,
				root.TryGetProperty("parsed", out var parsed) && parsed.ValueKind == JsonValueKind.True,
				GetString(root, "parseError"),
				root.TryGetProperty("parseSkipped", out var skipped) && skipped.ValueKind == JsonValueKind.True,
				GetString(root, "imageUrl"));
		}
		catch
		{
			return new UploadBoxScoreResult(null, null, 0, false, null, false, null);
		}
	}

	private static string GetString(JsonElement root, string name)
	{
		return root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
			? prop.GetString()
			: null;
	}
}
