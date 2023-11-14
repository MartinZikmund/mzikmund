using AutoMapper;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Mappings;

public class TagMap : Profile
{
	public TagMap()
	{
		CreateMap<TagEntity, Tag>();
	}
}
