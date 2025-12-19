using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Controllers.Admin;
using MZikmund.Web.Core.Features.Posts;

namespace MZikmund.Web.Controllers;

/// <summary>
/// Represents admin operations for blog posts.
/// </summary>
[ApiController]
[Route("api/v1/posts")]
public class PostsController : Controller
{
	private const int PageSize = 12; // TODO: Make page size configurable.
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="PostsAdminController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	public PostsController(IMediator mediator) =>
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

	/// <summary>
	/// Gets all posts.
	/// </summary>
	/// <returns>All blog posts.</returns>
	[HttpGet]
	[Route("")]
	public async Task<IActionResult> GetAll(int pageNumber = 1) =>
		Ok(await _mediator.Send(new GetPostsQuery(pageNumber, PageSize)));

	/// <summary>
	/// Gets a blog post by ID.
	/// </summary>
	/// <param name="id">Post ID.</param>
	/// <returns>The requested blog post.</returns>
	[HttpGet]
	[Route("{id}")]
	public async Task<IActionResult> GetById(Guid id) =>
		Ok(await _mediator.Send(new GetPostByIdQuery(id)));

	/// <summary>
	/// Gets a blog post by preview token (for preview purposes without authentication).
	/// </summary>
	/// <param name="previewToken">Preview token.</param>
	/// <returns>The requested blog post.</returns>
	[HttpGet]
	[Route("preview/{previewToken}")]
	public async Task<IActionResult> GetByPreviewToken(Guid previewToken) =>
		Ok(await _mediator.Send(new GetPostByPreviewTokenQuery(previewToken)));
}
