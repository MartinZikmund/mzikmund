using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MZikmund.App.Presentation
{
	public partial class MainViewModel : ObservableObject
	{
		public string? Title { get; }

		[ObservableProperty]
		private string? name;

		public ICommand GoToSecond { get; }

		public MainViewModel(
			INavigator navigator,
			IStringLocalizer localizer)
		{
			_navigator = navigator;
			Title = $"Main - {localizer["ApplicationName"]}";
			GoToSecond = new AsyncRelayCommand(GoToSecondView);
		}

		private async Task GoToSecondView()
		{
			await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: new Entity(Name!));
		}

		private INavigator _navigator;
	}
}