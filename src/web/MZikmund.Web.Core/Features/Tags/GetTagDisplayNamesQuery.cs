using MediatR;

namespace MZikmund.Web.Core.Features.Tags;

public record GetTagDisplayNamesQuery : IRequest<IReadOnlyList<string>>;
