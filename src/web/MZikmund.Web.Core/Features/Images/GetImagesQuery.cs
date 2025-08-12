using MediatR;
using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Features.Images;

public record GetImagesQuery : IRequest<IEnumerable<StorageItemInfo>>;
