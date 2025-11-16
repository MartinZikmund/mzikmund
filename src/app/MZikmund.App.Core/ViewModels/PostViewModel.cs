using Microsoft.Extensions.Options;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public class PostViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly AppConfig _appConfig;

	public PostViewModel(IMZikmundApi api, IOptions<AppConfig> appConfig)
	{
		_api = api;
		_appConfig = appConfig.Value;
	}

	public override async void ViewNavigatedTo(object? parameter)
	{
		var postId = (Guid)parameter!;
		var postResponse = await _api.GetPostAsync(postId);
		Post = postResponse.Content!;
		
		// Build the chromeless URL by extracting base URL from ApiUrl
		var baseUrl = _appConfig.ApiUrl?.Replace("/api", "") ?? "https://mzikmund.dev";
		ChromelessUrl = $"{baseUrl}/Blog/Chromeless/{postId}";
	}

	public Post? Post { get; private set; }

	public string ChromelessUrl { get; private set; } = "";
}
