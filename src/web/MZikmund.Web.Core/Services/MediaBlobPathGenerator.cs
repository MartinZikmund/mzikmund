namespace MZikmund.Web.Core.Services;

public class MediaBlobPathGenerator : IMediaBlobPathGenerator
{
	private readonly IDateProvider _dateProvider;

	public MediaBlobPathGenerator(IDateProvider dateProvider)
	{
		_dateProvider = dateProvider;
	}

	public string GenerateBlobPath(string fileName)
	{
		var date = _dateProvider.UtcNow;
		return $"{date.Year}/{date.Month:00}/{date.Day:00}/{fileName}";
	}
}
