using MZikmund.DataContracts.Blog;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/categories")]
	Task<ApiResponse<Category[]>> GetCategoriesAsync();

	[Post("/v1/admin/categories")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Category>> AddCategoryAsync(Category category); // TODO: Should be EditCategory

	[Put("/v1/admin/categories/{categoryId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Category>> UpdateCategoryAsync(Guid categoryId, Category category);

	[Delete("/v1/admin/categories/{categoryId}")]
	[Headers("Authorization: Bearer")]
	Task<IApiResponse> DeleteCategoryAsync(Guid categoryId); // 204 No Content: a typed response would fail to deserialize the empty body
}
