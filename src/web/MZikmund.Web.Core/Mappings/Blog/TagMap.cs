using AutoMapper;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Mappings.Blog;

public class TagMap : Profile
{
	public TagMap()
	{
		CreateMap<TagEntity, Tag>();
	}
}
