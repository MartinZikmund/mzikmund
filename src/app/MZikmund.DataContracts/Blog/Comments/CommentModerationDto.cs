using System.Diagnostics.CodeAnalysis;

namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// A comment as the moderation feed shows it: the public shape plus the author's id, so a
/// moderator can ban straight from the feed. Never returned by the public endpoints.
/// </summary>
public record CommentModerationDto : CommentDto
{
	public CommentModerationDto()
	{
	}

	[SetsRequiredMembers]
	public CommentModerationDto(CommentDto comment, Guid authorId)
		: base(comment)
	{
		AuthorId = authorId;
	}

	/// <summary>
	/// Identifier of the comment author.
	/// </summary>
	public required Guid AuthorId { get; init; }
}
