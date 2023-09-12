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
			document.addEventListener("DOMContentLoaded", () => {
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
				document.querySelectorAll("#theme-dropdown a.dropdown-item").forEach(item => item.classList.remove("selected"));
				document.querySelectorAll("#theme-dropdown a[data-value='" + requestedTheme.toString().toLowerCase() + "'").forEach(item => item.classList.add("selected"));
			}

			var displayTheme: DisplayTheme;

			if (requestedTheme == RequestedTheme.Auto) {
				displayTheme = ThemeSwitchManager.determineOsTheme();
			} else {
				displayTheme = <DisplayTheme><string>requestedTheme;
			}

			document.documentElement.removeAttribute("data-bs-theme");
			document.documentElement.setAttribute("data-bs-theme", displayTheme.toString().toLowerCase());
		}

		private static initTheme() {

			document.querySelectorAll("#theme-dropdown a.dropdown-item").forEach(item => item.addEventListener("click", function (e) {
				e.preventDefault();
				localStorage.setItem('ui-theme', (e.currentTarget as HTMLElement).getAttribute("data-value") ?? "");
				ThemeSwitchManager.updateTheme(true);
			}));

			if (window.matchMedia) {
				window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", () => {
					ThemeSwitchManager.updateTheme(true);
				});
			}

			ThemeSwitchManager.updateTheme(true);
		}
	}
}
