using MZikmund.Shared.Blog;

namespace MZikmund.Shared.Tests.Blog;

public class PostPublishDatesTests
{
	private static readonly TimeZoneInfo Prague = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

	[Theory]
	[InlineData(2027, 1, 15, 1)]
	[InlineData(2027, 7, 1, 2)]
	public void Combine_UsesTheOffsetOfThePickedDate(int year, int month, int day, int expectedOffsetHours)
	{
		var result = PostPublishDates.Combine(new DateTime(year, month, day), new TimeSpan(9, 0, 0), Prague);

		Assert.Equal(new DateTime(year, month, day, 9, 0, 0), result.DateTime);
		Assert.Equal(TimeSpan.FromHours(expectedOffsetHours), result.Offset);
	}

	[Fact]
	public void Combine_IgnoresTheTimeAndKindOfThePickedDate()
	{
		var picked = new DateTime(2027, 7, 1, 23, 59, 0, DateTimeKind.Utc);

		var result = PostPublishDates.Combine(picked, new TimeSpan(9, 0, 0), Prague);

		Assert.Equal(new DateTimeOffset(2027, 7, 1, 9, 0, 0, TimeSpan.FromHours(2)), result);
	}

	[Fact]
	public void Combine_AcceptsATimeSkippedByTheClockChange()
	{
		var result = PostPublishDates.Combine(new DateTime(2027, 3, 28), new TimeSpan(2, 30, 0), Prague);

		Assert.Equal(TimeSpan.FromHours(1), result.Offset);
	}
}
