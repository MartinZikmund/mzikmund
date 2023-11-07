using MediatR;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Blog;

public record UpdatePostCommand(Guid Id, PostEditModel UpdatedPost) : IRequest<PostEntity>;

