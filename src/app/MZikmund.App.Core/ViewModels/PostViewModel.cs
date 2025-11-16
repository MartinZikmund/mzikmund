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
		var baseUrl = _appConfig.Value.WebUrl;
		if (baseUrl is null)
		{
			throw new InvalidOperationException("WebUrl is not set in configuration");
		}
		EmbedUrl = new Uri(new Uri(baseUrl), $"{baseUrl}embed/post/{postId}");
		OnPropertyChanged(nameof(EmbedUrl));
	}

	public Post? Post { get; private set; }

	public Uri? EmbedUrl { get; private set; }
}
