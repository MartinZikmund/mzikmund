using Microsoft.UI.Xaml.Controls;
using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class SettingsView : SettingsViewBase
{
	public SettingsView()
	{
		this.InitializeComponent();
	}
}

public partial class SettingsViewBase : PageBase<SettingsViewModel>
{
}
