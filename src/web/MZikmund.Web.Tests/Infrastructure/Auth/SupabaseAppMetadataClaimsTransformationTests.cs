using System.Security.Claims;
using MZikmund.Web.Infrastructure.Auth;

namespace MZikmund.Web.Tests.Infrastructure.Auth;

public class SupabaseAppMetadataClaimsTransformationTests
{
	private const string AdminAppMetadata = "{\"provider\":\"email\",\"providers\":[\"email\"],\"role\":\"admin\"}";
	private const string ReaderAppMetadata = "{\"provider\":\"google\",\"providers\":[\"google\"]}";

	[Fact]
	public async Task TransformAsync_AdminAppMetadata_ProjectsAdminRoleClaim()
	{
		// Arrange
		SupabaseAppMetadataClaimsTransformation transformation = new();
		var principal = CreateAuthenticatedPrincipal(AdminAppMetadata, userMetadataJson: null);

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		Assert.Equal("admin", result.FindFirst("supabase:role")?.Value);
	}

	[Fact]
	public async Task TransformAsync_ReaderAppMetadata_ProjectsNoRoleClaim()
	{
		// Arrange
		SupabaseAppMetadataClaimsTransformation transformation = new();
		var principal = CreateAuthenticatedPrincipal(ReaderAppMetadata, userMetadataJson: null);

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		Assert.Null(result.FindFirst("supabase:role"));
	}

	[Fact]
	public async Task TransformAsync_MalformedAppMetadata_ProjectsNoRoleClaimAndDoesNotThrow()
	{
		// Arrange
		SupabaseAppMetadataClaimsTransformation transformation = new();
		var principal = CreateAuthenticatedPrincipal("{not json at all", userMetadataJson: null);

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		Assert.Null(result.FindFirst("supabase:role"));
	}

	[Fact]
	public async Task TransformAsync_CalledTwice_ProjectsRoleClaimOnlyOnce()
	{
		// Arrange
		SupabaseAppMetadataClaimsTransformation transformation = new();
		var principal = CreateAuthenticatedPrincipal(AdminAppMetadata, userMetadataJson: null);

		// Act
		var once = await transformation.TransformAsync(principal);
		var twice = await transformation.TransformAsync(once);

		// Assert
		Assert.Single(twice.FindAll("supabase:role"));
	}

	[Fact]
	public async Task TransformAsync_UserMetadataFullNameAndAvatarUrl_ProjectsBoth()
	{
		// Arrange
		SupabaseAppMetadataClaimsTransformation transformation = new();
		var principal = CreateAuthenticatedPrincipal(
			ReaderAppMetadata,
			"{\"full_name\":\"Martin Zikmund\",\"avatar_url\":\"https://cdn.example/a.png\"}");

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		Assert.Equal("Martin Zikmund", result.FindFirst("supabase:display_name")?.Value);
		Assert.Equal("https://cdn.example/a.png", result.FindFirst("supabase:avatar_url")?.Value);
	}

	[Fact]
	public async Task TransformAsync_UserMetadataNameAndPictureOnly_ProjectsBoth()
	{
		// Arrange
		SupabaseAppMetadataClaimsTransformation transformation = new();
		var principal = CreateAuthenticatedPrincipal(
			ReaderAppMetadata,
			"{\"name\":\"M. Zikmund\",\"picture\":\"https://cdn.example/p.png\"}");

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		Assert.Equal("M. Zikmund", result.FindFirst("supabase:display_name")?.Value);
		Assert.Equal("https://cdn.example/p.png", result.FindFirst("supabase:avatar_url")?.Value);
	}

	[Fact]
	public async Task TransformAsync_AnonymousPrincipal_ReturnsSameInstance()
	{
		// Arrange
		SupabaseAppMetadataClaimsTransformation transformation = new();
		ClaimsPrincipal principal = new(new ClaimsIdentity());

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		Assert.Same(principal, result);
	}

	private static ClaimsPrincipal CreateAuthenticatedPrincipal(string appMetadataJson, string? userMetadataJson)
	{
		List<Claim> claims =
		[
			new Claim("sub", "8f14e45f-ceea-467a-9575-1b0e02f0b8f6"),
			new Claim("email", "martin@zikmund.dev"),
			new Claim("role", "authenticated"),
			new Claim("app_metadata", appMetadataJson)
		];

		if (userMetadataJson is not null)
		{
			claims.Add(new Claim("user_metadata", userMetadataJson));
		}

		return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
	}
}
