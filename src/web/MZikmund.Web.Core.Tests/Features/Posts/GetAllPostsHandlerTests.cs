using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using Moq;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Tests.Features.Posts;

public class GetAllPostsHandlerTests
{
	[Theory]
	[InlineData(PostStatus.Published, true)]
	[InlineData(PostStatus.Draft, false)]
	public async Task Handle_ExposesPublishedState(PostStatus status, bool expectedIsPublished)
	{
		PostEntity entity = new() { Id = Guid.NewGuid(), Title = "Post", Status = status };

		Mock<IRepository<PostEntity>> repository = new();
		repository
			.Setup(r => r.SelectAsync(It.IsAny<ISpecification<PostEntity>>(), It.IsAny<Expression<Func<PostEntity, PostListItem>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ISpecification<PostEntity> _, Expression<Func<PostEntity, PostListItem>> selector, CancellationToken _) =>
				new[] { selector.Compile()(entity) });

		Mock<IMediator> mediator = new();
		mediator.Setup(m => m.Send(It.IsAny<CountAllPostsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

		Mock<IMapper> mapper = new();
		mapper.Setup(m => m.Map<Tag[]>(It.IsAny<object>())).Returns(Array.Empty<Tag>());
		mapper.Setup(m => m.Map<Category[]>(It.IsAny<object>())).Returns(Array.Empty<Category>());

		GetAllPostsHandler handler = new(repository.Object, mediator.Object, mapper.Object);

		var result = await handler.Handle(new GetAllPostsQuery(1, 10), CancellationToken.None);

		Assert.Equal(expectedIsPublished, Assert.Single(result.Data).IsPublished);
	}
}
