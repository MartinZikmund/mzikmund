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

public class PostEditorViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IPostContentProcessor _postContentProcessor;
	private readonly ITimerFactory _timerFactory;
	private string _postTitle = "";
	private DispatcherQueueTimer? _previewTimer;
	private bool _isPreviewDirty = true;
	private string _postContent = "";

	public PostEditorViewModel(IMZikmundApi api, IDialogService dialogService, ILoadingIndicator loadingIndicator, IPostContentProcessor postContentProcessor, ITimerFactory timerFactory)
	{
		_api = api;
		_dialogService = dialogService;
		_loadingIndicator = loadingIndicator;
		_postContentProcessor = postContentProcessor;
		_timerFactory = timerFactory;
	}

	public override string Title => Post?.Title ?? "";

	public string PostTitle
	{
		get => _postTitle;
		set
		{
			_postTitle = value;
			PostRouteName = _postTitle.GenerateRouteName();
		}
	}

	public string PostRouteName { get; set; } = "";

	public string Tags { get; set; } = "";

	public Category[] Categories { get; set; } = Array.Empty<Category>();

	public string PostContent
	{
		get => _postContent;
		set
		{
			if (SetProperty(ref _postContent, value))
			{
				_isPreviewDirty = true;
			}
		}
	}

	public string HtmlPreview { get; set; } = "";

	public string CategoriesText => Categories is null or { Length: 0 } ?
		Localizer.Instance.GetString("NoCategoriesSelected") :
		string.Join(", ", Categories.Select(c => c.DisplayName));

	public Post? Post { get; set; }

	public ICommand SaveCommand => GetOrCreateAsyncCommand(SaveAsync);

	public ICommand PickCategoriesCommand => GetOrCreateCommand(PickCategories);

	private async void PickCategories()
	{
		var categoyPickerDialogViewModel = new CategoryPickerDialogViewModel(Categories.Select(c => c.Id).ToArray(), _api);
		var result = await _dialogService.ShowAsync(categoyPickerDialogViewModel);
		if (result == ContentDialogResult.Primary)
		{
			Categories = categoyPickerDialogViewModel.SelectedCategories.ToArray();
		}
	}

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
		Post.IsPublished = true;
		Post.PublishedDate = DateTimeOffset.UtcNow; // TODO: Don't always publish!

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

	public override async void ViewNavigatedTo(object parameter)
	{
		using var _ = _loadingIndicator.BeginLoading();
		var postId = (Guid)parameter;
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
	}
}
