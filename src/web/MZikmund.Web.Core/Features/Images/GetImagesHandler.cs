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
		var (thumbnails, totalCount) = await _blobStorage.ListPagedAsync(BlobKind.Image, request.PageNumber, request.PageSize, "thumbnail");
		
		// Remove the prefix "thumbnail/" from the blob names
		var images = thumbnails
			.Select(blob => new StorageItemInfo(blob.BlobPath.Replace("thumbnail/", string.Empty), blob.LastModified))
			.ToArray();

		return new PagedResponse<StorageItemInfo>(images, request.PageNumber, request.PageSize, totalCount);
	}
}
