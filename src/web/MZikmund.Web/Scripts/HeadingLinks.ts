namespace MZikmund.Blog {
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
				button.className = 'heading-copy-link';
				button.setAttribute('aria-label', 'Copy link to this section');
				button.title = 'Copy link';
				button.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M6.75 8.75a3.25 3.25 0 0 0 4.596.444l1.904-1.904a3.25 3.25 0 0 0-4.596-4.596L7.5 3.847"/><path d="M9.25 7.25a3.25 3.25 0 0 0-4.596-.444L2.75 8.71a3.25 3.25 0 0 0 4.596 4.596L8.5 12.153"/></svg>`;

				button.addEventListener('click', (e) => {
					e.preventDefault();
					e.stopPropagation();
					const url = `${window.location.origin}${window.location.pathname}#${el.id}`;
					navigator.clipboard.writeText(url).then(() => {
						this.showCopiedFeedback(button);
					});
				});

				el.appendChild(button);
			});
		}

		private showCopiedFeedback(button: HTMLElement): void {
			button.classList.add('copied');
			setTimeout(() => button.classList.remove('copied'), 1500);
		}
	}
}
