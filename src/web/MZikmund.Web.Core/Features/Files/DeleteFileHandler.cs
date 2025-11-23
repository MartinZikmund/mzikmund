using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data;

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
		await _blobStorage.DeleteAsync(Services.Blobs.BlobKind.File, request.Path);

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
