namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Base of the expected, client-facing failures of the comment handlers. The message is written
/// to be shown to the caller; any other exception type is a bug and must not reach the client.
/// </summary>
public abstract class CommentException : Exception
{
	protected CommentException(string message)
		: base(message)
	{
	}
}
