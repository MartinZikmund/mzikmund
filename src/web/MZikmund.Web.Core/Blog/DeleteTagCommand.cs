using MediatR;

namespace MZikmund.Web.Core.Blog;

public record DeleteTagCommand(Guid TagId) : IRequest;
