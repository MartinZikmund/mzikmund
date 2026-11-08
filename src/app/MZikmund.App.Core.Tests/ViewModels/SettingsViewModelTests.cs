using Moq;
using MZikmund.Services.Account;
using MZikmund.Services.Preferences;
using MZikmund.Services.Theming;
using MZikmund.ViewModels;

namespace MZikmund.App.Core.Tests.ViewModels;

public class SettingsViewModelTests
{
	private readonly Mock<IUserService> _userService = new();

	private SettingsViewModel CreateViewModel() =>
		new(Mock.Of<IAppPreferences>(), Mock.Of<IThemeManager>(), _userService.Object);

	private void RaiseAuthenticationChanged() =>
		_userService.Raise(u => u.AuthenticationChanged += null, EventArgs.Empty);

	private static List<string?> TrackChanges(SettingsViewModel viewModel)
	{
		List<string?> changes = new();
		viewModel.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
		return changes;
	}

	[Fact]
	public void SigningInWhileTheViewIsOpen_RefreshesTheAccountCard()
	{
		var viewModel = CreateViewModel();
		viewModel.ViewLoaded();
		var changes = TrackChanges(viewModel);

		RaiseAuthenticationChanged();

		Assert.Contains(nameof(SettingsViewModel.AccountDescription), changes);
	}

	[Fact]
	public async Task SigningInAwayFromTheUiThread_RefreshesTheAccountCardOnTheUiThread()
	{
		RecordingContext uiContext = new();
		var viewModel = CreateViewModel();
		var previous = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(uiContext);
		try
		{
			viewModel.ViewLoaded();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previous);
		}

		var changes = TrackChanges(viewModel);
		await Task.Run(RaiseAuthenticationChanged);
		Assert.Empty(changes);

		uiContext.RunPosted();
		Assert.Contains(nameof(SettingsViewModel.AccountDescription), changes);
	}

	[Fact]
	public void AfterLeavingTheView_AuthenticationChangesAreIgnored()
	{
		var viewModel = CreateViewModel();
		viewModel.ViewLoaded();
		viewModel.ViewUnloaded();
		var changes = TrackChanges(viewModel);

		RaiseAuthenticationChanged();

		Assert.Empty(changes);
	}

	[Fact]
	public void ReturningToTheView_RefreshesTheAccountCardOnlyOncePerChange()
	{
		var viewModel = CreateViewModel();
		viewModel.ViewLoaded();
		viewModel.ViewUnloaded();
		viewModel.ViewLoaded();
		var changes = TrackChanges(viewModel);

		RaiseAuthenticationChanged();

		Assert.Single(changes, nameof(SettingsViewModel.AccountDescription));
	}

	private sealed class RecordingContext : SynchronizationContext
	{
		private readonly List<(SendOrPostCallback Callback, object? State)> _posted = new();

		public override void Post(SendOrPostCallback d, object? state) => _posted.Add((d, state));

		public void RunPosted()
		{
			foreach (var (callback, state) in _posted)
			{
				callback(state);
			}
		}
	}
}
