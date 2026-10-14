namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// The caller is not allowed to do this: not signed in, not the owner, or banned.
/// </summary>
public sealed class CommentForbiddenException : CommentException
{
	public CommentForbiddenException(string message)
		: base(message)
	{
	}

	public CommentForbiddenException(string message, string code)
		: base(message, code)
	{
	}
}
