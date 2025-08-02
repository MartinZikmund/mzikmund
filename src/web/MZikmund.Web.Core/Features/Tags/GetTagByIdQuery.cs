using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Tags;

public record GetTagByIdQuery(Guid TagId) : IRequest<Tag>;
