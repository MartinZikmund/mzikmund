using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace MZikmund.App.Core.Tests;

/// <summary>
/// The static Localizer resolves through Ioc.Default, which can only be configured once per process.
/// </summary>
internal static class TestLocalization
{
	static TestLocalization() =>
		Ioc.Default.ConfigureServices(new ServiceCollection()
			.AddSingleton<IStringLocalizer, FakeStringLocalizer>()
			.BuildServiceProvider());

	public static void EnsureConfigured()
	{
	}

	private sealed class FakeStringLocalizer : IStringLocalizer
	{
		private static readonly Dictionary<string, string> Strings = new()
		{
			["GoesLiveInFormat"] = "Goes live in {0}",
			["CountdownDays"] = "{0}d {1}h",
			["CountdownHours"] = "{0}h {1}m",
			["CountdownMinutes"] = "{0}m {1}s",
			["CountdownSeconds"] = "{0}s",
		};

		public LocalizedString this[string name] => new(name, Strings.GetValueOrDefault(name, name));

		public LocalizedString this[string name, params object[] arguments] => this[name];

		public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
	}
}
