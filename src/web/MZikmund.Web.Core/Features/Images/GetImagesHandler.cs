using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Extensions;

namespace MZikmund.Web.Core.Features.Images;

public class GetImagesHandler : IRequestHandler<GetImagesQuery, PagedResponse<StorageItemInfo>>
{
	private readonly DatabaseContext _dbContext;

	public GetImagesHandler(DatabaseContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<PagedResponse<StorageItemInfo>> Handle(GetImagesQuery request, CancellationToken cancellationToken)
	{
		var query = _dbContext.BlobMetadata.Where(b => b.Kind == MZikmund.Web.Data.Entities.BlobKind.Image);

		var totalCount = await query.CountAsync(cancellationToken);

		var skip = (request.PageNumber - 1) * request.PageSize;
		var items = await query
			.OrderByDescending(b => b.LastModified)
			.Skip(skip)
			.Take(request.PageSize)
			.Select(b => new StorageItemInfo(b.BlobPath, b.Kind.ToStorageItemType(), b.LastModified))
			.ToArrayAsync(cancellationToken);

		return new PagedResponse<StorageItemInfo>(items, request.PageNumber, request.PageSize, totalCount);
	}
}
