using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Controllers.Admin;
using MZikmund.Web.Core.Blog;

namespace MZikmund.Web.Controllers;

/// <summary>
/// Represents admin operations for blog categories.
/// </summary>
[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : Controller
{
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="CategoriesAdminController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	public CategoriesController(IMediator mediator) =>
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

	/// <summary>
	/// Gets all categories.
	/// </summary>
	/// <returns>All blog categories.</returns>
	[HttpGet]
	[Route("")]
	public async Task<IActionResult> GetAll() =>
		Ok(await _mediator.Send(new GetCategoriesQuery()));

	/// <summary>
	/// Creates a blog category.
	/// </summary>
	/// <param name="newCategory">Blog category to create.</param>
	/// <returns>Created blog category.</returns>
	[HttpGet]
	[Route("{id}")]
	public async Task<IActionResult> GetById(Guid id) =>
		Ok(await _mediator.Send(new GetCategoryByIdQuery(id)));
}
