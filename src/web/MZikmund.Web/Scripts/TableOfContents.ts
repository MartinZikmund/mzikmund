namespace MZikmund.Blog {
	export interface TocItem {
		id: string;
		text: string;
		level: number;
		element: HTMLElement;
	}

	/**
	 * Builds the article table of contents from the rendered post headings.
	 *
	 * Runs before HeadingLinks: it assigns ids to headings that lack them, and
	 * HeadingLinks skips any heading without an id.
	 */
	export class TableOfContents {
		// Must match the `@media (min-width: 80rem)` switch in _layout.scss where
		// the TOC moves from stacked-above to the right-hand column.
		private static readonly StackedBreakpoint = "(max-width: 1279.98px)";

		private tocContainer: HTMLElement | null = null;
		private contentContainer: HTMLElement | null = null;
		private tocList: HTMLElement | null = null;
		private activeItem: HTMLElement | null = null;
		private observer: IntersectionObserver | null = null;
		private mediaQuery: MediaQueryList | null = null;
		private mediaQueryHandler: (() => void) | null = null;
		private usedIds: Set<string> = new Set();

		public init(): void {
			if (document.readyState === 'loading') {
				document.addEventListener('DOMContentLoaded', () => this.initialize());
			} else {
				this.initialize();
			}
		}

		private initialize(): void {
			this.tocContainer = document.getElementById('toc-container');
			this.contentContainer = document.querySelector('.post-content');

			if (!this.tocContainer || !this.contentContainer) {
				return;
			}

			const headings = this.extractHeadings();
			if (headings.length === 0) {
				// Stays hidden — an empty TOC box is worse than none.
				return;
			}

			this.buildToc(headings);
			this.setupScrollSpy(headings);
			this.setupSmoothScroll();
			this.setupMobileToggle();

			this.tocContainer.classList.add('is-visible');
		}

		private extractHeadings(): TocItem[] {
			if (!this.contentContainer) {
				return [];
			}

			const headings: TocItem[] = [];
			const headingElements = this.contentContainer.querySelectorAll('h2, h3, h4');

			// First pass: reserve every id already present so generated ids cannot collide.
			headingElements.forEach((heading) => {
				const element = heading as HTMLElement;
				if (element.id) {
					this.usedIds.add(element.id);
				}
			});

			headingElements.forEach((heading) => {
				const element = heading as HTMLElement;
				if (!element.id) {
					element.id = this.generateId(element.textContent || '');
				}

				headings.push({
					id: element.id,
					text: element.textContent || '',
					level: parseInt(element.tagName.substring(1)),
					element: element
				});
			});

			return headings;
		}

		private generateId(text: string): string {
			let baseId = text
				.toLowerCase()
				.replace(/[^\w\s-]/g, '')
				.replace(/\s+/g, '-')
				.replace(/--+/g, '-')
				.trim();

			// A leading digit makes `document.querySelector('#1-foo')` invalid, so prefix it.
			if (!baseId || /^\d/.test(baseId)) {
				baseId = `section-${baseId}`;
			}

			let id = baseId;
			let counter = 1;
			while (this.usedIds.has(id)) {
				id = `${baseId}-${counter}`;
				counter++;
			}
			this.usedIds.add(id);
			return id;
		}

		private buildToc(headings: TocItem[]): void {
			if (!this.tocContainer) {
				return;
			}

			const tocNav = document.createElement('nav');
			tocNav.className = 'toc__nav';
			tocNav.setAttribute('aria-label', 'Table of contents');

			const title = document.createElement('h2');
			title.className = 'toc__title';
			title.textContent = 'On this page';
			tocNav.appendChild(title);

			this.tocList = document.createElement('ul');
			this.tocList.className = 'toc__list';
			this.tocList.id = 'toc-list';

			let currentList = this.tocList;
			let lastLevel = headings.length > 0 ? headings[0].level : 2;

			headings.forEach((heading) => {
				const listItem = document.createElement('li');
				listItem.className = `toc__item toc__item--${heading.level}`;

				const link = document.createElement('a');
				link.href = `#${heading.id}`;
				link.textContent = heading.text;
				link.className = 'toc__link';
				link.dataset.target = heading.id;

				listItem.appendChild(link);

				if (heading.level > lastLevel) {
					let levelsDown = heading.level - lastLevel;
					while (levelsDown > 0) {
						const nestedList = document.createElement('ul');
						nestedList.className = 'toc__list toc__list--nested';
						if (currentList.lastElementChild) {
							currentList.lastElementChild.appendChild(nestedList);
						} else {
							currentList.appendChild(nestedList);
						}
						currentList = nestedList;
						levelsDown--;
					}
				} else if (heading.level < lastLevel) {
					let levelsUp = lastLevel - heading.level;
					while (levelsUp > 0 && currentList.parentElement) {
						const parent = currentList.parentElement.closest('ul');
						if (parent && parent.classList.contains('toc__list')) {
							currentList = parent;
						}
						levelsUp--;
					}
				}

				currentList.appendChild(listItem);
				lastLevel = heading.level;
			});

			tocNav.appendChild(this.tocList);
			this.tocContainer.appendChild(tocNav);
		}

		private setupScrollSpy(headings: TocItem[]): void {
			// Top margin offsets the sticky header, so a heading is not marked
			// active while it is still hidden behind it. Bottom -80% keeps only
			// the topmost visible heading active.
			const headerHeight = parseFloat(
				getComputedStyle(document.documentElement).getPropertyValue('--mz-header-height')
			) || 3.5;
			const offset = Math.round(headerHeight * 16);

			const options = {
				rootMargin: `-${offset}px 0px -80% 0px`,
				threshold: 0
			};

			this.observer = new IntersectionObserver((entries) => {
				const visibleEntries = entries.filter(entry => entry.isIntersecting);
				if (visibleEntries.length === 0) {
					return;
				}

				let topEntry = visibleEntries[0];
				let minTop = Math.abs(visibleEntries[0].boundingClientRect.top);

				visibleEntries.forEach(entry => {
					const top = Math.abs(entry.boundingClientRect.top);
					if (top < minTop) {
						minTop = top;
						topEntry = entry;
					}
				});

				if (topEntry && topEntry.target.id) {
					this.setActiveItem(topEntry.target.id);
				}
			}, options);

			headings.forEach((heading) => {
				this.observer?.observe(heading.element);
			});
		}

		private setActiveItem(id: string): void {
			if (!this.tocList) {
				return;
			}

			if (this.activeItem) {
				this.activeItem.classList.remove('is-active');
			}

			const link = this.tocList.querySelector(`[data-target="${CSS.escape(id)}"]`);
			if (link) {
				link.classList.add('is-active');
				this.activeItem = link as HTMLElement;

				// Keep the active entry in view within the sticky sidebar.
				link.scrollIntoView({ block: 'nearest' });
			}
		}

		private setupSmoothScroll(): void {
			if (!this.tocList) {
				return;
			}

			this.tocList.addEventListener('click', (e) => {
				const target = e.target as HTMLElement;
				const link = target.closest('a');

				if (link && link instanceof HTMLAnchorElement) {
					const id = link.dataset.target;
					if (!id) {
						return;
					}
					const targetElement = document.getElementById(id);
					if (!targetElement) {
						return;
					}
					e.preventDefault();
					const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
					// scroll-margin-top on prose headings supplies the header offset.
					targetElement.scrollIntoView({
						behavior: reduced ? 'auto' : 'smooth',
						block: 'start'
					});
					history.replaceState(null, '', `#${id}`);
				}
			});
		}

		private setupMobileToggle(): void {
			if (!this.tocContainer || !this.tocList) {
				return;
			}

			const title = this.tocContainer.querySelector('.toc__title');
			if (!title) {
				return;
			}

			const tocList = this.tocList;

			const handleToggleClick = () => {
				const willOpen = tocList.hidden;
				tocList.hidden = !willOpen;
				title.setAttribute('aria-expanded', String(willOpen));
			};

			this.mediaQuery = window.matchMedia(TableOfContents.StackedBreakpoint);

			this.mediaQueryHandler = () => {
				if (this.mediaQuery!.matches) {
					tocList.hidden = true;
					title.setAttribute('role', 'button');
					title.setAttribute('tabindex', '0');
					title.setAttribute('aria-expanded', 'false');
					title.setAttribute('aria-controls', 'toc-list');
					title.addEventListener('click', handleToggleClick);
					title.addEventListener('keydown', this.onTitleKey);
				} else {
					tocList.hidden = false;
					title.removeAttribute('role');
					title.removeAttribute('tabindex');
					title.removeAttribute('aria-expanded');
					title.removeAttribute('aria-controls');
					title.removeEventListener('click', handleToggleClick);
					title.removeEventListener('keydown', this.onTitleKey);
				}
			};

			this.mediaQueryHandler();
			this.mediaQuery.addEventListener('change', this.mediaQueryHandler);
		}

		// role="button" must respond to Enter/Space like a real button.
		private onTitleKey = (e: Event): void => {
			const key = (e as KeyboardEvent).key;
			if (key === 'Enter' || key === ' ') {
				e.preventDefault();
				(e.currentTarget as HTMLElement).click();
			}
		};

		public destroy(): void {
			if (this.observer) {
				this.observer.disconnect();
				this.observer = null;
			}

			if (this.mediaQuery && this.mediaQueryHandler) {
				this.mediaQuery.removeEventListener('change', this.mediaQueryHandler);
				this.mediaQuery = null;
				this.mediaQueryHandler = null;
			}
		}
	}
}
