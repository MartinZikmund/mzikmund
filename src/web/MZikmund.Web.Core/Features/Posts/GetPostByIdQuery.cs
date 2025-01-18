using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Posts;

public record GetPostByIdQuery(Guid Id) : IRequest<Post>;
