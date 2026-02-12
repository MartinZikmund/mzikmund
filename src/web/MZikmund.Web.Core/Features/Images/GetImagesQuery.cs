using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Features.Images;

public record GetImagesQuery(int PageNumber, int PageSize) : IRequest<PagedResponse<StorageItemInfo>>;
