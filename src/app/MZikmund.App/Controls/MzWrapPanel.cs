using Windows.Foundation;

namespace MZikmund.Controls;

/// <summary>
/// Flows children left to right and wraps onto new rows, for variable-width chips like tag pills.
/// </summary>
public partial class MzWrapPanel : Panel
{
	public static readonly DependencyProperty SpacingProperty =
		DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(MzWrapPanel), new PropertyMetadata(8d, (d, _) => ((MzWrapPanel)d).InvalidateMeasure()));

	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		double x = 0, y = 0, rowHeight = 0, width = 0;
		foreach (var child in Children)
		{
			child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
			var size = child.DesiredSize;
			if (x > 0 && x + size.Width > availableSize.Width)
			{
				y += rowHeight + Spacing;
				x = 0;
				rowHeight = 0;
			}

			x += size.Width + Spacing;
			rowHeight = Math.Max(rowHeight, size.Height);
			width = Math.Max(width, x - Spacing);
		}

		return new Size(width, y + rowHeight);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		double x = 0, y = 0, rowHeight = 0;
		foreach (var child in Children)
		{
			var size = child.DesiredSize;
			if (x > 0 && x + size.Width > finalSize.Width)
			{
				y += rowHeight + Spacing;
				x = 0;
				rowHeight = 0;
			}

			child.Arrange(new Rect(x, y, size.Width, size.Height));
			x += size.Width + Spacing;
			rowHeight = Math.Max(rowHeight, size.Height);
		}

		return finalSize;
	}
}
