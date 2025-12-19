using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/posts")]
	Task<ApiResponse<PagedResponse<PostListItem>>> GetPostsAsync(int pageNumber = 1);

	[Get("/v1/posts/{postId}")]
	Task<ApiResponse<Post>> GetPostAsync(Guid postId);

	[Get("/v1/admin/posts")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<PagedResponse<PostListItem>>> GetAllPostsAsync(int pageNumber = 1);

	[Get("/v1/admin/posts/{postId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Post>> GetPostForAdminAsync(Guid postId);

	[Post("/v1/admin/posts")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Post>> AddPostAsync([Body] Post post); // TODO: Should be EditPost

	[Put("/v1/admin/posts/{postId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<Post>> UpdatePostAsync(Guid postId, [Body] Post post);

	[Delete("/v1/admin/posts/{postId}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeletePostAsync(Guid postId);
}
