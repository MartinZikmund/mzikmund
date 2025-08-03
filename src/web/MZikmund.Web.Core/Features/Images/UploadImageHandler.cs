using ImageMagick;
using MediatR;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Images;

public class UploadImageHandler : IRequestHandler<UploadImageCommand, BlobInfo>
{
	private const string OriginalPathPrefix = "original";
	private const string ResizedPathPrefix = "resized";
	private const string ThumbnailPathPrefix = "thumbnail";
	private const uint ThumbnailWidth = 200;

	private readonly IBlobStorage _blobStorage;
	private readonly IBlobPathGenerator _blobPathGenerator;
	private static readonly uint[] ResizeWidths = { 1200, 1000, 800, 400 };

	public UploadImageHandler(IBlobStorage blobStorage, IBlobPathGenerator blobPathGenerator)
	{
		_blobStorage = blobStorage;
		_blobPathGenerator = blobPathGenerator;
	}

	public async Task<BlobInfo> Handle(UploadImageCommand request, CancellationToken cancellationToken)
	{
		var path = _blobPathGenerator.GenerateBlobPath(request.FileName);

		var uploadedBlobs = new List<BlobInfo>();
		var isGif = request.FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);

		var stream = new MemoryStream();
		await request.Stream.CopyToAsync(stream);

		stream.Position = 0;
		var originalWidth = GetOriginalWidth(stream, isGif);

		uploadedBlobs.Add(await UploadAsnc(stream, Path.Combine(OriginalPathPrefix, path))); // Original size

		foreach (var resizeWidth in ResizeWidths)
		{
			if (originalWidth > resizeWidth && !cancellationToken.IsCancellationRequested)
			{
				stream.Position = 0; // Reset stream position
				using var resizedStream = isGif ? await ResizeGif(stream, resizeWidth, cancellationToken) : await ResizeImageAsync(stream, resizeWidth, cancellationToken);
				var resizedFileName = Path.Combine(ResizedPathPrefix, GetPathWithSizeSuffix(path, resizeWidth));
				uploadedBlobs.Add(await UploadAsnc(resizedStream, resizedFileName));
			}
		}

		// Create thumbnail
		stream.Position = 0; // Reset stream position
		using var thumbnailStream = isGif ? await ResizeGif(stream, ThumbnailWidth, cancellationToken) : await ResizeImageAsync(stream, ThumbnailWidth, cancellationToken);
		var thumbnailFileName = Path.Combine(ThumbnailPathPrefix, path);
		uploadedBlobs.Add(await UploadAsnc(thumbnailStream, thumbnailFileName));

		return new BlobInfo(path, uploadedBlobs.Last().LastModified);
	}

	private string GetPathWithSizeSuffix(string path, uint width)
	{
		var extension = Path.GetExtension(path);
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		var directory = Path.GetDirectoryName(path) ?? "";
		return Path.Combine(directory, $"{fileNameWithoutExtension}-{width}{extension}");
	}

	private uint GetOriginalWidth(Stream stream, bool isGif)
	{
		if (!isGif)
		{
			using var image = new MagickImage(stream);
			return image.Width;
		}
		else
		{
			using var gif = new MagickImageCollection(stream);
			return gif[0].Width;
		}
	}

	private async Task<BlobInfo> UploadAsnc(Stream stream, string fileName) => await _blobStorage.AddAsync(BlobKind.Image, fileName, stream);

	private static async Task<Stream> ResizeGif(Stream sourceStream, uint width, CancellationToken cancellationToken)
	{
		using var gif = new MagickImageCollection(sourceStream);

		// This will remove the optimization and change the image to how it looks at that point
		// during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
		gif.Coalesce();

		// Resize each image in the collection to a width of 200. When zero is specified for the height
		// the height will be calculated with the aspect ratio.
		foreach (var image in gif)
		{
			image.Resize(width, 0);
		}

		var outputStream = new MemoryStream();
		await gif.WriteAsync(outputStream, MagickFormat.Gif, cancellationToken);
		outputStream.Position = 0;
		return outputStream;
	}

	private static async Task<Stream> ResizeImageAsync(Stream stream, uint width, CancellationToken cancellationToken)
	{
		using var image = new MagickImage(stream);
		image.Resize(width, 0);
		var outputStream = new MemoryStream();
		await image.WriteAsync(outputStream, cancellationToken);
		outputStream.Position = 0;
		return outputStream;
	}
}
