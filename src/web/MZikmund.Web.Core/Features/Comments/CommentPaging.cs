using MZikmund.Web.Core.Services.Comments;

namespace MZikmund.Web.Core.Features.Comments;

internal static class CommentPaging
{
	/// <summary>
	/// Far beyond any real thread, and low enough that Skip((page - 1) * size) cannot overflow.
	/// </summary>
	public const int MaxPageNumber = 10_000;

	public const int MaxPageSize = 100;

	public static void Validate(int pageNumber, int pageSize)
	{
		if (pageNumber < 1 || pageNumber > MaxPageNumber)
		{
			throw new CommentValidationException($"Page number must be between 1 and {MaxPageNumber}.");
		}

		if (pageSize < 1 || pageSize > MaxPageSize)
		{
			throw new CommentValidationException($"Page size must be between 1 and {MaxPageSize}.");
		}
	}
}
