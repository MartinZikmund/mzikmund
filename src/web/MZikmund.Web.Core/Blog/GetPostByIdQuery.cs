using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public record GetPostByIdQuery(Guid Id) : IRequest<Post>;
