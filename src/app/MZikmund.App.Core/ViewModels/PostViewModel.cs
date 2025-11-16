using Microsoft.Extensions.Options;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blog;

namespace MZikmund.ViewModels;

public class PostViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IOptions<AppConfig> _appConfig;

	public PostViewModel(IMZikmundApi api, IOptions<AppConfig> appConfig)
	{
		_api = api;
		_appConfig = appConfig;
	}

	public override async void ViewNavigatedTo(object? parameter)
	{
		var postId = (Guid)parameter!;
		var postResponse = await _api.GetPostAsync(postId);
		Post = postResponse.Content!;
		
		// Get the base URL from configuration
		var baseUrl = _appConfig.Value.ApiUrl?.Replace("/api", "") ?? "https://mzikmund.dev";
		EmbedUrl = $"{baseUrl}/embed/post/{postId}";
		OnPropertyChanged(nameof(EmbedUrl));
	}

	public Post? Post { get; private set; }

	public string EmbedUrl { get; private set; } = "";
}
