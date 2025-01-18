using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Tags;

public record GetTagsQuery : IRequest<IReadOnlyList<Tag>>;
