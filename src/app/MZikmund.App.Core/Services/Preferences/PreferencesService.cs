using System.Text.Json;

namespace MZikmund.Services.Preferences;

public class PreferencesService : IPreferencesService
{
	private readonly Dictionary<string, object> _preferenceCache = new Dictionary<string, object>();

	public T? GetComplex<T>(string key, Func<T>? defaultValueBuilder = null, bool roamed = false)
	{
		if (!_preferenceCache.TryGetValue(key, out var boxedValue))
		{
			// retrieve directly from settings and cache
			var value = RetrieveComplex(key, defaultValueBuilder, roamed);
			_preferenceCache.Add(key, value!);
			return value;
		}
		else
		{
			return (T)boxedValue;
		}
	}

	public T GetSetting<T>(string key, Func<T>? defaultValueBuilder = null, bool roamed = false)
	{
		if (!_preferenceCache.TryGetValue(key, out var boxedValue))
		{
			// retrieve directly from settings and cache
			var value = RetrieveSetting(key, defaultValueBuilder, roamed);
			_preferenceCache.Add(key, value!);
			return value;
		}
		else
		{
			return (T)boxedValue;
		}
	}

	public void SetComplex<T>(string key, T value, bool roamed = false)
	{
		var container = roamed ? ApplicationData.Current.RoamingSettings : ApplicationData.Current.LocalSettings;
		var serialized = JsonSerializer.Serialize(value, PreferencesSerializerContext.Default.Options);
		if (container.Values.ContainsKey(key))
		{
			container.Values[key] = serialized;
		}
		else
		{
			container.Values.Add(key, serialized);
		}
		_preferenceCache[key] = value!;
	}

	public void SetSetting<T>(string key, T value, bool roamed = false)
	{
		var container = roamed ? ApplicationData.Current.RoamingSettings : ApplicationData.Current.LocalSettings;
		if (container.Values.ContainsKey(key))
		{
			container.Values[key] = value;
		}
		else
		{
			container.Values.Add(key, value);
		}
		_preferenceCache[key] = value!;
	}

	private T? RetrieveComplex<T>(string key, Func<T>? defaultValueBuilder = null, bool roamed = false)
	{
		var container = roamed ? ApplicationData.Current.RoamingSettings : ApplicationData.Current.LocalSettings;
		if (container.Values.TryGetValue(key, out var value))
		{
			//get existing
			try
			{
				var serialized = (string)value;
				return JsonSerializer.Deserialize<T>(serialized, PreferencesSerializerContext.Default.Options);
			}
			catch
			{
#if DEBUG
				throw new InvalidOperationException("Value stored in the setting does not match expected type.");
#else
				//invalid value, remove
				container.Values.Remove(key);
#endif
			}
		}
		if (defaultValueBuilder == null)
		{
			return default!;
		}
		else
		{
			return defaultValueBuilder();
		}
	}

	private T RetrieveSetting<T>(string key, Func<T>? defaultValueBuilder = null, bool roamed = false)
	{
		var container = roamed ? ApplicationData.Current.RoamingSettings : ApplicationData.Current.LocalSettings;
		if (container.Values.TryGetValue(key, out var value))
		{
			//get existing
			try
			{
				return (T)value;
			}
			catch
			{
#if DEBUG
				throw new InvalidOperationException("Value stored in the setting does not match expected type.");
#else
				//invalid value, remove
				container.Values.Remove(key);
#endif
			}
		}
		if (defaultValueBuilder == null)
		{
			return default!;
		}
		else
		{
			return defaultValueBuilder();
		}
	}
}
