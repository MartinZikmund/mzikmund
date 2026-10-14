using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Pages.Blog;

public class PostModel : PageModel, IPostPageModel
{
	private readonly IHostEnvironment _host;
	private readonly IMediator _mediator;
	private readonly IPostContentProcessor _postContentProcessor;
	private readonly CommentsOptions _commentsOptions;
	private readonly SupabaseOptions _supabaseOptions;
	private readonly TurnstileOptions _turnstileOptions;

	public PostModel(
		IHostEnvironment host,
		IMediator mediator,
		IPostContentProcessor postContentProcessor,
		IOptions<CommentsOptions> commentsOptions,
		IOptions<SupabaseOptions> supabaseOptions,
		IOptions<TurnstileOptions> turnstileOptions)
	{
		_host = host;
		_mediator = mediator;
		_postContentProcessor = postContentProcessor;
		_commentsOptions = commentsOptions.Value;
		_supabaseOptions = supabaseOptions.Value;
		_turnstileOptions = turnstileOptions.Value;
	}

	public Post BlogPost { get; set; } = null!;

	public string HtmlContent { get; set; } = "";

	public string MetaKeywords { get; set; } = "";

	public string? SafeHeroImageUrl { get; set; }

	/// <summary>
	/// Null when Comments:Enabled is false, in which case the section is not rendered at all.
	/// </summary>
	public CommentsSectionModel? CommentsSection { get; set; }

	public async Task OnGet(string routeName, Guid? comment, string? sort)
	{
		BlogPost = await _mediator.Send(new GetPostByRouteNameQuery(routeName));
		HtmlContent = await _postContentProcessor.ProcessAsync(BlogPost.Content);
		MetaKeywords = string.Join(", ", BlogPost.Tags.Select(t => t.DisplayName));

		// Validate and sanitize hero image URL
		// Only allow http:// and https:// URLs for security
		if (!string.IsNullOrWhiteSpace(BlogPost.HeroImageUrl) &&
			Uri.TryCreate(BlogPost.HeroImageUrl, UriKind.Absolute, out var uri) &&
			(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			SafeHeroImageUrl = uri.ToString();
		}

		if (_commentsOptions.Enabled)
		{
			CommentsSection = await LoadCommentsAsync(comment, sort);
		}
	}

	private async Task<CommentsSectionModel> LoadCommentsAsync(Guid? highlightCommentId, string? sort)
	{
		var sortOrder = string.Equals(sort, "new", StringComparison.OrdinalIgnoreCase)
			? CommentSortOrder.New
			: CommentSortOrder.Top;

		// A #fragment never reaches the server, so ?comment={id} is what puts a deep-linked
		// comment from page 3 into the first paint.
		var pageNumber = 1;
		if (highlightCommentId is { } commentId)
		{
			pageNumber = await _mediator.Send(new FindCommentPageQuery(commentId, sortOrder, CommentLimits.PageSize)) ?? 1;
		}

		var comments = await _mediator.Send(
			new GetPostCommentsQuery(BlogPost.Id, sortOrder, pageNumber, CommentLimits.PageSize));

		return new CommentsSectionModel
		{
			PostId = BlogPost.Id,
			Comments = comments,
			Sort = sortOrder,
			HighlightCommentId = highlightCommentId,
			SupabaseUrl = _supabaseOptions.BaseUrl,
			SupabasePublishableKey = _supabaseOptions.PublishableKey,
			TurnstileSiteKey = _turnstileOptions.SiteKey,
		};
	}
}
