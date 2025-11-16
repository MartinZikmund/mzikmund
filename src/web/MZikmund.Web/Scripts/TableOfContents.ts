namespace MZikmund.Blog {
	export interface TocItem {
		id: string;
		text: string;
		level: number;
		element: HTMLElement;
	}

	export class TableOfContents {
		private tocContainer: HTMLElement | null = null;
		private contentContainer: HTMLElement | null = null;
		private tocList: HTMLElement | null = null;
		private activeItem: HTMLElement | null = null;
		private observer: IntersectionObserver | null = null;
		private mediaQuery: MediaQueryList | null = null;
		private mediaQueryHandler: (() => void) | null = null;
		private usedIds: Set<string> = new Set();

		public init(): void {
			// Wait for DOM to be ready
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
				// Keep TOC hidden if no headings found
				return;
			}

			this.buildToc(headings);
			this.setupScrollSpy(headings);
			this.setupSmoothScroll();
			this.setupMobileToggle();
			
			// Make TOC visible after building
			this.tocContainer.classList.add('toc-visible');
		}

		private extractHeadings(): TocItem[] {
			if (!this.contentContainer) {
				return [];
			}

			const headings: TocItem[] = [];
			const headingElements = this.contentContainer.querySelectorAll('h2, h3, h4');

			headingElements.forEach((heading) => {
				const element = heading as HTMLElement;
				// Markdig's AutoIdentifiers should have created IDs
				if (!element.id) {
					// Create an ID if one doesn't exist
					element.id = this.generateId(element.textContent || '');
				} else {
					// Track existing IDs to avoid duplicates
					this.usedIds.add(element.id);
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
			
			// Ensure unique ID by appending a number if duplicate
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
			tocNav.className = 'toc-nav';

			const title = document.createElement('h2');
			title.className = 'toc-title';
			title.textContent = 'Table of contents';
			tocNav.appendChild(title);

			this.tocList = document.createElement('ul');
			this.tocList.className = 'toc-list';

			let currentList = this.tocList;
			let lastLevel = 2; // Start with h2 as base level

			headings.forEach((heading) => {
				const listItem = document.createElement('li');
				listItem.className = `toc-item toc-level-${heading.level}`;

				const link = document.createElement('a');
				link.href = `#${heading.id}`;
				link.textContent = heading.text;
				link.className = 'toc-link';
				link.dataset.target = heading.id;

				listItem.appendChild(link);

				// Handle nesting - go up or down multiple levels if needed
				if (heading.level > lastLevel) {
					// Create nested list(s) for going deeper
					let levelsDown = heading.level - lastLevel;
					while (levelsDown > 0) {
						const nestedList = document.createElement('ul');
						nestedList.className = 'toc-list toc-nested';
						if (currentList.lastElementChild) {
							currentList.lastElementChild.appendChild(nestedList);
						} else {
							// If no previous item, append to current list
							currentList.appendChild(nestedList);
						}
						currentList = nestedList;
						levelsDown--;
					}
				} else if (heading.level < lastLevel) {
					// Go back to parent level(s)
					let levelsUp = lastLevel - heading.level;
					while (levelsUp > 0 && currentList.parentElement) {
						const parent = currentList.parentElement.closest('ul');
						if (parent && parent.classList.contains('toc-list')) {
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
			// The rootMargin values fine-tune when a heading is considered "active":
			// - Top margin (-80px) offsets fixed header height
			// - Bottom margin (-80%) reduces intersection area so only topmost visible heading is active
			const options = {
				rootMargin: '-80px 0px -80% 0px',
				threshold: 0
			};

			this.observer = new IntersectionObserver((entries) => {
				// Find the topmost visible heading
				const visibleEntries = entries.filter(entry => entry.isIntersecting);
				if (visibleEntries.length === 0) {
					return;
				}

				// Find entry closest to top of viewport
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

			// Remove previous active
			if (this.activeItem) {
				this.activeItem.classList.remove('active');
			}

			// Set new active
			const link = this.tocList.querySelector(`[data-target="${id}"]`);
			if (link) {
				link.classList.add('active');
				this.activeItem = link as HTMLElement;
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
					e.preventDefault();
					const href = link.getAttribute('href');
					if (href) {
						const targetElement = document.querySelector(href);
						if (targetElement) {
							targetElement.scrollIntoView({
								behavior: 'smooth',
								block: 'start'
							});
							// Use replaceState instead of pushState to avoid polluting browser history
							history.replaceState(null, '', href);
						}
					}
				}
			});
		}

		private setupMobileToggle(): void {
			if (!this.tocContainer || !this.tocList) {
				return;
			}

			const title = this.tocContainer.querySelector('.toc-title');
			if (!title) {
				return;
			}

			const tocList = this.tocList;

			// Create toggle handler
			const handleToggleClick = () => {
				tocList.classList.toggle('collapsed');
				title.classList.toggle('collapsed');
			};

			// Only add toggle functionality on mobile/tablet
			this.mediaQuery = window.matchMedia('(max-width: 1199px)');
			
			this.mediaQueryHandler = () => {
				if (this.mediaQuery!.matches) {
					// Start collapsed on mobile
					tocList.classList.add('collapsed');
					title.classList.add('collapsed');
					title.addEventListener('click', handleToggleClick);
				} else {
					// Remove collapse classes on desktop
					tocList.classList.remove('collapsed');
					title.classList.remove('collapsed');
					title.removeEventListener('click', handleToggleClick);
				}
			};

			this.mediaQueryHandler();
			this.mediaQuery.addEventListener('change', this.mediaQueryHandler);
		}

		public destroy(): void {
			// Cleanup IntersectionObserver
			if (this.observer) {
				this.observer.disconnect();
				this.observer = null;
			}

			// Cleanup media query listener
			if (this.mediaQuery && this.mediaQueryHandler) {
				this.mediaQuery.removeEventListener('change', this.mediaQueryHandler);
				this.mediaQuery = null;
				this.mediaQueryHandler = null;
			}
		}
	}
}
