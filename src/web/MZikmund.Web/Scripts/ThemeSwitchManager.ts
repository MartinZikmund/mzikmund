enum RequestedTheme {
	Dark = "dark",
	Light = "light",
	Auto = "auto"
}

enum DisplayTheme {
	Dark = "dark",
	Light = "light"
}

namespace MZikmund.Theming {
	export class ThemeSwitchManager {

		public static init() {
			$(() => {
				ThemeSwitchManager.initTheme();
			});

			//ensure theme is set immediately to avoid flash of unstyled content
			ThemeSwitchManager.updateTheme(false);
		}

		private static determineOsTheme(): DisplayTheme {
			if (window.matchMedia &&
				window.matchMedia('(prefers-color-scheme: dark)').matches) {
				return DisplayTheme.Dark;
			} else {
				return DisplayTheme.Light;
			}
		}

		private static updateTheme(loaded: boolean) {
			var requestedTheme: RequestedTheme = <RequestedTheme>localStorage.getItem('ui-theme');

			if (requestedTheme == null) {
				requestedTheme = RequestedTheme.Auto;
			}

			if (loaded) {
				//clear theme selection
				$("#theme-dropdown a.dropdown-item").removeClass("selected");
				$("#theme-dropdown a[data-value='" + requestedTheme.toString().toLowerCase() + "'").addClass("selected");
			}

			var displayTheme: DisplayTheme;

			if (requestedTheme == RequestedTheme.Auto) {
				displayTheme = ThemeSwitchManager.determineOsTheme();
			} else {
				displayTheme = <DisplayTheme><string>requestedTheme;
			}

			document.documentElement.removeAttribute("data-theme");

			document.documentElement.setAttribute("data-theme", displayTheme.toString().toLowerCase());
		}

		private static initTheme() {

			$("#theme-dropdown a.dropdown-item").click(function (e) {
				e.preventDefault();
				localStorage.setItem('ui-theme', $(this).data('value'));
				ThemeSwitchManager.updateTheme(true);
			});

			if (window.matchMedia) {
				window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", () => {
					ThemeSwitchManager.updateTheme(true);
				});
			}

			ThemeSwitchManager.updateTheme(true);
		}
	}
}
