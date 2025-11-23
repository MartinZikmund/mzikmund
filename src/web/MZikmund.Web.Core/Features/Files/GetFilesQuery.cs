using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public record GetFilesQuery(int PageNumber, int PageSize) : IRequest<PagedResponse<StorageItemInfo>>;
