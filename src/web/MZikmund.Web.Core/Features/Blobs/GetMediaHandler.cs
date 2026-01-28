using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;
using MZikmund.DataContracts.Storage;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Extensions;

namespace MZikmund.Web.Core.Features.Blobs;

public class GetMediaHandler : IRequestHandler<GetMediaQuery, PagedResponse<StorageItemInfo>>
{
	private readonly DatabaseContext _dbContext;
	private readonly IBlobUrlProvider _blobUrlProvider;

	public GetMediaHandler(DatabaseContext dbContext, IBlobUrlProvider blobUrlProvider)
	{
		_dbContext = dbContext;
		_blobUrlProvider = blobUrlProvider;
	}

	public async Task<PagedResponse<StorageItemInfo>> Handle(GetMediaQuery request, CancellationToken cancellationToken)
	{
		if (request.PageNumber < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(request.PageNumber), "Page number must be at least 1.");
		}

		if (request.PageSize < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(request.PageSize), "Page size must be at least 1.");
		}

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
			.Select(b => new StorageItemInfo(b.BlobPath, _blobUrlProvider.GetUrl(b.Kind, b.Kind != BlobKind.Image ? b.BlobPath : $"thumbnail/{b.BlobPath}"), b.LastModified, b.Size))
			.ToArrayAsync(cancellationToken);

		return new PagedResponse<StorageItemInfo>(items, request.PageNumber, request.PageSize, totalCount);
	}
}
