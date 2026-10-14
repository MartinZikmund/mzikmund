using MZikmund.Shared.Authentication;

namespace MZikmund.Shared.Tests.Authentication;

public class SupabaseAuthUrisTests
{
	[Fact]
	public void Authorize_BuildsEscapedPkceUri()
	{
		// Arrange
		Uri redirect = new("http://127.0.0.1:51842/callback");

		// Act
		var uri = SupabaseAuthUris.Authorize("http://127.0.0.1:54321", SupabaseOAuthProvider.Google, redirect, "ch+al/len=ge");

		// Assert
		Assert.Equal(
			"http://127.0.0.1:54321/auth/v1/authorize?provider=google" +
			"&redirect_to=http%3A%2F%2F127.0.0.1%3A51842%2Fcallback" +
			"&code_challenge=ch%2Bal%2Flen%3Dge&code_challenge_method=s256",
			uri.AbsoluteUri);
	}

	[Fact]
	public void Authorize_LowerCasesTheProvider()
	{
		// Arrange / Act
		var uri = SupabaseAuthUris.Authorize("https://x.supabase.co", SupabaseOAuthProvider.Apple, new Uri("mzikmund-app://oauthcallback"), "c");

		// Assert
		Assert.Contains("provider=apple&", uri.AbsoluteUri);
	}

	[Theory]
	[InlineData("https://x.supabase.co")]
	[InlineData("https://x.supabase.co/")]
	public void Endpoint_ToleratesATrailingSlash(string baseUrl)
	{
		// Arrange / Act
		var uri = SupabaseAuthUris.Endpoint(baseUrl, "token?grant_type=pkce");

		// Assert
		Assert.Equal("https://x.supabase.co/auth/v1/token?grant_type=pkce", uri.AbsoluteUri);
	}
}
