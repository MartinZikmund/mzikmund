using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetTagByIdQuery(Guid TagId) : IRequest<Tag>;
