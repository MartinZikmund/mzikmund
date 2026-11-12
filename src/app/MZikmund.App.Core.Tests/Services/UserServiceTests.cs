using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using MZikmund.Services.Account;
using MZikmund.Services.Preferences;

namespace MZikmund.App.Core.Tests.Services;

public class UserServiceTests
{
	private const string BaseUrl = "http://127.0.0.1:54321";

	private readonly Mock<IAppPreferences> _preferences = new();
	private readonly StubHandler _handler = new();
	private readonly ManualTimeProvider _time = new();

	[Theory]
	[InlineData(HttpStatusCode.TooManyRequests)]
	[InlineData(HttpStatusCode.RequestTimeout)]
	[InlineData(HttpStatusCode.InternalServerError)]
	public async Task GetAccessTokenAsync_KeepsTheSession_WhenRefreshIsRateLimitedOrFailing(HttpStatusCode status)
	{
		// Arrange
		var token = CreateToken(_time.GetUtcNow().AddMinutes(2));
		var service = CreateService(token);
		_handler.Respond(status, "{\"msg\":\"nope\"}");

		// Act
		var result = await service.GetAccessTokenAsync();

		// Assert
		Assert.Equal(token, result);
		Assert.True(service.IsLoggedIn);
		_preferences.VerifySet(p => p.AccessToken = null, Times.Never);
	}

	[Theory]
	[InlineData(HttpStatusCode.BadRequest)]
	[InlineData(HttpStatusCode.Unauthorized)]
	[InlineData(HttpStatusCode.Forbidden)]
	public async Task GetAccessTokenAsync_SignsOut_WhenRefreshIsRejected(HttpStatusCode status)
	{
		// Arrange
		var service = CreateService(CreateToken(_time.GetUtcNow().AddMinutes(2)));
		_handler.Respond(status, "{\"msg\":\"Invalid Refresh Token\"}");

		// Act / Assert
		await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.GetAccessTokenAsync());
		Assert.False(service.IsLoggedIn);
	}

	[Fact]
	public async Task VerifyEmailCodeAsync_DoesNotBlameTheCode_OnRateLimit()
	{
		// Arrange
		var service = CreateService(null);
		_handler.Respond(HttpStatusCode.TooManyRequests, "{\"msg\":\"rate limit\"}");

		// Act
		var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.VerifyEmailCodeAsync("a@b.test", "123456"));

		// Assert
		Assert.DoesNotContain("wrong", exception.Message);
		Assert.Contains("Too many", exception.Message);
	}

	[Fact]
	public async Task VerifyEmailCodeAsync_SaysTheCodeIsWrong_OnRejection()
	{
		// Arrange
		var service = CreateService(null);
		_handler.Respond(HttpStatusCode.Forbidden, "{\"msg\":\"expired\"}");

		// Act
		var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.VerifyEmailCodeAsync("a@b.test", "123456"));

		// Assert
		Assert.Contains("wrong or has expired", exception.Message);
	}

	private UserService CreateService(string? accessToken)
	{
		_preferences.SetupAllProperties();
		_preferences.Object.AccessToken = accessToken;
		_preferences.Object.RefreshToken = accessToken is null ? null : "refresh";

		SupabaseConfig config = new() { Url = BaseUrl, PublishableKey = "sb_publishable_test" };
		return new(_preferences.Object, Options.Create(config), new HttpClient(_handler), _time);
	}

	private static string CreateToken(DateTimeOffset expires)
	{
		static string Encode(object value) =>
			Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

		return $"{Encode(new { alg = "none" })}.{Encode(new { sub = "u1", email = "a@b.test", iss = $"{BaseUrl}/auth/v1", exp = expires.ToUnixTimeSeconds() })}.sig";
	}

	private sealed class ManualTimeProvider : TimeProvider
	{
		private DateTimeOffset _now = DateTimeOffset.UtcNow;

		public override DateTimeOffset GetUtcNow() => _now;

		public void Advance(TimeSpan by) => _now += by;
	}

	private sealed class StubHandler : HttpMessageHandler
	{
		private Func<CancellationToken, Task<HttpResponseMessage>> _respond = _ => throw new InvalidOperationException("No response set up.");

		public int Calls { get; private set; }

		public void Respond(HttpStatusCode status, string body) =>
			_respond = _ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

		public void Throw(Exception exception) => _respond = _ => throw exception;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Calls++;
			return _respond(cancellationToken);
		}
	}
}
