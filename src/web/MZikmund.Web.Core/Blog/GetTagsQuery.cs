using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetTagsQuery : IRequest<IReadOnlyList<Tag>>;
