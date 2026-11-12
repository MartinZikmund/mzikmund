using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MZikmund.Web.Configuration;

namespace MZikmund.Web.Core.Tests.Configuration;

public class CommentsConfigurationTests
{
	[Fact]
	public void BaseUrl_WithProjectRefOnly_PointsAtHostedSupabase()
	{
		// Arrange
		SupabaseOptions options = new() { ProjectRef = "abcdefghijklmnop" };

		// Act
		var baseUrl = options.BaseUrl;

		// Assert
		Assert.Equal("https://abcdefghijklmnop.supabase.co", baseUrl);
	}

	[Fact]
	public void BaseUrl_WithLocalUrl_PrefersTheLocalStack()
	{
		// Arrange
		SupabaseOptions options = new()
		{
			ProjectRef = "abcdefghijklmnop",
			LocalUrl = "http://127.0.0.1:54321/"
		};

		// Act
		var baseUrl = options.BaseUrl;

		// Assert
		Assert.Equal("http://127.0.0.1:54321", baseUrl);
	}

	[Fact]
	public void IssuerAndJwksUri_AreDerivedFromBaseUrl()
	{
		// Arrange
		SupabaseOptions options = new() { LocalUrl = "http://127.0.0.1:54321" };

		// Act & Assert
		Assert.Equal("http://127.0.0.1:54321/auth/v1", options.Issuer);
		Assert.Equal("http://127.0.0.1:54321/auth/v1/.well-known/jwks.json", options.JwksUri);
	}

	[Fact]
	public void Configure_BindsCommentsSupabaseAndTurnstileSections()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Comments:Enabled"] = "true",
				["Supabase:ProjectRef"] = "abcdefghijklmnop",
				["Supabase:PublishableKey"] = "sb_publishable_test",
				["Supabase:LocalUrl"] = "http://127.0.0.1:54321",
				["Turnstile:SiteKey"] = "1x00000000000000000000AA",
				["Turnstile:SecretKey"] = "1x0000000000000000000000000000000AA"
			})
			.Build();

		ServiceCollection services = new();
		services.Configure<CommentsOptions>(configuration.GetSection("Comments"));
		services.Configure<SupabaseOptions>(configuration.GetSection("Supabase"));
		services.Configure<TurnstileOptions>(configuration.GetSection("Turnstile"));

		// Act
		using var provider = services.BuildServiceProvider();
		var comments = provider.GetRequiredService<IOptions<CommentsOptions>>().Value;
		var supabase = provider.GetRequiredService<IOptions<SupabaseOptions>>().Value;
		var turnstile = provider.GetRequiredService<IOptions<TurnstileOptions>>().Value;

		// Assert
		Assert.True(comments.Enabled);
		Assert.Equal("abcdefghijklmnop", supabase.ProjectRef);
		Assert.Equal("sb_publishable_test", supabase.PublishableKey);
		Assert.Equal("http://127.0.0.1:54321/auth/v1", supabase.Issuer);
		Assert.Equal("1x00000000000000000000AA", turnstile.SiteKey);
		Assert.Equal("1x0000000000000000000000000000000AA", turnstile.SecretKey);
	}
}
