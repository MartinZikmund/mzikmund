using System.Text.Json;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Pages.Blog;

/// <summary>
/// Everything _Comments.cshtml needs. Deliberately its own model rather than a widening of
/// IPostPageModel, which is shared with the draft preview page.
/// </summary>
public class CommentsSectionModel
{
	public required Guid PostId { get; init; }

	/// <summary>
	/// Top-level comments of the resolved page.
	/// </summary>
	public required PagedResponse<CommentDto> Comments { get; init; }

	public required CommentSortOrder Sort { get; init; }

	/// <summary>
	/// Comment from <c>?comment={id}</c>, highlighted and scrolled to by the browser module.
	/// </summary>
	public Guid? HighlightCommentId { get; init; }

	/// <summary>
	/// Supabase origin the browser talks to: the local stack in Development, the hosted project otherwise.
	/// </summary>
	public required string SupabaseUrl { get; init; }

	/// <summary>
	/// Publishable key. Public by design, never the secret key.
	/// </summary>
	public required string SupabasePublishableKey { get; init; }

	/// <summary>
	/// Turnstile site key; empty disables the widget.
	/// </summary>
	public required string TurnstileSiteKey { get; init; }

	public int PageSize => Comments.PageSize;

	public int PageNumber => Comments.PageNumber;

	public int TotalCount => Comments.TotalCount;

	public bool HasMore => PageNumber * PageSize < TotalCount;

	public string SortValue => Sort == CommentSortOrder.New ? "new" : "top";

	/// <summary>
	/// Rendered into a script block of type application/json. The default System.Text.Json encoder
	/// escapes angle brackets and ampersands, so the payload cannot close the element early.
	/// </summary>
	public string SupabaseConfigJson => JsonSerializer.Serialize(new Dictionary<string, string>
	{
		["url"] = SupabaseUrl,
		["publishableKey"] = SupabasePublishableKey,
	});
}
