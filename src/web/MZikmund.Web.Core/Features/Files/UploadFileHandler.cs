using MediatR;
using Microsoft.AspNetCore.StaticFiles;
using MZikmund.DataContracts.Blobs;
using MZikmund.DataContracts.Storage;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Files;

public class UploadFileHandler : IRequestHandler<UploadFileCommand, StorageItemInfo>
{
	private readonly IBlobStorage _blobStorage;
	private readonly IBlobPathGenerator _blobPathGenerator;
	private readonly DatabaseContext _dbContext;
	private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

	public UploadFileHandler(IBlobStorage blobStorage, IBlobPathGenerator blobPathGenerator, DatabaseContext dbContext)
	{
		_blobStorage = blobStorage;
		_blobPathGenerator = blobPathGenerator;
		_dbContext = dbContext;
	}

	public async Task<StorageItemInfo> Handle(UploadFileCommand request, CancellationToken cancellationToken)
	{
		var path = _blobPathGenerator.GenerateBlobPath(request.FileName);

		// Copy to memory stream to get size
		var memoryStream = new MemoryStream();
		await request.Stream.CopyToAsync(memoryStream, cancellationToken);
		var fileSize = memoryStream.Length;
		memoryStream.Position = 0;

		var result = await _blobStorage.AddAsync(Services.Blobs.BlobKind.File, path, memoryStream);

		// Save metadata to database
		var fileName = Path.GetFileName(path);
		_contentTypeProvider.TryGetContentType(request.FileName, out var contentType);

		var metadata = new BlobMetadataEntity
		{
			Id = Guid.NewGuid(),
			Kind = MZikmund.Web.Data.Entities.BlobKind.File,
			BlobPath = path,
			FileName = fileName,
			LastModified = result.LastModified ?? DateTimeOffset.UtcNow,
			Size = fileSize,
			ContentType = contentType ?? "application/octet-stream"
		};

		_dbContext.BlobMetadata.Add(metadata);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return new StorageItemInfo(path, StorageItemType.File, result.LastModified);
	}
}
