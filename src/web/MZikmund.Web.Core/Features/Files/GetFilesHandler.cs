using MediatR;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class GetFilesHandler : IRequestHandler<GetFilesQuery, IEnumerable<StorageItemInfo>>
{
	private readonly IBlobStorage _blobStorage;

	public GetFilesHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task<IEnumerable<StorageItemInfo>> Handle(GetFilesQuery request, CancellationToken cancellationToken)
	{
		return await _blobStorage.ListAsync(BlobKind.File);
	}
}
