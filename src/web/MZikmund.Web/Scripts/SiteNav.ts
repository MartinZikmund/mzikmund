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
				this.setOpen(!this.isOpen);
			});

			if (this.supportsPopover) {
				// Light-dismiss and Escape close the popover without going through
				// setOpen(), so mirror the state back onto the toggle.
				this.panel.addEventListener("toggle", e => {
					const open = (e as ToggleEvent).newState === "open";
					this.toggle?.setAttribute("aria-expanded", String(open));
					if (!open && this.panel?.contains(document.activeElement)) {
						this.toggle?.focus();
					}
				});
			} else {
				document.addEventListener("keydown", e => {
					if (e.key === "Escape" && this.isOpen && this.isMobile()) {
						this.setOpen(false);
						this.toggle!.focus();
					}
				});

				document.addEventListener("click", e => {
					if (!this.isMobile() || !this.isOpen) {
						return;
					}
					const target = e.target as Node;
					if (!this.panel!.contains(target) && !this.toggle!.contains(target)) {
						this.setOpen(false);
					}
				});
			}

			this.desktop = window.matchMedia("(min-width: 48rem)");
			this.desktop.addEventListener("change", () => this.applyMode());
			window.addEventListener("resize", () => {
				if (this.isMobile() && this.isOpen) {
					this.position();
				}
			});
			this.applyMode();
		}

		private get supportsPopover(): boolean {
			return typeof (this.panel as any)?.showPopover === "function";
		}

		private isMobile(): boolean {
			return !this.desktop || !this.desktop.matches;
		}

		private get isOpen(): boolean {
			if (!this.panel) {
				return false;
			}
			if (this.isMobile() && this.supportsPopover && this.panel.hasAttribute("popover")) {
				return this.panel.matches(":popover-open");
			}
			return !this.panel.hidden;
		}

		/**
		 * The same <ul> is the desktop nav row and the mobile panel, so the
		 * popover attribute is applied only below the breakpoint — a popover is
		 * display:none until opened, which would erase the desktop nav.
		 *
		 * The server renders the panel `hidden` so it cannot flash open before
		 * this runs; once popover owns visibility, that attribute is cleared.
		 */
		private applyMode(): void {
			const panel = this.panel;
			if (!panel) {
				return;
			}

			if (this.isMobile()) {
				if (this.supportsPopover) {
					if (!panel.hasAttribute("popover")) {
						panel.setAttribute("popover", "auto");
					}
					// popover now controls visibility; `hidden` would fight it.
					panel.hidden = false;
				} else {
					panel.hidden = true;
				}
				this.toggle?.setAttribute("aria-expanded", "false");
			} else {
				if (panel.hasAttribute("popover")) {
					// Must close before removing the attribute, or it is stranded
					// in the top layer.
					if (panel.matches(":popover-open")) {
						(panel as any).hidePopover();
					}
					panel.removeAttribute("popover");
				}
				panel.hidden = false;
				this.toggle?.setAttribute("aria-expanded", "false");
			}
		}

		/** Full-width panel anchored directly beneath the header. */
		private position(): void {
			const header = document.querySelector(".site-header");
			if (!this.panel || !header) {
				return;
			}
			this.panel.style.top = `${Math.round(header.getBoundingClientRect().bottom)}px`;
			this.panel.style.left = "0px";
		}

		private setOpen(open: boolean): void {
			const panel = this.panel;
			if (!panel || !this.toggle || !this.isMobile()) {
				return;
			}

			if (this.supportsPopover && panel.hasAttribute("popover")) {
				if (open) {
					(panel as any).showPopover();
					this.position();
				} else if (panel.matches(":popover-open")) {
					(panel as any).hidePopover();
				}
				// aria-expanded is synced by the toggle event handler.
				return;
			}

			panel.hidden = !open;
			this.toggle.setAttribute("aria-expanded", String(open));
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
