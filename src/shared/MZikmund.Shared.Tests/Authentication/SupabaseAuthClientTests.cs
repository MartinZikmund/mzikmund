using System.Net;
using System.Text;
using MZikmund.Shared.Authentication;

namespace MZikmund.Shared.Tests.Authentication;

public class SupabaseAuthClientTests
{
	private const string BaseUrl = "http://127.0.0.1:54321";
	private const string Key = "sb_publishable_test";
	private const string SessionJson = "{\"access_token\":\"acc\",\"token_type\":\"bearer\",\"expires_in\":3600,\"refresh_token\":\"ref\",\"user\":{}}";

	[Fact]
	public async Task ExchangeCodeAsync_PostsThePkceBody()
	{
		// Arrange
		StubHandler handler = new(HttpStatusCode.OK, SessionJson);
		var client = CreateClient(handler);

		// Act
		var session = await client.ExchangeCodeAsync("code1", "verifier1");

		// Assert
		Assert.Equal(HttpMethod.Post, handler.Method);
		Assert.Equal($"{BaseUrl}/auth/v1/token?grant_type=pkce", handler.RequestUri);
		Assert.Equal(Key, handler.ApiKey);
		Assert.Equal("{\"auth_code\":\"code1\",\"code_verifier\":\"verifier1\"}", handler.Body);
		Assert.Equal(new SupabaseSession("acc", "ref", 3600), session);
	}

	[Fact]
	public async Task RefreshAsync_PostsTheRefreshToken()
	{
		// Arrange
		StubHandler handler = new(HttpStatusCode.OK, SessionJson);
		var client = CreateClient(handler);

		// Act
		await client.RefreshAsync("old-refresh");

		// Assert
		Assert.Equal($"{BaseUrl}/auth/v1/token?grant_type=refresh_token", handler.RequestUri);
		Assert.Equal(Key, handler.ApiKey);
		Assert.Equal("{\"refresh_token\":\"old-refresh\"}", handler.Body);
	}

	[Fact]
	public async Task SendEmailCodeAsync_PostsTheEmailAndAllowsSignUp()
	{
		// Arrange
		StubHandler handler = new(HttpStatusCode.OK, "{}");
		var client = CreateClient(handler);

		// Act
		await client.SendEmailCodeAsync("reader@mzikmund.test");

		// Assert
		Assert.Equal($"{BaseUrl}/auth/v1/otp", handler.RequestUri);
		Assert.Equal(Key, handler.ApiKey);
		Assert.Equal("{\"email\":\"reader@mzikmund.test\",\"create_user\":true}", handler.Body);
	}

	[Fact]
	public async Task VerifyEmailCodeAsync_PostsTheEmailOtp()
	{
		// Arrange
		StubHandler handler = new(HttpStatusCode.OK, SessionJson);
		var client = CreateClient(handler);

		// Act
		var session = await client.VerifyEmailCodeAsync("reader@mzikmund.test", "123456");

		// Assert
		Assert.Equal($"{BaseUrl}/auth/v1/verify", handler.RequestUri);
		Assert.Equal("{\"type\":\"email\",\"email\":\"reader@mzikmund.test\",\"token\":\"123456\"}", handler.Body);
		Assert.Equal("acc", session.AccessToken);
	}

	[Fact]
	public async Task SignOutAsync_SendsTheBearerTokenWithLocalScope()
	{
		// Arrange
		StubHandler handler = new(HttpStatusCode.NoContent, "");
		var client = CreateClient(handler);

		// Act
		await client.SignOutAsync("my-access");

		// Assert
		Assert.Equal($"{BaseUrl}/auth/v1/logout?scope=local", handler.RequestUri);
		Assert.Equal(Key, handler.ApiKey);
		Assert.Equal("Bearer my-access", handler.Authorization);
	}

	[Fact]
	public async Task RefreshAsync_FoldsTheErrorBodyIntoARejection_On4xx()
	{
		// Arrange
		StubHandler handler = new(HttpStatusCode.BadRequest, "{\"code\":400,\"error_code\":\"validation_failed\",\"msg\":\"Refresh token is not valid\"}");
		var client = CreateClient(handler);

		// Act
		var exception = await Assert.ThrowsAsync<SupabaseAuthException>(() => client.RefreshAsync("bad"));

		// Assert
		Assert.True(exception.IsRejected);
		Assert.Contains("Refresh token is not valid", exception.Message);
	}

	[Fact]
	public async Task ExchangeCodeAsync_ReadsTheOAuthStyleErrorBody()
	{
		// Arrange
		StubHandler handler = new(HttpStatusCode.BadRequest, "{\"error\":\"invalid_grant\",\"error_description\":\"Code expired\"}");
		var client = CreateClient(handler);

		// Act
		var exception = await Assert.ThrowsAsync<SupabaseAuthException>(() => client.ExchangeCodeAsync("c", "v"));

		// Assert
		Assert.True(exception.IsRejected);
		Assert.Contains("Code expired", exception.Message);
	}

	[Theory]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.BadGateway)]
	public async Task RefreshAsync_IsNotARejection_On5xx(HttpStatusCode status)
	{
		// Arrange
		StubHandler handler = new(status, "upstream exploded");
		var client = CreateClient(handler);

		// Act
		var exception = await Assert.ThrowsAsync<SupabaseAuthException>(() => client.RefreshAsync("r"));

		// Assert
		Assert.False(exception.IsRejected);
	}

	[Fact]
	public async Task SendEmailCodeAsync_ExposesTheStatusCode_OnRateLimit()
	{
		// Arrange
		StubHandler handler = new(HttpStatusCode.TooManyRequests, "{\"msg\":\"rate limit\"}");
		var client = CreateClient(handler);

		// Act
		var exception = await Assert.ThrowsAsync<SupabaseAuthException>(() => client.SendEmailCodeAsync("a@b.c"));

		// Assert
		Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
	}

	[Fact]
	public async Task RefreshAsync_LetsTransportFailuresThrough()
	{
		// Arrange
		StubHandler handler = new(new HttpRequestException("offline"));
		var client = CreateClient(handler);

		// Act / Assert
		await Assert.ThrowsAsync<HttpRequestException>(() => client.RefreshAsync("r"));
	}

	private static SupabaseAuthClient CreateClient(StubHandler handler) => new(new HttpClient(handler), BaseUrl + "/", Key);

	private sealed class StubHandler : HttpMessageHandler
	{
		private readonly HttpStatusCode _status;
		private readonly string _responseBody;
		private readonly Exception? _failure;

		public StubHandler(HttpStatusCode status, string responseBody)
		{
			_status = status;
			_responseBody = responseBody;
		}

		public StubHandler(Exception failure)
		{
			_failure = failure;
			_responseBody = "";
		}

		public HttpMethod? Method { get; private set; }

		public string? RequestUri { get; private set; }

		public string? ApiKey { get; private set; }

		public string? Authorization { get; private set; }

		public string? Body { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (_failure is not null)
			{
				throw _failure;
			}

			Method = request.Method;
			RequestUri = request.RequestUri?.AbsoluteUri;
			ApiKey = request.Headers.TryGetValues("apikey", out var values) ? values.Single() : null;
			Authorization = request.Headers.Authorization?.ToString();
			Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

			return new HttpResponseMessage(_status)
			{
				Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
			};
		}
	}
}
