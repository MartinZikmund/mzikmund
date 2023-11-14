using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Controllers.Admin;
using MZikmund.Web.Core.Blog;

namespace MZikmund.Web.Controllers;

/// <summary>
/// Represents admin operations for blog tags.
/// </summary>
[ApiController]
[Route("api/v1/tags")]
public class TagsController : Controller
{
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="TagsAdminController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	public TagsController(IMediator mediator) =>
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

	/// <summary>
	/// Gets all tags.
	/// </summary>
	/// <returns>All blog tags.</returns>
	[HttpGet]
	[Route("")]
	public async Task<IActionResult> GetAll() =>
		Ok(await _mediator.Send(new GetTagsQuery()));

	/// <summary>
	/// Creates a blog tag.
	/// </summary>
	/// <param name="newTag">Blog tag to create.</param>
	/// <returns>Created blog tag.</returns>
	[HttpGet]
	[Route("{id}")]
	public async Task<IActionResult> GetById(Guid id) =>
		Ok(await _mediator.Send(new GetTagByIdQuery(id)));
}
