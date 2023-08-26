using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MZikmund.Web.Core.Infrastructure;

namespace MZikmund.Web.Core.Tests.Infrastructure;

public class LegacyUrlRedirectRuleTests
{
	[Theory]
	[InlineData("https://mzikmund.com", "https://mzikmund.dev")]
	[InlineData("https://www.mzikmund.com", "https://mzikmund.dev")]
	[InlineData("https://blog.mzikmund.dev/wp-content/uploads/2022/12/christmastree.png", "https://cdn.mzikmund.dev/wp-content/uploads/2022/12/christmastree.png")]
	[InlineData("https://blog.mzikmund.com/wp-content/uploads/2022/12/christmastree.png", "https://cdn.mzikmund.dev/wp-content/uploads/2022/12/christmastree.png")]
	[InlineData("https://blog.mzikmund.dev/2020/04/reseni-problemu-uiwindow-uno/", "https://mzikmund.dev/blog/reseni-problemu-uiwindow-uno")]
	[InlineData("https://blog.mzikmund.com/2020/04/reseni-problemu-uiwindow-uno/", "https://mzikmund.dev/blog/reseni-problemu-uiwindow-uno")]
	[InlineData("https://blog.mzikmund.com/somethingelse/", "https://mzikmund.dev")]
	[InlineData("https://blog.mzikmund.dev/somethingelse/", "https://mzikmund.dev")]
	public async Task CheckRedirectPath(string requestUrl, string expectedUrl)
	{
		var options = new RewriteOptions().Add(new LegacyUrlRedirectRule());
		using var host = new HostBuilder()
			.ConfigureWebHost(webHostBuilder =>
			{
				webHostBuilder
				.UseTestServer()
				.Configure(app =>
				{
					app.UseRewriter(options);
				});
			}).Build();

		await host.StartAsync();

		var server = host.GetTestServer();

		var response = await server.CreateClient().GetAsync(requestUrl);

		Assert.Equal(expectedUrl, response.Headers.Location!.OriginalString);
	}
}
