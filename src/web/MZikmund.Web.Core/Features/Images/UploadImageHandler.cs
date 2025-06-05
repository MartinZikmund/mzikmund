using ImageMagick;
using MediatR;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Images;
public class UploadImageHandler : IRequestHandler<UploadImageCommand, BlobInfo>
{
	private readonly IBlobStorage _blobStorage;
	private static int[] ResizeWidths = { 1200, 1000, 800, 400 };

	public UploadImageHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task<BlobInfo> Handle(UploadImageCommand request, CancellationToken cancellationToken)
	{
		List<BlobInfo> uploadedFiles = new List<BlobInfo>();
		var isGif = request.FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);

		var stream = new MemoryStream();
		await request.Stream.CopyToAsync(stream);

		var originalWidth = GetOriginalWidth(stream, isGif);

		uploadedFiles.Add(await UploadAsnc(stream, request.FileName)); // Original size
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

	private static async Task<Stream> ResizeGif(Stream sourceStream, int width)
	{
		using var gif = new MagickImageCollection(sourceStream);

		// This will remove the optimization and change the image to how it looks at that point
		// during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
		gif.Coalesce();

		// Resize each image in the collection to a width of 200. When zero is specified for the height
		// the height will be calculated with the aspect ratio.
		foreach (var image in gif)
		{
			image.Resize(200, 0);
		}

		var outputStream = new MemoryStream();
		await gif.WriteAsync(outputStream, MagickFormat.Gif);
		outputStream.Position = 0;
		return outputStream;
	}

	private static async Task<Stream> ResizeImageAsync(Stream stream, string extension)
	{
		using var image = new MagickImage(stream);
		image.Resize(new MagickGeometry(1024, 1024)
		{
			IgnoreAspectRatio = true
		});
		var outputStream = new MemoryStream();
		await image.WriteAsync(outputStream, GetFormat(extension));
		outputStream.Position = 0;
		return outputStream;
	}
}
