using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MZikmund.Web.Data.Entities.Configurations;

/// <summary>
/// SQL Server hands back <see cref="DateTimeKind.Unspecified"/> values, which serialize without a
/// <c>Z</c>. Writing UTC and reading it back as UTC keeps timestamps unambiguous end to end.
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
	public UtcDateTimeConverter()
		: base(
			value => value.ToUniversalTime(),
			value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
	{
	}
}
