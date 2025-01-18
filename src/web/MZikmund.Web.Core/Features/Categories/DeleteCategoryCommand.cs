using MediatR;

namespace MZikmund.Web.Core.Features.Categories;

public record DeleteCategoryCommand(Guid CategoryId) : IRequest;
