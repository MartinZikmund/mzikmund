using MZikmund.Services.Localization;

namespace MZikmund.Extensions.Xaml;

public class Localize : MarkupExtension
{
	public string Key { get; set; } = "";

	protected override object ProvideValue() => Localizer.Instance.GetString(Key);
}
