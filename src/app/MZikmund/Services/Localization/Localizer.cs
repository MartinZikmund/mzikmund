using Windows.ApplicationModel.Resources;

namespace MZikmund.Services.Localization;

public class Localizer
{
	private static ResourceLoader? _resourceLoader;

	private Localizer()
	{
	}

	public static Localizer Instance { get; } = new Localizer();

	public string GetString(string key)
	{
		_resourceLoader ??= ResourceLoader.GetForViewIndependentUse();
		var result = _resourceLoader.GetString(key);
		return !string.IsNullOrEmpty(result) ? result : $"???{key}???";
	}

	public string this[string key] => GetString(key);
}
