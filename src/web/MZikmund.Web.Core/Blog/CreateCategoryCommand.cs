using System.ComponentModel.DataAnnotations;
using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public class CreateCategoryCommand(Category NewCategory) : IRequest<Category>;
