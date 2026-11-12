namespace MZikmund.Controls;

/// <summary>
/// Gives an <see cref="ItemsRepeater"/> its items only while it is loaded. Use it in place of
/// <c>ItemsSource</c> when the collection outlives the page, as on a window-scoped view model.
/// </summary>
/// <remarks>
/// WinUI's ItemsRepeater stays subscribed to its source after its page is gone. Once that page
/// is collected, the next change to the shared collection fails with E_FAIL.
/// </remarks>
public static class MzRepeater
{
	public static readonly DependencyProperty ItemsSourceProperty =
		DependencyProperty.RegisterAttached(
			"ItemsSource",
			typeof(object),
			typeof(MzRepeater),
			new PropertyMetadata(null, OnItemsSourceChanged));

	private static readonly DependencyProperty IsTrackedProperty =
		DependencyProperty.RegisterAttached(
			"IsTracked",
			typeof(bool),
			typeof(MzRepeater),
			new PropertyMetadata(false));

	public static object? GetItemsSource(DependencyObject element) => element.GetValue(ItemsSourceProperty);

	public static void SetItemsSource(DependencyObject element, object? value) => element.SetValue(ItemsSourceProperty, value);

	private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not ItemsRepeater repeater)
		{
			return;
		}

		if (!(bool)repeater.GetValue(IsTrackedProperty))
		{
			repeater.SetValue(IsTrackedProperty, true);
			repeater.Loaded += (_, _) => repeater.ItemsSource = GetItemsSource(repeater);
			repeater.Unloaded += (_, _) => repeater.ItemsSource = null;
		}

		if (repeater.IsLoaded)
		{
			repeater.ItemsSource = e.NewValue;
		}
	}
}
