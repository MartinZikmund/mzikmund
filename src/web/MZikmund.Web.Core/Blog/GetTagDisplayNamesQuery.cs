using MediatR;

namespace MZikmund.Web.Core.Blog;

public record GetTagDisplayNamesQuery : IRequest<IReadOnlyList<string>>;
