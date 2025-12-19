using AutoMapper;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Mappings;

public class PostMap : Profile
{
	public PostMap()
	{
		// Public mapping - excludes PreviewToken
		CreateMap<PostEntity, Post>()
			.ForMember(dest => dest.PreviewToken, opt => opt.Ignore());
		
		// Admin mapping - includes PreviewToken
		CreateMap<PostEntity, PostAdmin>();
		
		CreateMap<PostEntity, PostListItem>();
	}
}
