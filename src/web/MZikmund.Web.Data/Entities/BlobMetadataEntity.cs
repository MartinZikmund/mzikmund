namespace MZikmund.Web.Data.Entities;

public class BlobMetadataEntity
{
	public Guid Id { get; set; }

	public BlobKind Kind { get; set; }

	public string BlobPath { get; set; } = "";

	public string FileName { get; set; } = "";

	public DateTimeOffset LastModified { get; set; }

	public long Size { get; set; }

	public string? ContentType { get; set; }
}

public enum BlobKind
{
	Image = 0,
	File = 1
}
