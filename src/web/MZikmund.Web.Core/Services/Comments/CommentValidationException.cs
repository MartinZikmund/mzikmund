namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// The request itself is invalid: empty or oversized content, a bad paging value, an unknown report reason.
/// </summary>
public sealed class CommentValidationException : CommentException
{
	public CommentValidationException(string message)
		: base(message)
	{
	}
}
