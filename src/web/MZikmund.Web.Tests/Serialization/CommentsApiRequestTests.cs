using System.Net;
using MZikmund.Api.Client;
using Refit;

namespace MZikmund.Web.Tests.Serialization;

public class CommentsApiRequestTests
{
	[Fact]
	public async Task CountNewCommentsAsync_SendsSinceAsRoundTripUtcWithMilliseconds()
	{
		// Arrange
		CapturingHandler handler = new();
		var api = RestService.For<IMZikmundApi>(
			new HttpClient(handler) { BaseAddress = new Uri("https://example.test/api") });
		var since = new DateTime(2026, 8, 1, 12, 0, 0, 123, DateTimeKind.Utc);

		// Act
		await api.CountNewCommentsAsync(since);

		// Assert
		Assert.Equal("2026-08-01T12:00:00.1230000Z", handler.QueryValue("since"));
	}

	[Fact]
	public async Task GetModerationFeedAsync_SendsSinceAsRoundTripUtcWithMilliseconds()
	{
		// Arrange
		CapturingHandler handler = new();
		var api = RestService.For<IMZikmundApi>(
			new HttpClient(handler) { BaseAddress = new Uri("https://example.test/api") });
		var since = new DateTime(2026, 8, 1, 12, 0, 0, 123, DateTimeKind.Utc);

		// Act
		await api.GetModerationFeedAsync(since, 2);

		// Assert
		Assert.Equal("2026-08-01T12:00:00.1230000Z", handler.QueryValue("since"));
		Assert.Equal("2", handler.QueryValue("pageNumber"));
	}

	private sealed class CapturingHandler : HttpMessageHandler
	{
		private const string Body = "{\"data\":[],\"pageNumber\":1,\"pageSize\":20,\"totalCount\":0}";

		private Uri? _uri;

		public string? QueryValue(string name) =>
			System.Web.HttpUtility.ParseQueryString(_uri!.Query)[name];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			_uri = request.RequestUri;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(request.RequestUri!.AbsolutePath.EndsWith("/count", StringComparison.Ordinal) ? "0" : Body, System.Text.Encoding.UTF8, "application/json"),
			});
		}
	}
}
