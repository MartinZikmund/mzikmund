using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MZikmund.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/test")]
public class TestController
{
	[HttpGet]
	[Route("")]
	public IActionResult Get() => new OkObjectResult("Hello world!");
}
