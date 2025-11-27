using MZikmund.DataContracts.Storage;

namespace MZikmund.DataContracts.Blobs;

public record BlobStorageItem(string BlobPath, DateTimeOffset? LastModified, long? Size = null)
{
	public string FileName => Path.GetFileName(BlobPath);

	public string Extension => Path.GetExtension(BlobPath);
}
