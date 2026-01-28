using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Images;

public class DeleteImageHandler : IRequestHandler<DeleteImageCommand>
{
	private readonly IBlobStorage _blobStorage;
	private readonly DatabaseContext _dbContext;

	public DeleteImageHandler(IBlobStorage blobStorage, DatabaseContext dbContext)
	{
		_blobStorage = blobStorage;
		_dbContext = dbContext;
	}

	public async Task Handle(DeleteImageCommand request, CancellationToken cancellationToken)
	{
		// Delete all variants from blob storage (original, resized, thumbnail)
		await _blobStorage.DeleteAsync(BlobKind.Image, Path.Combine("original", request.Path).Replace("\\", "/"));
		await _blobStorage.DeleteAsync(BlobKind.Image, Path.Combine("thumbnail", request.Path).Replace("\\", "/"));

		// Delete resized variants
		foreach (var width in ImageVariantHelper.ResizeWidths)
		{
			var resizedPath = GetPathWithSizeSuffix(request.Path, width);
			await _blobStorage.DeleteAsync(BlobKind.Image, Path.Combine("resized", resizedPath).Replace("\\", "/"));
		}

		// Delete metadata from database
		var metadata = await _dbContext.BlobMetadata
			.FirstOrDefaultAsync(b => b.BlobPath == request.Path && b.Kind == MZikmund.Web.Data.Entities.BlobKind.Image, cancellationToken);

		if (metadata != null)
		{
			_dbContext.BlobMetadata.Remove(metadata);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}
	}

	private string GetPathWithSizeSuffix(string path, uint width)
	{
		var extension = Path.GetExtension(path);
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		var directory = Path.GetDirectoryName(path) ?? "";
		return Path.Combine(directory, $"{fileNameWithoutExtension}-{width}{extension}").Replace("\\", "/");
	}
}
