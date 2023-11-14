using MediatR;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Blog;

public record CreatePostCommand(PostEditModel NewPost) : IRequest<PostEntity>;
