using Humanizer;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels.Items;

public sealed class PostListItemViewModel : ObservableObject
{
	private readonly IMarkdownConverter _markdownConverter;

	public PostListItemViewModel(PostListItem item, IMarkdownConverter markdownConverter)
	{
		Item = item;
		_markdownConverter = markdownConverter;
	}

	public Uri? HeroImageUri => Item?.HeroImageUrl is not null ? new Uri(Item.HeroImageUrl) : null;

	public PostListItem Item { get; }

	public string AbstractPlain => _markdownConverter.ToPlainText(Item.Abstract);

	public string PublishedDate => (Item.PublishedDate ?? Item.LastModifiedDate)?.Humanize() ?? "";
}
