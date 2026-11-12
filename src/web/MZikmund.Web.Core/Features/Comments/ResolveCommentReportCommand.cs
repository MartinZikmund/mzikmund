using MediatR;

namespace MZikmund.Web.Core.Features.Comments;

public record ResolveCommentReportCommand(Guid CommentId) : IRequest;
