using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Blog;

public record UpdatePostCommand(Guid Id, Post UpdatedPost) : IRequest<Post>;

