using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using Refit;

namespace MZikmund.Api.Client;

public partial interface IMZikmundApi
{
	[Get("/v1/admin/media")]
	[Headers("Authorization: Bearer")]
	Task<ApiResponse<PagedResponse<StorageItemInfo>>> GetMediaAsync(
		[Query] int pageNumber = 1,
		[Query] int pageSize = 50,
		[Query] BlobKindFilter? kind = null,
		[Query] string? search = null);
}
