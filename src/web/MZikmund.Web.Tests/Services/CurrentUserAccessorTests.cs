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
			"{\"full_name\":\"Jane Doe\",\"avatar_url\":\"https://lh3.googleusercontent.com/a.png\"}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.NotNull(current);
		Assert.Equal(Guid.Parse(UserId), current!.Id);
		Assert.Equal("Jane Doe", current.DisplayName);
		Assert.Equal("https://lh3.googleusercontent.com/a.png", current.AvatarUrl);
		Assert.Equal("martin@zikmund.dev", current.Email);
		Assert.False(current.IsAdmin);
	}

	[Fact]
	public void Current_NoDisplayNameClaim_FallsBackToReaderNameNotTheEmail()
	{
		// Arrange — Apple never returns a name in the web OAuth response.
		var principal = CreateSupabaseUser(ReaderAppMetadata, userMetadataJson: null);
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.NotNull(current);
		Assert.Matches("^Reader [0-9A-F]{4}$", current!.DisplayName);
		Assert.DoesNotContain("martin", current.DisplayName, StringComparison.OrdinalIgnoreCase);
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

	[Theory]
	[InlineData("http://lh3.googleusercontent.com/a.png")]
	[InlineData("javascript:alert(1)")]
	[InlineData("data:image/png;base64,AAAA")]
	[InlineData("//lh3.googleusercontent.com/a.png")]
	[InlineData("/relative/a.png")]
	[InlineData("https://user:secret@lh3.googleusercontent.com/a.png")]
	[InlineData("not a url")]
	public void Current_NonHttpsOrUnsafeAvatarUrl_IsDropped(string avatarUrl)
	{
		// Arrange
		var principal = CreateSupabaseUser(
			ReaderAppMetadata,
			$"{{\"full_name\":\"Martin\",\"avatar_url\":\"{avatarUrl}\"}}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.NotNull(current);
		Assert.Null(current!.AvatarUrl);
	}

	[Fact]
	public void Current_EmailUserWithAvatarUrl_HasNoAvatar()
	{
		// Arrange
		var principal = CreateSupabaseUser(
			"{\"provider\":\"email\",\"providers\":[\"email\"]}",
			"{\"avatar_url\":\"https://lh3.googleusercontent.com/a.png\"}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Null(current!.AvatarUrl);
	}

	[Theory]
	[InlineData("https://googleusercontent.com/a.png", true)]
	[InlineData("https://lh3.googleusercontent.com/a.png", true)]
	[InlineData("https://cdn.example/a.png", false)]
	[InlineData("https://evilgoogleusercontent.com/a.png", false)]
	[InlineData("https://googleusercontent.com.evil.example/a.png", false)]
	public void Current_GoogleUser_KeepsOnlyGoogleAvatarHosts(string avatarUrl, bool kept)
	{
		// Arrange
		var principal = CreateSupabaseUser(ReaderAppMetadata, $"{{\"avatar_url\":\"{avatarUrl}\"}}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Equal(kept ? avatarUrl : null, current!.AvatarUrl);
	}

	[Fact]
	public void Current_OverlongAvatarUrl_IsDropped()
	{
		// Arrange
		var url = "https://lh3.googleusercontent.com/" + new string('a', 2040);
		var principal = CreateSupabaseUser(ReaderAppMetadata, $"{{\"full_name\":\"Martin\",\"avatar_url\":\"{url}\"}}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Null(current!.AvatarUrl);
	}

	[Fact]
	public void Current_DisplayName_IsTrimmedAndStrippedOfControlCharacters()
	{
		// Arrange
		var principal = CreateSupabaseUser(ReaderAppMetadata, "{\"full_name\":\"  Jane\\u0000 \\u200BDoe\\n\"}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Equal("Jane Doe", current!.DisplayName);
	}

	[Theory]
	[InlineData("Martin Zikmund")]
	[InlineData("Admin")]
	[InlineData("J")]
	[InlineData("!!")]
	public void Current_ReservedOrInvalidTokenName_FallsBackToReaderNameForReaders(string name)
	{
		// Arrange
		var principal = CreateSupabaseUser(ReaderAppMetadata, $"{{\"full_name\":\"{name}\"}}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Matches("^Reader [0-9A-F]{4}$", current!.DisplayName);
	}

	[Fact]
	public void Current_ReservedTokenName_IsKeptForAdmins()
	{
		// Arrange
		var principal = CreateSupabaseUser(AdminAppMetadata, "{\"full_name\":\"Martin Zikmund\"}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Equal("Martin Zikmund", current!.DisplayName);
	}

	[Fact]
	public void Current_OverlongTokenName_FallsBackToReaderName()
	{
		// Arrange
		var principal = CreateSupabaseUser(ReaderAppMetadata, $"{{\"full_name\":\"{new string('x', 400)}\"}}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Matches("^Reader [0-9A-F]{4}$", current!.DisplayName);
	}

	[Fact]
	public void Current_WhitespaceOnlyDisplayName_FallsBackToReaderName()
	{
		// Arrange
		var principal = CreateSupabaseUser(ReaderAppMetadata, "{\"full_name\":\"  	 \"}");
		var currentUserAccessor = CreateAccessor(principal);

		// Act
		var current = currentUserAccessor.Current;

		// Assert
		Assert.Matches("^Reader [0-9A-F]{4}$", current!.DisplayName);
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
