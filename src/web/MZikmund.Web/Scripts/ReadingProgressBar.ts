export class ReadingProgressBar {
    private progressBar: HTMLElement | null = null;

    public init(): void {
        // Wait for DOM to be ready before checking for blog post page
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                this.initialize();
            });
        } else {
            // DOM already loaded
            this.initialize();
        }
    }

    private initialize(): void {
        // Only initialize if we're on a blog post page
        if (!this.isBlogPostPage()) {
            return;
        }

        this.createProgressBar();
        this.setupScrollListener();
    }

    private isBlogPostPage(): boolean {
        // Check if we're on a blog post page by looking for the article with class blog-post
        return document.querySelector('article.blog-post') !== null;
    }

    private createProgressBar(): void {
        // Create the progress bar element
        this.progressBar = document.createElement('div');
        this.progressBar.id = 'reading-progress-bar';
        this.progressBar.className = 'reading-progress-bar';

        // Insert at the beginning of the body
        document.body.insertBefore(this.progressBar, document.body.firstChild);
    }

    private setupScrollListener(): void {
        window.addEventListener('scroll', () => {
            this.updateProgress();
        }, { passive: true });

        // Initial update
        this.updateProgress();
    }

    private updateProgress(): void {
        if (!this.progressBar) {
            return;
        }

        // Calculate scroll progress
        const windowHeight = window.innerHeight;
        const documentHeight = document.documentElement.scrollHeight;
        const scrollTop = window.scrollY || document.documentElement.scrollTop;

        // Calculate percentage (0-100)
        const maxScroll = documentHeight - windowHeight;
        const scrollPercentage = maxScroll > 0 ? (scrollTop / maxScroll) * 100 : 0;

        // Clamp between 0 and 100
        const progress = Math.min(Math.max(scrollPercentage, 0), 100);

        // Update the progress bar width
        this.progressBar.style.width = `${progress}%`;
    }
}
