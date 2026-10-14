// Bundled to wwwroot/js/site.js as an ES module. Module scripts are deferred by
// default, which is what every module below already assumes.
import { ThemeSwitchManager } from "./ThemeSwitchManager";
import { SiteNav, BackToTop, ContentEnhancements } from "./SiteNav";
import { ConnectedAnimation, Reveal } from "./Motion";
import { ReadingProgressBar } from "./ReadingProgressBar";
import { TableOfContents } from "./TableOfContents";
import { HeadingLinks } from "./HeadingLinks";

// Wires up the theme flyout. The theme itself was already applied by
// theme-init.js, render-blocking, before first paint.
ThemeSwitchManager.init();

new SiteNav().init();
new BackToTop().init();
new ContentEnhancements().init();

// Fluent motion: connected animation between a card and its post, and the
// pointer-tracked Reveal highlight. Both are no-ops without support or when
// the visitor prefers reduced motion.
new ConnectedAnimation().init();
new Reveal().init();

new ReadingProgressBar().init();

// Order matters: TableOfContents assigns ids to headings that lack them, and
// HeadingLinks skips any heading without one.
new TableOfContents().init();
new HeadingLinks().init();
