using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

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
