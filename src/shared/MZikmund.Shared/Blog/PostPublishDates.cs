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

	/// <summary>
	/// The server reports such a post as published, but it stays hidden until its date passes.
	/// </summary>
	public static bool IsScheduled(bool? isPublished, DateTimeOffset? publishedDate, DateTimeOffset now) =>
		isPublished == true && publishedDate > now;
}
