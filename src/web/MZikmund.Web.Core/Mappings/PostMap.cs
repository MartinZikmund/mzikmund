using AutoMapper;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Mappings;

public class PostMap : Profile
{
	public PostMap()
	{
		CreateMap<PostEntity, Post>();
		CreateMap<PostEntity, PostListItem>();
	}
}
