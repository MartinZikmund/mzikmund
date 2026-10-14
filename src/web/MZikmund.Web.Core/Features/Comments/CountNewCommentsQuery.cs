using MediatR;

namespace MZikmund.Web.Core.Features.Comments;

public record CountNewCommentsQuery(DateTime? Since) : IRequest<int>;
