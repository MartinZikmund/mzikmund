using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System.Collections;

namespace MZikmund.Converters;

public class EmptyCollectionToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is ICollection collection)
		{
			return collection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		if (value is int count)
		{
			return count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotImplementedException();
	}
}
