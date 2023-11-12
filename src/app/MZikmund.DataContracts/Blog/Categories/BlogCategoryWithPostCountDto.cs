using MZikmund.DataContracts.Blog.Categories;

namespace MZikmund.Dtos;

public class BlogCategoryWithPostCountDto : BlogCategoryDto
{
	public int PostCount { get; set; }
}
