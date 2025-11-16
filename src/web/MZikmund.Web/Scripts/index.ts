/// <reference path="ThemeSwitchManager.ts" />
/// <reference path="TableOfContents.ts" />

MZikmund.Theming.ThemeSwitchManager.init();

// Initialize table of contents on blog post pages only
if (document.getElementById('toc-container')) {
	const toc = new MZikmund.Blog.TableOfContents();
	toc.init();
}
