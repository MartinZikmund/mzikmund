using MZikmund.DataContracts.Storage;

namespace MZikmund.DataContracts.Blobs;

public record StorageItemInfo(string BlobPath, Uri? Url, DateTimeOffset? LastModified)
{
	public string FileName => Path.GetFileName(BlobPath);

	public string Extension => Path.GetExtension(BlobPath);
}
