"use strict";
var MZikmund;
(function (MZikmund) {
    var Blog;
    (function (Blog) {
        class ReadingProgressBar {
            constructor() {
                this.progressBar = null;
            }
            init() {
                // Wait for DOM to be ready before checking for blog post page
                if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', () => {
                        this.initialize();
                    });
                }
                else {
                    // DOM already loaded
                    this.initialize();
                }
            }
            initialize() {
                // Only initialize if we're on a blog post page
                if (!this.isBlogPostPage()) {
                    return;
                }
                this.createProgressBar();
                this.setupScrollListener();
            }
            isBlogPostPage() {
                // Check if we're on a blog post page by looking for the article with class blog-post
                return document.querySelector('article.blog-post') !== null;
            }
            createProgressBar() {
                // Create the progress bar element
                this.progressBar = document.createElement('div');
                this.progressBar.id = 'reading-progress-bar';
                this.progressBar.className = 'reading-progress-bar';
                // Insert at the beginning of the body
                document.body.insertBefore(this.progressBar, document.body.firstChild);
            }
            setupScrollListener() {
                window.addEventListener('scroll', () => {
                    this.updateProgress();
                }, { passive: true });
                // Initial update
                this.updateProgress();
            }
            updateProgress() {
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
        Blog.ReadingProgressBar = ReadingProgressBar;
    })(Blog = MZikmund.Blog || (MZikmund.Blog = {}));
})(MZikmund || (MZikmund = {}));
var RequestedTheme;
(function (RequestedTheme) {
    RequestedTheme["Dark"] = "dark";
    RequestedTheme["Light"] = "light";
    RequestedTheme["Auto"] = "auto";
})(RequestedTheme || (RequestedTheme = {}));
var DisplayTheme;
(function (DisplayTheme) {
    DisplayTheme["Dark"] = "dark";
    DisplayTheme["Light"] = "light";
})(DisplayTheme || (DisplayTheme = {}));
var MZikmund;
(function (MZikmund) {
    var Theming;
    (function (Theming) {
        class ThemeSwitchManager {
            static init() {
                document.addEventListener("DOMContentLoaded", () => {
                    ThemeSwitchManager.initTheme();
                });
                //ensure theme is set immediately to avoid flash of unstyled content
                ThemeSwitchManager.updateTheme(false);
            }
            static determineOsTheme() {
                if (window.matchMedia &&
                    window.matchMedia('(prefers-color-scheme: dark)').matches) {
                    return DisplayTheme.Dark;
                }
                else {
                    return DisplayTheme.Light;
                }
            }
            static updateThemeIcon(requestedTheme) {
                var icon = document.getElementById("theme-icon");
                if (icon) {
                    icon.className = "bi";
                    switch (requestedTheme) {
                        case RequestedTheme.Light:
                            icon.classList.add("bi-sun");
                            break;
                        case RequestedTheme.Dark:
                            icon.classList.add("bi-moon-stars");
                            break;
                        default:
                            icon.classList.add("bi-circle-half");
                            break;
                    }
                }
            }
            static updateTheme(loaded) {
                var requestedTheme = localStorage.getItem('ui-theme');
                if (requestedTheme == null) {
                    requestedTheme = RequestedTheme.Auto;
                }
                if (loaded) {
                    //clear theme selection
                    document.querySelectorAll("#theme-dropdown a.dropdown-item").forEach(item => item.classList.remove("selected"));
                    document.querySelectorAll("#theme-dropdown a[data-value='" + requestedTheme.toString().toLowerCase() + "'").forEach(item => item.classList.add("selected"));
                }
                ThemeSwitchManager.updateThemeIcon(requestedTheme);
                var displayTheme;
                if (requestedTheme == RequestedTheme.Auto) {
                    displayTheme = ThemeSwitchManager.determineOsTheme();
                }
                else {
                    displayTheme = requestedTheme;
                }
                document.documentElement.removeAttribute("data-bs-theme");
                document.documentElement.setAttribute("data-bs-theme", displayTheme.toString().toLowerCase());
            }
            static initTheme() {
                document.querySelectorAll("#theme-dropdown a.dropdown-item").forEach(item => item.addEventListener("click", function (e) {
                    var _a;
                    e.preventDefault();
                    localStorage.setItem('ui-theme', (_a = e.currentTarget.getAttribute("data-value")) !== null && _a !== void 0 ? _a : "");
                    ThemeSwitchManager.updateTheme(true);
                }));
                if (window.matchMedia) {
                    window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", () => {
                        ThemeSwitchManager.updateTheme(true);
                    });
                }
                ThemeSwitchManager.updateTheme(true);
            }
        }
        Theming.ThemeSwitchManager = ThemeSwitchManager;
    })(Theming = MZikmund.Theming || (MZikmund.Theming = {}));
})(MZikmund || (MZikmund = {}));
/// <reference path="ThemeSwitchManager.ts" />
/// <reference path="ReadingProgressBar.ts" />
MZikmund.Theming.ThemeSwitchManager.init();
new MZikmund.Blog.ReadingProgressBar().init();
