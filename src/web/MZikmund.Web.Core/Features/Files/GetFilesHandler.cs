using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Extensions;

namespace MZikmund.Web.Core.Features.Files;

public class GetFilesHandler : IRequestHandler<GetFilesQuery, PagedResponse<StorageItemInfo>>
{
	private readonly DatabaseContext _dbContext;

	public GetFilesHandler(DatabaseContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<PagedResponse<StorageItemInfo>> Handle(GetFilesQuery request, CancellationToken cancellationToken)
	{
		var query = _dbContext.BlobMetadata.Where(b => b.Kind == MZikmund.Web.Data.Entities.BlobKind.File);

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
