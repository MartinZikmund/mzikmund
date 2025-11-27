using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/admin/images")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<PagedResponse<StorageItemInfo>>> GetImagesAsync([Query] int pageNumber = 1, [Query] int pageSize = 50);

	[Post("/v1/admin/images")]
	[Headers("Authorization: Bearer")]
	[Multipart]
	Task<ApiResponse<StorageItemInfo>> UploadImageAsync([AliasAs("file")] StreamPart streamPart, [Query] string? desiredFileName = null);

	[Delete("/v1/admin/images/{path}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<object?>> DeleteImageAsync(string path);

	[Get("/v1/admin/images/variants/{imagePath}")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<ImageVariant[]>> GetImageVariantsAsync(string imagePath);
}
