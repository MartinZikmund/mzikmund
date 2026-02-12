namespace MZikmund.Web.Core.Services.Blobs;

public record BlobStorageItem(string BlobPath, DateTimeOffset? LastModified, long Size)
{
	public string FileName => Path.GetFileName(BlobPath);

	public string Extension => Path.GetExtension(BlobPath);
}
