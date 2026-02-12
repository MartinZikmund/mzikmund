namespace MZikmund.DataContracts.Blobs;

public static class ImageVariantHelper
{
	public static readonly uint[] ResizeWidths = { 1200, 1000, 800, 400 };
	public const uint ThumbnailWidth = 200;

	public static List<ImageVariant> GetImageVariants(string basePath, string baseUrl)
	{
		var variants = new List<ImageVariant>();

		// Original
		variants.Add(new ImageVariant("Original", new Uri($"{baseUrl}/original/{basePath}"), null, 0));

		// Resized variants
		foreach (var width in ResizeWidths)
		{
			var resizedPath = GetPathWithSizeSuffix(basePath, width);
			variants.Add(new ImageVariant($"Resized", new Uri($"{baseUrl}/resized/{resizedPath}"), width, 0));
		}

		// Thumbnail
		variants.Add(new ImageVariant("Thumbnail", new Uri($"{baseUrl}/thumbnail/{basePath}"), ThumbnailWidth, 0));

		return variants;
	}

	private static string GetPathWithSizeSuffix(string path, uint width)
	{
		var extension = Path.GetExtension(path);
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		var directory = Path.GetDirectoryName(path) ?? "";
		return Path.Combine(directory, $"{fileNameWithoutExtension}-{width}{extension}").Replace("\\", "/");
	}
}
