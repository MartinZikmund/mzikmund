using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Extensions;

namespace MZikmund.Web.Core.Features.Files;

public class GetFilesHandler : IRequestHandler<GetFilesQuery, PagedResponse<StorageItemInfo>>
{
	private readonly DatabaseContext _dbContext;
	private readonly IBlobUrlProvider _blobUrlProvider;

	public GetFilesHandler(DatabaseContext dbContext, IBlobUrlProvider blobUrlProvider)
	{
		_dbContext = dbContext;
		_blobUrlProvider = blobUrlProvider;
	}

	public async Task<PagedResponse<StorageItemInfo>> Handle(GetFilesQuery request, CancellationToken cancellationToken)
	{
		if (request.PageNumber < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(request.PageNumber), "Page number must be at least 1.");
		}

		if (request.PageSize < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(request.PageSize), "Page size must be at least 1.");
		}

		var query = _dbContext.BlobMetadata.Where(b => b.Kind == MZikmund.Web.Data.Entities.BlobKind.File);

		var totalCount = await query.CountAsync(cancellationToken);

		var skip = (request.PageNumber - 1) * request.PageSize;
		var entities = await query
			.OrderByDescending(b => b.LastModified)
			.Skip(skip)
			.Take(request.PageSize)
			.ToArrayAsync(cancellationToken);

		var items = entities.Select(b => new StorageItemInfo(
			b.BlobPath,
			_blobUrlProvider.GetUrl(b.Kind, b.BlobPath),
			b.LastModified,
			b.Size)).ToArray();

		return new PagedResponse<StorageItemInfo>(items, request.PageNumber, request.PageSize, totalCount);
	}
}
