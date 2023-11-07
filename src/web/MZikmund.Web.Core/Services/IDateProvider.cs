namespace MZikmund.Web.Core.Services;

public interface IDateProvider
{
	DateTimeOffset Now { get; }

	DateTimeOffset UtcNow { get; }
}
