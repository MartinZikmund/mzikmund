using Microsoft.UI.Dispatching;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Timers;
using MZikmund.Shared.Extensions;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels.Admin;

public partial class PostEditorViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IPostContentProcessor _postContentProcessor;
	private readonly ITimerFactory _timerFactory;
	private DispatcherQueueTimer? _previewTimer;
	private bool _isPreviewDirty = true;

	public PostEditorViewModel(IMZikmundApi api, IDialogService dialogService, ILoadingIndicator loadingIndicator, IPostContentProcessor postContentProcessor, ITimerFactory timerFactory)
	{
		_api = api;
		_dialogService = dialogService;
		_loadingIndicator = loadingIndicator;
		_postContentProcessor = postContentProcessor;
		_timerFactory = timerFactory;
	}

	public override string Title => Post?.Title ?? "";

	[ObservableProperty]
	public partial string PostContent { get; set; } = "";

	[ObservableProperty]
	public partial string PostRouteName { get; set; } = "";

	[ObservableProperty]
	public partial string Tags { get; set; } = "";

	[ObservableProperty]
	public partial string PostTitle { get; set; } = "";

	[ObservableProperty]
	public partial string HtmlPreview { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CategoriesText))]
	public partial Category[] Categories { get; set; } = Array.Empty<Category>();

	[ObservableProperty]
	public partial Post? Post { get; set; } = null;

	[ObservableProperty]
	public partial bool IsPublished { get; set; }

	partial void OnPostTitleChanged(string value)
	{
		PostRouteName = value.GenerateRouteName();
	}

	partial void OnPostContentChanged(string value)
	{
		_isPreviewDirty = true;
	}

	public string CategoriesText => Categories is null or { Length: 0 } ?
		Localizer.Instance.GetString("NoCategoriesSelected") :
		string.Join(", ", Categories.Select(c => c.DisplayName));

	[RelayCommand]
	private async Task PickCategoriesAsync()
	{
		var categoyPickerDialogViewModel = new CategoryPickerDialogViewModel(Categories.Select(c => c.Id).ToArray(), _api);
		var result = await _dialogService.ShowAsync(categoyPickerDialogViewModel);
		if (result == ContentDialogResult.Primary)
		{
			Categories = categoyPickerDialogViewModel.SelectedCategories.ToArray();
		}
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		if (Post is null)
		{
			return;
		}

		var tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(t => new Tag { DisplayName = t.Trim() })
			.ToArray();

		Post.Title = PostTitle;
		Post.RouteName = PostRouteName;
		Post.Tags = tags;
		Post.Categories = Categories;
		Post.IsPublished = IsPublished;
		Post.PublishedDate = Post.PublishedDate ?? DateTimeOffset.UtcNow;
		Post.Content = PostContent;

		if (Post.Id == Guid.Empty)
		{
			await _api.AddPostAsync(Post);
		}
		else
		{
			await _api.UpdatePostAsync(Post.Id, Post);
		}
	}

	public override void ViewLoaded()
	{
		base.ViewLoaded();
		_previewTimer ??= _timerFactory.CreateTimer();
		_previewTimer.Interval = TimeSpan.FromMilliseconds(500);
		_previewTimer.Tick += PreviewTimerOnTick;
		_previewTimer.Start();
	}

	private async void PreviewTimerOnTick(DispatcherQueueTimer sender, object args)
	{
		if (_isPreviewDirty)
		{
			_previewTimer!.Stop();
			try
			{
				_isPreviewDirty = false;
				HtmlPreview = await _postContentProcessor.ProcessAsync(PostContent);
			}
			finally
			{
				_previewTimer.Start();
			}
		}
	}

	public override async void ViewNavigatedTo(object? parameter)
	{
		using var _ = _loadingIndicator.BeginLoading();
		var postId = (Guid)parameter!;
		if (postId == Guid.Empty)
		{
			Post = new Post();
		}
		else
		{
			Post = (await _api.GetPostAsync(postId)).Content;
		}

		PopulateInfo(Post!);
	}

	public override void ViewUnloaded()
	{
		base.ViewUnloaded();
		_previewTimer?.Stop();
	}

	private void PopulateInfo(Post post)
	{
		if (post is null)
		{
			throw new ArgumentNullException(nameof(post));
		}

		Tags = string.Join(", ", post.Tags.Select(t => t.DisplayName));
		Categories = post.Categories.ToArray();
		PostTitle = post.Title;
		PostRouteName = post.RouteName;
		PostContent = post.Content;
		IsPublished = post.PublishedDate is not null; // TODO: This logic is wrong, we should work with post status!
	}
}
