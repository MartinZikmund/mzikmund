namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// The request is valid but conflicts with the current state of the comment.
/// </summary>
public sealed class CommentConflictException : CommentException
{
	public CommentConflictException(string message)
		: base(message)
	{
	}

	public CommentConflictException(string message, string code)
		: base(message, code)
	{
	}
}
