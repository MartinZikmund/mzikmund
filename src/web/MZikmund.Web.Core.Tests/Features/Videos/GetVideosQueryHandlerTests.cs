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
	private readonly Mock<ILogger<GetVideosQueryHandler>> _mockLogger;
	private readonly Mock<ICache> _mockCache;
	private readonly YouTubeOptions _youtubeOptions;

	public GetVideosQueryHandlerTests()
	{
		_mockLogger = new Mock<ILogger<GetVideosQueryHandler>>();
		_mockCache = new Mock<ICache>();
		_youtubeOptions = new YouTubeOptions
		{
			FeedUrl = "https://www.youtube.com/feeds/videos.xml?channel_id=UCB6Td35bzTvcJN_HG6TLtwA",
			CacheTtlMinutes = 30
		};
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

		var mockParserLogger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var mockHttpClient = new HttpClient();
		var parser = new YouTubeRssFeedParser(mockParserLogger.Object, mockHttpClient);
		var handler = new GetVideosQueryHandler(
			_mockLogger.Object,
			_mockCache.Object,
			parser,
			Options.Create(_youtubeOptions));

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

		var mockParserLogger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var mockHttpClient = new HttpClient();
		var parser = new YouTubeRssFeedParser(mockParserLogger.Object, mockHttpClient);
		var handler = new GetVideosQueryHandler(
			_mockLogger.Object,
			_mockCache.Object,
			parser,
			Options.Create(_youtubeOptions));

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
	public async Task Handle_WithCacheMiss_ReturnsNullWhenParserFails()
	{
		// Arrange
		_mockCache
			.Setup(x => x.GetAsync<List<VideoDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		// Create a parser with a mock HTTP client that will fail
		var mockParserLogger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var mockHttpClient = new HttpClient();
		// We can't easily mock HttpClient, so we rely on the parser throwing when URL is invalid
		var parser = new YouTubeRssFeedParser(mockParserLogger.Object, mockHttpClient);
		var handler = new GetVideosQueryHandler(
			_mockLogger.Object,
			_mockCache.Object,
			parser,
			Options.Create(new YouTubeOptions { FeedUrl = "invalid-url" })); // Invalid URL will cause parser to fail

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

		var mockParserLogger = new Mock<ILogger<YouTubeRssFeedParser>>();
		var mockHttpClient = new HttpClient();
		var parser = new YouTubeRssFeedParser(mockParserLogger.Object, mockHttpClient);
		var handler = new GetVideosQueryHandler(
			_mockLogger.Object,
			_mockCache.Object,
			parser,
			Options.Create(new YouTubeOptions { FeedUrl = "https://example.com" }));

		// Test that the handler correctly names cache keys
		var queryAll = new GetVideosQuery();
		var queryWithCount = new GetVideosQuery { Count = 5 };

		// The handler will attempt to get from cache (and fail because parser also fails)
		// But we can verify it's using the right cache key format
		try
		{
			await handler.Handle(queryAll, CancellationToken.None);
		}
		catch { }

		try
		{
			await handler.Handle(queryWithCount, CancellationToken.None);
		}
		catch { }

		// Verify cache keys were used correctly
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
			ChannelId = "UCB6Td35bzTvcJN_HG6TLtwA"
		};
	}
}
