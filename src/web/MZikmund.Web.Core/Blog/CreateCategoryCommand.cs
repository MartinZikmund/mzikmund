using System.ComponentModel.DataAnnotations;
using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public class CreateCategoryCommand : IRequest<Category>
{
	[Required]
	[Display(Name = "Display Name")]
	[MaxLength(64)]
	public string DisplayName { get; set; } = "";

	[Required]
	[Display(Name = "Route Name")]
	[RegularExpression("(?!-)([a-z0-9-]+)")]
	[MaxLength(64)]
	public string RouteName { get; set; } = "";

	[Required]
	[Display(Name = "Description")]
	[MaxLength(256)]
	public string Description { get; set; } = "";
}
