using MediatR;
using MZikmund.Web.Core.Features.Images;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class GetImagesHandler : IRequestHandler<GetImagesQuery, IEnumerable<BlobInfo>>
{
	private readonly IBlobStorage _blobStorage;

	public GetImagesHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task<IEnumerable<BlobInfo>> Handle(GetImagesQuery request, CancellationToken cancellationToken)
	{
		var thumbnails = await _blobStorage.ListAsync(BlobKind.Image, "thumbnail");
		// Remove the prefix "thumbnail/" from the blob names
		var images = thumbnails.Select(blob => new BlobInfo(blob.BlobPath.Replace("thumbnail/", string.Empty), blob.LastModified));
		return images;
	}
}
