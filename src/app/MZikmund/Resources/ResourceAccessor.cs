namespace MZikmund.Resources;

public static class ResourceAccessor
{
	public static T GetResource<T>(string key) => (T)Application.Current.Resources[key];
}
