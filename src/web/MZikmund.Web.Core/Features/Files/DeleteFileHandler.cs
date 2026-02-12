using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Files;

public class DeleteFileHandler : IRequestHandler<DeleteFileCommand>
{
	private readonly IBlobStorage _blobStorage;
	private readonly DatabaseContext _dbContext;

	public DeleteFileHandler(IBlobStorage blobStorage, DatabaseContext dbContext)
	{
		_blobStorage = blobStorage;
		_dbContext = dbContext;
	}

	public async Task Handle(DeleteFileCommand request, CancellationToken cancellationToken)
	{
		// Delete blob from storage first to avoid orphaned blobs if DB deletion fails
		await _blobStorage.DeleteAsync(BlobKind.File, request.Path);

		// Delete metadata from database
		var metadata = await _dbContext.BlobMetadata
			.FirstOrDefaultAsync(b => b.BlobPath == request.Path && b.Kind == MZikmund.Web.Data.Entities.BlobKind.File, cancellationToken);

		if (metadata != null)
		{
			_dbContext.BlobMetadata.Remove(metadata);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}
	}
}
