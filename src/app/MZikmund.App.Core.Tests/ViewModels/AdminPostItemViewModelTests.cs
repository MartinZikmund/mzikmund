using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels.Items;

namespace MZikmund.App.Core.Tests.ViewModels;

public class AdminPostItemViewModelTests
{
	private static readonly DateTimeOffset Now = new(2027, 1, 15, 9, 0, 0, TimeSpan.Zero);

	public AdminPostItemViewModelTests() => TestLocalization.EnsureConfigured();

	private static AdminPostItemViewModel Row(bool? isPublished, TimeSpan fromNow) =>
		new(new PostListItem { Title = "Post", IsPublished = isPublished, PublishedDate = Now + fromNow }, Now);

	[Fact]
	public void PublishedPostWithAFutureDate_IsScheduledWithACountdown()
	{
		var row = Row(true, new TimeSpan(2, 4, 30, 0));

		Assert.True(row.IsScheduled);
		Assert.Equal("Goes live in 2d 4h", row.Countdown);
	}

	[Fact]
	public void PublishedPostWithAPastDate_IsNotScheduled()
	{
		var row = Row(true, TimeSpan.FromDays(-1));

		Assert.False(row.IsScheduled);
		Assert.Equal("", row.Countdown);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(null)]
	public void UnpublishedPostWithAFutureDate_IsNotScheduled(bool? isPublished)
	{
		var row = Row(isPublished, TimeSpan.FromDays(1));

		Assert.False(row.IsScheduled);
		Assert.Equal("", row.Countdown);
	}

	[Fact]
	public void UpdateSchedule_TicksTheCountdownDown()
	{
		var row = Row(true, TimeSpan.FromMinutes(2));

		row.UpdateSchedule(Now.AddSeconds(75));

		Assert.Equal("Goes live in 45s", row.Countdown);
	}

	[Fact]
	public void UpdateSchedule_ReportsTheMomentThePostGoesLive()
	{
		var row = Row(true, TimeSpan.FromMinutes(1));
		List<string?> changes = new();
		row.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

		row.UpdateSchedule(Now.AddMinutes(1));

		Assert.False(row.IsScheduled);
		Assert.Equal("", row.Countdown);
		Assert.Contains(nameof(AdminPostItemViewModel.IsScheduled), changes);
	}
}
