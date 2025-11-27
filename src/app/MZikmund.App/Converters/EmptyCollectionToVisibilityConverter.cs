using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System.Collections;

namespace MZikmund.Converters;

public class EmptyCollectionToVisibilityConverter : IValueConverter
{
	public bool Invert { get; set; }

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		var notEmptyVisibility = Invert ? Visibility.Collapsed : Visibility.Visible;
		var emptyVisibility = Invert ? Visibility.Visible : Visibility.Collapsed;
		if (value is ICollection collection)
		{
			return collection.Count == 0 ? notEmptyVisibility : emptyVisibility;
		}

		if (value is int count)
		{
			return count == 0 ? notEmptyVisibility : emptyVisibility;
		}

		return emptyVisibility;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotImplementedException();
	}
}
