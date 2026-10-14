using Microsoft.Extensions.Configuration;
using MZikmund.Web.Configuration;

namespace MZikmund.Web.Tests.Configuration;

public class RateLimitOptionsTests
{
	[Fact]
	public void Defaults_AreTheSpecStartingLimits()
	{
		// Arrange
		RateLimitOptions options = new();

		// Act
		var create = options.CommentCreate;
		var vote = options.CommentVote;
		var report = options.CommentReport;
		var edit = options.CommentEdit;
		var globalWrites = options.GlobalWrites;

		// Assert
		Assert.Equal(5, create.PermitLimit);
		Assert.Equal(TimeSpan.FromMinutes(10), create.Window);
		Assert.Equal(30, vote.PermitLimit);
		Assert.Equal(TimeSpan.FromMinutes(1), vote.Window);
		Assert.Equal(10, report.PermitLimit);
		Assert.Equal(TimeSpan.FromHours(1), report.Window);
		Assert.Equal(20, edit.PermitLimit);
		Assert.Equal(TimeSpan.FromMinutes(10), edit.Window);
		Assert.Equal(20, globalWrites.PermitLimit);
		Assert.Equal(TimeSpan.FromMinutes(1), globalWrites.Window);
	}

	[Fact]
	public void Bind_FromRateLimitsSection_OverridesOnlyTheConfiguredRule()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["RateLimits:CommentCreate:PermitLimit"] = "2",
				["RateLimits:CommentCreate:WindowMinutes"] = "3"
			})
			.Build();

		// Act
		var options = configuration.GetSection("RateLimits").Get<RateLimitOptions>() ?? new RateLimitOptions();

		// Assert
		Assert.Equal(2, options.CommentCreate.PermitLimit);
		Assert.Equal(TimeSpan.FromMinutes(3), options.CommentCreate.Window);
		Assert.Equal(30, options.CommentVote.PermitLimit);
	}

	[Fact]
	public void Bind_WithNoRateLimitsSection_FallsBackToTheDefaults()
	{
		// Arrange
		var configuration = new ConfigurationBuilder().Build();

		// Act
		var options = configuration.GetSection("RateLimits").Get<RateLimitOptions>() ?? new RateLimitOptions();

		// Assert
		Assert.Equal(5, options.CommentCreate.PermitLimit);
		Assert.Equal(20, options.GlobalWrites.PermitLimit);
	}
}
