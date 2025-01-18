using MediatR;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class DeleteFileHandler : IRequestHandler<DeleteFileCommand>
{
	private readonly IBlobStorage _blobStorage;

	public DeleteFileHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task Handle(DeleteFileCommand request, CancellationToken cancellationToken) =>
		await _blobStorage.DeleteAsync(BlobKind.File, request.Path);
}
