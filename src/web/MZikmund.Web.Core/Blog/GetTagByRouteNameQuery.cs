using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public record GetTagByRouteNameQuery(string RouteName) : IRequest<Tag>;
