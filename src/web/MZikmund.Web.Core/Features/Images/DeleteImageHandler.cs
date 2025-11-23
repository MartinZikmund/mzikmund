using MediatR;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Images;

public class DeleteImageHandler : IRequestHandler<DeleteImageCommand>
{
	private readonly IBlobStorage _blobStorage;

	public DeleteImageHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task Handle(DeleteImageCommand request, CancellationToken cancellationToken) =>
		await _blobStorage.DeleteAsync(BlobKind.Image, request.Path);
}
