using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Files;
using MZikmund.Web.Core.Features.Images;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents operations for image files.
/// </summary>
[ApiController]
[Authorize(Policy = "AdminPolicy")]
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

	[HttpDelete]
	[Route("{path}")]
	public async Task<IActionResult> Delete(string path)
	{
		await _mediator.Send(new DeleteImageCommand(path));
		return NoContent();
	}

	[HttpPost]
	public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string desiredFileName)
	{
		if (file == null || file.Length == 0)
		{
			return BadRequest("File is empty");
		}

		var fileName = FormFileNameHelper.GetFileName(file, desiredFileName);

		await using var stream = file.OpenReadStream();
		var result = await _mediator.Send(new UploadImageCommand(fileName, stream));
		return Ok(result);
	}
}
