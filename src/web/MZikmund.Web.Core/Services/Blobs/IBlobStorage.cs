using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Services.Blobs;

public interface IBlobStorage
{
	Task InitializeAsync();

	Task<StorageItemInfo> AddAsync(BlobKind blobKind, string blobPath, byte[] fileBytes);

	Task<StorageItemInfo> AddAsync(BlobKind blobKind, string blobPath, Stream stream);

	Task DeleteAsync(BlobKind blobKind, string blobPath);

	Task<StorageItemInfo[]> ListAsync(BlobKind blobKind, string? prefix = null);
}
