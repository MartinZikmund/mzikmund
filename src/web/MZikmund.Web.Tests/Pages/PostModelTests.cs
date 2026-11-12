using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Pages.Blog;

namespace MZikmund.Web.Tests.Pages;

public class PostModelTests
{
	[Fact]
	public async Task OnGet_WhenLoadingCommentsThrows_RendersThePostWithoutComments()
	{
		// Arrange
		var model = CreateModel(new InvalidOperationException("boom"));

		// Act
		await model.OnGet("a-post", null, null);

		// Assert
		Assert.Equal("Title", model.BlogPost.Title);
		Assert.Null(model.CommentsSection);
	}

	[Fact]
	public async Task OnGet_WhenLoadingCommentsIsCancelled_Propagates()
	{
		// Arrange
		var model = CreateModel(new OperationCanceledException());

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => model.OnGet("a-post", null, null));
	}

	private static PostModel CreateModel(Exception commentsFailure)
	{
		Mock<IMediator> mediator = new();
		mediator
			.Setup(x => x.Send(It.IsAny<GetPostByRouteNameQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Post { Id = Guid.NewGuid(), Title = "Title", Content = "body" });
		mediator
			.Setup(x => x.Send(It.IsAny<GetPostCommentsQuery>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(commentsFailure);

		Mock<IPostContentProcessor> processor = new();
		processor.Setup(x => x.ProcessAsync(It.IsAny<string>())).ReturnsAsync("<p>body</p>");

		return new PostModel(
			new Mock<IHostEnvironment>().Object,
			mediator.Object,
			processor.Object,
			Options.Create(new CommentsOptions { Enabled = true }),
			Options.Create(new SupabaseOptions()),
			Options.Create(new TurnstileOptions()),
			NullLogger<PostModel>.Instance);
	}
}
