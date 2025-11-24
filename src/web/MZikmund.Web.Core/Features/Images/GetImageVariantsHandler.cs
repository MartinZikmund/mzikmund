using MediatR;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Images;

public class GetImageVariantsHandler : IRequestHandler<GetImageVariantsQuery, List<ImageVariant>>
{
private readonly IBlobStorage _blobStorage;
private readonly ISiteConfiguration _siteConfiguration;
private static readonly uint[] ResizeWidths = { 1200, 1000, 800, 400 };
private const uint ThumbnailWidth = 200;

public GetImageVariantsHandler(IBlobStorage blobStorage, ISiteConfiguration siteConfiguration)
{
_blobStorage = blobStorage;
_siteConfiguration = siteConfiguration;
}

public async Task<List<ImageVariant>> Handle(GetImageVariantsQuery request, CancellationToken cancellationToken)
{
var variants = new List<ImageVariant>();
// TODO: Get account name from configuration or parse from connection string
var baseUrl = $"https://mzikmund.blob.core.windows.net/{_siteConfiguration.BlobStorage.MediaContainerName}";

// Check for original
var originalPath = Path.Combine("original", request.ImagePath);
var originalExists = await BlobExistsAsync(originalPath);
if (originalExists)
{
variants.Add(new ImageVariant("Original", $"{baseUrl}/{originalPath}"));
}

// Check for resized variants
foreach (var width in ResizeWidths)
{
var resizedPath = Path.Combine("resized", GetPathWithSizeSuffix(request.ImagePath, width));
var resizedExists = await BlobExistsAsync(resizedPath);
if (resizedExists)
{
variants.Add(new ImageVariant("Resized", $"{baseUrl}/{resizedPath}", width));
}
}

// Check for thumbnail
var thumbnailPath = Path.Combine("thumbnail", request.ImagePath);
var thumbnailExists = await BlobExistsAsync(thumbnailPath);
if (thumbnailExists)
{
variants.Add(new ImageVariant("Thumbnail", $"{baseUrl}/{thumbnailPath}", ThumbnailWidth));
}

return variants;
}

private async Task<bool> BlobExistsAsync(string blobPath)
{
try
{
var blobs = await _blobStorage.ListAsync(Services.Blobs.BlobKind.Image, blobPath);
return blobs.Any(b => b.BlobPath == blobPath);
}
catch
{
return false;
}
}

private string GetPathWithSizeSuffix(string path, uint width)
{
var extension = Path.GetExtension(path);
var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
var directory = Path.GetDirectoryName(path) ?? "";
return Path.Combine(directory, $"{fileNameWithoutExtension}-{width}{extension}");
}
}
