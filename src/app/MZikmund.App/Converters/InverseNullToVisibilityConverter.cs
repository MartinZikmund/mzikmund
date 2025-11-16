using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MZikmund.Converters;

public class InverseNullToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
		=> value is null ? Visibility.Visible : Visibility.Collapsed;

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}
