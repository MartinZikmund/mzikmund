namespace MZikmund.Shared.Diagnostics;

public static class GuardEx
{
	public static void IsEnumValueDefined<TEnum>(TEnum value, string parameterName)
		where TEnum : struct
	{
		if (parameterName is null)
		{
			throw new ArgumentNullException(nameof(parameterName));
		}

		if (!Enum.IsDefined(typeof(TEnum), value))
		{
			throw new ArgumentOutOfRangeException(
				nameof(parameterName),
				$"Value {value} is not defined in {typeof(TEnum).Name} enum.");
		}
	}
}
