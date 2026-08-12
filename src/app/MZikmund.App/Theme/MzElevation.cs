#if WINDOWS
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Media;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
#endif
using Microsoft.UI.Xaml;

namespace MZikmund.Theme;

/// <summary>
/// Fluent elevation levels, named after what a surface is rather than how deep its shadow is.
/// </summary>
public enum MzElevationLevel
{
	None,
	Layer,
	Control,
	Card,
	Tooltip,
	Flyout,
	Dialog,
}

/// <summary>
/// Attached property that applies a Fluent elevation shadow on Windows. Every level pairs with a
/// 1px stroke in the design, so other heads are a no-op and the stroke alone carries the hierarchy.
/// </summary>
public static class MzElevation
{
	public static readonly DependencyProperty LevelProperty =
		DependencyProperty.RegisterAttached(
			"Level",
			typeof(MzElevationLevel),
			typeof(MzElevation),
			new PropertyMetadata(MzElevationLevel.None, OnLevelChanged));

	public static MzElevationLevel GetLevel(DependencyObject element) =>
		(MzElevationLevel)element.GetValue(LevelProperty);

	public static void SetLevel(DependencyObject element, MzElevationLevel value) =>
		element.SetValue(LevelProperty, value);

	private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
#if WINDOWS
		if (d is not FrameworkElement element)
		{
			return;
		}

		element.ActualThemeChanged -= OnActualThemeChanged;
		element.ActualThemeChanged += OnActualThemeChanged;
		Apply(element);
#endif
	}

#if WINDOWS
	private static void OnActualThemeChanged(FrameworkElement sender, object args) => Apply(sender);

	private static void Apply(FrameworkElement element)
	{
		var level = GetLevel(element);
		if (level == MzElevationLevel.None)
		{
			Effects.SetShadow(element, null!);
			return;
		}

		var isDark = element.ActualTheme == ElementTheme.Dark;

		// Composition gives one shadow per element, so each level uses its directional
		// shadow; the ambient companion is carried by the paired 1px stroke.
		var (offsetY, blur, opacity) = level switch
		{
			MzElevationLevel.Layer => (0d, 2d, isDark ? 0.24 : 0.12),
			MzElevationLevel.Control => (1d, 2d, isDark ? 0.28 : 0.14),
			MzElevationLevel.Card => (4d, 8d, isDark ? 0.28 : 0.14),
			MzElevationLevel.Tooltip => (8d, 16d, isDark ? 0.32 : 0.14),
			MzElevationLevel.Flyout => (14d, 28d, isDark ? 0.44 : 0.24),
			MzElevationLevel.Dialog => (32d, 64d, isDark ? 0.48 : 0.24),
			_ => (0d, 0d, 0d),
		};

		// FrameworkElement itself has no CornerRadius - only Border and Control do.
		var cornerRadius = element switch
		{
			Border border => border.CornerRadius.TopLeft,
			Control control => control.CornerRadius.TopLeft,
			_ => 0d,
		};

		AttachedCardShadow shadow = new()
		{
			// Windows.UI.Colors isn't projected by WindowsAppSDK; build the color by hand.
			Color = Color.FromArgb(255, 0, 0, 0),
			Offset = $"0,{offsetY.ToString(System.Globalization.CultureInfo.InvariantCulture)},0",
			BlurRadius = blur,
			Opacity = opacity,
			CornerRadius = cornerRadius,
		};

		Effects.SetShadow(element, shadow);
	}
#endif
}
