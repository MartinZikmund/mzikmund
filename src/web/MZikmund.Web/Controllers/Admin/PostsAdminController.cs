using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Posts;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents admin operations for blog posts.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/posts")]
public class PostsAdminController : Controller
{
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="PostsAdminController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	public PostsAdminController(IMediator mediator) =>
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

	/// <summary>
	/// Creates a blog post.
	/// </summary>
	/// <param name="newPost">Blog post to create.</param>
	/// <returns>Created blog post.</returns>
	[HttpPost]
	[Route("")]
	public async Task<Post> Add([FromBody] Post newPost) =>
		await _mediator.Send(new CreatePostCommand(newPost));

	/// <summary>
	/// Updates a given blog post.
	/// </summary>
	/// <param name="postId">Post ID.</param>
	/// <param name="blogPost">Blog post.</param>
	/// <returns>Updated post.</returns>
	[HttpPut]
	[Route("{postId}")]
	public async Task<IActionResult> Update(Guid postId, [FromBody] Post updatedPost)
	{
		return Ok(await _mediator.Send(new UpdatePostCommand(postId, updatedPost)));
	}

	/// <summary>
	/// Deletes a given blog post.
	/// </summary>
	/// <param name="postId">Post ID.</param>
	/// <returns>No content, when successful.</returns>
	[HttpDelete]
	[Route("{postId}")]
	public async Task<IActionResult> Delete(Guid postId)
	{
		await _mediator.Send(new DeletePostCommand(postId, true));
		return NoContent();
	}
}
