namespace MZikmund.Motion {
	const REVEAL_SELECTOR = ".card, .taxonomy-card";
	const HERO_NAME = "post-hero-image";

	const prefersReducedMotion = (): boolean =>
		window.matchMedia("(prefers-reduced-motion: reduce)").matches;

	/**
	 * Fluent Connected Animation, via cross-document View Transitions.
	 *
	 * A view-transition-name must be unique in the document, so the name is put
	 * on the one image being opened at click time rather than on every card.
	 * The destination post banner carries the same name permanently (set in
	 * _motion.scss), so the browser tweens the card image into the article
	 * header instead of cross-fading the whole page.
	 *
	 * Silently does nothing where cross-document view transitions are
	 * unsupported — the navigation is then just a normal navigation.
	 */
	export class ConnectedAnimation {
		public init(): void {
			// `navigation: auto` in CSS is what actually enables the transition;
			// this check keeps us from tagging elements in browsers that ignore it.
			if (!("startViewTransition" in document) || prefersReducedMotion()) {
				return;
			}

			document.addEventListener("click", e => {
				const link = (e.target as HTMLElement)?.closest?.("a.card__link");
				if (!link) {
					return;
				}
				// Let modified clicks (new tab, download, etc.) behave normally.
				const me = e as MouseEvent;
				if (me.metaKey || me.ctrlKey || me.shiftKey || me.altKey || me.button !== 0) {
					return;
				}

				const image = link.closest(".card")?.querySelector<HTMLElement>(".card__image");
				if (image) {
					image.style.viewTransitionName = HERO_NAME;
				}
			}, true);

			// Restore from bfcache: clear the name so a later transition on this
			// page does not find two elements claiming it.
			window.addEventListener("pageshow", () => {
				document.querySelectorAll<HTMLElement>(".card__image").forEach(el => {
					el.style.viewTransitionName = "";
				});
			});
		}
	}

	/**
	 * Fluent Reveal: a soft radial highlight that tracks the pointer across a
	 * surface. Coordinates are published as --mz-reveal-x / --mz-reveal-y and
	 * consumed by the gradient in _motion.scss.
	 *
	 * One delegated listener for the whole document, coalesced into a single
	 * rAF per frame, so hovering a grid of cards costs one style write a frame.
	 */
	export class Reveal {
		private frame = 0;
		private pending: { el: HTMLElement; x: number; y: number } | null = null;

		public init(): void {
			// Pointer-tracking chrome is meaningless on touch and unwanted when
			// motion is reduced.
			if (!window.matchMedia("(hover: hover)").matches || prefersReducedMotion()) {
				return;
			}

			document.addEventListener("pointermove", e => {
				const target = (e.target as HTMLElement)?.closest?.(REVEAL_SELECTOR) as HTMLElement | null;
				if (!target) {
					return;
				}
				const rect = target.getBoundingClientRect();
				this.pending = {
					el: target,
					x: ((e.clientX - rect.left) / rect.width) * 100,
					y: ((e.clientY - rect.top) / rect.height) * 100,
				};
				this.schedule();
			}, { passive: true });
		}

		private schedule(): void {
			if (this.frame) {
				return;
			}
			this.frame = requestAnimationFrame(() => {
				this.frame = 0;
				const p = this.pending;
				if (!p) {
					return;
				}
				p.el.style.setProperty("--mz-reveal-x", `${p.x.toFixed(1)}%`);
				p.el.style.setProperty("--mz-reveal-y", `${p.y.toFixed(1)}%`);
			});
		}
	}
}
