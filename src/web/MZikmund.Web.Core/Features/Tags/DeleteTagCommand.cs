using MediatR;

namespace MZikmund.Web.Core.Features.Tags;

public record DeleteTagCommand(Guid TagId) : IRequest;
