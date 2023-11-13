using MZikmund.Resources;

namespace MZikmund.Extensions.Xaml;

public class MzFontIcon : MarkupExtension
{
	public string Glyph { get; set; } = "";

	public string GlyphKey { get; set; } = "";

	protected override object ProvideValue()
	{
		var glyph = "?";
		if (!string.IsNullOrEmpty(Glyph))
		{
			glyph = Glyph;
		}

		if (!string.IsNullOrEmpty(GlyphKey))
		{
			glyph = (string)Application.Current.Resources[GlyphKey];
		}

		return new FontIcon
		{
			Glyph = glyph,
			FontFamily = FontResources.IconFont,
		};
	}
}
