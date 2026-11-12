using MediatR;
using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Core.Features.Comments;

public record GetMyCommentProfileQuery : IRequest<CommentProfile>;
