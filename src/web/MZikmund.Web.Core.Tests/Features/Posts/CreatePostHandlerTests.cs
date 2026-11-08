using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Tests.Features.Posts;

public class CreatePostHandlerTests
{
	[Theory]
	[InlineData("A cat on a keyboard", "A cat on a keyboard")]
	[InlineData("   ", null)]
	[InlineData(null, null)]
	public async Task Handle_PersistsHeroImageAlt(string? alt, string? expected)
	{
		Mock<IRepository<PostEntity>> posts = new();
		posts
			.Setup(r => r.AddAsync(It.IsAny<PostEntity>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((PostEntity entity, CancellationToken _) => entity);

		CreatePostHandler handler = new(
			posts.Object,
			Mock.Of<IRepository<CategoryEntity>>(),
			Mock.Of<IRepository<TagEntity>>(),
			Mock.Of<ISiteConfiguration>(),
			Mock.Of<IDateProvider>(),
			Mock.Of<IMapper>(),
			NullLogger<CreatePostHandler>.Instance);

		await handler.Handle(new CreatePostCommand(new Post { Title = "Title", RouteName = "title", HeroImageAlt = alt }), CancellationToken.None);

		posts.Verify(r => r.AddAsync(It.Is<PostEntity>(p => p.HeroImageAlt == expected), It.IsAny<CancellationToken>()));
	}
}
