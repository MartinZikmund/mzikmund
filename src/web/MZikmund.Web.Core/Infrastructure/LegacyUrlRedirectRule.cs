using System.CodeDom.Compiler;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;

namespace MZikmund.Web.Core.Infrastructure;

public partial class LegacyUrlRedirectRule : IRule
{
	private const string BlogLegacyUrl = "blog.mzikmund.";
	private const string ComLegacyUrl = "mzikmund.com";
	private const string WwwComLegacyUrl = "www.mzikmund.com";

	private const string NewUrl = "https://mzikmund.dev";

	[GeneratedRegex(@"\d{4}/\d{1,2}/([a-zA-Z0-9\-]+)/?", RegexOptions.None)]
	private static partial Regex LegacyBlogPostRegex();

	public void ApplyRule(RewriteContext context)
	{
		var hostString = context.HttpContext?.Request?.Host.Host;

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

		if (LegacyBlogPostRegex().Match(request.Path.Value) is { Success: true } blogPostMatch)
		{
			var postRouteName = blogPostMatch.Groups[1].Value;

			var newPostUrl = $"{NewUrl}/blog/{postRouteName}";
			SetResponse(context, newPostUrl);
			return;
		}

		SetResponse(context, NewUrl);
	}

	private void SetResponse(RewriteContext context, string url)
	{
		var response = context.HttpContext.Response;
		response.StatusCode = (int)HttpStatusCode.MovedPermanently;
		response.Headers[HeaderNames.Location] = url;
		context.Result = RuleResult.EndResponse;
	}
}
