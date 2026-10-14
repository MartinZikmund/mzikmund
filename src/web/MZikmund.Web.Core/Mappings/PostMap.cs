using AutoMapper;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Mappings;

public class PostMap : Profile
{
	public PostMap()
	{
		// Public mapping - Post doesn't include PreviewToken
		CreateMap<PostEntity, Post>()
			.ForMember(dto => dto.IsPublished, options => options.MapFrom(entity => entity.Status == PostStatus.Published));

		// Admin mapping - PostAdmin includes PreviewToken
		CreateMap<PostEntity, PostAdmin>()
			.IncludeBase<PostEntity, Post>();

		CreateMap<PostEntity, PostListItem>();
	}
}
