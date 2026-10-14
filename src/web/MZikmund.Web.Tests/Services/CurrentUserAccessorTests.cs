using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using MZikmund.Web.Infrastructure.Auth;
using MZikmund.Web.Services;

namespace MZikmund.Web.Tests.Services;

public class CurrentUserAccessorTests
{
	private const string AdminAppMetadata = "{\"provider\":\"email\",\"providers\":[\"email\"],\"role\":\"admin\"}";
	private const string ReaderAppMetadata = "{\"provider\":\"google\",\"providers\":[\"google\"]}";
	private const string UserId = "8f14e45f-ceea-467a-9575-1b0e02f0b8f6";

	[Fact]
	public void Current_NoHttpContext_ReturnsNull()
	{
		// Arrange
		Mock<IHttpContextAccessor> accessor = new();
		accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
		CurrentUserAccessor currentUserAccessor = new(accessor.Object);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Null(current);
	}

	[Fact]
	public void Current_AnonymousUser_ReturnsNull()
	{
		// Arrange
		var currentUserAccessor = CreateAccessor(new ClaimsPrincipal(new ClaimsIdentity()));

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Null(current);
	}

	[Fact]
	public void Current_MissingSubjectClaim_ReturnsNull()
	{
		// Arrange
		ClaimsPrincipal principal = new(new ClaimsIdentity(
			new[] { new Claim("email", "martin@zikmund.dev") },
			"Bearer"));
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Null(current);
	}

	[Fact]
	public void Current_GoogleReader_ReturnsProfileFromUserMetadata()
	{
		// Arrange
		var principal = CreateSupabaseUser(
			ReaderAppMetadata,
			"{\"full_name\":\"Martin Zikmund\",\"avatar_url\":\"https://cdn.example/a.png\"}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.NotNull(current);
		Assert.Equal(Guid.Parse(UserId), current!.Id);
		Assert.Equal("Martin Zikmund", current.DisplayName);
		Assert.Equal("https://cdn.example/a.png", current.AvatarUrl);
		Assert.Equal("martin@zikmund.dev", current.Email);
		Assert.False(current.IsAdmin);
	}

	[Fact]
	public void Current_NoDisplayNameClaim_FallsBackToEmailLocalPart()
	{
		// Arrange — Apple never returns a name in the web OAuth response.
		var principal = CreateSupabaseUser(ReaderAppMetadata, userMetadataJson: null);
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.NotNull(current);
		Assert.Equal("martin", current!.DisplayName);
		Assert.Null(current.AvatarUrl);
	}

	[Fact]
	public void Current_AdminAppMetadataRole_SetsIsAdmin()
	{
		// Arrange
		var principal = CreateSupabaseUser(AdminAppMetadata, userMetadataJson: null);
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.NotNull(current);
		Assert.True(current!.IsAdmin);
	}

	private static CurrentUserAccessor CreateAccessor(ClaimsPrincipal principal)
	{
		DefaultHttpContext httpContext = new() { User = principal };
		Mock<IHttpContextAccessor> accessor = new();
		accessor.SetupGet(a => a.HttpContext).Returns(httpContext);
		return new CurrentUserAccessor(accessor.Object);
	}

	private static ClaimsPrincipal CreateSupabaseUser(string appMetadataJson, string? userMetadataJson)
	{
		List<Claim> claims =
		[
			new Claim("sub", UserId),
			new Claim("email", "martin@zikmund.dev"),
			new Claim("role", "authenticated"),
			new Claim("app_metadata", appMetadataJson)
		];

		if (userMetadataJson is not null)
		{
			claims.Add(new Claim("user_metadata", userMetadataJson));
		}

		SupabaseAppMetadataClaimsTransformation transformation = new();
		return transformation.TransformAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))).GetAwaiter().GetResult();
	}
}
