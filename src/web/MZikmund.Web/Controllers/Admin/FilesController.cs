using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Represents operations for generic files.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/files")]
public class FilesController
{
}
