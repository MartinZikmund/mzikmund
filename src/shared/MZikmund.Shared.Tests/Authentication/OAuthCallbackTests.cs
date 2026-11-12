using MZikmund.Shared.Authentication;

namespace MZikmund.Shared.Tests.Authentication;

public class OAuthCallbackTests
{
	[Fact]
	public void Parse_ReturnsCode_WhenStateMatches()
	{
		var result = OAuthCallback.Parse(new Uri("http://127.0.0.1:7890/callback?code=abc%2B1&state=xyz"), "xyz");

		Assert.Equal("abc+1", result.Code);
		Assert.Null(result.Error);
	}

	[Fact]
	public void Parse_Fails_WhenStateDiffers()
	{
		var result = OAuthCallback.Parse(new Uri("http://127.0.0.1:7890/callback?code=abc&state=evil"), "xyz");

		Assert.Null(result.Code);
		Assert.Equal("state_mismatch", result.Error);
	}

	[Fact]
	public void Parse_ReturnsProviderError()
	{
		var result = OAuthCallback.Parse(
			new Uri("myapp://callback?error=access_denied&error_description=User%20cancelled&state=xyz"),
			"xyz");

		Assert.Null(result.Code);
		Assert.Equal("access_denied", result.Error);
		Assert.Equal("User cancelled", result.ErrorDescription);
	}

	[Fact]
	public void Parse_Fails_WhenCodeMissing()
	{
		var result = OAuthCallback.Parse(new Uri("http://127.0.0.1:7890/callback?state=xyz"), "xyz");

		Assert.Null(result.Code);
		Assert.Equal("missing_code", result.Error);
	}
}
