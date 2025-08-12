using MediatR;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class UploadFileHandler : IRequestHandler<UploadFileCommand, StorageItemInfo>
{
	private readonly IBlobStorage _blobStorage;
	private readonly IBlobPathGenerator _blobPathGenerator;

	public UploadFileHandler(IBlobStorage blobStorage, IBlobPathGenerator blobPathGenerator)
	{
		_blobStorage = blobStorage;
		_blobPathGenerator = blobPathGenerator;
	}

	public Task<StorageItemInfo> Handle(UploadFileCommand request, CancellationToken cancellationToken)
	{
		var path = _blobPathGenerator.GenerateBlobPath(request.FileName);

		return _blobStorage.AddAsync(BlobKind.File, path, request.Stream);
	}
}
