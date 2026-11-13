using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Tests.Features.Posts;

public class GetPostsHandlerTests
{
	[Fact]
	public async Task Handle_ReportsTheReadingTimeWithoutReturningTheContent()
	{
		PostEntity entity = new()
		{
			Id = Guid.NewGuid(),
			Title = "Post",
			LastModifiedDate = DateTimeOffset.UtcNow,
			Content = string.Join(' ', Enumerable.Repeat("word", 600)),
		};

		Mock<IRepository<PostEntity>> repository = new();
		repository
			.Setup(r => r.SelectAsync(It.IsAny<ISpecification<PostEntity>>(), It.IsAny<Expression<Func<PostEntity, PostListItem>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ISpecification<PostEntity> _, Expression<Func<PostEntity, PostListItem>> selector, CancellationToken _) =>
				new[] { selector.Compile()(entity) });

		Mock<IMediator> mediator = new();
		mediator.Setup(m => m.Send(It.IsAny<CountPostsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

		Mock<IMapper> mapper = new();
		mapper.Setup(m => m.Map<Tag[]>(It.IsAny<object>())).Returns(Array.Empty<Tag>());
		mapper.Setup(m => m.Map<Category[]>(It.IsAny<object>())).Returns(Array.Empty<Category>());

		ReadingTimeProvider readingTime = new(new Cache(new MemoryCache(new MemoryCacheOptions())));
		GetPostsHandler handler = new(repository.Object, Mock.Of<IMarkdownConverter>(), mediator.Object, mapper.Object, readingTime);

		var result = await handler.Handle(new GetPostsQuery(1, 10), CancellationToken.None);

		var item = Assert.Single(result.Data);
		Assert.Equal(3, item.ReadingTimeMinutes);
		Assert.Equal("", item.Content);
	}
}
