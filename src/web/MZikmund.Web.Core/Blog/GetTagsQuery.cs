using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public record GetTagsQuery : IRequest<IReadOnlyList<Tag>>;
