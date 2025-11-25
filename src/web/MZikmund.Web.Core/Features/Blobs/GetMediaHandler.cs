using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Blobs;

public class GetMediaHandler : IRequestHandler<GetMediaQuery, PagedResponse<StorageItemInfo>>
{
	private readonly DatabaseContext _dbContext;

	public GetMediaHandler(DatabaseContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<PagedResponse<StorageItemInfo>> Handle(GetMediaQuery request, CancellationToken cancellationToken)
	{
		var query = _dbContext.BlobMetadata.AsQueryable();

		// Apply kind filter
		if (request.Kind.HasValue && request.Kind != BlobKindFilter.All)
		{
			var blobKind = request.Kind.Value == BlobKindFilter.Images
				? BlobKind.Image
				: BlobKind.File;
			query = query.Where(b => b.Kind == blobKind);
		}

		// Apply search filter
		if (!string.IsNullOrWhiteSpace(request.Search))
		{
			query = query.Where(b => b.FileName.Contains(request.Search));
		}

		var totalCount = await query.CountAsync(cancellationToken);

		var skip = (request.PageNumber - 1) * request.PageSize;
		var items = await query
			.OrderByDescending(b => b.LastModified)
			.Skip(skip)
			.Take(request.PageSize)
			.Select(b => new StorageItemInfo(b.BlobPath, b.LastModified))
			.ToArrayAsync(cancellationToken);

		return new PagedResponse<StorageItemInfo>(items, request.PageNumber, request.PageSize, totalCount);
	}
}
