using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.Services.Timers;
using MZikmund.Shared.Extensions;
using MZikmund.ViewModels.Admin;
using MZikmund.Web.Core.Services;
using Newtonsoft.Json;
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
	private readonly ILogger<MediaBrowserDialogViewModel> _mediaBrowserLogger;
	private DispatcherQueueTimer? _previewTimer;
	private DispatcherQueueTimer? _draftTimer;
	private bool _isPreviewDirty = true;

	public PostEditorViewModel(IMZikmundApi api, IWindowShellProvider windowShellProvider, IDialogService dialogService, ILoadingIndicator loadingIndicator, IPostContentProcessor postContentProcessor, ITimerFactory timerFactory, IOptions<AppConfig> appConfig, ILogger<MediaBrowserDialogViewModel> mediaBrowserLogger)
	{
		_api = api;
		_windowShellProvider = windowShellProvider;
		_dialogService = dialogService;
		_loadingIndicator = loadingIndicator;
		_postContentProcessor = postContentProcessor;
		_timerFactory = timerFactory;
		_appConfig = appConfig;
		_mediaBrowserLogger = mediaBrowserLogger;
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

	public event EventHandler? UpdateSelectionRequested;

	[ObservableProperty]
	public partial string? HeroImageUrl { get; set; }

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

	private DateTimeOffset GetCombinedPublishDateTime()
	{
		// Combine publish date and time into a single DateTimeOffset
		var publishDateTime = PublishDate.Date.Add(PublishTime);
		return new DateTimeOffset(publishDateTime, PublishDate.Offset);
	}

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
	private async Task OpenPreviewAsync() => await Launcher.LaunchUriAsync(new Uri(new Uri(_appConfig.Value.WebUrl!), "/blog/preview/" + Post?.PreviewToken));

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
		Post.PublishedDate = GetCombinedPublishDateTime();
		Post.Content = PostContent;
		Post.HeroImageUrl = HeroImageUrl;

		if (Post.Id == Guid.Empty)
		{
			await _api.AddPostAsync(Post);
		}
		else
		{
			await _api.UpdatePostAsync(Post.Id, Post);
		}
	}

	[RelayCommand]
	private async Task BrowseImagesAsync()
	{
		var dialogViewModel = new MediaBrowserDialogViewModel(_api, _windowShellProvider, _appConfig, _mediaBrowserLogger, isImageMode: true);
		var result = await _dialogService.ShowAsync(dialogViewModel);

		if (result == ContentDialogResult.Primary &&
			dialogViewModel.SelectedUrl != null &&
			Post != null)
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
			// Use selected text as alt text if available, otherwise use filename
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

			// Use selected text as link text if available, otherwise use filename
			var linkText = GetSelectedText();
			if (string.IsNullOrWhiteSpace(linkText))
			{
				linkText = dialogViewModel.SelectedFile.FileName;
			}

			var markdownLink = $"[{linkText}]({fileUrl})";
			InsertTextAtCursor(markdownLink);
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

		// Ensure selection start is within bounds
		var insertPosition = Math.Max(0, Math.Min(SelectionStart, PostContent.Length));
		var selectionLength = Math.Max(0, Math.Min(SelectionLength, PostContent.Length - insertPosition));

		// Insert text at cursor position, replacing any selected text
		var beforeText = PostContent.Substring(0, insertPosition);
		var afterText = PostContent.Substring(insertPosition + selectionLength);

		PostContent = beforeText + text + afterText;

		// Update cursor position to be after the inserted text
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

		if (selectionLength == 0)
		{
			return string.Empty;
		}

		return PostContent.Substring(insertPosition, selectionLength);
	}

	private string GetPublicUrl(string blobPath)
	{
		var cdnUrl = _appConfig.Value.CdnUrl.TrimEnd('/');
		return $"{cdnUrl}/files/{blobPath.TrimStart('/')}";
	}

	public override void ViewLoaded()
	{
		base.ViewLoaded();
		_previewTimer ??= CreatePreviewTimer();
		_previewTimer.Start();

		_draftTimer ??= CreateDraftTimer();
		_draftTimer.Start();
	}

	private DispatcherQueueTimer CreatePreviewTimer()
	{
		var timer = _timerFactory.CreateTimer();
		timer.Interval = TimeSpan.FromMilliseconds(500);
		timer.Tick += PreviewTimerOnTick;
		return timer;
	}

	private DispatcherQueueTimer CreateDraftTimer()
	{
		var timer = _timerFactory.CreateTimer();
		timer.Interval = TimeSpan.FromSeconds(30);
		timer.Tick += DraftTimerOnTick;
		return timer;
	}

	private async void DraftTimerOnTick(DispatcherQueueTimer sender, object args)
	{
		if (Post is null)
		{
			return;
		}

		// Save to a draft file
		var draft = new Post
		{
			Id = Post.Id,
			Title = PostTitle,
			RouteName = PostRouteName,
			Content = PostContent,
			Tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(t => new Tag { DisplayName = t.Trim() })
				.ToArray(),
			Categories = Categories,
			IsPublished = IsPublished,
			PublishedDate = GetCombinedPublishDateTime(),
			HeroImageUrl = Post.HeroImageUrl,
			HeroImageAlt = Post.HeroImageAlt
		};

		var serialized = JsonConvert.SerializeObject(draft, Formatting.Indented);
		// Add time to draft file name
		var fileName = $"{Post.Id}_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.txt";
		var draftFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Drafts", CreationCollisionOption.OpenIfExists);
		var draftFile = await draftFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
		await FileIO.WriteTextAsync(draftFile, serialized);
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
			Post = new PostAdmin();
		}
		else
		{
			Post = (await _api.GetPostForAdminAsync(postId)).Content;
		}

		PopulateInfo(Post!);
	}

	public override void ViewUnloaded()
	{
		base.ViewUnloaded();
		if (_previewTimer != null)
		{
			_previewTimer.Stop();
			_previewTimer.Tick -= PreviewTimerOnTick;
		}
		if (_draftTimer != null)
		{
			_draftTimer.Stop();
			_draftTimer.Tick -= DraftTimerOnTick;
		}
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
		HeroImageUrl = post.HeroImageUrl;
		IsPublished = post.IsPublished;

		// Load existing publish date and time or use current date/time
		if (post.PublishedDate.HasValue)
		{
			PublishDate = post.PublishedDate.Value;
			PublishTime = post.PublishedDate.Value.TimeOfDay;
		}
		else
		{
			PublishDate = DateTimeOffset.Now;
			PublishTime = DateTimeOffset.Now.TimeOfDay;
		}
	}

	public void SetSelection(int selectionStart, int selectionLength)
	{
		SelectionStart = selectionStart;
		SelectionLength = selectionLength;
	}
}
