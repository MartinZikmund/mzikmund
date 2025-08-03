using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Tags;

/// <summary>
/// Represents a request to update a category.
/// </summary>
public record UpdateTagCommand(Guid TagId, Tag UpdatedTag) : IRequest<Tag>;
