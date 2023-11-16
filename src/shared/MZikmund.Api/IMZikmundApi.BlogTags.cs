using MZikmund.DataContracts.Blog;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/tags")]
	Task<ApiResponse<Tag[]>> GetBlogTagsAsync();

	[Post("/v1/admin/tags")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Tag>> AddBlogTagAsync([Body] Tag tag); // TODO: Should be EditTag

	[Put("/v1/admin/tags/{tagId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Tag>> UpdateTagAsync(int tagId, [Body] EditTag tag);

	[Delete("/v1/admin/tags/{tagId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeleteBlogTagAsync(int tagId);
}
