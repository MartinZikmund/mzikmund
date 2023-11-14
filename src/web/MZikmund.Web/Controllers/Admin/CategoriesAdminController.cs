﻿using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.DataContracts.Blog;
using MZikmund.Logic.Requests.Blog.Categories;
using MZikmund.Web.Core.Blog;

namespace MZikmund.Web.Controllers.Api.Admin.Blog;

/// <summary>
/// Represents admin operations for blog categories.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/categories")]
public class BlogCategoriesAdminController : Controller
{
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="BlogCategoriesAdminController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	public BlogCategoriesAdminController(IMediator mediator) =>
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
	public async Task<IActionResult> Update(Guid categoryId, [FromBody] EditCategory updatedCategory)
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
