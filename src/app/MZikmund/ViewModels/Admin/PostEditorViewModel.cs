using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Loading;

namespace MZikmund.ViewModels.Admin;

public class PostEditorViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILoadingIndicator _loadingIndicator;

	public PostEditorViewModel(IMZikmundApi api, ILoadingIndicator loadingIndicator)
	{
		_api = api;
		_loadingIndicator = loadingIndicator;
	}

	public override string Title => Post?.Title ?? "";

	public string Tags { get; set; } = "";

	public Category[] Categories { get; set; } = Array.Empty<Category>();

	public string CategoriesText => Categories is null ? "" : string.Join(", ", Categories.Select(c => c.DisplayName));

	public Post? Post { get; set; }

	public ICommand SaveCommand => GetOrCreateAsyncCommand(SaveAsync);

	private async Task SaveAsync()
	{
		if (Post is null)
		{
			return;
		}

		var tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(t => new Tag { DisplayName = t.Trim() })
			.ToArray();

		Post.Tags = tags;
		Post.Categories = Categories;

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
	}
}
