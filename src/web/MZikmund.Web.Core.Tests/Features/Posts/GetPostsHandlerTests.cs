using AutoMapper;
using MediatR;
using Moq;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Tests.Features.Posts;

public class GetPostsHandlerTests
{
	[Fact]
	public async Task Handle_WithSearchTerm_FiltersPostsBySearchTerm()
	{
		// Arrange
		var searchTerm = "test";
		var postRepositoryMock = new Mock<IRepository<PostEntity>>();
		var markdownConverterMock = new Mock<IMarkdownConverter>();
		var mediatorMock = new Mock<IMediator>();
		var mapperMock = new Mock<IMapper>();

		// Setup mediator to return count
		mediatorMock.Setup(m => m.Send(It.IsAny<CountPostsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(1);

		// Setup repository to return posts
		var posts = new List<PostListItem>
		{
			new PostListItem
			{
				Id = Guid.NewGuid(),
				Title = "Test Post",
				Abstract = "Test abstract",
				Content = "Test content"
			}
		};

		postRepositoryMock.Setup(r => r.SelectAsync(
			It.IsAny<GetPostsSpecification>(),
			It.IsAny<Func<PostEntity, PostListItem>>()))
			.ReturnsAsync(posts);

		var handler = new GetPostsHandler(
			postRepositoryMock.Object,
			markdownConverterMock.Object,
			mediatorMock.Object,
			mapperMock.Object);

		var query = new GetPostsQuery(1, 10, SearchTerm: searchTerm);

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(1, result.TotalCount);
		Assert.Single(result.Data);

		// Verify that CountPostsQuery was called with the search term
		mediatorMock.Verify(m => m.Send(
			It.Is<CountPostsQuery>(q => q.SearchTerm == searchTerm),
			It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task Handle_WithoutSearchTerm_ReturnsAllPosts()
	{
		// Arrange
		var postRepositoryMock = new Mock<IRepository<PostEntity>>();
		var markdownConverterMock = new Mock<IMarkdownConverter>();
		var mediatorMock = new Mock<IMediator>();
		var mapperMock = new Mock<IMapper>();

		// Setup mediator to return count
		mediatorMock.Setup(m => m.Send(It.IsAny<CountPostsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(2);

		// Setup repository to return posts
		var posts = new List<PostListItem>
		{
			new PostListItem { Id = Guid.NewGuid(), Title = "Post 1" },
			new PostListItem { Id = Guid.NewGuid(), Title = "Post 2" }
		};

		postRepositoryMock.Setup(r => r.SelectAsync(
			It.IsAny<GetPostsSpecification>(),
			It.IsAny<Func<PostEntity, PostListItem>>()))
			.ReturnsAsync(posts);

		var handler = new GetPostsHandler(
			postRepositoryMock.Object,
			markdownConverterMock.Object,
			mediatorMock.Object,
			mapperMock.Object);

		var query = new GetPostsQuery(1, 10);

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.TotalCount);
		Assert.Equal(2, result.Data.Count());

		// Verify that CountPostsQuery was called without search term
		mediatorMock.Verify(m => m.Send(
			It.Is<CountPostsQuery>(q => q.SearchTerm == null),
			It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
