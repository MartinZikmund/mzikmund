using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Services;

public interface IPostContentProcessor
{
	Task<string> ProcessAsync(Post post);
}
