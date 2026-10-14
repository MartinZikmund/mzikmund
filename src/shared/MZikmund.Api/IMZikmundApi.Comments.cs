using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	// Reads come in two flavours on purpose. The plain method carries no Authorization header,
	// so browsing a post never triggers RefitSettings.AuthorizationHeaderValueGetter and its
	// interactive sign-in. The ...AuthenticatedAsync variant is called only when the user is
	// already signed in, and is what populates HasUpvoted / IsOwn.

	[Get("/v1/posts/{postId}/comments")]
	Task<ApiResponse<PagedResponse<CommentDto>>> GetPostCommentsAsync(
		Guid postId,
		[Query] CommentSortOrder sort = CommentSortOrder.Top,
		[Query] int pageNumber = 1);

	[Get("/v1/posts/{postId}/comments")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<PagedResponse<CommentDto>>> GetPostCommentsAuthenticatedAsync(
		Guid postId,
		[Query] CommentSortOrder sort = CommentSortOrder.Top,
		[Query] int pageNumber = 1);

	[Get("/v1/comments/{id}/replies")]
	Task<ApiResponse<PagedResponse<CommentDto>>> GetCommentRepliesAsync(
		Guid id,
		[Query] int pageNumber = 1);

	[Get("/v1/comments/{id}/replies")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<PagedResponse<CommentDto>>> GetCommentRepliesAuthenticatedAsync(
		Guid id,
		[Query] int pageNumber = 1);

	[Post("/v1/posts/{postId}/comments")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<CommentDto>> CreateCommentAsync(Guid postId, [Body] CreateCommentRequest request);

	[Patch("/v1/comments/{id}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<CommentDto>> UpdateCommentAsync(Guid id, [Body] UpdateCommentRequest request);

	[Delete("/v1/comments/{id}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeleteCommentAsync(Guid id);

	[Put("/v1/comments/{id}/vote")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<CommentDto>> AddCommentVoteAsync(Guid id);

	[Delete("/v1/comments/{id}/vote")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<CommentDto>> RemoveCommentVoteAsync(Guid id);

	[Post("/v1/comments/{id}/report")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> ReportCommentAsync(Guid id, [Body] ReportCommentRequest request);

	[Get("/v1/admin/comments/reports")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<PagedResponse<CommentReportDto>>> GetCommentReportsAsync([Query] int pageNumber = 1);

	[Post("/v1/admin/comments/reports/{commentId}/resolve")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> ResolveCommentReportAsync(Guid commentId);

	// "since" is sent as a round-trip ISO 8601 instant ("o"), keeping the offset and the
	// milliseconds. Callers must pass a UTC DateTime: an Unspecified one is read as UTC by the server.
	[Get("/v1/admin/comments")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<PagedResponse<CommentModerationDto>>> GetModerationFeedAsync(
		[Query(Format = "o")] DateTime? since = null,
		[Query] int pageNumber = 1);

	[Get("/v1/admin/comments/count")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<int>> CountNewCommentsAsync([Query(Format = "o")] DateTime? since = null);

	[Post("/v1/admin/comments/{id}/hide")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> HideCommentAsync(Guid id);

	[Delete("/v1/admin/comments/{id}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> AdminDeleteCommentAsync(Guid id);

	[Post("/v1/admin/authors/{id}/ban")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> BanCommentAuthorAsync(Guid id);
}
