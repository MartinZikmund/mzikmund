using Azure.Storage.Blobs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Blobs;

public class SyncBlobMetadataHandler : IRequestHandler<SyncBlobMetadataCommand, SyncBlobMetadataResult>
{
private readonly DatabaseContext _dbContext;
private readonly ILogger<SyncBlobMetadataHandler> _logger;
private readonly BlobContainerClient _mediaContainer;
private readonly BlobContainerClient _filesContainer;

public SyncBlobMetadataHandler(
DatabaseContext dbContext,
ISiteConfiguration siteConfiguration,
IConnectionStringProvider connectionStringProvider,
ILogger<SyncBlobMetadataHandler> logger)
{
_dbContext = dbContext;
_logger = logger;

_mediaContainer = new(connectionStringProvider.AzureBlobStorage, siteConfiguration.BlobStorage.MediaContainerName);
_filesContainer = new(connectionStringProvider.AzureBlobStorage, siteConfiguration.BlobStorage.FilesContainerName);
}

public async Task<SyncBlobMetadataResult> Handle(SyncBlobMetadataCommand request, CancellationToken cancellationToken)
{
_logger.LogInformation("Starting blob metadata synchronization");

var imagesAdded = await SyncContainer(_mediaContainer, MZikmund.Web.Data.Entities.BlobKind.Image, cancellationToken);
var filesAdded = await SyncContainer(_filesContainer, MZikmund.Web.Data.Entities.BlobKind.File, cancellationToken);

var totalAdded = imagesAdded + filesAdded;

_logger.LogInformation($"Blob metadata synchronization completed. Images: {imagesAdded}, Files: {filesAdded}, Total: {totalAdded}");

return new SyncBlobMetadataResult(imagesAdded, filesAdded, totalAdded);
}

private async Task<int> SyncContainer(BlobContainerClient containerClient, MZikmund.Web.Data.Entities.BlobKind kind, CancellationToken cancellationToken)
{
var added = 0;

await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
{
// Check if metadata already exists
var exists = await _dbContext.BlobMetadata
.AnyAsync(b => b.BlobPath == blobItem.Name && b.Kind == kind, cancellationToken);

if (!exists)
{
var fileName = Path.GetFileName(blobItem.Name);
var metadata = new BlobMetadataEntity
{
Id = Guid.NewGuid(),
Kind = kind,
BlobPath = blobItem.Name,
FileName = fileName,
LastModified = blobItem.Properties.LastModified ?? DateTimeOffset.UtcNow,
Size = blobItem.Properties.ContentLength ?? 0,
ContentType = blobItem.Properties.ContentType
};

_dbContext.BlobMetadata.Add(metadata);
added++;

_logger.LogInformation($"Added metadata for blob: {blobItem.Name}");
}
}

if (added > 0)
{
await _dbContext.SaveChangesAsync(cancellationToken);
}

return added;
}
}
