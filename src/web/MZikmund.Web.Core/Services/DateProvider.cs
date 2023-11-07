namespace MZikmund.Web.Core.Services;

public class DateProvider : IDateProvider
{
	public DateTimeOffset Now => DateTimeOffset.Now;

	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
