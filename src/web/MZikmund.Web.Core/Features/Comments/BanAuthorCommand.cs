using MediatR;

namespace MZikmund.Web.Core.Features.Comments;

public record BanAuthorCommand(Guid AuthorId) : IRequest;
