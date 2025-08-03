using MZikmund.Web.Core.Services.Blobs;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/admin/files")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<IEnumerable<BlobInfo>>> GetFilesAsync();

	[Post("/v1/admin/files")]
	[Headers("Authorization: Bearer")]
	[Multipart]
	Task<ApiResponse<BlobInfo>> UploadFileAsync([AliasAs("file")] StreamPart streamPart, [AliasAs("desiredFileName")] string? desiredFileName = null);

	[Delete("/v1/admin/files/{path}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeleteFileAsync(string path);
}