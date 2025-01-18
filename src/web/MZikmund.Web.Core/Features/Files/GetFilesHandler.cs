using MediatR;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class GetFilesHandler : IRequestHandler<GetFilesQuery, IEnumerable<BlobInfo>>
{
	private readonly IBlobStorage _blobStorage;

	public GetFilesHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task<IEnumerable<BlobInfo>> Handle(GetFilesQuery request, CancellationToken cancellationToken)
	{
		return await _blobStorage.ListAsync(BlobKind.File);
	}
}
