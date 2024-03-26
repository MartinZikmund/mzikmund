using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public class BlogViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly INavigationService _navigationService;

	public BlogViewModel(
		IMZikmundApi api,
		ILoadingIndicator loadingIndicator,
		IMarkdownConverter markdownConverter,
		INavigationService navigationService)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_loadingIndicator = loadingIndicator;
		_markdownConverter = markdownConverter;
		_navigationService = navigationService;
	}

	public override string Title => Localizer.Instance.GetString("Blog");

	public ObservableCollection<PostListItemViewModel> Posts { get; } = new();

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		using var _ = _loadingIndicator.BeginLoading();
		Posts.Clear();
		var posts = await _api.GetPostsAsync();
		foreach (var post in posts.Content!.Data)
		{
			Posts.Add(new(post, _markdownConverter));
		}
	}

	public void ItemClicked(object sender, ItemClickEventArgs args)
	{
		var item = (PostListItemViewModel)args.ClickedItem;
		_navigationService.Navigate<PostViewModel>(item.Item.Id);
	}
}
