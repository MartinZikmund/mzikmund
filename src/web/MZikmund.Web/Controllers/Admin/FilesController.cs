using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents operations for generic files.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/files")]
public class FilesController : Controller
{
	private readonly IBlobStorage _blobStorage;

	public FilesController(IBlobStorage blobStorage)
	{
		_blobStorage = blobStorage;
	}

	[HttpPost("upload")]
	public async Task<IActionResult> UploadBlob([FromForm] IFormFile file)
	{
		if (file == null || file.Length == 0)
			return BadRequest("File is empty");

		await using var stream = file.OpenReadStream();
		var url = await _blobStorage.AddAsync(file.FileName, stream);
		return Ok(new { Url = url });
	}
}
