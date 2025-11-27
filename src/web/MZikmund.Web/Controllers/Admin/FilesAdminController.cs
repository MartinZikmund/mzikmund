using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Files;

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
	public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50) => 
		Ok(await _mediator.Send(new GetFilesQuery(pageNumber, pageSize)));

	[HttpDelete]
	[Route("{path}")]
	public async Task<IActionResult> Delete(string path)
	{
		var decodedPath = Uri.UnescapeDataString(path);
		await _mediator.Send(new DeleteFileCommand(decodedPath));
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
		var result = await _mediator.Send(new UploadFileCommand(fileName, stream));
		return Ok(result);
	}
}
