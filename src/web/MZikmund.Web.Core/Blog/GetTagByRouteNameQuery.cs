using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetTagByRouteNameQuery(string RouteName) : IRequest<Tag>;
