using AutoMapper;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Mappings;

public class CatgegoryMap : Profile
{
	public CatgegoryMap()
	{
		CreateMap<CategoryEntity, Category>();
	}
}
