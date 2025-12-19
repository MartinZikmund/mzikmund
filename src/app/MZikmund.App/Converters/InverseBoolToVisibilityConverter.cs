using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MZikmund.Converters;

public class InverseBoolToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
		=> value is bool boolValue && boolValue ? Visibility.Collapsed : Visibility.Visible;

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> value is Visibility visibility && visibility == Visibility.Collapsed;
}
