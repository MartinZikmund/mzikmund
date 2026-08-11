namespace MZikmund.Chrome {
	/**
	 * Mobile navigation toggle. Replaces Bootstrap's Collapse plugin.
	 *
	 * The old markup had one toggler driving TWO separate .navbar-collapse
	 * panels via a class selector, and an aria-controls pointing at an id that
	 * did not exist. The panel is now a single element with a real id.
	 */
	export class SiteNav {
		private toggle: HTMLElement | null = null;
		private panel: HTMLElement | null = null;
		private desktop: MediaQueryList | null = null;

		public init(): void {
			if (document.readyState === "loading") {
				document.addEventListener("DOMContentLoaded", () => this.initialize());
			} else {
				this.initialize();
			}
		}

		private initialize(): void {
			this.toggle = document.querySelector("[data-nav-toggle]");
			this.panel = document.getElementById("site-nav");
			if (!this.toggle || !this.panel) {
				return;
			}

			this.toggle.addEventListener("click", e => {
				e.preventDefault();
				this.setOpen(this.panel!.hidden);
			});

			document.addEventListener("keydown", e => {
				if (e.key === "Escape" && !this.panel!.hidden && this.isMobile()) {
					this.setOpen(false);
					this.toggle!.focus();
				}
			});

			document.addEventListener("click", e => {
				if (!this.isMobile() || this.panel!.hidden) {
					return;
				}
				const target = e.target as Node;
				if (!this.panel!.contains(target) && !this.toggle!.contains(target)) {
					this.setOpen(false);
				}
			});

			// The panel is hidden by the `hidden` attribute on mobile, but must be
			// unconditionally visible on desktop where there is no toggle.
			this.desktop = window.matchMedia("(min-width: 48rem)");
			const sync = () => this.setOpen(this.desktop!.matches ? true : false, true);
			this.desktop.addEventListener("change", sync);
			sync();
		}

		private isMobile(): boolean {
			return !this.desktop || !this.desktop.matches;
		}

		private setOpen(open: boolean, silent = false): void {
			if (!this.panel || !this.toggle) {
				return;
			}
			this.panel.hidden = !open;
			if (!silent || this.isMobile()) {
				this.toggle.setAttribute("aria-expanded", String(open && this.isMobile()));
			}
		}
	}

	/** Floating back-to-top control (previously an inline script in Post.cshtml). */
	export class BackToTop {
		public init(): void {
			if (document.readyState === "loading") {
				document.addEventListener("DOMContentLoaded", () => this.initialize());
			} else {
				this.initialize();
			}
		}

		private initialize(): void {
			const button = document.getElementById("back-to-top");
			if (!button) {
				return;
			}

			const update = () => button.classList.toggle("is-visible", window.scrollY > 400);
			window.addEventListener("scroll", update, { passive: true });
			update();

			button.addEventListener("click", () => {
				const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
				window.scrollTo({ top: 0, behavior: reduced ? "auto" : "smooth" });
			});
		}
	}

	/**
	 * Post-processes rendered post HTML.
	 *
	 * Markdig emits bare <table> elements with no wrapper, so a wide table used
	 * to force the whole page to scroll sideways. Wrapping each one lets it
	 * scroll inside its own column instead.
	 */
	export class ContentEnhancements {
		public init(): void {
			if (document.readyState === "loading") {
				document.addEventListener("DOMContentLoaded", () => this.initialize());
			} else {
				this.initialize();
			}
		}

		private initialize(): void {
			const content = document.querySelector(".post-content");
			if (!content) {
				return;
			}

			content.querySelectorAll("table").forEach(table => {
				if (table.parentElement?.classList.contains("table-wrap")) {
					return;
				}
				const wrap = document.createElement("div");
				wrap.className = "table-wrap";
				table.parentNode?.insertBefore(wrap, table);
				wrap.appendChild(table);
			});

			// Bare autolinked URLs can be longer than the column is wide.
			content.querySelectorAll("a").forEach(link => {
				const text = link.textContent ?? "";
				if (/^https?:\/\//i.test(text) && !/\s/.test(text)) {
					link.classList.add("break-word");
				}
			});
		}
	}
}
