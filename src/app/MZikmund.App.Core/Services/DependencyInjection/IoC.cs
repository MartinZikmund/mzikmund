namespace MZikmund.Services.DependencyInjection;

public static class IoC
{
	private static IServiceProvider? _serviceProvider;

	public static T? GetService<T>()
	{
		if (_serviceProvider == null)
		{
			throw new InvalidOperationException("Service provider was not registered.");
		}
		return _serviceProvider.GetService<T>();
	}

	public static T GetRequiredService<T>()
	{
		if (_serviceProvider == null)
		{
			throw new InvalidOperationException("Service provider was not registered.");
		}
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
		return _serviceProvider.GetRequiredService<T>();
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
	}

	public static void RegisterProvider(IServiceProvider serviceProvider)
	{
		if (_serviceProvider != null)
		{
			throw new InvalidOperationException("Service provider can be registered only once.");
		}
		_serviceProvider = serviceProvider;
	}
}
