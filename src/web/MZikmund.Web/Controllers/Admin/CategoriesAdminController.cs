using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Categories;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents admin operations for blog categories.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/categories")]
public class CategoriesAdminController : Controller
{
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="CategoriesAdminController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	public CategoriesAdminController(IMediator mediator) =>
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

	/// <summary>
	/// Creates a blog category.
	/// </summary>
	/// <param name="newCategory">Blog category to create.</param>
	/// <returns>Created blog category.</returns>
	[HttpPost]
	[Route("")]
	public async Task<IActionResult> Add([FromBody] Category newCategory) =>
		Ok(await _mediator.Send(new CreateCategoryCommand(newCategory)));

	/// <summary>
	/// Updates a given blog category.
	/// </summary>
	/// <param name="categoryId">Category ID.</param>
	/// <param name="blogCategory">Blog category.</param>
	/// <returns>Updated category.</returns>
	[HttpPut]
	[Route("{categoryId}")]
	public async Task<IActionResult> Update(Guid categoryId, [FromBody] Category updatedCategory)
	{
		return Ok(await _mediator.Send(new UpdateCategoryCommand(categoryId, updatedCategory)));
	}

	/// <summary>
	/// Deletes a given blog category.
	/// </summary>
	/// <param name="categoryId">Category ID.</param>
	/// <returns>No content, when successful.</returns>
	[HttpDelete]
	[Route("{categoryId}")]
	public async Task<IActionResult> Delete(Guid categoryId)
	{
		await _mediator.Send(new DeleteCategoryCommand(categoryId));
		return NoContent();
	}
}
