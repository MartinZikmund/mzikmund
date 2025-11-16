/// <reference path="ThemeSwitchManager.ts" />
/// <reference path="ReadingProgressBar.ts" />
/// <reference path="TableOfContents.ts" />

MZikmund.Theming.ThemeSwitchManager.init();
new MZikmund.Blog.ReadingProgressBar().init();

// Initialize table of contents on blog post pages
const toc = new MZikmund.Blog.TableOfContents();
toc.init();
