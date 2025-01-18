using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Services.Blobs;

public interface IBlobStorage
{
	Task InitializeAsync();

	Task<BlobInfo> AddAsync(BlobKind blobKind, string blobPath, byte[] fileBytes);

	Task<BlobInfo> AddAsync(BlobKind blobKind, string blobPath, Stream stream);

	Task DeleteAsync(BlobKind blobKind, string blobPath);

	Task<BlobInfo[]> ListAsync(BlobKind blobKind);
}
