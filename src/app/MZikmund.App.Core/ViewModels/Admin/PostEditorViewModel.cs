using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blog;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Account;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.Services.Timers;
using MZikmund.Shared.Blog;
using MZikmund.Shared.Extensions;
using MZikmund.Web.Core.Services;
using Newtonsoft.Json;
using Refit;
using Launcher = Windows.System.Launcher;

namespace MZikmund.ViewModels.Admin;

public partial class PostEditorViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IWindowShellProvider _windowShellProvider;
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IPostContentProcessor _postContentProcessor;
	private readonly ITimerFactory _timerFactory;
	private readonly IOptions<AppConfig> _appConfig;
	private readonly ILogger<PostEditorViewModel> _logger;
	private readonly ILogger<MediaBrowserDialogViewModel> _mediaBrowserLogger;
	private DispatcherQueueTimer? _previewTimer;
	private DispatcherQueueTimer? _draftTimer;
	private bool _isPreviewDirty = true;
	private bool _isDraftDirty;
	private bool _isPopulating;

	public PostEditorViewModel(
		IMZikmundApi api,
		IWindowShellProvider windowShellProvider,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator,
		IPostContentProcessor postContentProcessor,
		ITimerFactory timerFactory,
		IOptions<AppConfig> appConfig,
		ILogger<PostEditorViewModel> logger,
		ILogger<MediaBrowserDialogViewModel> mediaBrowserLogger)
	{
		_api = api;
		_windowShellProvider = windowShellProvider;
		_dialogService = dialogService;
		_loadingIndicator = loadingIndicator;
		_postContentProcessor = postContentProcessor;
		_timerFactory = timerFactory;
		_appConfig = appConfig;
		_logger = logger;
		_mediaBrowserLogger = mediaBrowserLogger;
	}

	public override string Title => string.IsNullOrWhiteSpace(PostTitle) ?
		Localizer.Instance.GetString("NewPost") :
		PostTitle;

	[ObservableProperty]
	public partial string PostContent { get; set; } = "";

	[ObservableProperty]
	public partial string PostRouteName { get; set; } = "";

	[ObservableProperty]
	public partial string Tags { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Title))]
	public partial string PostTitle { get; set; } = "";

	[ObservableProperty]
	public partial string PostAbstract { get; set; } = "";

	[ObservableProperty]
	public partial string HtmlPreview { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CategoriesText))]
	public partial Category[] Categories { get; set; } = Array.Empty<Category>();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsExistingPost))]
	[NotifyCanExecuteChangedFor(nameof(OpenPreviewCommand))]
	public partial PostAdmin? Post { get; set; } = null;

	[ObservableProperty]
	public partial bool IsPublished { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset PublishDate { get; set; } = DateTimeOffset.Now;

	[ObservableProperty]
	public partial TimeSpan PublishTime { get; set; } = DateTimeOffset.Now.TimeOfDay;

	[ObservableProperty]
	public partial int SelectionStart { get; set; }

	[ObservableProperty]
	public partial int SelectionLength { get; set; }

	[ObservableProperty]
	public partial string? HeroImageUrl { get; set; }

	[ObservableProperty]
	public partial string? HeroImageAlt { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SaveStatusText))]
	public partial bool IsDirty { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SaveStatusText))]
	public partial DateTimeOffset? LastSavedAt { get; set; }

	[ObservableProperty]
	public partial bool IsLoadFailed { get; set; }

	public event EventHandler? UpdateSelectionRequested;

	public bool IsExistingPost => Post is { Id: var id } && id != Guid.Empty;

	public string SaveStatusText => IsDirty ?
		Localizer.Instance.GetString("UnsavedChanges") :
		LastSavedAt is { } savedAt ?
			string.Format(CultureInfo.CurrentCulture, Localizer.Instance.GetString("SavedAtFormat"), savedAt.ToLocalTime().ToString("t", CultureInfo.CurrentCulture)) :
			"";

	public string CategoriesText => Categories is null or { Length: 0 } ?
		Localizer.Instance.GetString("NoCategoriesSelected") :
		string.Join(", ", Categories.Select(c => c.DisplayName));

	partial void OnPostTitleChanged(string? oldValue, string newValue)
	{
		// Renaming a live post must not silently break its URL, nor overwrite a hand-tuned route.
		if (!_isPopulating && Post is not { IsPublished: true } && PostRouteNames.FollowsTitle(PostRouteName, oldValue ?? ""))
		{
			PostRouteName = newValue.GenerateRouteName();
		}

		MarkDirty();
	}

	partial void OnPostContentChanged(string value)
	{
		_isPreviewDirty = true;
		MarkDirty();
	}

	partial void OnPostRouteNameChanged(string value) => MarkDirty();

	partial void OnTagsChanged(string value) => MarkDirty();

	partial void OnPostAbstractChanged(string value) => MarkDirty();

	partial void OnCategoriesChanged(Category[] value) => MarkDirty();

	partial void OnIsPublishedChanged(bool value) => MarkDirty();

	partial void OnPublishDateChanged(DateTimeOffset value) => MarkDirty();

	partial void OnPublishTimeChanged(TimeSpan value) => MarkDirty();

	partial void OnHeroImageUrlChanged(string? value) => MarkDirty();

	partial void OnHeroImageAltChanged(string? value) => MarkDirty();

	private void MarkDirty()
	{
		if (_isPopulating)
		{
			return;
		}

		IsDirty = true;
		_isDraftDirty = true;
	}

	private DateTimeOffset GetCombinedPublishDateTime()
	{
		var publishDateTime = PublishDate.Date.Add(PublishTime);
		return new DateTimeOffset(publishDateTime, PublishDate.Offset);
	}

	private Tag[] ParseTags() =>
		Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(t => new Tag { DisplayName = t })
			.ToArray();

	[RelayCommand]
	private async Task PickCategoriesAsync()
	{
		var categoryPickerDialogViewModel = new CategoryPickerDialogViewModel(Categories.Select(c => c.Id).ToArray(), _api);
		var result = await _dialogService.ShowAsync(categoryPickerDialogViewModel);
		if (result == ContentDialogResult.Primary)
		{
			Categories = categoryPickerDialogViewModel.SelectedCategories.ToArray();
		}
	}

	private bool CanOpenPreview() => Post?.PreviewToken is not null;

	[RelayCommand(CanExecute = nameof(CanOpenPreview))]
	private async Task OpenPreviewAsync() =>
		await Launcher.LaunchUriAsync(new Uri(new Uri(_appConfig.Value.WebUrl!), "/blog/preview/" + Post?.PreviewToken));

	[RelayCommand]
	private async Task SaveAsync()
	{
		if (Post is null)
		{
			return;
		}

		Post.Title = PostTitle;
		Post.RouteName = PostRouteName;
		Post.Abstract = PostAbstract;
		Post.Tags = ParseTags();
		Post.Categories = Categories;
		Post.IsPublished = IsPublished;
		Post.PublishedDate = GetCombinedPublishDateTime();
		Post.Content = PostContent;
		Post.HeroImageUrl = HeroImageUrl;
		Post.HeroImageAlt = HeroImageAlt;

		try
		{
			var response = IsExistingPost ?
				await _api.UpdatePostAsync(Post.Id, Post) :
				await _api.AddPostAsync(Post);

			if (!response.IsSuccessStatusCode || response.Content is not { } savedPost)
			{
				await ShowSaveErrorAsync(response.Error?.Content ?? response.ReasonPhrase ?? response.StatusCode.ToString());
				return;
			}

			// Without the server-assigned Id the next save would create a duplicate post.
			var adminPost = await _api.GetPostForAdminAsync(savedPost.Id);
			Post = adminPost.IsSuccessStatusCode && adminPost.Content is { } refreshed ?
				refreshed :
				new PostAdmin { Id = savedPost.Id, PreviewToken = Post.PreviewToken };

			if (adminPost.Content is not null)
			{
				PopulateInfo(adminPost.Content, keepEditorText: true);
			}

			IsDirty = false;
			LastSavedAt = DateTimeOffset.Now;
		}
		catch (Exception ex) when (ex is ApiException or HttpRequestException or AuthenticationFailedException or TaskCanceledException)
		{
			_logger.LogError(ex, "Saving post failed");
			await ShowSaveErrorAsync(ex.Message);
		}
	}

	private Task<ContentDialogResult> ShowSaveErrorAsync(string detail) =>
		_dialogService.ShowStatusMessageAsync(
			StatusMessageDialogType.Error,
			Localizer.Instance.GetString("SaveFailed"),
			detail);

	[RelayCommand]
	private async Task BrowseImagesAsync()
	{
		var dialogViewModel = new MediaBrowserDialogViewModel(_api, _windowShellProvider, _appConfig, _mediaBrowserLogger, isImageMode: true);
		var result = await _dialogService.ShowAsync(dialogViewModel);

		if (result == ContentDialogResult.Primary && dialogViewModel.SelectedUrl != null)
		{
			HeroImageUrl = dialogViewModel.SelectedUrl.AbsoluteUri;
		}
	}

	[RelayCommand]
	private async Task InsertImageAsync()
	{
		var dialogViewModel = new MediaBrowserDialogViewModel(_api, _windowShellProvider, _appConfig, _mediaBrowserLogger, isImageMode: true);
		var result = await _dialogService.ShowAsync(dialogViewModel);

		if (result == ContentDialogResult.Primary && dialogViewModel.SelectedFile != null && dialogViewModel.SelectedUrl != null)
		{
			var altText = GetSelectedText();
			if (string.IsNullOrWhiteSpace(altText))
			{
				altText = dialogViewModel.SelectedFile.FileName;
			}

			var markdownImage = $"\r\r![{altText}]({dialogViewModel.SelectedUrl})\r^^^{altText}\r\r";
			InsertTextAtCursor(markdownImage);
		}
	}

	[RelayCommand]
	private async Task InsertFileAsync()
	{
		var dialogViewModel = new MediaBrowserDialogViewModel(_api, _windowShellProvider, _appConfig, _mediaBrowserLogger, isImageMode: false);
		var result = await _dialogService.ShowAsync(dialogViewModel);

		if (result == ContentDialogResult.Primary && dialogViewModel.SelectedFile != null)
		{
			var fileUrl = GetPublicUrl(dialogViewModel.SelectedFile.BlobPath);

			var linkText = GetSelectedText();
			if (string.IsNullOrWhiteSpace(linkText))
			{
				linkText = dialogViewModel.SelectedFile.FileName;
			}

			InsertTextAtCursor($"[{linkText}]({fileUrl})");
		}
	}

	private void InsertTextAtCursor(string text)
	{
		if (string.IsNullOrEmpty(PostContent))
		{
			PostContent = text;
			SelectionStart = text.Length;
			SelectionLength = 0;
			UpdateSelectionRequested?.Invoke(this, EventArgs.Empty);
			return;
		}

		var insertPosition = Math.Max(0, Math.Min(SelectionStart, PostContent.Length));
		var selectionLength = Math.Max(0, Math.Min(SelectionLength, PostContent.Length - insertPosition));

		PostContent = PostContent[..insertPosition] + text + PostContent[(insertPosition + selectionLength)..];

		SelectionStart = insertPosition + text.Length;
		SelectionLength = 0;
		UpdateSelectionRequested?.Invoke(this, EventArgs.Empty);
	}

	private string GetSelectedText()
	{
		if (string.IsNullOrEmpty(PostContent) || SelectionLength == 0)
		{
			return string.Empty;
		}

		var insertPosition = Math.Max(0, Math.Min(SelectionStart, PostContent.Length));
		var selectionLength = Math.Max(0, Math.Min(SelectionLength, PostContent.Length - insertPosition));

		return selectionLength == 0 ? string.Empty : PostContent.Substring(insertPosition, selectionLength);
	}

	private string GetPublicUrl(string blobPath)
	{
		var cdnUrl = _appConfig.Value.CdnUrl.TrimEnd('/');
		return $"{cdnUrl}/files/{blobPath.TrimStart('/')}";
	}

	public override void ViewLoaded()
	{
		base.ViewLoaded();
		_previewTimer ??= CreateTimer(TimeSpan.FromMilliseconds(500), PreviewTimerOnTick);
		_previewTimer.Start();

		_draftTimer ??= CreateTimer(TimeSpan.FromSeconds(30), DraftTimerOnTick);
		_draftTimer.Start();
	}

	private DispatcherQueueTimer CreateTimer(TimeSpan interval, Windows.Foundation.TypedEventHandler<DispatcherQueueTimer, object> tick)
	{
		var timer = _timerFactory.CreateTimer();
		timer.Interval = interval;
		timer.Tick += tick;
		return timer;
	}

	private async void DraftTimerOnTick(DispatcherQueueTimer sender, object args)
	{
		if (Post is null || !_isDraftDirty)
		{
			return;
		}

		_isDraftDirty = false;

		var draft = new Post
		{
			Id = Post.Id,
			Title = PostTitle,
			RouteName = PostRouteName,
			Abstract = PostAbstract,
			Content = PostContent,
			Tags = ParseTags(),
			Categories = Categories,
			IsPublished = IsPublished,
			PublishedDate = GetCombinedPublishDateTime(),
			HeroImageUrl = HeroImageUrl,
			HeroImageAlt = HeroImageAlt
		};

		try
		{
			var serialized = JsonConvert.SerializeObject(draft, Formatting.Indented);
			var fileName = $"{Post.Id}_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.txt";
			var draftFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Drafts", CreationCollisionOption.OpenIfExists);
			var draftFile = await draftFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(draftFile, serialized);
		}
		catch (Exception ex)
		{
			_isDraftDirty = true;
			_logger.LogWarning(ex, "Writing the local draft backup failed");
		}
	}

	private async void PreviewTimerOnTick(DispatcherQueueTimer sender, object args)
	{
		if (!_isPreviewDirty)
		{
			return;
		}

		_previewTimer!.Stop();
		try
		{
			_isPreviewDirty = false;
			HtmlPreview = await _postContentProcessor.ProcessAsync(PostContent);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Rendering the preview failed");
		}
		finally
		{
			_previewTimer.Start();
		}
	}

	public override async void ViewNavigatedTo(object? parameter)
	{
		using var _ = _loadingIndicator.BeginLoading();
		var postId = parameter is Guid id ? id : Guid.Empty;

		if (postId == Guid.Empty)
		{
			Post = new PostAdmin();
			PopulateInfo(Post);
			return;
		}

		try
		{
			var response = await _api.GetPostForAdminAsync(postId);
			if (response.IsSuccessStatusCode && response.Content is { } post)
			{
				Post = post;
				PopulateInfo(post);
				return;
			}

			_logger.LogError("Loading post {PostId} failed: {Status}", postId, response.StatusCode);
		}
		catch (Exception ex) when (ex is ApiException or HttpRequestException or AuthenticationFailedException or TaskCanceledException)
		{
			_logger.LogError(ex, "Loading post {PostId} failed", postId);
		}

		IsLoadFailed = true;
	}

	public override void ViewUnloaded()
	{
		base.ViewUnloaded();
		if (_previewTimer != null)
		{
			_previewTimer.Stop();
			_previewTimer.Tick -= PreviewTimerOnTick;
			_previewTimer = null;
		}

		if (_draftTimer != null)
		{
			_draftTimer.Stop();
			_draftTimer.Tick -= DraftTimerOnTick;
			_draftTimer = null;
		}
	}

	private void PopulateInfo(Post post, bool keepEditorText = false)
	{
		ArgumentNullException.ThrowIfNull(post);

		_isPopulating = true;
		try
		{
			Tags = string.Join(", ", post.Tags.Select(t => t.DisplayName));
			Categories = post.Categories.ToArray();
			PostTitle = post.Title;
			PostRouteName = post.RouteName;
			PostAbstract = post.Abstract;
			HeroImageUrl = post.HeroImageUrl;
			HeroImageAlt = post.HeroImageAlt;
			IsPublished = post.IsPublished;

			// Keeps the caret where it is after a save; the server only normalizes line endings.
			if (!keepEditorText)
			{
				PostContent = post.Content;
			}

			var publishedDate = post.PublishedDate ?? DateTimeOffset.Now;
			PublishDate = publishedDate;
			PublishTime = publishedDate.TimeOfDay;
		}
		finally
		{
			_isPopulating = false;
		}

		IsDirty = false;
		_isPreviewDirty = true;
	}

	public void SetSelection(int selectionStart, int selectionLength)
	{
		SelectionStart = selectionStart;
		SelectionLength = selectionLength;
	}
}
