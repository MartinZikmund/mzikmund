/// <reference path="ThemeSwitchManager.ts" />
/// <reference path="ReadingProgressBar.ts" />

// Initialize theme manager immediately as it needs to run before page renders
MZikmund.Theming.ThemeSwitchManager.init();

// Wait for DOM to be ready before initializing the reading progress bar
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        new MZikmund.Blog.ReadingProgressBar().init();
    });
} else {
    // DOM is already ready
    new MZikmund.Blog.ReadingProgressBar().init();
}
