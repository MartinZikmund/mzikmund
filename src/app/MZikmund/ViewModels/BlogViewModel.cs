using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels;

public class BlogViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILoadingIndicator _loadingIndicator;

	public BlogViewModel(IMZikmundApi api, ILoadingIndicator loadingIndicator)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_loadingIndicator = loadingIndicator;
	}

	public override string Title => Localizer.Instance.GetString("Blog");

	public ObservableCollection<PostListItem> Posts { get; } = new ObservableCollection<PostListItem>();

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		using var _ = _loadingIndicator.BeginLoading();
		Posts.Clear();
		var posts = await _api.GetPostsAsync();
		foreach (var post in posts.Content!.Data)
		{
			Posts.Add(post);
		}
	}
}
