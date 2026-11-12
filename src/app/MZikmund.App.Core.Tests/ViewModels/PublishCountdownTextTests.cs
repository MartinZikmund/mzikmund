using MZikmund.ViewModels.Items;

namespace MZikmund.App.Core.Tests.ViewModels;

public class PublishCountdownTextTests
{
	public PublishCountdownTextTests() => TestLocalization.EnsureConfigured();

	[Theory]
	[InlineData(2, 4, 30, 15, "Goes live in 2d 4h")]
	[InlineData(0, 4, 12, 5, "Goes live in 4h 12m")]
	[InlineData(0, 0, 12, 30, "Goes live in 12m 30s")]
	[InlineData(0, 0, 0, 45, "Goes live in 45s")]
	[InlineData(1, 0, 0, 0, "Goes live in 1d 0h")]
	public void Format_ShowsTheTwoLargestUnits(int days, int hours, int minutes, int seconds, string expected) =>
		Assert.Equal(expected, PublishCountdownText.Format(new TimeSpan(days, hours, minutes, seconds)));

	[Fact]
	public void Format_NeverReadsZeroWhileThePostIsStillPending() =>
		Assert.Equal("Goes live in 1s", PublishCountdownText.Format(TimeSpan.FromMilliseconds(200)));

	[Fact]
	public void Format_RoundsUpAcrossAUnitBoundary() =>
		Assert.Equal("Goes live in 1m 0s", PublishCountdownText.Format(TimeSpan.FromSeconds(59.5)));
}
