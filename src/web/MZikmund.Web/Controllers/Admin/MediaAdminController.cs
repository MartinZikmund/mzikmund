using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.DataContracts.Blobs;
using MZikmund.Web.Core.Features.Blobs;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents operations for media (blobs).
/// </summary>
[ApiController]
[Authorize(Policy = "AdminPolicy")]
[Route("api/v1/admin/media")]
public class MediaAdminController : Controller
{
	private readonly IMediator _mediator;

	public MediaAdminController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll(
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 50,
		[FromQuery] BlobKindFilter? kind = null,
		[FromQuery] string? search = null) =>
		Ok(await _mediator.Send(new GetMediaQuery(pageNumber, pageSize, kind, search)));
}
