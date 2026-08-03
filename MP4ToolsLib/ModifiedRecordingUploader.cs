using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MP4ToolsLib;

public sealed record UploadRevisionResult(
	string UpdatedRecordingId,
	string SourceRecordingId,
	string SourceViewUrl,
	bool Created);

/// <summary>
/// Uploads a modified Baseball Logger session JSON via <c>POST /api/revisions</c>
/// using the share token from a public share URL (see BaseballLoggerServer docs).
/// </summary>
public static class ModifiedRecordingUploader
{
	/// <summary>
	/// Parses a share URL or bare token. When <paramref name="shareUrlOrToken"/> is an absolute
	/// http(s) share URL, returns its origin as <paramref name="serverBaseUrl"/>.
	/// </summary>
	public static bool TryExtractShareToken(
		string shareUrlOrToken,
		out string token,
		out string serverBaseUrl)
	{
		token = null;
		serverBaseUrl = null;
		var value = shareUrlOrToken?.Trim();
		if (string.IsNullOrWhiteSpace(value))
			return false;

		if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
			// Prefer share/{token}; otherwise last segment (matches server doc helper).
			if (segments.Length >= 2
				&& segments[^2].Equals("share", StringComparison.OrdinalIgnoreCase))
			{
				token = segments[^1];
			}
			else if (segments.Length >= 1)
			{
				token = segments[^1];
			}
			else
			{
				return false;
			}

			if (string.IsNullOrWhiteSpace(token))
				return false;

			serverBaseUrl = $"{uri.Scheme}://{uri.Authority}";
			return true;
		}

		// Bare token only — reject local paths / file URLs.
		if (uri != null && uri.IsFile)
			return false;
		if (value.Contains('/') || value.Contains('\\') || File.Exists(value))
			return false;

		token = value;
		return true;
	}

	/// <summary>
	/// Uploads modified session JSON. Auth is the share token (<c>X-Share-Token</c>).
	/// Optional <paramref name="apiKey"/> is sent as <c>apikey</c> when non-empty (not required by the server for this route).
	/// </summary>
	public static async Task<UploadRevisionResult> UploadModifiedAsync(
		string serverBaseUrl,
		string shareUrlOrToken,
		string modifiedSessionJson,
		string apiKey = null,
		CancellationToken ct = default,
		Action<string> log = null)
	{
		if (string.IsNullOrWhiteSpace(serverBaseUrl))
			throw new ArgumentException("Server base URL is required.", nameof(serverBaseUrl));
		if (string.IsNullOrWhiteSpace(modifiedSessionJson))
			throw new ArgumentException("Modified session JSON is required.", nameof(modifiedSessionJson));
		if (!TryExtractShareToken(shareUrlOrToken, out var token, out _) || string.IsNullOrWhiteSpace(token))
			throw new ArgumentException("Could not parse share token from URL or token value.", nameof(shareUrlOrToken));

		var baseUri = NormalizeBaseUri(serverBaseUrl);
		log?.Invoke($"Uploading modified recording JSON to {baseUri}api/revisions …");

		using var http = new HttpClient { BaseAddress = baseUri };
		using var request = new HttpRequestMessage(HttpMethod.Post, "api/revisions");
		request.Headers.TryAddWithoutValidation("X-Share-Token", token);
		request.Headers.Accept.ParseAdd("application/json");
		if (!string.IsNullOrWhiteSpace(apiKey))
			request.Headers.TryAddWithoutValidation("apikey", apiKey.Trim());

		request.Content = new StringContent(modifiedSessionJson, Encoding.UTF8, "application/json");

		using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
		var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			var error = TryReadError(body) ?? body;
			throw new InvalidOperationException(
				$"Upload failed {(int)response.StatusCode}: {error}");
		}

		var result = ParseUploadResult(body);
		log?.Invoke(
			result.Created
				? $"Uploaded modified recording (created): {result.SourceViewUrl ?? result.UpdatedRecordingId}"
				: $"Uploaded modified recording (replaced): {result.SourceViewUrl ?? result.UpdatedRecordingId}");
		return result;
	}

	/// <summary>
	/// Returns true when <paramref name="sourcePathOrUrl"/> is a <c>/share/{token}</c> URL
	/// on the same host as <paramref name="configuredServerBaseUrl"/>.
	/// Local paths, other hosts, and bare tokens return false.
	/// </summary>
	public static bool CanUploadFromSource(string sourcePathOrUrl, string configuredServerBaseUrl)
	{
		var value = sourcePathOrUrl?.Trim();
		if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(configuredServerBaseUrl))
			return false;

		if (!TryParseConfiguredServer(configuredServerBaseUrl, out var configuredBase))
			return false;

		if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
			|| (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
		{
			return false;
		}

		if (!IsSameServer(uri, configuredBase))
			return false;

		var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		return segments.Length >= 2
			&& segments[^2].Equals("share", StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(segments[^1]);
	}

	/// <summary>
	/// Always uses the configured Baseball Logger server base URL for uploads.
	/// </summary>
	public static string ResolveServerBaseUrl(string configuredServerBaseUrl)
	{
		if (!TryParseConfiguredServer(configuredServerBaseUrl, out var configuredBase))
			return configuredServerBaseUrl?.Trim();

		return configuredBase.GetLeftPart(UriPartial.Authority);
	}

	private static bool TryParseConfiguredServer(string configuredServerBaseUrl, out Uri configuredBase)
	{
		configuredBase = null;
		var trimmed = configuredServerBaseUrl?.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
			return false;

		if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
			|| (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
		{
			return false;
		}

		configuredBase = uri;
		return true;
	}

	private static bool IsSameServer(Uri sourceUri, Uri configuredServer)
	{
		if (sourceUri is null || configuredServer is null)
			return false;

		return string.Equals(sourceUri.Scheme, configuredServer.Scheme, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(sourceUri.Host, configuredServer.Host, StringComparison.OrdinalIgnoreCase)
			&& sourceUri.Port == configuredServer.Port;
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

	private static UploadRevisionResult ParseUploadResult(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
			return new UploadRevisionResult(null, null, null, false);

		try
		{
			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			return new UploadRevisionResult(
				GetString(root, "id"),
				GetString(root, "sourceRecordingId"),
				GetString(root, "sourceViewUrl"),
				root.TryGetProperty("created", out var created) && created.ValueKind == JsonValueKind.True);
		}
		catch
		{
			return new UploadRevisionResult(null, null, null, false);
		}
	}

	private static string GetString(JsonElement root, string name)
	{
		return root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
			? prop.GetString()
			: null;
	}
}
