using MZikmund.DataContracts.Blobs;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/admin/images")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<IEnumerable<StorageItemInfo>>> GetImagesAsync();

	[Post("/v1/admin/images")]
	[Headers("Authorization: Bearer")]
	[Multipart]
	Task<ApiResponse<StorageItemInfo>> UploadImageAsync([AliasAs("file")] StreamPart streamPart, [AliasAs("desiredFileName")] string? desiredFileName = null);

	[Delete("/v1/admin/images/{path}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeleteImageAsync(string path);
}
