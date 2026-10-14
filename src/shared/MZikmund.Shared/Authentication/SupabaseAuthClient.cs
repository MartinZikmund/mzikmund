using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MZikmund.Shared.Authentication;

/// <summary>
/// The handful of GoTrue calls the app needs. Transport failures surface as the original
/// <see cref="HttpRequestException"/>; refusals and server errors as <see cref="SupabaseAuthException"/>.
/// </summary>
public sealed class SupabaseAuthClient
{
	private readonly HttpClient _httpClient;
	private readonly string _baseUrl;
	private readonly string _publishableKey;

	public SupabaseAuthClient(HttpClient httpClient, string baseUrl, string publishableKey)
	{
		_httpClient = httpClient;
		_baseUrl = baseUrl;
		_publishableKey = publishableKey;
	}

	public Task<SupabaseSession> ExchangeCodeAsync(string authCode, string codeVerifier, CancellationToken cancellationToken = default) =>
		RequestSessionAsync(
			"token?grant_type=pkce",
			new PkceExchangeRequest(authCode, codeVerifier),
			SupabaseAuthJsonContext.Default.PkceExchangeRequest,
			cancellationToken);

	public Task<SupabaseSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
		RequestSessionAsync(
			"token?grant_type=refresh_token",
			new RefreshRequest(refreshToken),
			SupabaseAuthJsonContext.Default.RefreshRequest,
			cancellationToken);

	public async Task SendEmailCodeAsync(string email, CancellationToken cancellationToken = default)
	{
		using var response = await SendAsync(
			"otp",
			JsonContent.Create(new SendOtpRequest(email, true), SupabaseAuthJsonContext.Default.SendOtpRequest),
			null,
			cancellationToken);
	}

	public Task<SupabaseSession> VerifyEmailCodeAsync(string email, string code, CancellationToken cancellationToken = default) =>
		RequestSessionAsync(
			"verify",
			new VerifyOtpRequest("email", email, code),
			SupabaseAuthJsonContext.Default.VerifyOtpRequest,
			cancellationToken);

	public async Task SignOutAsync(string accessToken, CancellationToken cancellationToken = default)
	{
		using var response = await SendAsync("logout?scope=local", null, accessToken, cancellationToken);
	}

	private async Task<SupabaseSession> RequestSessionAsync<TRequest>(
		string relative,
		TRequest body,
		JsonTypeInfo<TRequest> typeInfo,
		CancellationToken cancellationToken)
	{
		using var response = await SendAsync(relative, JsonContent.Create(body, typeInfo), null, cancellationToken);
		var content = await response.Content.ReadAsStringAsync(cancellationToken);

		SessionResponse? session;
		try
		{
			session = JsonSerializer.Deserialize(content, SupabaseAuthJsonContext.Default.SessionResponse);
		}
		catch (JsonException ex)
		{
			throw new SupabaseAuthException($"Sign-in server returned an unreadable response: {ex.Message}", response.StatusCode);
		}

		if (session is not { AccessToken.Length: > 0, RefreshToken.Length: > 0 })
		{
			throw new SupabaseAuthException("Sign-in server returned no session.", response.StatusCode);
		}

		return new(session.AccessToken, session.RefreshToken, session.ExpiresIn);
	}

	private async Task<HttpResponseMessage> SendAsync(
		string relative,
		HttpContent? content,
		string? accessToken,
		CancellationToken cancellationToken)
	{
		using HttpRequestMessage request = new(HttpMethod.Post, SupabaseAuthUris.Endpoint(_baseUrl, relative))
		{
			Content = content,
		};
		request.Headers.Add("apikey", _publishableKey);
		if (accessToken is not null)
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		}

		var response = await _httpClient.SendAsync(request, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			try
			{
				throw new SupabaseAuthException(
					await ReadErrorAsync(response, cancellationToken),
					response.StatusCode);
			}
			finally
			{
				response.Dispose();
			}
		}

		return response;
	}

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		var content = await response.Content.ReadAsStringAsync(cancellationToken);
		var prefix = $"Sign-in server responded {(int)response.StatusCode}";

		try
		{
			var error = JsonSerializer.Deserialize(content, SupabaseAuthJsonContext.Default.AuthErrorResponse);
			var message = error?.Msg ?? error?.Message ?? error?.ErrorDescription ?? error?.Error;
			if (message is not null)
			{
				return $"{prefix}: {message}";
			}
		}
		catch (JsonException)
		{
		}

		return prefix + ".";
	}
}
