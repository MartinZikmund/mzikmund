using MZikmund.ViewModels;
using Microsoft.UI.Xaml.Controls;

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
