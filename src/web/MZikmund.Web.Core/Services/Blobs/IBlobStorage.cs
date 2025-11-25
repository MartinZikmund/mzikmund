using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Services.Blobs;

public interface IBlobStorage
{
	Task InitializeAsync();

	Task<BlobStorageItem> AddAsync(BlobKind blobKind, string blobPath, byte[] fileBytes);

	Task<BlobStorageItem> AddAsync(BlobKind blobKind, string blobPath, Stream stream);

	Task DeleteAsync(BlobKind blobKind, string blobPath);

	Task<BlobStorageItem[]> ListAsync(BlobKind blobKind, string? prefix = null);
}
