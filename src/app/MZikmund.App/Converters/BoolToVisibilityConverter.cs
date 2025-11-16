using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MZikmund.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
		=> value is bool boolValue && boolValue ? Visibility.Visible : Visibility.Collapsed;

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> value is Visibility visibility && visibility == Visibility.Visible;
}
