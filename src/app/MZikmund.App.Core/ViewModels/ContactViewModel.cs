using MZikmund.Services.Localization;
using Launcher = Windows.System.Launcher;

namespace MZikmund.ViewModels;

public partial class ContactViewModel : PageViewModel
{
	public override string Title => Localizer.Instance.GetString("Contact");

	[RelayCommand]
	private static async Task OpenLinkAsync(string? url)
	{
		if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			await Launcher.LaunchUriAsync(uri);
		}
	}
}
