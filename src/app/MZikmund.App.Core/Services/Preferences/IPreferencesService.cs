namespace MZikmund.Services.Preferences;

public interface IPreferencesService
{
	T? GetComplex<T>(string key, Func<T>? defaultValueBuilder = null, bool roamed = false);

	void SetComplex<T>(string key, T value, bool roamed = false);

	T GetSetting<T>(string key, Func<T>? defaultValueBuilder = null, bool roamed = false);

	void SetSetting<T>(string key, T value, bool roamed = false);
}
