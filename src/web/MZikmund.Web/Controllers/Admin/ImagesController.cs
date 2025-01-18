using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents operations for images.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/images")]
public class ImagesController
{
	[HttpGet]
	[Route("{id}")]
	public async Task<IActionResult> GetById(Guid id)
	{

	}
}
