using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Tags;

public record GetTagByRouteNameQuery(string RouteName) : IRequest<Tag>;
