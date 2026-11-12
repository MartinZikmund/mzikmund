using Ganss.Xss;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Renders user-authored comment markdown to sanitized HTML.
/// </summary>
/// <remarks>
/// This deliberately does NOT reuse <c>IMarkdownConverter</c>. That converter renders trusted,
/// admin-authored blog posts: its pipeline is permissive on purpose (raw HTML, media links,
/// generic attributes) and <c>PostContentProcessor</c> even injects raw gist &lt;script&gt; tags
/// before conversion. None of that is acceptable for user-generated content, and restricting the
/// shared converter would break the blog. Comments therefore get their own pipeline plus an
/// HtmlSanitizer allow-list that matches exactly what this pipeline can emit.
/// </remarks>
public sealed class CommentContentRenderer : ICommentContentRenderer
{
	// Markdig writes these into rel="..." on every anchor it emits.
	private static readonly string[] LinkRels = ["nofollow", "ugc", "noopener", "noreferrer"];

	// Exactly the tags the restricted pipeline can emit. Thematic breaks and indented code are
	// core CommonMark and cannot be turned off by an extension flag, hence hr/pre/code.
	private static readonly string[] AllowedHtmlTags =
	[
		"p", "br", "strong", "em", "del", "code", "pre",
		"blockquote", "ul", "ol", "li", "a", "hr"
	];

	// "class" is deliberately absent: fenced code emits class="language-<whatever the user typed>".
	private static readonly string[] AllowedHtmlAttributes = ["href", "title", "rel", "target"];

	private static readonly string[] AllowedUrlSchemes = ["http", "https", "mailto"];

	private readonly MarkdownPipeline _pipeline;
	private readonly HtmlSanitizer _sanitizer;

	public CommentContentRenderer()
	{
		var builder = new MarkdownPipelineBuilder()
			.DisableHtml()      // raw HTML blocks and inline tags are escaped, not parsed
			.DisableHeadings()  // no ATX or setext headings in comments
			.UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
			.UseReferralLinks(LinkRels);

		builder.DocumentProcessed += static document =>
		{
			// Materialised first: ReplaceBy mutates the tree the lazy enumeration walks.
			foreach (var node in document.Descendants().ToList())
			{
				switch (node)
				{
					// Images become their alt text. An EMPTY literal plus copyChildren keeps the
					// label exactly once; a filled literal with copyChildren would duplicate it.
					case LinkInline { IsImage: true } image:
						image.ReplaceBy(new LiteralInline(), copyChildren: true);
						break;

					// Scheme filtering happens here as well as in the sanitizer: the sanitizer
					// alone would only strip the href and leave a link-styled anchor behind.
					case LinkInline link when !IsAllowedUrl(link.Url):
						link.ReplaceBy(new LiteralInline(), copyChildren: true);
						break;

					case LinkInline link:
						link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
						break;

					// <https://...> autolinks are AutolinkInline, a different type from LinkInline.
					case AutolinkInline autolink when !IsAllowedUrl(autolink.Url):
						autolink.ReplaceBy(new LiteralInline(autolink.Url), copyChildren: false);
						break;

					case AutolinkInline autolink:
						autolink.GetAttributes().AddPropertyIfNotExist("target", "_blank");
						break;
				}
			}
		};

		_pipeline = builder.Build();

		// Every default set must be cleared first: the defaults allow 95+ tags (img, form, input,
		// button) and 100+ attributes (style, src, action).
		_sanitizer = new HtmlSanitizer();
		_sanitizer.AllowedTags.Clear();
		_sanitizer.AllowedTags.UnionWith(AllowedHtmlTags);
		_sanitizer.AllowedAttributes.Clear();
		_sanitizer.AllowedAttributes.UnionWith(AllowedHtmlAttributes);
		_sanitizer.AllowedCssProperties.Clear();
		_sanitizer.AllowedAtRules.Clear();
		_sanitizer.AllowedSchemes.Clear();
		_sanitizer.AllowedSchemes.UnionWith(AllowedUrlSchemes);
		_sanitizer.AllowDataAttributes = false;
		_sanitizer.KeepChildNodes = false;
	}

	public string RenderHtml(string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return string.Empty;
		}

		var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
		return _sanitizer.Sanitize(html).Trim();
	}

	private static bool IsAllowedUrl(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}

		if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
		{
			return false;
		}

		return !uri.IsAbsoluteUri || AllowedUrlSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);
	}
}
