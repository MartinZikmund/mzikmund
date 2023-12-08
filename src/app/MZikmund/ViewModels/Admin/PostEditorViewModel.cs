﻿using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Shared.Extensions;

namespace MZikmund.ViewModels.Admin;

public class PostEditorViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private string _postTitle = "";

	public PostEditorViewModel(IMZikmundApi api, IDialogService dialogService, ILoadingIndicator loadingIndicator)
	{
		_api = api;
		_dialogService = dialogService;
		_loadingIndicator = loadingIndicator;
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

	public string CategoriesText => Categories is null ? "" : string.Join(", ", Categories.Select(c => c.DisplayName));

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

	public override void ViewAppeared() => base.ViewAppeared();

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
	}
}
