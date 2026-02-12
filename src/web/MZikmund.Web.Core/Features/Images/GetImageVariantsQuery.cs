using MediatR;
using MZikmund.DataContracts.Blobs;

namespace MZikmund.Web.Core.Features.Images;

public record GetImageVariantsQuery(string ImagePath) : IRequest<List<ImageVariant>>;
