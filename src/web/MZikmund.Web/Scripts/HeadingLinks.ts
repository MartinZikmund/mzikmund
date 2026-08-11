namespace MZikmund.Blog {
	/**
	 * Adds a copy-link button to each article heading.
	 *
	 * Depends on TableOfContents having run first — headings without an id are
	 * skipped, and the TOC is what assigns them.
	 */
	export class HeadingLinks {
		private contentContainer: HTMLElement | null = null;

		public init(): void {
			if (document.readyState === 'loading') {
				document.addEventListener('DOMContentLoaded', () => this.initialize());
			} else {
				this.initialize();
			}
		}

		private initialize(): void {
			this.contentContainer = document.querySelector('.post-content');
			if (!this.contentContainer) return;

			const headings = this.contentContainer.querySelectorAll('h2, h3, h4');
			headings.forEach((heading) => {
				const el = heading as HTMLElement;
				if (!el.id) return;

				const button = document.createElement('button');
				button.type = 'button';
				button.className = 'heading-copy-link';
				button.setAttribute('aria-label', 'Copy link to this section');
				button.title = 'Copy link';
				// Uses the shared inline sprite rather than a bespoke inline SVG.
				button.innerHTML = '<svg class="icon" aria-hidden="true"><use href="#i-link-45deg"></use></svg>';

				button.addEventListener('click', (e) => {
					e.preventDefault();
					e.stopPropagation();
					const url = `${window.location.origin}${window.location.pathname}#${el.id}`;
					// clipboard.writeText needs a secure context and can reject —
					// handle rejection rather than leaving it unhandled.
					navigator.clipboard?.writeText(url).then(
						() => this.showCopiedFeedback(button),
						() => { /* copying unavailable; nothing useful to show */ }
					);
				});

				el.appendChild(button);
			});
		}

		private showCopiedFeedback(button: HTMLElement): void {
			button.classList.add('is-copied');
			setTimeout(() => button.classList.remove('is-copied'), 1500);
		}
	}
}
