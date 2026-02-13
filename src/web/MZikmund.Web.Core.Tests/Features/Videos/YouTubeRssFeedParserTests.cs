using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using MZikmund.Web.Core.Features.Videos.RssParsing;

namespace MZikmund.Web.Core.Tests.Features.Videos;

public class YouTubeRssFeedParserTests
{
	private const string SampleRssFeed = """
		<?xml version="1.0" encoding="UTF-8"?>
		<feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns:media="http://search.yahoo.com/mrss/" xmlns="http://www.w3.org/2005/Atom">
			<title>Channel Title</title>
			<entry>
				<id>yt:video:abc123</id>
				<yt:videoId>abc123</yt:videoId>
				<yt:channelId>UC123456</yt:channelId>
				<title>First Video Title</title>
				<link rel="alternate" href="https://www.youtube.com/watch?v=abc123"/>
				<author>
					<name>TestChannel</name>
				</author>
				<published>2024-06-15T10:00:00+00:00</published>
				<media:description>Description of first video</media:description>
				<media:thumbnail url="https://i.ytimg.com/vi/abc123/maxresdefault.jpg" width="480" height="360"/>
			</entry>
			<entry>
				<id>yt:video:def456</id>
				<yt:videoId>def456</yt:videoId>
				<yt:channelId>UC123456</yt:channelId>
				<title>Second Video Title</title>
				<link rel="alternate" href="https://www.youtube.com/watch?v=def456"/>
				<author>
					<name>TestChannel</name>
				</author>
				<published>2024-06-14T10:00:00+00:00</published>
				<media:description>Description of second video</media:description>
				<media:thumbnail url="https://i.ytimg.com/vi/def456/maxresdefault.jpg" width="480" height="360"/>
			</entry>
		</feed>
		""";

	private const string EmptyRssFeed = """
		<?xml version="1.0" encoding="UTF-8"?>
		<feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns:media="http://search.yahoo.com/mrss/" xmlns="http://www.w3.org/2005/Atom">
			<title>Empty Channel</title>
		</feed>
		""";

	private const string MalformedEntryFeed = """
		<?xml version="1.0" encoding="UTF-8"?>
		<feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns:media="http://search.yahoo.com/mrss/" xmlns="http://www.w3.org/2005/Atom">
			<title>Channel Title</title>
			<entry>
				<id>yt:video:nolink</id>
				<title>Entry Without Link</title>
			</entry>
			<entry>
				<id>yt:video:good789</id>
				<yt:videoId>good789</yt:videoId>
				<title>Good Video</title>
				<link rel="alternate" href="https://www.youtube.com/watch?v=good789"/>
				<author>
					<name>TestChannel</name>
				</author>
				<published>2024-06-10T10:00:00+00:00</published>
				<media:description>Good description</media:description>
				<media:thumbnail url="https://i.ytimg.com/vi/good789/maxresdefault.jpg" width="480" height="360"/>
			</entry>
		</feed>
		""";

	private static IHttpClientFactory CreateMockFactory(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
	{
		var handler = new FakeHttpMessageHandler(responseContent, statusCode);
		var httpClient = new HttpClient(handler);
		var mockFactory = new Mock<IHttpClientFactory>();
		mockFactory.Setup(f => f.CreateClient("YouTube")).Returns(httpClient);
		return mockFactory.Object;
	}

	[Fact]
	public async Task ParseAsync_WithValidFeed_ReturnsCorrectVideos()
	{
		// Arrange
		var factory = CreateMockFactory(SampleRssFeed);
		var logger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var parser = new YouTubeRssFeedParser(logger.Object, factory);

		// Act
		var result = await parser.ParseAsync("https://www.youtube.com/feeds/videos.xml?channel_id=UC123");

		// Assert
		Assert.Equal(2, result.Count);
		Assert.Equal("abc123", result[0].VideoId);
		Assert.Equal("First Video Title", result[0].Title);
		Assert.Equal("Description of first video", result[0].Description);
		Assert.Equal("https://i.ytimg.com/vi/abc123/maxresdefault.jpg", result[0].ThumbnailUrl);
		Assert.Equal("https://www.youtube.com/watch?v=abc123", result[0].YouTubeUrl);

		Assert.Equal("def456", result[1].VideoId);
		Assert.Equal("Second Video Title", result[1].Title);
	}

	[Fact]
	public async Task ParseAsync_WithEmptyFeed_ReturnsEmptyList()
	{
		// Arrange
		var factory = CreateMockFactory(EmptyRssFeed);
		var logger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var parser = new YouTubeRssFeedParser(logger.Object, factory);

		// Act
		var result = await parser.ParseAsync("https://www.youtube.com/feeds/videos.xml?channel_id=UC123");

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public async Task ParseAsync_WithMalformedEntries_SkipsInvalidAndReturnsValid()
	{
		// Arrange
		var factory = CreateMockFactory(MalformedEntryFeed);
		var logger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var parser = new YouTubeRssFeedParser(logger.Object, factory);

		// Act
		var result = await parser.ParseAsync("https://www.youtube.com/feeds/videos.xml?channel_id=UC123");

		// Assert
		Assert.Single(result);
		Assert.Equal("good789", result[0].VideoId);
		Assert.Equal("Good Video", result[0].Title);
	}

	[Fact]
	public async Task ParseAsync_WithHttpError_ThrowsHttpRequestException()
	{
		// Arrange
		var factory = CreateMockFactory("Not Found", HttpStatusCode.NotFound);
		var logger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var parser = new YouTubeRssFeedParser(logger.Object, factory);

		// Act & Assert
		await Assert.ThrowsAsync<HttpRequestException>(
			() => parser.ParseAsync("https://www.youtube.com/feeds/videos.xml?channel_id=UC123"));
	}

	[Fact]
	public async Task ParseAsync_ResultsAreSortedByDateDescending()
	{
		// Arrange
		var factory = CreateMockFactory(SampleRssFeed);
		var logger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var parser = new YouTubeRssFeedParser(logger.Object, factory);

		// Act
		var result = await parser.ParseAsync("https://www.youtube.com/feeds/videos.xml?channel_id=UC123");

		// Assert
		Assert.True(result[0].PublishedDate >= result[1].PublishedDate);
	}

	private sealed class FakeHttpMessageHandler : HttpMessageHandler
	{
		private readonly string _responseContent;
		private readonly HttpStatusCode _statusCode;

		public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
		{
			_responseContent = responseContent;
			_statusCode = statusCode;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var response = new HttpResponseMessage(_statusCode)
			{
				Content = new StringContent(_responseContent, Encoding.UTF8, "application/xml")
			};

			if (!response.IsSuccessStatusCode)
			{
				response.EnsureSuccessStatusCode(); // This will throw
			}

			return Task.FromResult(response);
		}
	}
}
