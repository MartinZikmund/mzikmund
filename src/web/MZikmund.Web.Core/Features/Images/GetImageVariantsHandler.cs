using MediatR;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Images;

public class GetImageVariantsHandler : IRequestHandler<GetImageVariantsQuery, List<ImageVariant>>
{
	private readonly IBlobStorage _blobStorage;
	private readonly ISiteConfiguration _siteConfiguration;
	private const uint ThumbnailWidth = 200;

	public GetImageVariantsHandler(IBlobStorage blobStorage, ISiteConfiguration siteConfiguration)
	{
		_blobStorage = blobStorage;
		_siteConfiguration = siteConfiguration;
	}

	public async Task<List<ImageVariant>> Handle(GetImageVariantsQuery request, CancellationToken cancellationToken)
	{
		var variants = new List<ImageVariant>();
		var baseUrl = new Uri(_siteConfiguration.General.CdnUrl, _siteConfiguration.BlobStorage.MediaContainerName);

		var fileNameWithoutExt = Path.GetFileNameWithoutExtension(request.ImagePath);
		var directory = Path.GetDirectoryName(request.ImagePath) ?? "";

		// Check for original
		var originalPath = Path.Combine("original", request.ImagePath).Replace("\\", "/");
		var originalBlobs = await _blobStorage.ListAsync(BlobKind.Image, originalPath);
		var originalBlob = originalBlobs.FirstOrDefault(b => b.BlobPath == originalPath);
		if (originalBlob != null)
		{
			variants.Add(new ImageVariant("Original", new Uri($"{baseUrl}/{originalPath}"), null, originalBlob.Size));
		}

		// Use prefix search for resized variants - get all blobs starting with the filename
		var resizedPrefix = Path.Combine("resized", directory, fileNameWithoutExt).Replace("\\", "/");
		var resizedBlobs = await _blobStorage.ListAsync(BlobKind.Image, resizedPrefix);

		// Parse resized variants from the returned blobs
		foreach (var blob in resizedBlobs)
		{
			// Extract width from filename pattern: {filename}-{width}.{ext}
			var blobFileName = Path.GetFileNameWithoutExtension(blob.BlobPath);
			var parts = blobFileName.Split('-');
			if (parts.Length >= 2 && uint.TryParse(parts[^1], out var width))
			{
				variants.Add(new ImageVariant("Resized", new Uri($"{baseUrl}/{blob.BlobPath}"), width, blob.Size));
			}
		}

		// Check for thumbnail
		var thumbnailPath = Path.Combine("thumbnail", request.ImagePath).Replace("\\", "/");
		var thumbnailBlobs = await _blobStorage.ListAsync(BlobKind.Image, thumbnailPath);
		var thumbnailBlob = thumbnailBlobs.FirstOrDefault(b => b.BlobPath == thumbnailPath);
		if (thumbnailBlob != null)
		{
			variants.Add(new ImageVariant("Thumbnail", new Uri($"{baseUrl}/{thumbnailPath}"), ThumbnailWidth, thumbnailBlob.Size));
		}

		return variants;
	}
}
