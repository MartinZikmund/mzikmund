using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Services;

public interface IBlobStorage
{
	Task InitializeAsync();

	Task<string> AddAsync(BlobKind blobKind, string fileName, byte[] fileBytes);

	Task<string> AddAsync(BlobKind blobKind, string fileName, Stream stream);

	Task<BlobInfo?> GetAsync(BlobKind blobKind, string fileName);

	Task DeleteAsync(BlobKind blobKind, string fileName);

	Task<BlobInfo?[]> ListAsync(BlobKind blobKind, string container);
}
