using MZikmund.Shared.Authentication;

namespace MZikmund.Shared.Tests.Authentication;

public class PkceTests
{
	[Fact]
	public void CreateCodeChallenge_MatchesRfc7636Vector() =>
		Assert.Equal(
			"E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
			Pkce.CreateCodeChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));

	[Fact]
	public void CreateCodeVerifier_IsUrlSafeAndWithinRfcLength()
	{
		var verifier = Pkce.CreateCodeVerifier();

		Assert.InRange(verifier.Length, 43, 128);
		Assert.Matches("^[A-Za-z0-9_-]+$", verifier);
	}

	[Fact]
	public void CreateCodeVerifier_IsRandom() =>
		Assert.NotEqual(Pkce.CreateCodeVerifier(), Pkce.CreateCodeVerifier());
}
