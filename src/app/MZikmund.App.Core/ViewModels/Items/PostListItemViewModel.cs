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

	public PostListItem Item { get; }

	public string AbstractPlain => _markdownConverter.ToPlainText(Item.Abstract);
}
