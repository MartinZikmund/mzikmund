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
	const STORAGE_KEY = "ui-theme";

	/**
	 * Applies the colour theme and drives the theme flyout.
	 *
	 * Bootstrap's Dropdown used to provide the open/close behaviour; it is
	 * reimplemented here (outside-click, Escape, roving arrow keys) so no
	 * framework JS is needed.
	 */
	export class ThemeSwitchManager {

		private static menu: HTMLElement | null = null;
		private static trigger: HTMLElement | null = null;

		public static init(): void {
			// Runs render-blocking in <head> so the theme is applied before the
			// first paint — without this there is a flash of the wrong theme.
			ThemeSwitchManager.applyTheme(ThemeSwitchManager.readPreference());

			document.addEventListener("DOMContentLoaded", () => ThemeSwitchManager.wireUp());
		}

		private static readPreference(): RequestedTheme {
			try {
				const stored = localStorage.getItem(STORAGE_KEY) as RequestedTheme | null;
				if (stored === RequestedTheme.Light || stored === RequestedTheme.Dark || stored === RequestedTheme.Auto) {
					return stored;
				}
			} catch {
				// localStorage can throw in private/blocked contexts — fall through to Auto.
			}
			return RequestedTheme.Auto;
		}

		private static resolve(requested: RequestedTheme): DisplayTheme {
			if (requested !== RequestedTheme.Auto) {
				return requested === RequestedTheme.Dark ? DisplayTheme.Dark : DisplayTheme.Light;
			}
			return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
				? DisplayTheme.Dark
				: DisplayTheme.Light;
		}

		private static applyTheme(requested: RequestedTheme): void {
			// The attribute always carries a concrete theme. Gist embeds and the
			// code themes key off it, and they cannot read a media query.
			document.documentElement.setAttribute("data-theme", ThemeSwitchManager.resolve(requested));
		}

		private static updateIcon(requested: RequestedTheme): void {
			const use = document.querySelector("#theme-icon use");
			if (!use) {
				return;
			}
			const id = requested === RequestedTheme.Light ? "i-sun"
				: requested === RequestedTheme.Dark ? "i-moon-stars"
					: "i-circle-half";
			use.setAttribute("href", `#${id}`);
		}

		private static updateSelection(requested: RequestedTheme): void {
			const items = document.querySelectorAll<HTMLElement>("#theme-dropdown [data-value]");
			items.forEach(item => {
				const isSelected = item.getAttribute("data-value") === requested;
				item.setAttribute("aria-checked", String(isSelected));
			});
		}

		private static setTheme(requested: RequestedTheme): void {
			try {
				localStorage.setItem(STORAGE_KEY, requested);
			} catch {
				// Preference simply does not persist if storage is unavailable.
			}
			ThemeSwitchManager.applyTheme(requested);
			ThemeSwitchManager.updateIcon(requested);
			ThemeSwitchManager.updateSelection(requested);
		}

		// --- Flyout ---------------------------------------------------------

		private static get isOpen(): boolean {
			return !!ThemeSwitchManager.menu && !ThemeSwitchManager.menu.hidden;
		}

		private static openMenu(): void {
			if (!ThemeSwitchManager.menu || !ThemeSwitchManager.trigger) {
				return;
			}
			ThemeSwitchManager.menu.hidden = false;
			ThemeSwitchManager.trigger.setAttribute("aria-expanded", "true");
			ThemeSwitchManager.menu.querySelector<HTMLElement>('[aria-checked="true"]')?.focus();
		}

		private static closeMenu(returnFocus = false): void {
			if (!ThemeSwitchManager.menu || !ThemeSwitchManager.trigger) {
				return;
			}
			ThemeSwitchManager.menu.hidden = true;
			ThemeSwitchManager.trigger.setAttribute("aria-expanded", "false");
			if (returnFocus) {
				ThemeSwitchManager.trigger.focus();
			}
		}

		private static moveFocus(delta: number): void {
			if (!ThemeSwitchManager.menu) {
				return;
			}
			const items = Array.from(ThemeSwitchManager.menu.querySelectorAll<HTMLElement>("[data-value]"));
			if (items.length === 0) {
				return;
			}
			const current = items.indexOf(document.activeElement as HTMLElement);
			// Wraps at both ends.
			const next = (current + delta + items.length) % items.length;
			items[next].focus();
		}

		private static wireUp(): void {
			const root = document.getElementById("theme-dropdown");
			if (!root) {
				return;
			}

			ThemeSwitchManager.trigger = root.querySelector<HTMLElement>("[data-theme-trigger]");
			ThemeSwitchManager.menu = root.querySelector<HTMLElement>("[data-theme-menu]");

			const requested = ThemeSwitchManager.readPreference();
			ThemeSwitchManager.updateIcon(requested);
			ThemeSwitchManager.updateSelection(requested);

			ThemeSwitchManager.trigger?.addEventListener("click", e => {
				e.preventDefault();
				e.stopPropagation();
				ThemeSwitchManager.isOpen ? ThemeSwitchManager.closeMenu() : ThemeSwitchManager.openMenu();
			});

			root.querySelectorAll<HTMLElement>("[data-value]").forEach(item => {
				item.addEventListener("click", e => {
					e.preventDefault();
					const value = item.getAttribute("data-value") as RequestedTheme;
					ThemeSwitchManager.setTheme(value);
					ThemeSwitchManager.closeMenu(true);
				});
			});

			root.addEventListener("keydown", e => {
				const key = (e as KeyboardEvent).key;
				if (key === "Escape" && ThemeSwitchManager.isOpen) {
					e.preventDefault();
					ThemeSwitchManager.closeMenu(true);
				} else if (key === "ArrowDown") {
					e.preventDefault();
					if (!ThemeSwitchManager.isOpen) {
						ThemeSwitchManager.openMenu();
					} else {
						ThemeSwitchManager.moveFocus(1);
					}
				} else if (key === "ArrowUp" && ThemeSwitchManager.isOpen) {
					e.preventDefault();
					ThemeSwitchManager.moveFocus(-1);
				}
			});

			document.addEventListener("click", e => {
				if (ThemeSwitchManager.isOpen && !root.contains(e.target as Node)) {
					ThemeSwitchManager.closeMenu();
				}
			});

			// Follow the OS while the preference is Auto.
			if (window.matchMedia) {
				window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
					if (ThemeSwitchManager.readPreference() === RequestedTheme.Auto) {
						ThemeSwitchManager.applyTheme(RequestedTheme.Auto);
					}
				});
			}
		}
	}
}
