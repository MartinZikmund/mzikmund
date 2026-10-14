using MZikmund.Web.Core.Services.Comments;

namespace MZikmund.Web.Core.Features.Comments;

internal static class ModerationPaging
{
	public static void Validate(int pageNumber, int pageSize)
	{
		if (pageNumber < 1)
		{
			throw new CommentValidationException("Page number must be at least 1.");
		}

		if (pageSize < 1)
		{
			throw new CommentValidationException("Page size must be at least 1.");
		}
	}
}
