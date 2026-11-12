using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MZikmund.Web.Infrastructure.Auth;

namespace MZikmund.Web.Tests.Infrastructure.Auth;

public class AdminPolicyTests
{
	private const string AdminAppMetadata = "{\"provider\":\"email\",\"providers\":[\"email\"],\"role\":\"admin\"}";
	private const string ReaderAppMetadata = "{\"provider\":\"google\",\"providers\":[\"google\"]}";

	[Fact]
	public async Task AdminPolicy_AdminAppMetadataRole_Succeeds()
	{
		// Arrange
		var authorizationService = CreateAuthorizationService();
		var user = await CreateSupabaseUserAsync(AdminAppMetadata);

		// Act
		var result = await authorizationService.AuthorizeAsync(user, null, SupabaseAuthenticationExtensions.AdminPolicyName);

		// Assert
		Assert.True(result.Succeeded);
	}

	[Fact]
	public async Task AdminPolicy_PlainReaderToken_Fails()
	{
		// Arrange
		var authorizationService = CreateAuthorizationService();
		var user = await CreateSupabaseUserAsync(ReaderAppMetadata);

		// Act
		var result = await authorizationService.AuthorizeAsync(user, null, SupabaseAuthenticationExtensions.AdminPolicyName);

		// Assert
		Assert.False(result.Succeeded);
	}

	[Fact]
	public async Task AdminPolicy_SupabasePostgresRoleClaimOnly_Fails()
	{
		// Arrange — the flat "role":"authenticated" claim every signed-in user carries must never grant admin.
		var authorizationService = CreateAuthorizationService();
		var user = await CreateSupabaseUserAsync("{\"provider\":\"google\",\"providers\":[\"google\"],\"role\":\"authenticated\"}");

		// Act
		var result = await authorizationService.AuthorizeAsync(user, null, SupabaseAuthenticationExtensions.AdminPolicyName);

		// Assert
		Assert.False(result.Succeeded);
	}

	[Fact]
	public async Task AdminPolicy_LegacyAuth0PermissionsClaim_Fails()
	{
		// Arrange
		var authorizationService = CreateAuthorizationService();
		ClaimsPrincipal user = new(new ClaimsIdentity(
			new[]
			{
				new Claim("sub", "auth0|abc123"),
				new Claim("permissions", "admin:all")
			},
			"Bearer"));

		// Act
		var result = await authorizationService.AuthorizeAsync(user, null, SupabaseAuthenticationExtensions.AdminPolicyName);

		// Assert
		Assert.False(result.Succeeded);
	}

	[Fact]
	public async Task AdminPolicy_AnonymousUser_Fails()
	{
		// Arrange
		var authorizationService = CreateAuthorizationService();
		ClaimsPrincipal user = new(new ClaimsIdentity());

		// Act
		var result = await authorizationService.AuthorizeAsync(user, null, SupabaseAuthenticationExtensions.AdminPolicyName);

		// Assert
		Assert.False(result.Succeeded);
	}

	[Fact]
	public void AddSupabaseAuthentication_MissingConfiguration_DoesNotThrow()
	{
		// Arrange — the JWKS is fetched lazily, so an unconfigured site must still boot and serve anonymous pages.
		IConfiguration configuration = new ConfigurationBuilder().Build();
		ServiceCollection services = new();
		services.AddLogging();

		// Act
		var result = services.AddSupabaseAuthentication(configuration);

		// Assert
		Assert.Same(services, result);
	}

	[Fact]
	public void AddSupabaseAuthentication_LocalUrlOnly_DoesNotThrow()
	{
		// Arrange — the local CLI stack has no project ref.
		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Supabase:LocalUrl"] = "http://127.0.0.1:54321",
				["Supabase:PublishableKey"] = "sb_publishable_localtestkey"
			})
			.Build();

		ServiceCollection services = new();
		services.AddLogging();

		// Act
		var result = services.AddSupabaseAuthentication(configuration);

		// Assert
		Assert.Same(services, result);
	}

	private static IAuthorizationService CreateAuthorizationService()
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
		return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
	}

	private static async Task<ClaimsPrincipal> CreateSupabaseUserAsync(string appMetadataJson)
	{
		ClaimsPrincipal principal = new(new ClaimsIdentity(
			new[]
			{
				new Claim("sub", "8f14e45f-ceea-467a-9575-1b0e02f0b8f6"),
				new Claim("email", "martin@zikmund.dev"),
				new Claim("role", "authenticated"),
				new Claim("app_metadata", appMetadataJson)
			},
			"Bearer"));

		SupabaseAppMetadataClaimsTransformation transformation = new();
		return await transformation.TransformAsync(principal);
	}
}
