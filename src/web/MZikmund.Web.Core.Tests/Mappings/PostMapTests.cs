using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Mappings;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Tests.Mappings;

public class PostMapTests
{
	private readonly IMapper _mapper = new MapperConfiguration(c => c.AddProfile<PostMap>(), NullLoggerFactory.Instance).CreateMapper();

	[Theory]
	[InlineData(PostStatus.Published, true)]
	[InlineData(PostStatus.Draft, false)]
	public void PostAdmin_ExposesPublishedState(PostStatus status, bool expected) =>
		Assert.Equal(expected, _mapper.Map<PostAdmin>(new PostEntity { Status = status }).IsPublished);

	[Theory]
	[InlineData(PostStatus.Published, true)]
	[InlineData(PostStatus.Draft, false)]
	public void Post_ExposesPublishedState(PostStatus status, bool expected) =>
		Assert.Equal(expected, _mapper.Map<Post>(new PostEntity { Status = status }).IsPublished);
}
