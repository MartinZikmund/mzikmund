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
		var allFiles = (await _blobStorage.ListAsync(BlobKind.File))
			.OrderByDescending(x => x.LastModified)
			.ToList();

		var totalCount = allFiles.Count;
		var skip = (request.PageNumber - 1) * request.PageSize;
		var pagedFiles = allFiles.Skip(skip).Take(request.PageSize);

		return new PagedResponse<StorageItemInfo>(pagedFiles, request.PageNumber, request.PageSize, totalCount);
	}
}
