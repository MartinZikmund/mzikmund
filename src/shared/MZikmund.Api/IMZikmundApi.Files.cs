using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/admin/files")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<PagedResponse<StorageItemInfo>>> GetFilesAsync([Query] int pageNumber = 1, [Query] int pageSize = 50);

	[Post("/v1/admin/files")]
	[Headers("Authorization: Bearer")]
	[Multipart]
	Task<ApiResponse<StorageItemInfo>> UploadFileAsync([AliasAs("file")] StreamPart streamPart, [Query] string? desiredFileName = null);

	[Delete("/v1/admin/files/{path}")]
	[Headers("Authorization: Bearer")]
	Task<HttpResponseMessage> DeleteFileAsync(string path);
}
