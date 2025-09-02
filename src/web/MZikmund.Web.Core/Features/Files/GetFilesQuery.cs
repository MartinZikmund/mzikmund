using MediatR;
using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Features.Files;

public record GetFilesQuery : IRequest<IEnumerable<StorageItemInfo>>;
