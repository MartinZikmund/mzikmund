/// <reference path="ThemeSwitchManager.ts" />
/// <reference path="ReadingProgressBar.ts" />
/// <reference path="TableOfContents.ts" />
/// <reference path="HeadingLinks.ts" />

MZikmund.Theming.ThemeSwitchManager.init();
new MZikmund.Blog.ReadingProgressBar().init();

// Initialize table of contents on blog post pages only
new MZikmund.Blog.TableOfContents().init();

// Add copy-link buttons to headings in blog posts
new MZikmund.Blog.HeadingLinks().init();
