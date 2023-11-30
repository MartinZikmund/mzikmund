using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetPostByIdQuery(Guid Id) : IRequest<Post>;
