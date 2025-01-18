namespace MZikmund.Web.Core.Services.Blobs;

public record BlobInfo(string BlobPath, DateTimeOffset? LastModified)
{
	public string FileName => Path.GetFileName(BlobPath);

	public string Extension => Path.GetExtension(BlobPath);
}
