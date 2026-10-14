namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Base of the expected, client-facing failures of the comment handlers. The message is written
/// to be shown to the caller; any other exception type is a bug and must not reach the client.
/// </summary>
public abstract class CommentException : Exception
{
	protected CommentException(string message, string? code = null)
		: base(message)
	{
		Code = code;
	}

	/// <summary>
	/// One of <see cref="MZikmund.DataContracts.Blog.Comments.CommentErrorCodes"/> when the failure has a
	/// specific reason; otherwise the filter falls back to the code of the exception type.
	/// </summary>
	public string? Code { get; }
}
