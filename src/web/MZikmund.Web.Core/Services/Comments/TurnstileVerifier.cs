using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MZikmund.Web.Configuration;

namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Verifies Turnstile tokens against Cloudflare's siteverify endpoint.
/// </summary>
public sealed class TurnstileVerifier : ITurnstileVerifier
{
	/// <summary>
	/// Name of the named HttpClient registered in Program.cs.
	/// </summary>
	public const string HttpClientName = "Turnstile";

	private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

	// Cloudflare caps the token at 2048 characters.
	private const int MaxTokenLength = 2048;

	private readonly IHttpClientFactory _httpClientFactory;
	private readonly TurnstileOptions _options;
	private readonly ILogger<TurnstileVerifier> _logger;

	public TurnstileVerifier(
		IHttpClientFactory httpClientFactory,
		IOptions<TurnstileOptions> options,
		ILogger<TurnstileVerifier> logger)
	{
		_httpClientFactory = httpClientFactory;
		_options = options.Value;
		_logger = logger;
	}

	public async Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(token) || token.Length > MaxTokenLength)
		{
			_logger.LogWarning("Turnstile token missing or oversized.");
			return false;
		}

		var fields = new Dictionary<string, string>
		{
			["secret"] = _options.SecretKey,
			["response"] = token,
			// One key per logical verification. If a retry loop is ever added, every attempt must
			// reuse THIS value or Cloudflare counts the token as replayed.
			["idempotency_key"] = Guid.NewGuid().ToString()
		};

		if (!string.IsNullOrEmpty(remoteIp))
		{
			fields["remoteip"] = remoteIp;
		}

		try
		{
			var httpClient = _httpClientFactory.CreateClient(HttpClientName);
			using var content = new FormUrlEncodedContent(fields);
			using var response = await httpClient.PostAsync(SiteVerifyUrl, content, cancellationToken);
			response.EnsureSuccessStatusCode();

			var result = await response.Content.ReadFromJsonAsync<TurnstileSiteVerifyResponse>(cancellationToken);

			if (result is null)
			{
				_logger.LogWarning("Turnstile siteverify returned an empty body.");
				return false;
			}

			if (!result.Success)
			{
				_logger.LogWarning(
					"Turnstile verification failed: {ErrorCodes}",
					string.Join(", ", result.ErrorCodes ?? []));
				return false;
			}

			// Cloudflare echoes `action` back but does not enforce it, so we must.
			if (!string.IsNullOrEmpty(_options.ExpectedAction) &&
				!string.Equals(result.Action, _options.ExpectedAction, StringComparison.Ordinal))
			{
				_logger.LogWarning(
					"Turnstile action mismatch. Expected {Expected}, got {Actual}.",
					_options.ExpectedAction,
					result.Action);
				return false;
			}

			return true;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
		{
			// Fail closed: a captcha we cannot check is a captcha that did not pass.
			_logger.LogError(ex, "Turnstile siteverify request failed.");
			return false;
		}
	}
}
