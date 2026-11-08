using AutoMapper;
using Moq;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Tests.Features.Posts;

public class UpdatePostHandlerTests
{
	[Theory]
	[InlineData("A cat on a keyboard", "A cat on a keyboard")]
	[InlineData("   ", null)]
	[InlineData(null, null)]
	public async Task Handle_PersistsHeroImageAlt(string? alt, string? expected)
	{
		Guid id = Guid.NewGuid();
		PostEntity entity = new() { Id = id, HeroImageAlt = "Previous alt", Status = PostStatus.Draft };

		Mock<IRepository<PostEntity>> posts = new();
		posts.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

		UpdatePostCommandHandler handler = new(
			Mock.Of<IRepository<PostCategoryEntity>>(),
			Mock.Of<IRepository<PostTagEntity>>(),
			Mock.Of<IRepository<TagEntity>>(),
			Mock.Of<IRepository<CategoryEntity>>(),
			posts.Object,
			Mock.Of<IMapper>(),
			Mock.Of<IDateProvider>(),
			Mock.Of<ISiteConfiguration>());

		await handler.Handle(new UpdatePostCommand(id, new Post { Id = id, Title = "Title", RouteName = "title", HeroImageAlt = alt }), CancellationToken.None);

		Assert.Equal(expected, entity.HeroImageAlt);
	}
}
