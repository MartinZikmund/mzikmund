using MZikmund.DataContracts.Blog.Tags;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/blog/tags")]
	Task<ApiResponse<BlogTagDto[]>> GetBlogTagsAsync();

	[Post("/v1/blog/tags")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<BlogTagDto>> AddBlogTagAsync([Body] BlogTagDto category);

	[Put("/v1/blog/tags/{categoryId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<BlogTagDto>> UpdateTagAsync(int categoryId, [Body] BlogTagDto category);

	[Delete("/v1/blog/tags/{categoryId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeleteBlogTagAsync(int categoryId);
}
