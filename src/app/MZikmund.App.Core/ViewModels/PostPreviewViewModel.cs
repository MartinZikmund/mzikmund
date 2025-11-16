using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public class PostPreviewViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IPostContentProcessor _postContentProcessor;

	public PostPreviewViewModel(IMZikmundApi api, IPostContentProcessor postContentProcessor)
	{
		_api = api;
		_postContentProcessor = postContentProcessor;
	}

	public override async void ViewNavigatedTo(object? parameter)
	{
		var postId = (Guid)parameter!;
		var postResponse = await _api.GetPostAsync(postId);
		Post = postResponse.Content!;
		HtmlPreview = await _postContentProcessor.ProcessAsync(Post.Content);
		OnPropertyChanged(nameof(HtmlPreview));
	}

	public Post? Post { get; private set; }

	public string HtmlPreview { get; private set; } = "";
}
