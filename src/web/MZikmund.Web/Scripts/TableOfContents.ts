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
			return text
				.toLowerCase()
				.replace(/[^\w\s-]/g, '')
				.replace(/\s+/g, '-')
				.replace(/--+/g, '-')
				.trim();
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
			const options = {
				rootMargin: '-80px 0px -80% 0px',
				threshold: 0
			};

			this.observer = new IntersectionObserver((entries) => {
				entries.forEach((entry) => {
					if (entry.isIntersecting) {
						const id = entry.target.id;
						this.setActiveItem(id);
					}
				});
			}, options);

			headings.forEach((heading) => {
				this.observer!.observe(heading.element);
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
				if (target.tagName === 'A') {
					e.preventDefault();
					const href = target.getAttribute('href');
					if (href) {
						const targetElement = document.querySelector(href);
						if (targetElement) {
							targetElement.scrollIntoView({
								behavior: 'smooth',
								block: 'start'
							});
							// Update URL without scrolling
							history.pushState(null, '', href);
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

			// Only add toggle functionality on mobile/tablet
			const mediaQuery = window.matchMedia('(max-width: 1199px)');
			
			const setupToggle = () => {
				if (mediaQuery.matches) {
					// Start collapsed on mobile
					this.tocList!.classList.add('collapsed');
					title.classList.add('collapsed');

					title.addEventListener('click', this.handleToggleClick);
				} else {
					// Remove collapse classes on desktop
					this.tocList!.classList.remove('collapsed');
					title.classList.remove('collapsed');
					title.removeEventListener('click', this.handleToggleClick);
				}
			};

			this.handleToggleClick = () => {
				this.tocList!.classList.toggle('collapsed');
				title.classList.toggle('collapsed');
			};

			setupToggle();
			mediaQuery.addEventListener('change', setupToggle);
		}

		private handleToggleClick: () => void = () => {};
	}
}
