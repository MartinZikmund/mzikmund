using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MZikmund.Web.Configuration;
using MZikmund.Web.Infrastructure;

namespace MZikmund.Web.Tests.Infrastructure;

public class CommentRateLimitingTests
{
	[Fact]
	public async Task CommentWrite_PastTheLimit_Returns429WithRetryAfter()
	{
		// Arrange
		RateLimitOptions limits = new() { CommentCreate = new() { PermitLimit = 2, WindowMinutes = 10 } };
		using var host = await StartHostAsync(limits);
		using var client = host.GetTestClient();

		// Act
		var first = await client.PostAsync("/comment", null);
		var second = await client.PostAsync("/comment", null);
		var third = await client.PostAsync("/comment", null);

		// Assert
		Assert.Equal(HttpStatusCode.OK, first.StatusCode);
		Assert.Equal(HttpStatusCode.OK, second.StatusCode);
		Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
		Assert.True(third.Headers.TryGetValues("Retry-After", out var retryAfter));
		Assert.InRange(int.Parse(retryAfter!.Single(), System.Globalization.CultureInfo.InvariantCulture), 1, 600);
	}

	[Fact]
	public async Task NonCommentWrite_IsNeverThrottled_EvenWhenTheCommentIpLimitIsTiny()
	{
		// Arrange
		RateLimitOptions limits = new() { GlobalWrites = new() { PermitLimit = 1, WindowMinutes = 1 } };
		using var host = await StartHostAsync(limits);
		using var client = host.GetTestClient();

		// Act
		List<HttpStatusCode> statuses = new();
		for (var i = 0; i < 5; i++)
		{
			statuses.Add((await client.PostAsync("/admin-upload", null)).StatusCode);
		}

		// Assert
		Assert.All(statuses, status => Assert.Equal(HttpStatusCode.OK, status));
	}

	[Fact]
	public async Task CommentReads_AreNeverThrottled()
	{
		// Arrange
		RateLimitOptions limits = new() { GlobalWrites = new() { PermitLimit = 1, WindowMinutes = 1 } };
		using var host = await StartHostAsync(limits);
		using var client = host.GetTestClient();

		// Act
		List<HttpStatusCode> statuses = new();
		for (var i = 0; i < 5; i++)
		{
			statuses.Add((await client.GetAsync("/comment")).StatusCode);
		}

		// Assert
		Assert.All(statuses, status => Assert.Equal(HttpStatusCode.OK, status));
	}

	[Fact]
	public async Task CommentWrites_ShareThePerIpLimit_AcrossPolicies()
	{
		// Arrange
		RateLimitOptions limits = new() { GlobalWrites = new() { PermitLimit = 2, WindowMinutes = 1 } };
		using var host = await StartHostAsync(limits);
		using var client = host.GetTestClient();

		// Act
		var first = await client.PostAsync("/comment", null);
		var second = await client.PutAsync("/vote", null);
		var third = await client.PostAsync("/report", null);

		// Assert
		Assert.Equal(HttpStatusCode.OK, first.StatusCode);
		Assert.Equal(HttpStatusCode.OK, second.StatusCode);
		Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
	}

	private static async Task<IHost> StartHostAsync(RateLimitOptions limits) =>
		await new HostBuilder()
			.ConfigureWebHost(web => web
				.UseTestServer()
				.ConfigureServices(services =>
				{
					services.AddRouting();
					services.AddCommentRateLimiting(limits);
				})
				.Configure(app =>
				{
					app.UseRouting();
					app.UseRateLimiter();
					app.UseEndpoints(endpoints =>
					{
						endpoints.MapGet("/comment", () => "ok");
						endpoints.MapPost("/comment", () => "ok").RequireRateLimiting(RateLimitPolicies.CommentCreate);
						endpoints.MapPut("/vote", () => "ok").RequireRateLimiting(RateLimitPolicies.CommentVote);
						endpoints.MapPost("/report", () => "ok").RequireRateLimiting(RateLimitPolicies.CommentReport);
						endpoints.MapPost("/admin-upload", () => "ok");
					});
				}))
			.StartAsync();
}
