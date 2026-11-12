using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MZikmund.Web.Infrastructure.Auth;

namespace MZikmund.Web.Tests.Infrastructure.Auth;

public class AnonymousUserRejectionTests
{
	[Fact]
	public async Task OnTokenValidated_AnonymousSupabaseToken_Fails()
	{
		// Arrange
		var context = await ValidateAsync(new Claim("is_anonymous", "true", ClaimValueTypes.Boolean));

		// Assert
		Assert.False(context.Result?.Succeeded ?? true);
		Assert.NotNull(context.Result?.Failure);
	}

	[Theory]
	[InlineData("false")]
	[InlineData(null)]
	public async Task OnTokenValidated_NonAnonymousToken_IsLeftAlone(string? isAnonymous)
	{
		// Arrange
		var context = await ValidateAsync(isAnonymous is null ? null : new Claim("is_anonymous", isAnonymous, ClaimValueTypes.Boolean));

		// Assert
		Assert.Null(context.Result);
	}

	private static async Task<TokenValidatedContext> ValidateAsync(Claim? anonymousClaim)
	{
		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Supabase:ProjectRef"] = "abcdefghijklmnopqrst",
				["Supabase:PublishableKey"] = "sb_publishable_testkey"
			})
			.Build();
		ServiceCollection services = new();
		services.AddLogging();
		services.AddSupabaseAuthentication(configuration);
		using var provider = services.BuildServiceProvider();
		var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

		List<Claim> claims = [new Claim("sub", "8f14e45f-ceea-467a-9575-1b0e02f0b8f6"), new Claim("role", "authenticated")];
		if (anonymousClaim is not null)
		{
			claims.Add(anonymousClaim);
		}

		AuthenticationScheme scheme = new(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
		TokenValidatedContext context = new(new DefaultHttpContext(), scheme, options)
		{
			Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
		};

		await options.Events.TokenValidated(context);
		return context;
	}
}
