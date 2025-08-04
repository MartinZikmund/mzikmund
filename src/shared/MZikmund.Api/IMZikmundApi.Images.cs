using MZikmund.Web.Core.Services.Blobs;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/admin/images")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<IEnumerable<BlobInfo>>> GetImagesAsync();

	[Post("/v1/admin/images")]
	[Headers("Authorization: Bearer")]
	[Multipart]
	Task<ApiResponse<BlobInfo>> UploadImageAsync([AliasAs("file")] StreamPart streamPart, [AliasAs("desiredFileName")] string? desiredFileName = null);
}