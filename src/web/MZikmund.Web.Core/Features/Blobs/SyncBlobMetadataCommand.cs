using MediatR;

namespace MZikmund.Web.Core.Features.Blobs;

/// <summary>
/// Command to sync existing blobs from Azure Storage to the database.
/// This should be run once after adding the BlobMetadata table.
/// </summary>
public record SyncBlobMetadataCommand : IRequest<SyncBlobMetadataResult>;

public record SyncBlobMetadataResult(int ImagesAdded, int FilesAdded, int TotalAdded);
