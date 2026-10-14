using MediatR;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Infrastructure;

namespace MZikmund.Web.Core.Features.Comments;

public record ReportCommentCommand(Guid CommentId, CommentReportReason Reason, string? Note) : IRequest, IAuthoredRequest;
