# mzikmund.dev design system

Fluent Design translated to the web. Bootstrap was removed; this is the complete
replacement vocabulary.

## Cascade

Declared once at the top of `Styles/_tokens.scss`:

```css
@layer tokens, base, layout, components, utilities;
```

A component rule can never be beaten by a base rule regardless of selector
specificity, and utilities always win. Never write `!important`.

## Files

| File | Contains |
|---|---|
| `Styles/_tokens.scss` | All custom properties, light/dark/high-contrast |
| `Styles/_fonts.scss` | Self-hosted `@font-face` (Inter, Source Serif 4, JetBrains Mono) |
| `Styles/_base.scss` | Reset, element defaults, focus, skip link |
| `Styles/_layout.scss` | `.container`, `.section`, `.card-grid`, `.article-layout` |
| `Styles/_code.scss` | ColorCode token colours + GitHub Gist dark theme |
| `Styles/components/*.scss` | nav, controls, card, hero, article, toc, footer |
| `Styles/_motion.scss` | Page transitions, connected animation, Reveal, entrances |
| `Styles/_utilities.scss` | The ~25 utilities actually used |

## Tokens

Never hard-code a colour, size, radius or duration. Everything is a
`--mz-*` custom property.

- **Type**: `--mz-text-caption|body|body-large|subtitle|title|title-large|display`
- **Fonts**: `--mz-font-sans` (UI), `--mz-font-serif` (article body), `--mz-font-mono` (code)
- **Weight**: `--mz-weight-regular|medium|semibold` — Fluent uses SemiBold, never Bold
- **Space**: `--mz-space-1..10` on a strict 4px grid (4, 8, 12, 16, 24, 32, 36, 48, 64, 96)
- **Radius**: `--mz-radius-control` (4px), `--mz-radius-card` / `--mz-radius-overlay` (8px), `--mz-radius-pill`
- **Elevation**: `--mz-elev-1..4`
- **Surfaces**: `--mz-bg-base|secondary|tertiary|elevated`, `--mz-card`, `--mz-layer`, `--mz-acrylic`
- **Text**: `--mz-text-primary|secondary|tertiary|disabled|on-accent`
- **Strokes**: `--mz-stroke-card|control|control-strong|divider|focus`
- **Accent**: `--mz-accent-light-3..dark-3`, applied via `--mz-accent-text|fill|fill-hover|subtle`

Colour values are the real WinUI 3 theme resources, not approximations.

### Theming

`<html>` carries `data-theme="light|dark"`, written by `ThemeSwitchManager`
render-blocking in `<head>`. The server renders **no** theme attribute, so
without JS the `prefers-color-scheme` fallback in `_tokens.scss` applies.

## Components

### Header
```html
<header class="site-header">
  <div class="container site-header__inner">
    <button class="site-header__toggle" data-nav-toggle aria-expanded="false" aria-controls="site-nav">
    <a class="site-brand">
    <ul class="site-nav" id="site-nav">
      <li><a class="site-nav__link" aria-current="page">
    <div class="site-header__actions">
```

### Buttons
`.btn` plus exactly one variant: `.btn--accent` (primary), `.btn--standard`
(secondary), `.btn--subtle` (tertiary), `.btn--accent-subtle`.
Sizes: `.btn--sm`, `.btn--lg`. Works on `<a>`, `<button>` and `<span>`.

Use `<span class="btn ...">` when the element is **inside** a card's wrapping
anchor — a nested anchor or button there is invalid HTML.

Arrow nudge: `<span class="btn__arrow">→</span>`.

### Cards
```html
<article class="card">
  <div class="card__media">                     <!-- or card__media card__media--empty -->
    <img class="card__image" loading="lazy">
    <div class="card__badges"><span class="badge">…</span></div>
  </div>
  <div class="card__body">
    <div class="meta"><span class="meta__item">…</span></div>
    <h3 class="card__title">
      <a class="card__link" href="…">Title</a>   <!-- stretched link covers the card -->
    </h3>
    <p class="card__excerpt">
    <div class="card__footer">
  </div>
</article>
```

`.card__link::after` covers the whole card, so the card must contain **no other
interactive element**. The accessible name goes on `.card__link`.

Grid wrapper: `.card-grid` (auto-fitting, no column classes).

### Badges and tags
`.badge` + `.badge--accent|subtle|neutral`. Tag pills: `.tag`.

### Hero
`.hero` > `.container` > `.hero__inner` with `.hero__eyebrow`, `.hero__title`,
`.hero__rule`, `.hero__lead`, `.hero__actions`, `.hero__topics`.

Post hero: `.post-hero` > `.post-hero__banner` (image) + `.post-hero__body` >
`.post-hero__inner`. **The title sits below the image on a solid surface**, not
overlaid on it.

### Page header
`.page-header` > `.page-header__title`, `.page-header__lead`, `.page-header__actions`.

### Article
`.prose` on the content wrapper. Styles every element the Markdig pipeline can
emit — do not add per-element classes in templates.

### Other
`.empty-state`, `.pagination` / `.pagination__link`, `.taxonomy-card`,
`.author-bio`, `.toc`, `.reading-progress`, `.back-to-top`.

## Motion

Uses the **published Fluent values**, not approximations
([timing and easing](https://learn.microsoft.com/windows/apps/design/motion/timing-and-easing)):

| Token | Value | Use for |
|---|---|---|
| `--mz-ease-entrance` | `cubic-bezier(0, 0, 0, 1)` | Anything arriving — "fast out, slow in" |
| `--mz-ease-exit` | `cubic-bezier(1, 0, 1, 1)` | Anything leaving |
| `--mz-ease-standard` | `cubic-bezier(0.55, 0.55, 0, 1)` | Point-to-point on existing elements |
| `--mz-duration-faster` | 83ms | Opacity-only changes |
| `--mz-duration-fast` | 167ms | `ControlFastAnimationDuration`; exits |
| `--mz-duration-normal` | 250ms | `ControlNormalAnimationDuration` |
| `--mz-duration-slow` | 333ms | Larger travel, page transitions |

Four mechanisms, each a progressive enhancement that degrades to nothing:

- **Page transitions** — cross-document View Transitions (`@view-transition { navigation: auto }`).
  The header and footer hold still via `view-transition-name`, so only content moves.
- **Connected animation** — Fluent's card→detail morph. `Motion.ts` assigns
  `view-transition-name: post-hero-image` to the clicked card's image; the post
  banner carries the same name, so the image travels between pages.
- **Entrances** — scroll-driven via `animation-timeline: view()`. No JS, no
  IntersectionObserver, and they replay as you browse.
- **Reveal** — the pointer-tracked radial highlight. `Motion.ts` publishes
  `--mz-reveal-x/y` from one delegated, rAF-coalesced listener.

## Materials

Per the [materials guidance](https://learn.microsoft.com/windows/apps/design/signature-experiences/materials):
**Mica is opaque and for long-lived surfaces; Acrylic is translucent and for
transient, light-dismiss ones.**

| Surface | Material | Why |
|---|---|---|
| Site header | Acrylic-style translucency | Long-lived, but the web idiom of content scrolling under glass is worth keeping |
| Theme flyout | True Acrylic | Transient and light-dismiss — textbook Acrylic |
| Mobile nav panel | Opaque | Full-width surface; legibility over an arbitrary hero beats translucency |

The theme flyout is a **popover**, which matters for more than z-order: a
`backdrop-filter` nested inside an ancestor that itself has a `backdrop-filter`
samples that ancestor's output rather than the page behind it, so acrylic on a
flyout inside the header never actually frosts. The top layer escapes that, and
also supplies light-dismiss and Escape — `ThemeSwitchManager` only handles
positioning, focus and arrow keys, and keeps a non-popover fallback path.

### Two traps

`@view-transition` **must not sit inside `@layer`** — Chrome silently ignores it
there, disabling page transitions entirely. It is deliberately un-layered in
`_motion.scss`.

Raising card content above the Reveal highlight uses `z-index` **without**
`position: relative`. Cards are flex containers, so `z-index` applies to their
static flex items; adding `position` would make `.card__body` the containing
block for `.card__link::after` and shrink the stretched link, making the card
image un-clickable.

## Icons

Inline SVG sprite in `Pages/Shared/_IconSprite.cshtml`, generated from the
`bootstrap-icons` package. Never use `<i class="bi bi-*">`.

```html
<svg class="icon" aria-hidden="true"><use href="#i-github"></use></svg>
```

Sizes: `.icon` (1em), `.icon--lg` (1.25em). Available ids: `i-github`,
`i-twitter-x`, `i-youtube`, `i-facebook`, `i-code-slash`, `i-calendar3`,
`i-arrow-left`, `i-arrow-up`, `i-inbox`, `i-circle-half`, `i-sun`,
`i-moon-stars`, `i-play-circle`, `i-play-fill`, `i-file-earmark-pdf`, `i-list`,
`i-link-45deg`, `i-check-lg`, `i-image`, `i-rss`, `i-tag`, `i-folder2-open`.

To add one: edit the `ICONS` array in `Scripts/build-assets.mjs` and run
`npm run build:assets`.

## Utilities

`.mt-0..8`, `.mb-0..8`, `.text-center`, `.text-secondary`, `.text-tertiary`,
`.text-accent`, `.text-caption`, `.text-body-large`, `.lead`, `.fw-semibold`,
`.no-underline`, `.break-word`, `.flex`, `.items-center`, `.justify-center`,
`.justify-between`, `.flex-wrap`, `.gap-2|3|4`, `.full-bleed`, `.hide-sm`,
`.show-sm-only`.

Prefer letting a component own its spacing over reaching for a utility.

## Contract with JavaScript

Renaming any of these silently breaks a feature — no error:

| Hook | Used by |
|---|---|
| `article.blog-post` | reading progress bar page gate |
| `.post-content` | TOC, heading anchors, table wrapping |
| `h2/h3/h4` inside `.post-content` | TOC + heading anchors (h1/h5/h6 ignored) |
| `#toc-container` | TOC mount point; hidden until `.is-visible` |
| `#theme-dropdown`, `[data-theme-trigger]`, `[data-theme-menu]`, `[data-value]` | theme switcher |
| `#theme-icon use` | theme icon swap |
| `[data-nav-toggle]`, `#site-nav` | mobile navigation |
| `#back-to-top` + `.is-visible` | back-to-top control |
| `localStorage["ui-theme"]` | stored theme preference — changing the key resets everyone |

`Scripts/index.ts` initialises `TableOfContents` **before** `HeadingLinks`,
because the TOC assigns the heading ids that HeadingLinks requires.

The 1280px breakpoint appears in both `TableOfContents.ts`
(`StackedBreakpoint`) and `_layout.scss` (`@media (min-width: 80rem)`) — they
must move together.

## Rules

1. No Bootstrap classes. No `container-fluid`, `row`, `col-*`, `d-flex`, `mb-4`,
   `text-muted`, `btn-primary`, `card-body`, `badge bg-*`.
2. No hard-coded colours, sizes, radii or durations — use tokens.
3. All spacing on the 4px grid.
4. Every interactive control needs an accessible name.
5. Animation must respect `prefers-reduced-motion` (handled globally in `_base.scss`).
6. Icon-only controls need `aria-label`; decorative SVGs need `aria-hidden="true"`.
