using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Features.Images;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public class GetImagesHandler : IRequestHandler<GetImagesQuery, PagedResponse<StorageItemInfo>>
{
	private readonly IBlobStorage _blobStorage;

	public GetImagesHandler(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	public async Task<PagedResponse<StorageItemInfo>> Handle(GetImagesQuery request, CancellationToken cancellationToken)
	{
		var thumbnails = await _blobStorage.ListAsync(BlobKind.Image, "thumbnail");
		// Remove the prefix "thumbnail/" from the blob names
		var allImages = thumbnails
			.Select(blob => new StorageItemInfo(blob.BlobPath.Replace("thumbnail/", string.Empty), blob.LastModified))
			.OrderByDescending(x => x.LastModified)
			.ToList();

		var totalCount = allImages.Count;
		var skip = (request.PageNumber - 1) * request.PageSize;
		var pagedImages = allImages.Skip(skip).Take(request.PageSize);

		return new PagedResponse<StorageItemInfo>(pagedImages, request.PageNumber, request.PageSize, totalCount);
	}
}
