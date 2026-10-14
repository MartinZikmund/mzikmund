using System.Text;
using System.Text.Json;
using MZikmund.Shared.Authentication;

namespace MZikmund.Shared.Tests.Authentication;

public class SupabaseAccessTokenTests
{
	private const long Exp = 1900000000;

	[Fact]
	public void Read_IsAdmin_WhenRoleIsInAppMetadata()
	{
		// Arrange
		var token = CreateToken(new { sub = "u1", exp = Exp, app_metadata = new { role = "admin" } });

		// Act
		var identity = SupabaseAccessToken.Read(token);

		// Assert
		Assert.True(identity.IsAdmin);
	}

	[Fact]
	public void Read_IsNotAdmin_WhenRoleIsOnlyInUserMetadata()
	{
		// Arrange
		var token = CreateToken(new { sub = "u1", exp = Exp, user_metadata = new { role = "admin" } });

		// Act
		var identity = SupabaseAccessToken.Read(token);

		// Assert
		Assert.False(identity.IsAdmin);
	}

	[Fact]
	public void Read_IsNotAdmin_WhenOnlyThePostgresRoleClaimIsPresent()
	{
		// Arrange
		var token = CreateToken(new { sub = "u1", exp = Exp, role = "authenticated" });

		// Act
		var identity = SupabaseAccessToken.Read(token);

		// Assert
		Assert.False(identity.IsAdmin);
	}

	[Fact]
	public void Read_PrefersFullNameOverNameAndEmail()
	{
		// Arrange
		var token = CreateToken(new { sub = "u1", exp = Exp, email = "a@b.c", user_metadata = new { full_name = "Full Name", name = "Short" } });

		// Act
		var identity = SupabaseAccessToken.Read(token);

		// Assert
		Assert.Equal("Full Name", identity.DisplayName);
	}

	[Fact]
	public void Read_FallsBackToNameThenEmailThenUser()
	{
		// Arrange / Act
		var byName = SupabaseAccessToken.Read(CreateToken(new { sub = "u1", exp = Exp, email = "a@b.c", user_metadata = new { name = "Short" } }));
		var byEmail = SupabaseAccessToken.Read(CreateToken(new { sub = "u1", exp = Exp, email = "a@b.c" }));
		var anonymous = SupabaseAccessToken.Read(CreateToken(new { sub = "u1", exp = Exp }));

		// Assert
		Assert.Equal("Short", byName.DisplayName);
		Assert.Equal("a@b.c", byEmail.DisplayName);
		Assert.Equal("User", anonymous.DisplayName);
	}

	[Fact]
	public void Read_AvatarFallsBackToPicture()
	{
		// Arrange / Act
		var avatar = SupabaseAccessToken.Read(CreateToken(new { sub = "u1", exp = Exp, user_metadata = new { avatar_url = "https://a/1.png", picture = "https://a/2.png" } }));
		var picture = SupabaseAccessToken.Read(CreateToken(new { sub = "u1", exp = Exp, user_metadata = new { picture = "https://a/2.png" } }));

		// Assert
		Assert.Equal("https://a/1.png", avatar.AvatarUrl);
		Assert.Equal("https://a/2.png", picture.AvatarUrl);
	}

	[Fact]
	public void Read_MapsSubjectIssuerAndExpiry()
	{
		// Arrange
		var token = CreateToken(new { sub = "u1", iss = "http://127.0.0.1:54321/auth/v1", exp = Exp });

		// Act
		var identity = SupabaseAccessToken.Read(token);

		// Assert
		Assert.Equal("u1", identity.UserId);
		Assert.Equal("http://127.0.0.1:54321/auth/v1", identity.Issuer);
		Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(Exp), identity.ExpiresOn);
	}

	[Theory]
	[InlineData("")]
	[InlineData("not-a-jwt")]
	[InlineData("a.!!!.c")]
	[InlineData("a.e30.c")]
	public void Read_Throws_WhenTheTokenIsMalformed(string token)
	{
		// Arrange / Act / Assert
		Assert.Throws<FormatException>(() => SupabaseAccessToken.Read(token));
	}

	private static string CreateToken(object payload) =>
		$"{Encode("{\"alg\":\"ES256\"}")}.{Encode(JsonSerializer.Serialize(payload))}.sig";

	private static string Encode(string json) =>
		Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
