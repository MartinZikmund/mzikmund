using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Blog;

public record CreatePostCommand(PostEditModel NewPost) : IRequest<PostEntity>;
