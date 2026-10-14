using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Services.Comments;

namespace MZikmund.Web.Core.Tests.Services.Comments;

public class TurnstileVerifierTests
{
	private const string SuccessJson = """
		{
			"success": true,
			"challenge_ts": "2026-08-11T15:14:30.096Z",
			"hostname": "mzikmund.dev",
			"error-codes": [],
			"action": "comment-create"
		}
		""";

	private const string WrongActionJson = """
		{
			"success": true,
			"challenge_ts": "2026-08-11T15:14:30.096Z",
			"hostname": "mzikmund.dev",
			"error-codes": [],
			"action": "some-other-form"
		}
		""";

	private const string FailureJson = """
		{
			"success": false,
			"error-codes": ["invalid-input-response"]
		}
		""";

	private readonly TurnstileOptions _options = new()
	{
		SiteKey = "1x00000000000000000000AA",
		SecretKey = "test-secret",
		ExpectedAction = "comment-create"
	};

	private TurnstileVerifier CreateVerifier(FakeTurnstileHandler handler)
	{
		var httpClient = new HttpClient(handler);
		var mockFactory = new Mock<IHttpClientFactory>();
		mockFactory.Setup(f => f.CreateClient(TurnstileVerifier.HttpClientName)).Returns(httpClient);

		return new TurnstileVerifier(
			mockFactory.Object,
			Options.Create(_options),
			new Mock<ILogger<TurnstileVerifier>>().Object);
	}

	[Fact]
	public async Task VerifyAsync_WhenSiteverifyReportsFailure_ReturnsFalse()
	{
		// Arrange
		var handler = new FakeTurnstileHandler(FailureJson);
		var verifier = CreateVerifier(handler);

		// Act
		var result = await verifier.VerifyAsync("dummy-token", "203.0.113.7", CancellationToken.None);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public async Task VerifyAsync_WhenSiteverifySucceedsWithMatchingAction_ReturnsTrue()
	{
		// Arrange
		var handler = new FakeTurnstileHandler(SuccessJson);
		var verifier = CreateVerifier(handler);

		// Act
		var result = await verifier.VerifyAsync("dummy-token", "203.0.113.7", CancellationToken.None);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task VerifyAsync_WhenActionDoesNotMatch_ReturnsFalse()
	{
		// Arrange
		var handler = new FakeTurnstileHandler(WrongActionJson);
		var verifier = CreateVerifier(handler);

		// Act
		var result = await verifier.VerifyAsync("dummy-token", null, CancellationToken.None);

		// Assert
		Assert.False(result);
	}

	[Theory]
	[InlineData("not json at all")]
	[InlineData("{\"success\": ")]
	[InlineData("")]
	public async Task VerifyAsync_WhenTheBodyIsMalformed_ReturnsFalseInsteadOfThrowing(string body)
	{
		// Arrange
		var handler = new FakeTurnstileHandler(body);
		var verifier = CreateVerifier(handler);

		// Act
		var result = await verifier.VerifyAsync("dummy-token", null, CancellationToken.None);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public async Task VerifyAsync_WhenHostnameDoesNotMatchTheExpectedOne_ReturnsFalse()
	{
		// Arrange
		_options.ExpectedHostname = "example.org";
		var handler = new FakeTurnstileHandler(SuccessJson);
		var verifier = CreateVerifier(handler);

		// Act
		var result = await verifier.VerifyAsync("dummy-token", null, CancellationToken.None);

		// Assert
		Assert.False(result);
	}

	[Theory]
	[InlineData("mzikmund.dev")]
	[InlineData("MZikmund.Dev")]
	[InlineData("")]
	public async Task VerifyAsync_WhenHostnameMatchesOrNoneIsConfigured_ReturnsTrue(string expectedHostname)
	{
		// Arrange
		_options.ExpectedHostname = expectedHostname;
		var handler = new FakeTurnstileHandler(SuccessJson);
		var verifier = CreateVerifier(handler);

		// Act
		var result = await verifier.VerifyAsync("dummy-token", null, CancellationToken.None);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task VerifyAsync_WithEmptyToken_ReturnsFalseWithoutCallingSiteverify()
	{
		// Arrange
		var handler = new FakeTurnstileHandler(SuccessJson);
		var verifier = CreateVerifier(handler);

		// Act
		var result = await verifier.VerifyAsync("", null, CancellationToken.None);

		// Assert
		Assert.False(result);
		Assert.Equal(0, handler.SendCount);
	}

	[Fact]
	public async Task VerifyAsync_WhenTransportFails_ReturnsFalse()
	{
		// Arrange
		var handler = new FakeTurnstileHandler(SuccessJson, throwOnSend: true);
		var verifier = CreateVerifier(handler);

		// Act
		var result = await verifier.VerifyAsync("dummy-token", null, CancellationToken.None);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public async Task VerifyAsync_WhenCallerCancels_PropagatesCancellation()
	{
		// Arrange
		var handler = new FakeTurnstileHandler(SuccessJson);
		var verifier = CreateVerifier(handler);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		// Act & Assert
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			verifier.VerifyAsync("dummy-token", null, cts.Token));
	}

	[Fact]
	public async Task VerifyAsync_PostsSecretAndResponseToSiteverifyEndpoint()
	{
		// Arrange
		var handler = new FakeTurnstileHandler(SuccessJson);
		var verifier = CreateVerifier(handler);

		// Act
		await verifier.VerifyAsync("dummy-token", "203.0.113.7", CancellationToken.None);

		// Assert
		Assert.Equal(
			"https://challenges.cloudflare.com/turnstile/v0/siteverify",
			handler.RequestUri!.ToString());
		Assert.Contains("secret=test-secret", handler.RequestBody);
		Assert.Contains("response=dummy-token", handler.RequestBody);
		Assert.Contains("remoteip=203.0.113.7", handler.RequestBody);
	}

	private sealed class FakeTurnstileHandler : HttpMessageHandler
	{
		private readonly string _responseJson;
		private readonly HttpStatusCode _statusCode;
		private readonly bool _throwOnSend;

		public FakeTurnstileHandler(
			string responseJson,
			HttpStatusCode statusCode = HttpStatusCode.OK,
			bool throwOnSend = false)
		{
			_responseJson = responseJson;
			_statusCode = statusCode;
			_throwOnSend = throwOnSend;
		}

		public int SendCount { get; private set; }

		public Uri? RequestUri { get; private set; }

		public string RequestBody { get; private set; } = "";

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			SendCount++;
			RequestUri = request.RequestUri;

			if (request.Content is not null)
			{
				RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
			}

			if (_throwOnSend)
			{
				throw new HttpRequestException("Simulated network failure.");
			}

			return new HttpResponseMessage(_statusCode)
			{
				Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
			};
		}
	}
}
