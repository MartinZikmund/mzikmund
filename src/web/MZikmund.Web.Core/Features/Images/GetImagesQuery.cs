using MediatR;

namespace MZikmund.Web.Core.Features.Images;

public record GetImagesQuery : IRequest<IEnumerable<BlobInfo>>;
