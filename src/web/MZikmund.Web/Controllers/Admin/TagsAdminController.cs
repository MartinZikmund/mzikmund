using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Blog;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents admin operations for blog tags.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/tags")]
public class TagsAdminController : Controller
{
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="TagsAdminController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	public TagsAdminController(IMediator mediator) =>
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

	/// <summary>
	/// Creates a blog tag.
	/// </summary>
	/// <param name="newTag">Blog tag to create.</param>
	/// <returns>Created blog tag.</returns>
	[HttpPost]
	[Route("")]
	public async Task<IActionResult> Add([FromBody] Tag newTag) =>
		Ok(await _mediator.Send(new CreateTagCommand(newTag)));

	/// <summary>
	/// Updates a given blog tag.
	/// </summary>
	/// <param name="tagId">Tag ID.</param>
	/// <param name="blogTag">Blog tag.</param>
	/// <returns>Updated tag.</returns>
	[HttpPut]
	[Route("{tagId}")]
	public async Task<IActionResult> Update(Guid tagId, [FromBody] EditTag updatedTag)
	{
		return Ok(await _mediator.Send(new UpdateTagCommand(tagId, updatedTag)));
	}

	/// <summary>
	/// Deletes a given blog tag.
	/// </summary>
	/// <param name="tagId">Tag ID.</param>
	/// <returns>No content, when successful.</returns>
	[HttpDelete]
	[Route("{tagId}")]
	public async Task<IActionResult> Delete(Guid tagId)
	{
		await _mediator.Send(new DeleteTagCommand(tagId));
		return NoContent();
	}
}
