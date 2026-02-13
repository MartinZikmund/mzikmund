using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MZikmund.DataContracts.Videos;
using MZikmund.Web.Controllers;
using MZikmund.Web.Core.Features.Videos.Queries;

namespace MZikmund.Web.Tests.Controllers;

public class VideosControllerTests
{
	private readonly Mock<IMediator> _mockMediator;
	private readonly Mock<ILogger<VideosController>> _mockLogger;
	private readonly VideosController _controller;

	public VideosControllerTests()
	{
		_mockMediator = new Mock<IMediator>();
		_mockLogger = new Mock<ILogger<VideosController>>();
		_controller = new VideosController(_mockMediator.Object, _mockLogger.Object);
	}

	[Fact]
	public async Task GetVideos_WithAvailableVideos_Returns200OkWithData()
	{
		// Arrange
		var videos = new List<VideoDto>
		{
			CreateTestVideo("video1", "Test Video 1"),
			CreateTestVideo("video2", "Test Video 2")
		};

		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(videos);

		// Act
		var result = await _controller.GetVideos();

		// Assert
		Assert.NotNull(result);
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(200, okResult.StatusCode);

		var returnValue = okResult.Value;
		Assert.NotNull(returnValue);

		// Verify the data property contains our videos
		var dataProperty = returnValue.GetType().GetProperty("data");
		Assert.NotNull(dataProperty);
		var returnedVideos = dataProperty.GetValue(returnValue) as List<VideoDto>;
		Assert.NotNull(returnedVideos);
		Assert.Equal(2, returnedVideos.Count);
	}

	[Fact]
	public async Task GetVideos_WithUnavailableFeed_Returns503ServiceUnavailable()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		// Act
		var result = await _controller.GetVideos();

		// Assert
		Assert.NotNull(result);
		var statusResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(503, statusResult.StatusCode);

		var returnValue = statusResult.Value;
		Assert.NotNull(returnValue);

		// Verify error message
		var errorProperty = returnValue.GetType().GetProperty("error");
		Assert.NotNull(errorProperty);
		var errorMessage = errorProperty.GetValue(returnValue) as string;
		Assert.NotNull(errorMessage);
		Assert.Contains("temporarily unavailable", errorMessage);
	}

	[Fact]
	public async Task GetVideos_Returns200Even_WithEmptyVideoList()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<VideoDto>());

		// Act
		var result = await _controller.GetVideos();

		// Assert
		Assert.NotNull(result);
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(200, okResult.StatusCode);
	}

	[Fact]
	public async Task GetLatestVideos_WithDefaultCount_ReturnsUpTo3Videos()
	{
		// Arrange
		var videos = new List<VideoDto>
		{
			CreateTestVideo("video1", "Latest 1"),
			CreateTestVideo("video2", "Latest 2"),
			CreateTestVideo("video3", "Latest 3")
		};

		_mockMediator
			.Setup(x => x.Send(It.Is<GetVideosQuery>(q => q.Count == 3), It.IsAny<CancellationToken>()))
			.ReturnsAsync(videos);

		// Act
		var result = await _controller.GetLatestVideos();

		// Assert
		Assert.NotNull(result);
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(200, okResult.StatusCode);

		// Verify query was sent with default count of 3
		_mockMediator.Verify(
			x => x.Send(It.Is<GetVideosQuery>(q => q.Count == 3), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetLatestVideos_WithCustomCount_ReturnsSpecifiedCount()
	{
		// Arrange
		var videos = new List<VideoDto>
		{
			CreateTestVideo("video1", "Latest 1"),
			CreateTestVideo("video2", "Latest 2")
		};

		_mockMediator
			.Setup(x => x.Send(It.Is<GetVideosQuery>(q => q.Count == 2), It.IsAny<CancellationToken>()))
			.ReturnsAsync(videos);

		// Act
		var result = await _controller.GetLatestVideos(count: 2);

		// Assert
		Assert.NotNull(result);
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(200, okResult.StatusCode);

		// Verify query was sent with custom count
		_mockMediator.Verify(
			x => x.Send(It.Is<GetVideosQuery>(q => q.Count == 2), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetLatestVideos_WithCountBelowMinimum_ReturnsBadRequest()
	{
		// Act
		var result = await _controller.GetLatestVideos(count: 0);

		// Assert
		Assert.NotNull(result);
		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Equal(400, badRequest.StatusCode);

		// Verify error message
		var errorProperty = badRequest.Value?.GetType().GetProperty("error");
		Assert.NotNull(errorProperty);
		var errorMessage = errorProperty?.GetValue(badRequest.Value) as string;
		Assert.NotNull(errorMessage);
		Assert.Contains("Invalid count", errorMessage);

		// Verify mediator was not called
		_mockMediator.Verify(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task GetLatestVideos_WithCountAboveMaximum_ReturnsBadRequest()
	{
		// Act
		var result = await _controller.GetLatestVideos(count: 11);

		// Assert
		Assert.NotNull(result);
		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Equal(400, badRequest.StatusCode);

		// Verify mediator was not called
		_mockMediator.Verify(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task GetLatestVideos_WithValidCountRange_Accepts1And10()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<VideoDto> { CreateTestVideo("video1", "Test") });

		// Act - Test boundary values
		var result1 = await _controller.GetLatestVideos(count: 1);
		var result10 = await _controller.GetLatestVideos(count: 10);

		// Assert
		Assert.IsType<OkObjectResult>(result1);
		Assert.IsType<OkObjectResult>(result10);

		// Verify both requests were sent to mediator
		_mockMediator.Verify(
			x => x.Send(It.Is<GetVideosQuery>(q => q.Count == 1), It.IsAny<CancellationToken>()),
			Times.Once);

		_mockMediator.Verify(
			x => x.Send(It.Is<GetVideosQuery>(q => q.Count == 10), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetLatestVideos_WithUnavailableFeed_Returns503ServiceUnavailable()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		// Act
		var result = await _controller.GetLatestVideos(count: 3);

		// Assert
		Assert.NotNull(result);
		var statusResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(503, statusResult.StatusCode);

		// Verify error message
		var errorProperty = statusResult.Value?.GetType().GetProperty("error");
		Assert.NotNull(errorProperty);
		var errorMessage = errorProperty?.GetValue(statusResult.Value) as string;
		Assert.NotNull(errorMessage);
		Assert.Contains("temporarily unavailable", errorMessage);
	}

	[Fact]
	public async Task GetLatestVideos_Returns503_WithCustomCountWhenUnavailable()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		// Act
		var result = await _controller.GetLatestVideos(count: 5);

		// Assert
		Assert.NotNull(result);
		var statusResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(503, statusResult.StatusCode);
	}

	[Fact]
	public async Task GetLatestVideos_ResponseIncludesTimestamp()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetVideosQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((List<VideoDto>?)null);

		// Act
		var result = await _controller.GetLatestVideos();

		// Assert
		var statusResult = Assert.IsType<ObjectResult>(result);
		var timestampProperty = statusResult.Value?.GetType().GetProperty("timestamp");
		Assert.NotNull(timestampProperty);
		var timestamp = timestampProperty?.GetValue(statusResult.Value);
		Assert.NotNull(timestamp);
	}

	private static VideoDto CreateTestVideo(string id, string title)
	{
		return new VideoDto
		{
			VideoId = id,
			Title = title,
			Description = $"Description for {title}",
			ThumbnailUrl = "https://i.ytimg.com/vi/test/maxresdefault.jpg",
			PublishedDate = DateTimeOffset.UtcNow.AddDays(-1),
			YouTubeUrl = $"https://www.youtube.com/watch?v={id}",
			ChannelId = "UCB6Td35bzTvcJN_HG6TLtwA"
		};
	}
}
