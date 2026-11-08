namespace MZikmund.Shared.Blog;

public static class PostPublishDates
{
	/// <summary>
	/// Uses the offset the zone has on the picked date, which differs from today's across daylight saving.
	/// </summary>
	public static DateTimeOffset Combine(DateTime date, TimeSpan time, TimeZoneInfo zone)
	{
		var local = DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified);
		return new DateTimeOffset(local, zone.GetUtcOffset(local));
	}
}
