using MediatR;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Features.Images;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class GetImagesHandler : IRequestHandler<GetImagesQuery, IEnumerable<StorageItemInfo>>
{
	private readonly IBlobStorage _blobStorage;

	public GetImagesHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task<IEnumerable<StorageItemInfo>> Handle(GetImagesQuery request, CancellationToken cancellationToken)
	{
		var thumbnails = await _blobStorage.ListAsync(BlobKind.Image, "thumbnail");
		// Remove the prefix "thumbnail/" from the blob names
		var images = thumbnails.Select(blob => new StorageItemInfo(blob.BlobPath.Replace("thumbnail/", string.Empty), blob.LastModified));
		return images;
	}
}
