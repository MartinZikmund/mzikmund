/// <reference path="ThemeSwitchManager.ts" />
/// <reference path="SiteNav.ts" />
/// <reference path="Motion.ts" />
/// <reference path="ReadingProgressBar.ts" />
/// <reference path="TableOfContents.ts" />
/// <reference path="HeadingLinks.ts" />

// Runs render-blocking in <head>: applies the stored theme before first paint.
MZikmund.Theming.ThemeSwitchManager.init();

new MZikmund.Chrome.SiteNav().init();
new MZikmund.Chrome.BackToTop().init();
new MZikmund.Chrome.ContentEnhancements().init();

// Fluent motion: connected animation between a card and its post, and the
// pointer-tracked Reveal highlight. Both are no-ops without support or when
// the visitor prefers reduced motion.
new MZikmund.Motion.ConnectedAnimation().init();
new MZikmund.Motion.Reveal().init();

new MZikmund.Blog.ReadingProgressBar().init();

// Order matters: TableOfContents assigns ids to headings that lack them, and
// HeadingLinks skips any heading without one.
new MZikmund.Blog.TableOfContents().init();
new MZikmund.Blog.HeadingLinks().init();
