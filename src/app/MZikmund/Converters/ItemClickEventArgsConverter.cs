using Microsoft.UI.Xaml.Data;

namespace MZikmund.Converters;

public class ItemClickEventArgsConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, string language) =>
		value is ItemClickEventArgs args ? args.ClickedItem : null;

	public object ConvertBack(object value, Type targetType, object parameter, string language) =>
		throw new InvalidOperationException("Converting back is not supported.");
}
