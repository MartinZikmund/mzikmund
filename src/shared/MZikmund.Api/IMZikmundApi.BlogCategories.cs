using MZikmund.DataContracts.Blog.Categories;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/blog/categories")]
	Task<ApiResponse<BlogCategoryDto[]>> GetBlogCategoriesAsync();

	[Post("/v1/blog/categories")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<BlogCategoryDto>> AddBlogCategoryAsync(BlogCategoryDto category);

	[Put("/v1/blog/categories/{categoryId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<BlogCategoryDto>> UpdateCategoryAsync(int categoryId, BlogCategoryDto category);

	[Delete("/v1/blog/categories/{categoryId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeleteBlogCategoryAsync(int categoryId);
}
