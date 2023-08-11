using MZikmund.Web.Core.Dtos.Blog;

namespace MZikmund.Web.Core.Services;

internal interface IPostContentProcessor
{
	Task<string> ProcessAsync(Post post);
}
