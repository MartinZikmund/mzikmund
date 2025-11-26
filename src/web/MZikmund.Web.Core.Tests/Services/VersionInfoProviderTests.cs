using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Core.Tests.Services;

public class VersionInfoProviderTests
{
	[Fact]
	public void VersionIsNotNull()
	{
		var provider = new VersionInfoProvider();
		Assert.NotNull(provider.Version);
		Assert.NotEmpty(provider.Version);
	}

	[Fact]
	public void CommitUrlIsValidWhenCommitShaExists()
	{
		var provider = new VersionInfoProvider();
		
		if (!string.IsNullOrEmpty(provider.CommitSha))
		{
			Assert.NotNull(provider.CommitUrl);
			Assert.StartsWith("https://github.com/MartinZikmund/mzikmund/commit/", provider.CommitUrl);
			Assert.Contains(provider.CommitSha, provider.CommitUrl);
		}
	}

	[Fact]
	public void CommitShaIsNullOrValidFormat()
	{
		var provider = new VersionInfoProvider();
		
		if (provider.CommitSha != null)
		{
			// Commit SHA should be a hex string with reasonable length
			Assert.Matches("^[0-9a-f]{7,40}$", provider.CommitSha);
		}
	}
}
