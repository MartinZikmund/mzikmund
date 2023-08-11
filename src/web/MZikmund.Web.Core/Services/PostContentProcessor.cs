using MZikmund.Web.Core.Dtos.Blog;
using MZikmund.Web.Services;

namespace MZikmund.Web.Core.Services;
internal class PostContentProcessor : IPostContentProcessor
{
	private readonly IMarkdownConverter _markdownConverter;

	public PostContentProcessor(
		IMarkdownConverter markdownConverter)
	{
		_markdownConverter = markdownConverter;
	}

	public async Task<string> ProcessAsync(Post post)
	{


		var content = _markdownConverter.ToHtml(post.Content);
	}
}
