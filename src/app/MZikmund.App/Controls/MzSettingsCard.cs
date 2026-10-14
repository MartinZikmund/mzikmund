namespace MZikmund.Controls;

/// <summary>
/// A settings row in the shape of the Toolkit's SettingsCard, which has no Uno 7 build yet.
/// </summary>
public partial class MzSettingsCard : ContentControl
{
	public static readonly DependencyProperty HeaderProperty =
		DependencyProperty.Register(nameof(Header), typeof(string), typeof(MzSettingsCard), new PropertyMetadata(""));

	public static readonly DependencyProperty DescriptionProperty =
		DependencyProperty.Register(nameof(Description), typeof(string), typeof(MzSettingsCard), new PropertyMetadata(""));

	public static readonly DependencyProperty GlyphProperty =
		DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(MzSettingsCard), new PropertyMetadata(""));

	public MzSettingsCard()
	{
		DefaultStyleKey = typeof(MzSettingsCard);
	}

	public string Header
	{
		get => (string)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	public string Description
	{
		get => (string)GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}

	public string Glyph
	{
		get => (string)GetValue(GlyphProperty);
		set => SetValue(GlyphProperty, value);
	}
}
