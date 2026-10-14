namespace MZikmund.Shared.Blog;

/// <summary>
/// Small display rules for the comments panel, kept here so they can be unit tested.
/// </summary>
public static class CommentDisplay
{
	public static string MentionPrefix(string? mentionedDisplayName) =>
		string.IsNullOrWhiteSpace(mentionedDisplayName) ? string.Empty : $"@{mentionedDisplayName.Trim()} ";

	public static int RemainingReplyCount(int replyCount, int shownReplyCount) =>
		Math.Max(0, replyCount - shownReplyCount);

	public static string FormatBadgeCount(int count) => count switch
	{
		<= 0 => string.Empty,
		> 99 => "99+",
		_ => count.ToString(System.Globalization.CultureInfo.InvariantCulture),
	};
}
