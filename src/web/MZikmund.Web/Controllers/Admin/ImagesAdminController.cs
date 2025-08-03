using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Images;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents operations for image files.
/// </summary>
[ApiController]
#if !DEBUG
[Authorize]
#endif
[Route("api/v1/admin/images")]
public class ImagesAdminController : Controller
{
	private readonly IMediator _mediator;

	public ImagesAdminController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetImagesQuery()));

	[HttpGet("{fileName}")]
	public async Task<IActionResult> GetSizes(string fileName)
	{
		throw new NotImplementedException("This method is not implemented yet.");
	}

	[HttpPost]
	public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string fileName)
	{
		if (file == null || file.Length == 0)
		{
			return BadRequest("File is empty");
		}

		var name = string.IsNullOrEmpty(fileName) ? file.FileName : fileName;

		await using var stream = file.OpenReadStream();
		var result = await _mediator.Send(new UploadImageCommand(file.FileName, stream));
		return Ok(result);
	}
}
