using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Files;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents operations for generic files.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/files")]
public class FilesAdminController : Controller
{
	private readonly IMediator _mediator;

	public FilesAdminController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetFilesQuery()));

	[HttpDelete]
	[Route("{path}")]
	public async Task<IActionResult> Delete(string path)
	{
		await _mediator.Send(new DeleteFileCommand(path));
		return NoContent();
	}

	[HttpPost]
	public async Task<IActionResult> Upload(IFormFile file)
	{
		if (file == null || file.Length == 0)
		{
			return BadRequest("File is empty");
		}

		await using var stream = file.OpenReadStream();
		var result = await _mediator.Send(new UploadFileCommand(file.FileName, stream));
		return Ok(result);
	}
}
