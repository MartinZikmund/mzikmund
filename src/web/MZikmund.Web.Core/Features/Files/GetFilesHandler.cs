using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class GetFilesHandler : IRequestHandler<GetFilesQuery, PagedResponse<StorageItemInfo>>
{
	private readonly IBlobStorage _blobStorage;

	public GetFilesHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task<PagedResponse<StorageItemInfo>> Handle(GetFilesQuery request, CancellationToken cancellationToken)
	{
		var (files, totalCount) = await _blobStorage.ListPagedAsync(BlobKind.File, request.PageNumber, request.PageSize);

		return new PagedResponse<StorageItemInfo>(files, request.PageNumber, request.PageSize, totalCount);
	}
}
