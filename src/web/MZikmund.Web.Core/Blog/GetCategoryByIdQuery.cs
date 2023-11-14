using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetCategoryByIdQuery(Guid CategoryId) : IRequest<Category>;
