using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Localization;
using Windows.ApplicationModel.Resources;

namespace MZikmund.Services.Localization;

public class Localizer
{
	private static readonly Lazy<IStringLocalizer> _stringLocalizer = new(Ioc.Default.GetRequiredService<IStringLocalizer>);

	private Localizer()
	{
	}

	public static Localizer Instance { get; } = new Localizer();

	public string GetString(string key)
	{
		var result = _stringLocalizer.Value.GetString(key);
		return !string.IsNullOrEmpty(result) ? result : $"???{key}???";
	}

	public string this[string key] => GetString(key);
}
