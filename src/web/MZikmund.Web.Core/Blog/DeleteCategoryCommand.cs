using MediatR;

namespace MZikmund.Web.Core.Blog;

public record DeleteCategoryCommand(Guid CategoryId) : IRequest;
