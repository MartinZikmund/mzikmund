namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// The comment, post or author the request refers to does not exist or is not public.
/// </summary>
public sealed class CommentNotFoundException : CommentException
{
	public CommentNotFoundException(string message)
		: base(message)
	{
	}

	public CommentNotFoundException(string message, string code)
		: base(message, code)
	{
	}
}
