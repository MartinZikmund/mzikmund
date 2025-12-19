using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Features.Blobs;

public record GetMediaQuery(
	int PageNumber,
	int PageSize,
	BlobKindFilter? Kind = null,
	string? Search = null) : IRequest<PagedResponse<StorageItemInfo>>;
