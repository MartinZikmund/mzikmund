using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MZikmund.DataContracts.Videos;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Features.Videos.Queries;
using MZikmund.Web.Core.Features.Videos.RssParsing;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Core.Tests.Features.Videos;

public class GetVideosQueryHandlerTests
{
	private const string SampleRssFeed = """
		<?xml version="1.0" encoding="UTF-8"?>
		<feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns:media="http://search.yahoo.com/mrss/" xmlns="http://www.w3.org/2005/Atom">
			<title>Channel Title</title>
			<entry>
				<id>yt:video:abc123</id>
				<yt:videoId>abc123</yt:videoId>
				<title>Video 1</title>
				<link rel="alternate" href="https://www.youtube.com/watch?v=abc123"/>
				<author><name>TestChannel</name></author>
				<published>2024-06-15T10:00:00+00:00</published>
				<media:description>Description 1</media:description>
				<media:thumbnail url="https://i.ytimg.com/vi/abc123/maxresdefault.jpg" width="480" height="360"/>
			</entry>
			<entry>
				<id>yt:video:def456</id>
				<yt:videoId>def456</yt:videoId>
				<title>Video 2</title>
				<link rel="alternate" href="https://www.youtube.com/watch?v=def456"/>
				<author><name>TestChannel</name></author>
				<published>2024-06-14T10:00:00+00:00</published>
				<media:description>Description 2</media:description>
				<media:thumbnail url="https://i.ytimg.com/vi/def456/maxresdefault.jpg" width="480" height="360"/>
			</entry>
			<entry>
				<id>yt:video:ghi789</id>
				<yt:videoId>ghi789</yt:videoId>
				<title>Video 3</title>
				<link rel="alternate" href="https://www.youtube.com/watch?v=ghi789"/>
				<author><name>TestChannel</name></author>
				<published>2024-06-13T10:00:00+00:00</published>
				<media:description>Description 3</media:description>
				<media:thumbnail url="https://i.ytimg.com/vi/ghi789/maxresdefault.jpg" width="480" height="360"/>
			</entry>
		</feed>
		""";

	private readonly Mock<ILogger<GetVideosQueryHandler>> _mockLogger;
	private readonly Mock<ICache> _mockCache;
	private readonly YouTubeOptions _youtubeOptions;

	public GetVideosQueryHandlerTests()
	{
		_mockLogger = new Mock<ILogger<GetVideosQueryHandler>>();
		_mockCache = new Mock<ICache>();
		_youtubeOptions = new YouTubeOptions
		{
			FeedUrl = "https://www.youtube.com/feeds/videos.xml?channel_id=UC123",
			CacheTtlMinutes = 30
		};
	}

	private static IHttpClientFactory CreateMockFactory(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
	{
		var handler = new FakeHttpMessageHandler(responseContent, statusCode);
		var httpClient = new HttpClient(handler);
		var mockFactory = new Mock<IHttpClientFactory>();
		mockFactory.Setup(f => f.CreateClient("YouTube")).Returns(httpClient);
		return mockFactory.Object;
	}

	private GetVideosQueryHandler CreateHandler(IHttpClientFactory? factory = null)
	{
		factory ??= CreateMockFactory(SampleRssFeed);
		var parserLogger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var parser = new YouTubeRssFeedParser(parserLogger.Object, factory);
		return new GetVideosQueryHandler(
			_mockLogger.Object,
			_mockCache.Object,
			parser,
			Options.Create(_youtubeOptions));
	}

	[Fact]
	public async Task Handle_WithCacheHit_ReturnsCachedVideosWithoutCallingParser()
	{
		// Arrange
		var cachedVideos = new List<VideoDto>
		{
			CreateTestVideo("video1", "Cached Video 1"),
			CreateTestVideo("video2", "Cached Video 2")
		};

		_mockCache
			.Setup(x => x.GetAsync<List<VideoDto>>("cache:videos:all", It.IsAny<CancellationToken>()))
			.ReturnsAsync(cachedVideos);

		var handler = CreateHandler();
		var query = new GetVideosQuery();

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		Assert.Equal("Cached Video 1", result[0].Title);

		// Verify cache was checked
		_mockCache.Verify(
			x => x.GetAsync<List<VideoDto>>("cache:videos:all", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task Handle_WithCountParameter_UsesCacheKeyWithCount()
	{
		// Arrange
		var cachedVideos = new List<VideoDto>
		{
			CreateTestVideo("video1", "Video 1"),
			CreateTestVideo("video2", "Video 2")
		};

		_mockCache
			.Setup(x => x.GetAsync<List<VideoDto>>("cache:videos:latest:2", It.IsAny<CancellationToken>()))
			.ReturnsAsync(cachedVideos);

		var handler = CreateHandler();
		var query = new GetVideosQuery { Count = 2 };

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);

		// Verify correct cache key was used
		_mockCache.Verify(
			x => x.GetAsync<List<VideoDto>>("cache:videos:latest:2", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task Handle_WithCacheMiss_FetchesFromParserAndCaches()
	{
		// Arrange
		_mockCache
			.Setup(x => x.GetAsync<List<VideoDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		var handler = CreateHandler();
		var query = new GetVideosQuery();

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(3, result.Count);

		// Verify result was cached
		_mockCache.Verify(
			x => x.SetAsync("cache:videos:all", It.IsAny<List<VideoDto>>(), TimeSpan.FromMinutes(30), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task Handle_WithCacheMissAndCount_ReturnsLimitedResults()
	{
		// Arrange
		_mockCache
			.Setup(x => x.GetAsync<List<VideoDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		var handler = CreateHandler();
		var query = new GetVideosQuery { Count = 2 };

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
	}

	[Fact]
	public async Task Handle_WhenParserFails_ReturnsNull()
	{
		// Arrange
		_mockCache
			.Setup(x => x.GetAsync<List<VideoDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		var factory = CreateMockFactory("Server Error", HttpStatusCode.InternalServerError);
		var handler = CreateHandler(factory);
		var query = new GetVideosQuery();

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task Handle_VerifiesCacheStructure_WithCorrectCacheKey()
	{
		// Arrange
		_mockCache
			.Setup(x => x.GetAsync<List<VideoDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		var handler = CreateHandler();

		// Act
		await handler.Handle(new GetVideosQuery(), CancellationToken.None);
		await handler.Handle(new GetVideosQuery { Count = 5 }, CancellationToken.None);

		// Assert - verify cache keys were used correctly
		_mockCache.Verify(
			x => x.GetAsync<List<VideoDto>>("cache:videos:all", It.IsAny<CancellationToken>()),
			Times.Once);

		_mockCache.Verify(
			x => x.GetAsync<List<VideoDto>>("cache:videos:latest:5", It.IsAny<CancellationToken>()),
			Times.Once);
	}

	private static VideoDto CreateTestVideo(string id, string title)
	{
		return new VideoDto
		{
			VideoId = id,
			Title = title,
			Description = $"Description for {title}",
			ThumbnailUrl = "https://i.ytimg.com/vi/test/maxresdefault.jpg",
			PublishedDate = DateTimeOffset.UtcNow,
			YouTubeUrl = $"https://www.youtube.com/watch?v={id}",
			ChannelId = "UC123"
		};
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
				response.EnsureSuccessStatusCode();
			}

			return Task.FromResult(response);
		}
	}
}
