using System.CodeDom.Compiler;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using Azure;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MZikmund.Web.Core.Infrastructure;

public partial class LegacyUrlRedirectRule : IRule
{
	private const string BlogLegacyUrl = "blog.mzikmund.";
	private const string ComLegacyUrl = "mzikmund.com";
	private const string WwwComLegacyUrl = "www.mzikmund.com";

	private const string NewUrl = "https://mzikmund.dev";
	private readonly ILogger<LegacyUrlRedirectRule> _logger;

	public LegacyUrlRedirectRule(ILogger<LegacyUrlRedirectRule> logger)
	{
		_logger = logger;
	}

	[GeneratedRegex(@"\d{4}/\d{1,2}/([a-zA-Z0-9\-]+)/?", RegexOptions.None)]
	private static partial Regex LegacyBlogPostRegex();

	public void ApplyRule(RewriteContext context)
	{
		var hostString = context.HttpContext?.Request?.Host.Host;

		_logger.LogInformation(
			"Applying rewrite rule for host '{Host}'",
			hostString ?? "null");

		if (hostString is null)
		{
			return;
		}

		if (hostString.StartsWith(BlogLegacyUrl, StringComparison.OrdinalIgnoreCase))
		{
			ProcessBlogLegacyUrls(context);
		}
		else if (
			hostString.Equals(ComLegacyUrl, StringComparison.OrdinalIgnoreCase) ||
			hostString.Equals(WwwComLegacyUrl, StringComparison.OrdinalIgnoreCase))
		{
			ProcessComUrl(context);
		}
	}

	private void ProcessComUrl(RewriteContext context) => SetResponse(context, NewUrl);

	private void ProcessBlogLegacyUrls(RewriteContext context)
	{
		var request = context.HttpContext.Request;

		if (!request.Path.HasValue || string.IsNullOrEmpty(request.Path.Value))
		{
			SetResponse(context, NewUrl);
			return;
		}

		if (request.Path.StartsWithSegments("/wp-content"))
		{
			var cdnUrl = UriHelper.BuildAbsolute(
				request.Scheme,
				new HostString("cdn.mzikmund.dev"),
				request.PathBase,
				request.Path,
				request.QueryString);
			SetResponse(context, cdnUrl);
			return;
		}

		if (request.Path.StartsWithSegments("/feed"))
		{
			var feedUrl = UriHelper.BuildAbsolute(
				request.Scheme,
				new HostString("mzikmund.dev"),
				request.PathBase,
				request.Path,
				request.QueryString);
			feedUrl = feedUrl.Replace("/feed", "/rss");
			SetResponse(context, feedUrl);
			return;
		}

		if (LegacyBlogPostRegex().Match(request.Path.Value) is { Success: true } blogPostMatch)
		{
			var postRouteName = blogPostMatch.Groups[1].Value;

			var newPostUrl = $"{NewUrl}/blog/{postRouteName}";
			SetResponse(context, newPostUrl);
			return;
		}

		// TODO: Handle category/tag pages

		SetResponse(context, NewUrl, false);
	}

	private void SetResponse(RewriteContext context, string url, bool permanent = true)
	{
		var response = context.HttpContext.Response;
		response.StatusCode = (int)(permanent ? HttpStatusCode.MovedPermanently : HttpStatusCode.TemporaryRedirect);
		response.Headers[HeaderNames.Location] = url;
		context.Result = RuleResult.EndResponse;
	}
}
