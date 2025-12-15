using MZikmund.DataContracts.Blog;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/tags")]
	Task<ApiResponse<Tag[]>> GetTagsAsync();

	[Post("/v1/admin/tags")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Tag>> AddTagAsync([Body] Tag tag); // TODO: Should be EditTag

	[Put("/v1/admin/tags/{tagId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Tag>> UpdateTagAsync(Guid tagId, [Body] Tag tag);

	[Delete("/v1/admin/tags/{tagId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeleteTagAsync(Guid tagId);
}
