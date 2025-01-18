using MediatR;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class UploadFileHandler : IRequestHandler<UploadFileCommand, BlobInfo>
{
	private readonly IBlobStorage _blobStorage;

	public UploadFileHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public Task<BlobInfo> Handle(UploadFileCommand request, CancellationToken cancellationToken)
	{
		return _blobStorage.AddAsync(BlobKind.File, request.FileName, request.Stream);
	}
}
