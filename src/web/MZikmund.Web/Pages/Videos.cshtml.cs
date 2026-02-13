using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MZikmund.DataContracts.Videos;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Features.Videos.Queries;

namespace MZikmund.Web.Pages;

public class VideosModel : PageModel
{
	private readonly IMediator _mediator;
	private readonly IOptions<YouTubeOptions> _options;

	public List<VideoDto>? Videos { get; set; }
	public string YouTubeChannelUrl { get; set; } = "";

	public VideosModel(IMediator mediator, IOptions<YouTubeOptions> options)
	{
		_mediator = mediator;
		_options = options;
	}

	public async Task OnGetAsync()
	{
		YouTubeChannelUrl = _options.Value.ChannelUrl;
		Videos = await _mediator.Send(new GetVideosQuery());
	}
}
