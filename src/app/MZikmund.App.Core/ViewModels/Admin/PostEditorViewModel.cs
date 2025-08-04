﻿using System.Globalization;
using Microsoft.UI.Dispatching;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Timers;
using MZikmund.Shared.Extensions;
using MZikmund.Web.Core.Services;
using MZikmund.ViewModels.Admin;
using Newtonsoft.Json;

namespace MZikmund.ViewModels.Admin;

public partial class PostEditorViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IPostContentProcessor _postContentProcessor;
	private readonly ITimerFactory _timerFactory;
	private DispatcherQueueTimer? _previewTimer;
	private DispatcherQueueTimer? _draftTimer;
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

	[ObservableProperty]
	public partial int SelectionStart { get; set; }

	[ObservableProperty]
	public partial int SelectionLength { get; set; }

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

	[RelayCommand]
	private async Task BrowseImagesAsync()
	{
		var dialogViewModel = new MediaBrowserDialogViewModel(_api, isImageMode: true);
		var result = await _dialogService.ShowAsync(dialogViewModel);
		
		if (result == ContentDialogResult.Primary && dialogViewModel.SelectedFile != null)
		{
			if (Post != null)
			{
				Post.HeroImageUrl = GetPublicUrl(dialogViewModel.SelectedFile.BlobPath);
			}
		}
	}

	[RelayCommand]
	private async Task InsertImageAsync()
	{
		var dialogViewModel = new MediaBrowserDialogViewModel(_api, isImageMode: true);
		var result = await _dialogService.ShowAsync(dialogViewModel);
		
		if (result == ContentDialogResult.Primary && dialogViewModel.SelectedFile != null)
		{
			var imageUrl = GetPublicUrl(dialogViewModel.SelectedFile.BlobPath);
			var markdownImage = $"![{dialogViewModel.SelectedFile.FileName}]({imageUrl})";
			InsertTextAtCursor(markdownImage);
		}
	}

	[RelayCommand]
	private async Task InsertFileAsync()
	{
		var dialogViewModel = new MediaBrowserDialogViewModel(_api, isImageMode: false);
		var result = await _dialogService.ShowAsync(dialogViewModel);
		
		if (result == ContentDialogResult.Primary && dialogViewModel.SelectedFile != null)
		{
			var fileUrl = GetPublicUrl(dialogViewModel.SelectedFile.BlobPath);
			var markdownLink = $"[{dialogViewModel.SelectedFile.FileName}]({fileUrl})";
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
	}

	private string GetPublicUrl(string blobPath)
	{
		// This would typically be configured based on your blob storage setup
		// For now, return a relative path
		return "/" + blobPath.TrimStart('/');
	}

	public override void ViewLoaded()
	{
		base.ViewLoaded();
		_previewTimer ??= _timerFactory.CreateTimer();
		_previewTimer.Interval = TimeSpan.FromMilliseconds(500);
		_previewTimer.Tick += PreviewTimerOnTick;
		_previewTimer.Start();

		_draftTimer ??= _timerFactory.CreateTimer();
		_draftTimer.Interval = TimeSpan.FromSeconds(30);
		_draftTimer.Tick += DraftTimerOnTick;
		_draftTimer.Start();
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
			PublishedDate = Post.PublishedDate ?? DateTimeOffset.UtcNow
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
		_draftTimer?.Stop();
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
