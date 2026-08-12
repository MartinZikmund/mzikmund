# Uno / WinUI App — Fluent Redesign

**Date:** 2026-08-11
**Branch:** `dev/mazi/appredesign`
**Status:** Design — approved sections, pending spec review

Port the Fluent design language established in `dev/mazi/fluent-redesign` + `fluent-polish` from the
website to the Uno Platform / WinUI 3 app, and move the app to the latest public Uno.Sdk dev bits.

Source of truth for the design language, in priority order:

1. `src/web/MZikmund.Web/Styles/` — the SCSS itself
2. `docs/design-system.md` — the written doc (audited; see [Known doc drift](#known-doc-drift))

---

## 1. Why this is mostly a homecoming

The web design system is *WinUI 3 Fluent restated as CSS custom properties*. Nearly every colour token
is the literal WinUI theme resource value, the spacing is a strict 4px grid, the type ramp maps onto the
WinUI ramp, and the elevation levels are named after the Fluent levels. Porting it back to XAML is
mostly going home.

Three deliberate divergences the web made must be reproduced, because they are the difference between
"blue app" and "the same product":

1. **The accent is theme-flipped.** Light steps *down* the ramp, dark steps *up*.
2. **Elevation is named after what a surface *is*,** not how deep its shadow is.
3. **Weight tops out at 600.** SemiBold, never Bold.

And one divergence that must **not** be reproduced — see [Accessibility](#10-accessibility).

## 2. Starting position

The app has **no design-system layer at all**:

- **Zero `<Style>` elements** in the entire project.
- All 12 `DataTemplate`s are inline in their views; no shared template dictionary.
- `App.xaml` merges only `XamlControlsResources` + a 14-line `Colors.xaml` + `Converters.xaml` + an
  8-line `Fonts.xaml`.
- `Fonts.xaml` points at `icomoon.ttf` and `CascadiaCode.ttf`, **neither of which exists in the repo** —
  so the markdown editor is silently not monospaced today.
- No Uno.Themes / Uno.Toolkit / CommunityToolkit UI packages. Raw `XamlControlsResources` only.

Live defects to fix as part of this work:

| Defect | Location |
|---|---|
| `SystemColorHighlightBrush` (the *high-contrast selection* brush) used as a **card background** | `PostsManagerView:50`, `CategoriesManagerView:50`, `TagsManagerView:50` |
| `d:Background` (design-time only) instead of `Background` → no runtime background | `SettingsView:11`, `CategoriesManagerView:10`, `TagsManagerView:10`, `MediaBrowserView:13` |
| `NavigationService.Initialize()` never called → no hardware/system back on Android/WASM | `App.xaml.cs` |
| `IsBackEnabled` bound to non-notifying `Frame.CanGoBack` → stale back button | `WindowShell.xaml` |
| Legacy WinUI 2 brushes beside modern Fluent ones | `BlogView:49`, `VideosView:51`, + 5 × `SystemControlForegroundBaseMediumBrush` |
| `MediaBrowserDialog` selection VSM targets `Border.BorderBrush`/`BorderThickness` **on a `Grid`** → no-op | `MediaBrowserDialog.xaml` |
| Whole-card click via code-behind `Tapped` → no hover, press, focus, or keyboard activation | `BlogView`, `VideosView` |

## 3. Decisions

| # | Decision | Choice |
|---|---|---|
| D1 | Scope | **Everything**, including admin information-architecture rebuild |
| D2 | Post view | **Hybrid** — native hero + `WebView2` body |
| D3 | Accent | **Adopt `#0078D4`** with the theme-flipped applied ramp |
| D4 | Fonts | **Ship Inter + JetBrains Mono** as app assets |
| D5 | Navigation architecture | **Fix in place.** No Uno.Extensions.Navigation |
| D6 | Elevation | `CommunityToolkit.WinUI.Media` **on the Windows head only**; other heads flat |
| D7 | Settings controls | Use `CommunityToolkit.WinUI.Controls.SettingsControls`; **smoke-test on Android + iOS** |
| D8 | Hero transition | Real `ConnectedAnimationService` on Windows; cross-fade elsewhere |
| D9 | Icons | **Fluent System Icons** (subsetted font) + Bootstrap Icons `PathIcon` for brand marks |
| D10 | New pages | Build **`HomeView`** (with the web's hero) and **`ContactView`** |
| D11 | Uno.Sdk | **`6.8.0-dev.7`** |
| D12 | Sequencing | Foundation + `BlogView` as reference view, then fan out |

Explicitly **not** adopted: Uno.Themes (Material/Cupertino — conflicts with Fluent parity),
Uno.Toolkit.WinUI, Uno.Extensions.Navigation.

## 4. Platform reality

The app has five heads. **Four of the five run the Skia renderer**; only `net10.0-windows10.0.26100`
runs real WinUI. Windows is the reference platform; other heads degrade in *designed* ways.

### 4.1 Verified constraints

These were verified empirically (throwaway projects, real restores, package inspection, Uno source),
not assumed:

| API | Status | Consequence |
|---|---|---|
| `CommunityToolkit.WinUI.Media` | **Windows-only.** Restore fails `NU1202` on `-desktop` and `-browserwasm` | Reference conditioned on the Windows TFM |
| `Compositor.CreateDropShadow()` | **Not implemented** in Uno; [uno#13291](https://github.com/unoplatform/uno/issues/13291) closed as not planned | No composition shadows off-Windows |
| `ThemeShadow` (Skia) | Implemented but **single shadow, always black**, derived solely from `Translation.Z` | Cannot express the ambient+directional pair |
| `ConnectedAnimationService.GetForCurrentView()` | **Throws on every Uno target** | D8 — Windows only |
| `ScalarTransition` / `Vector3Transition` | `[NotImplemented]` everywhere | Use `Storyboard` |
| `UIElement.Translation` / `.Scale` | `[NotImplemented]`; `Translation` is not even a DependencyProperty | Use `RenderTransform` + `CompositeTransform` |
| `GridView` virtualization on Skia | **Does not virtualize** ([uno#21964](https://github.com/unoplatform/uno/issues/21964)) | `ItemsRepeater` + `UniformGridLayout` everywhere |
| `AcrylicBrush` | Backdrop variant works on **all** targets | Needs `AlwaysUseFallback="False"`; see 4.2 |
| `UISettings.AnimationsEnabled` | Hardcoded `true` on iOS, Desktop, WASM | Ship an in-app reduced-motion toggle |
| VSM animation on non-GPU property | **Silently skipped** without `EnableDependentAnimation="True"` | Audit every such animation |
| `BreadcrumbBar`, `ItemsRepeater`+`UniformGridLayout`, `NavigationView` modes, custom `.ttf`, `ISupportIncrementalLoading` | Work as designed | — |

### 4.2 Acrylic rule

On **Android, iOS and macOS** `AcrylicBrush` only works on elements with **no children**. Acrylic
surfaces are therefore authored as a bare `Border` *behind* content, never as the background of a
container that holds the content:

```xml
<Grid>
    <!-- background layer: no children -->
    <Border Grid.RowSpan="2">
        <Border.Background>
            <AcrylicBrush AlwaysUseFallback="False" TintColor="#FCFCFC" TintOpacity="0.85" />
        </Border.Background>
    </Border>
    <!-- content sits above it -->
    <TextBlock Text="…" />
</Grid>
```

### 4.3 Degradation matrix

| Feature | Windows | Desktop | WASM | Android | iOS |
|---|---|---|---|---|---|
| Elevation shadows | full ramp | stroke only | stroke only | stroke only | stroke only |
| Card hover lift | shadow + 2px rise + stroke + accent title | 2px rise + stroke + accent title | same | n/a (no hover) | n/a (no hover) |
| Acrylic | yes | yes | yes | yes (childless) | yes (childless) |
| Card → hero transition | `ConnectedAnimation` | cross-fade | cross-fade | cross-fade | cross-fade |
| Reduced motion | system | in-app toggle | in-app toggle | system | in-app toggle |

Because the web's own rule is that **every elevation level pairs its shadow with a 1px stroke**, the
contour still carries the hierarchy where shadows are absent. The flat look is designed, not accidental.

## 5. Token architecture

New `src/app/MZikmund.App/Theme/`, merged in `App.xaml`:

```
Theme/
  Colors.xaml        ThemeDictionaries: Light / Default (dark) / HighContrast
  Typography.xaml    FontFamily resources + the Mz*TextStyle ramp
  Spacing.xaml       MzSpace1..10 + common Thickness combos
  Radii.xaml         Control / Card / Large / Pill
  Elevation.xaml     six named levels
  Acrylic.xaml       AcrylicBrush resources
  Motion.xaml        Duration + KeySpline resources
  Icons.xaml         FontFamily, glyph constants, brand PathGeometry
  Styles/
    TextBlocks.xaml  Buttons.xaml  Cards.xaml  Badges.xaml  Tags.xaml
    Rows.xaml        Nav.xaml      Dialogs.xaml  Settings.xaml  EmptyState.xaml
```

All resources are `Mz`-prefixed.

**Values are ported literally, not aliased to WinUI resources.** The web's palette *is* the real WinUI
palette, but it deliberately diverges on accent, and aliasing would let Uno's Fluent values silently
drift away from the site on a future SDK bump. Key WinUI brush keys are then *overridden* to point at
the `Mz` brushes so stock controls (`ContentDialog`, `ComboBox`, `TextBox`, `NavigationView`) inherit
the palette instead of fighting it.

> **Port trap:** do **not** bind to `SystemAccentColor*`. Those follow the user's Windows
> personalization accent, and every contrast ratio the web verified goes out of the window. The ramp is
> hard-coded.

### 5.1 Colour — light

| Key | ARGB |
|---|---|
| `MzBgBase` | `#FFF3F3F3` |
| `MzBgSecondary` | `#FFEEEEEE` |
| `MzBgTertiary` | `#FFF9F9F9` |
| `MzBgElevated` | `#FFFFFFFF` |
| `MzLayer` | `#80FFFFFF` |
| `MzCard` | `#FFFFFFFF` |
| `MzCardSecondary` | `#80F6F6F6` |
| `MzAcrylicTint` | `#D9FCFCFC` (blur 20px, saturate **180%**) |
| `MzTextPrimary` | `#E4000000` |
| `MzTextSecondary` | `#9B000000` |
| `MzTextTertiary` | `#72000000` |
| `MzTextDisabled` | `#5C000000` |
| `MzTextOnAccent` | `#FFFFFFFF` |
| `MzStrokeCard` | `#0F000000` |
| `MzStrokeControl` | `#0F000000` |
| `MzStrokeControlStrong` | `#72000000` |
| `MzStrokeDivider` | `#14000000` |
| `MzStrokeFocus` | `#E4000000` |
| `MzAccentText` | `#FF005A9E` |
| `MzAccentFill` | `#FF005A9E` |
| `MzAccentFillHover` | `#FF004275` |
| `MzAccentFillActive` | `#FF002642` |
| `MzAccentSubtle` | `#140078D4` |
| `MzAccentSubtleStrong` | `#290078D4` |
| `MzShimmer` | `#0A000000` |

### 5.2 Colour — dark

| Key | ARGB |
|---|---|
| `MzBgBase` | `#FF202020` |
| `MzBgSecondary` | `#FF1C1C1C` |
| `MzBgTertiary` | `#FF282828` |
| `MzBgElevated` | `#FF2C2C2C` |
| `MzLayer` | `#4C3A3A3A` |
| `MzCard` | `#FF2B2B2B` |
| `MzCardSecondary` | `#08FFFFFF` |
| `MzAcrylicTint` | `#D92C2C2C` (blur 20px, saturate **150%** — not 180) |
| `MzTextPrimary` | `#FFFFFFFF` |
| `MzTextSecondary` | `#C8FFFFFF` |
| `MzTextTertiary` | `#8BFFFFFF` |
| `MzTextDisabled` | `#5DFFFFFF` |
| `MzTextOnAccent` | `#FFFFFFFF` (**but see 10.1**) |
| `MzStrokeCard` | `#1A000000` |
| `MzStrokeControl` | `#12FFFFFF` |
| `MzStrokeControlStrong` | `#8BFFFFFF` |
| `MzStrokeDivider` | `#15FFFFFF` |
| `MzStrokeFocus` | `#FFFFFFFF` |
| `MzAccentText` | `#FF76B9ED` |
| `MzAccentFill` | `#FF0078D4` |
| `MzAccentFillHover` | `#FF429CE3` |
| `MzAccentFillActive` | `#FF76B9ED` |
| `MzAccentSubtle` | `#1A76B9ED` |
| `MzAccentSubtleStrong` | `#3376B9ED` |
| `MzShimmer` | `#0DFFFFFF` |

### 5.3 Accent ramp (theme-invariant)

`#A6D8FF` · `#76B9ED` · `#429CE3` · **`#0078D4`** · `#005A9E` · `#004275` · `#002642`

**Light steps down; dark steps up.** This is what keeps accent text above 4.5:1 on both grounds.

### 5.4 Typography

| Role | Family |
|---|---|
| UI / headings / body | **Inter** (ship `.ttf`) |
| Code / markdown editor | **JetBrains Mono** (ship `.ttf`) |
| Article body | n/a — the post body is a `WebView`, so the site's serif applies |

Ship **`.ttf`, never `.woff2`** — SkiaSharp cannot parse woff2 and it would break Desktop, Android and iOS.

| Style | px (compact → wide) | Weight | Line height | Tracking |
|---|---|---|---|---|
| `MzCaptionTextStyle` | 13 | 500 | 1.3–1.5 | `0.02em` when uppercase |
| `MzBodyTextStyle` | 17 | 400 | 1.5 | 0 |
| `MzBodyLargeTextStyle` | 19 | 400 | 1.5 / 1.7 | 0 |
| `MzSubtitleTextStyle` | 20 → 22 | 600 | 1.15 (1.3 on card titles) | 0 |
| `MzTitleTextStyle` | 24 → 30 | 600 | 1.15 | `-0.022em` |
| `MzTitleLargeTextStyle` | 30 → 44 | 600 | 1.15 | `-0.022em` |
| `MzDisplayTextStyle` | 40 → 72 | 600 | 1.15 | `-0.022em` |

Rules:

- **Weights 400 / 500 / 600 only. Never 700.**
- Tight tracking applies **only at Title and above**. h3–h6 and card titles reset to normal.
- Card titles additionally override to `1.3` line height.
- XAML `CharacterSpacing` is in 1/1000 em → `-0.022em` = `-22`.
- XAML `LineHeight` is absolute px; each style sets it explicitly with
  `LineStackingStrategy="BlockLineHeight"`.
- The two-tier ramp (compact/wide) applies only from Subtitle upward. Base styles carry the compact
  value; a `MzTypeScale` VisualStateGroup on page roots swaps to the wide styles at ≥768.

### 5.5 Spacing, radius, layout

Strict 4px grid: `MzSpace1..10` = 4, 8, 12, 16, 24, 32, 36, 48, 64, 96.

Radius: `MzControlCornerRadius` **4** · `MzCardCornerRadius` **8** · `MzLargeCornerRadius` **12** ·
`MzPillCornerRadius`.

Layout: header height **56**, gutter **16** compact / **36** wide, container **1200**, narrow **780**,
reading measure ~**680**. Card grid `MinItemWidth=320`, spacing **24** both axes, `ItemsStretch="Fill"`.

**Breakpoints: 768 and 1280** (web parity; the app currently uses 768/1024).

### 5.6 Elevation

| Level | Shadow (light) | Shadow (dark) |
|---|---|---|
| `Layer` | `0 0 2px` @12% | @24% |
| `Control` | `0 1px 2px` @14% + `0 0 2px` @12% | @28% / @24% |
| `Card` | `0 4px 8px` @14% + `0 0 2px` @12% | @28% / @24% |
| `Tooltip` | `0 8px 16px` @14% + `0 0 2px` @12% | @32% / @28% |
| `Flyout` | `0 14px 28px` @24% + `0 0 8px` @20% | @44% / @36% |
| `Dialog` | `0 32px 64px` @24% + `0 0 8px` @20% | @48% / @40% |

> **Implementation reality (verified in Task 16):** `AttachedShadowBase` exposes `Color`, `Offset`,
> `BlurRadius` and `Opacity` as *singular* properties, and `Effects.Shadow` is a single attached
> property with no collection variant — **one shadow per element is a hard API ceiling.** So the
> ambient half of each pair cannot be applied. `Layer` is unaffected (its token is already a single
> `0 0 2px` shadow); the other five levels keep the **directional** half only. This is the right
> trade: the directional values must match the tokens exactly, a blended "compromise" shadow would
> distort that character without reproducing ambient, and the paired 1px stroke already supplies the
> tight occlusion contour the ambient half contributed. Nesting a second shadowed element per card,
> flyout and dialog was judged not worth the cost for a barely perceptible gain.

Implemented as an **`MzElevation.Level` attached property defined in the app assembly**, so XAML never
names a Windows-only type:

```csharp
public static class MzElevation
{
    public static readonly DependencyProperty LevelProperty = /* attached, enum MzElevationLevel */;

    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
#if WINDOWS
        // apply CommunityToolkit.WinUI.Media AttachedCardShadow for the level
#endif
        // elsewhere: no-op — the paired 1px stroke carries the hierarchy
    }
}
```

The entire platform split lives in this one file.

### 5.7 Motion

| Key | Value |
|---|---|
| `MzEaseEntrance` | `KeySpline="0,0,0,1"` (decelerate — anything arriving) |
| `MzEaseExit` | `KeySpline="1,0,1,1"` (accelerate — anything leaving) |
| `MzEaseStandard` | `KeySpline="0.55,0.55,0,1"` (point-to-point) |
| `MzDurationFast` | **167 ms** — colour / fill / border state changes |
| `MzDurationNormal` | **250 ms** — elevation, travel, flyouts |
| `MzDurationSlow` | **333 ms** — page transitions, image scale, hero entrance |

Budget rule: **167 fill · 250 depth/travel · 333 page & image.**

## 6. Component vocabulary

Three disjoint surface families. A component picks exactly one and never blends:

| Family | Composition | Used by |
|---|---|---|
| **Page ground** | `MzBg*` alone, never an elevation | page background, hero, footer, media placeholders, `pre` |
| **Content surface** | `MzCard` **+** `MzStrokeCard` **+** an elevation — always all three | cards (`Card`), taxonomy list / tag cloud / TOC / author bio (`Layer`) |
| **Material** | `MzAcrylicTint` **+** blur **+** `Flyout`/`Card` elevation | title bar, flyouts, mobile nav, back-to-top |

Interaction fills are their own tiny family and never come from the above:
**hover = `MzShimmer`**, **selected = `MzAccentSubtle(Strong)`**, **filled = `MzAccentFill` + `MzTextOnAccent`**.

| Web component | XAML counterpart |
|---|---|
| Content card | `MzContentCardStyle` — a `Button` with a `ContentTemplate`; hover/press/focus in its VSM |
| Card grid | `ItemsRepeater` + `UniformGridLayout` (`MinItemWidth=320`, spacing 24, `ItemsStretch=Fill`) |
| Taxonomy row | `MzTaxonomyRowStyle` — 44px min height `Button`, inside **one** `Layer` surface |
| Tag pill | `MzTagPillStyle` — pill `HyperlinkButton` + optional count chip; virtualized wrap layout |
| Badge | `MzBadgeStyle` — r**4**, not a pill; accent / subtle / neutral variants |
| Badge over photo | **theme-invariant** dark scrim + white text. Never themed |
| Buttons | `MzAccentButtonStyle` / `MzStandardButtonStyle` / `MzSubtleButtonStyle` / `MzAccentSubtleButtonStyle` + `Small` / `Large` size variants |
| Breadcrumb | `BreadcrumbBar`, above the page title |
| Empty state | `MzEmptyStateStyle` — 40px tertiary icon, body-large heading, secondary text, standard button |
| Settings row | `SettingsCard` / `SettingsExpander` |

### 6.1 Details that carry the look

- **Accent button inner border** — `inset 0 -1px 0 #000@14%` + `inset 0 1px 0 #fff@8%`. This is
  WinUI's `ControlElevationBorderBrush`: a vertical `LinearGradientBrush` on `BorderBrush` with
  `BorderThickness="1"`, bottom edge darker than the sides. The web's own note: *"that single detail
  does most of the work of feeling Fluent."*
- **Card hover** — rise **`-2px`**, border → `MzStrokeControlStrong`, title → `MzAccentText`, image
  `scale 1.03` over 333ms, elevation `Card` → `Tooltip` (Windows). The web is explicit that the old
  `-8px` with a heavy shadow *"reads as Material rather than Fluent."*
- **Button press** — `translateY(+1px)` at **0ms**. Fluent presses down, never scales.
- **Two indicator idioms** — a horizontal 16×3 pill under the item for **top nav only**; a vertical 3px
  bar on the leading edge for **everything list-shaped** (mobile nav, theme menu, TOC, taxonomy rows).
  The short bar *"reads as an artifact in a stacked list."*
- **Card title sits below the image on a solid surface**, never overlaid — previously legibility
  "depended entirely on which image had been uploaded."
- **Counts are right-aligned with `tabular-nums`** so they form a scannable column.
- **Zero-post tags** stay reachable at `opacity 0.55` so they "stop competing with tags that go somewhere."

## 7. Shell and navigation

- Title bar **56px** (matching `--mz-header-height`, up from 48), acrylic, logo with scale qualifiers,
  title at body/500 rather than 12px caption.
- Breakpoints **768 / 1280**.
- **Remove the `ContentHeader` band.** It renders `ViewModel.Title` while five pages also draw their
  own heading — the word appears twice below 768px. Pages own their header instead, in the web's shape:
  breadcrumb → title → lead → forward actions.
- Page titles use the **Title** step, not TitleLarge. The Fluent ramp reserves TitleLarge for hero and
  feature headings; only the Home hero goes bigger, via Display.
- **Forward actions only** in page headers ("Browse by category", "Subscribe") — not the way back.
  The breadcrumb tells you where you came from.

Navigation-service repairs (all in place; no Uno.Extensions.Navigation):

1. Call `NavigationService.Initialize()` → restores hardware/system back on Android and WASM.
2. Surface `CanGoBack` as an observable property on `WindowShellViewModel`.
3. Replace the hardcoded `MenuItemInvoked` if/else with `Tag`-based ViewModel-type dispatch (also stops
   Settings falling through into `ClearBackStack()`).
4. Add reverse selection sync so programmatic navigations update `AppNavigation.SelectedItem`.
5. **Replace `PageBase.EnsureViewModel`'s `XamlRoot.Content is WindowShell` cast** with an interface
   lookup. This redesign changes the shell; that cast silently nulls every page's ViewModel if the root
   type moves. Same for `DialogCoordinator`'s `IWindowShellProvider` lookup.
6. Fix the Wide/Mid visual states that force `TitleBarGrid` visible on platforms with no custom title
   bar, and replace the non-observable `HasCustomTitleBar` bool.
7. Either implement or delete `SetTitlebarColors()` and its live-but-dead `ColorValuesChanged` subscription.

## 8. Views

| View | Work |
|---|---|
| **HomeView** *(new)* | The web's Mica-inspired hero: accent glow (radial, offset top-right so it does not sit behind the text), fine noise overlay, Display-step title, staggered entrance at 0/60/120/160/220/280ms with the rule scaling in from the left. Plus a latest-posts strip. **No bottom rule** — the content below sits on its own layered surface. |
| **BlogView** | Reference view. Card grid on `MzContentCardStyle`; 16:9 media; category badges; calendar meta icon; 3-line excerpt; small accent-subtle "Read more" with the arrow nudge (*"the one flourish, kept small"*). Empty + error states. `ISupportIncrementalLoading` replaces the infinite-scroll-*and*-Load-More duplication. Localize. |
| **PostView** | Native hero (banner + badges + title + meta) above a `WebView2` on `/embed/post/{id}`. `Title` override on the ViewModel. Loading + failure states; stop swallowing exceptions. `ConnectedAnimation` from the blog card on Windows. |
| **VideosView** | Consumes the *same* card style as Blog — today the two grids look like different apps. Play affordance gets a scrim (`#000@25%` + white icon) so it survives light thumbnails. Header action replaces the naked Refresh button. Empty/error states. Localize. |
| **ContactView** *(new)* | Resolves the live no-op nav item. Contact routes + social links using the brand `PathIcon` set. |
| **SettingsView** | `SettingsCard` / `SettingsExpander`. Theme picker gets an `ItemTemplate` with localized labels + sun/moon/circle-half icons (today it renders raw enum `ToString()`). About section: version, site links, RSS, account state. **Reduced-motion toggle** (see 4.1). Fix the runtime background. |
| **PostsManagerView** | Dense rows replacing the highlight-blue tiles: status badge + title + route + date + category chips + per-row overflow. Search / status filter / sort. `CommandBar` replaces the hand-assembled `SymbolIcon`+`TextBlock` buttons (repeated 6× across the codebase). |
| **CategoriesManagerView** | `.taxonomy-list` shape — one r8 `Layer` surface holding 44px rows (folder icon + 500-weight name + right-aligned tabular count) with the centre-growing 3px hover bar. Kills the centred 120px `GridView`. |
| **TagsManagerView** | `.tag-cloud` — wrap layout of pill chips + 11px count chip, zero-post tags at `opacity 0.55`, on one r8 surface. **Must survive ~500 items — virtualized.** |
| **MediaBrowserView** | Virtualized `ItemsRepeater` + `UniformGridLayout` replacing the non-virtualizing `ItemsControl` + `ItemsWrapGrid`. Actions revealed on hover/focus rather than always-on. Real empty state. Load More out of the scrolled `StackPanel`. |
| **PostEditorView** | Adaptive states (it has none — the 50/50 split breaks on narrow). `CommandBar` + properties pane replacing the ragged action `StackPanel`. Monospaced editor once JetBrains Mono ships. Chips for categories and tags. Hero thumbnail. Dirty state, validation, save feedback. |
| **All 6 dialogs** | One shared `MzContentDialogStyle` (r8, `MzCard`, `MzStrokeCard`, `Dialog` elevation). Collapse the three one-`TextBlock` templates into one. De-clone the two MediaBrowser tile templates. Fix `MediaBrowserDialog`'s no-op selection VSM. Remove fixed `Width`/`Height`. Move status-severity colours out of the ViewModel into theme brushes and stop dumping `ex.ToString()` at the user. |

**Template duplication to collapse:** PostsManager / Categories / Tags → one row style. Blog / Videos →
one card style. MediaBrowserView / MediaBrowserDialog → one tile.

## 9. Iconography

Today there are **three and a half** parallel systems: PNG `BitmapIcon`s with `ShowAsMonochrome="False"`
(so they never tint), `SymbolIcon`s mixed into the *same* nav pane, raw MDL2 codepoints with no
`FontFamily` (tofu risk on four heads), and a dead icomoon pipeline pointing at a font that is not in
the app.

Replacement:

- **One subsetted `FluentSystemIcons-Resizable.ttf`** in `Assets/Fonts/`. Measured **1535 KB → 13.2 KB**
  for the ~41 needed concepts across regular and filled. Pin the upstream commit SHA; check in an
  `icons.txt` + regeneration script so future additions are not a mystery.
- **Brand marks come from Bootstrap Icons as `PathIcon`.** Fluent System Icons contains **zero** brand
  glyphs (verified across all 25k+ names). Their 16×16 viewBox maps 1:1 to `PathIcon` with no transform.
- Declared once as a `FontFamily` resource; `MzFontIcon` repointed at it.
- No iOS `UIAppFonts` and no WASM `@font-face` needed — those are pre-Uno-4.7 requirements.
- Delete: the nav PNGs, `IconFont`, the 4 icomoon glyph keys, the raw MDL2 usages, and the dead Roboto
  `@font-face` rules in `Platforms/WebAssembly/WasmCSS/Fonts.css` (they 404).
- Add the MIT notices for both icon sets to a `THIRD-PARTY-NOTICES.txt`.
- Give `NavigationViewLogo.png` scale qualifiers or make it vector; add `BackgroundColor` to the bare
  `<uap:SplashScreen />`; fix the filename typo `Resources/ColorResoruces.cs`.

## 10. Accessibility

### 10.1 A web bug that must not be ported

In dark theme the web's accent buttons keep `color: #FFFFFF` through hover and active, over
`#429CE3` and `#76B9ED`:

| State | Contrast | Verdict |
|---|---|---|
| dark accent hover — white on `#429CE3` | **2.96:1** | fails AA |
| dark accent active — white on `#76B9ED` | **2.11:1** | fails badly |

**The XAML port flips the dark-theme accent-button foreground to black on hover/pressed.** This is
exactly what WinUI does — dark `TextOnAccentFillColorPrimary` is `#FF000000`. Raise the equivalent fix
on the website separately.

### 10.2 Everything else

- **`HighContrast` theme dictionary**, mirroring the web's `forced-colors` mapping and killing all
  elevation. Only the 8 permitted `SystemColor*` brushes.
- Cards become real `Button`s — keyboard activation and focus visuals, replacing code-behind `Tapped`.
- `AutomationProperties.Name` on every icon-only control; `AutomationId` on interactive controls.
- Fluent's **two-tone focus ring** (dark inner stroke, light outer halo) so it survives any surface.
- Minimum 44px touch targets on nav and taxonomy rows.
- Full `en` / `cs` coverage for every string touched. Public views currently hardcode English
  ("Load More", "Read more →", "Refresh", "Watch on YouTube", `Header="Id"`), and there is no `Videos`
  key in the `.resw`.

## 11. SDK and build hygiene

```json
"msbuild-sdks": { "Uno.Sdk": "6.8.0-dev.7" }
```

1. **Add a repo-root `NuGet.config`** pinned to nuget.org only. This is the highest-value fix: there is
   no `NuGet.config` today, so local restore silently uses machine-level private feeds. That is why
   `6.7.7` appears to be a stable release — it resolves from the private *Uno Internal* Azure DevOps
   feed and **would fail CI**. There is no public 6.7.x stable. Afterwards the `NU1507` suppression can
   likely be dropped.
2. `global.json` → `6.8.0-dev.7`. **Never `6.7.7`.** Conservative fallback is `6.7.0-dev.165`.
3. **Delete `UnoWasmBootstrapVersion` `10.1.0-dev.54`** (`Directory.Build.props:30`) — the only live
   stale pin, holding the WASM head ~160 builds back. Then re-test whether the
   `UnoGenerateAssetsManifestDependsOn` workaround at lines 32–36 is still needed; it almost certainly
   existed because of that pin.
4. **Delete six inert `PackageVersion` entries** nothing references (`Xamarin.Google.Android.Material`,
   `Microsoft.Extensions.Logging.Console`, `Microsoft.Windows.Compatibility`, `SkiaSharp.Skottie`,
   `SkiaSharp.Views.Uno.WinUI`, `Xamarin.AndroidX.Annotation`). They are booby-traps: the day someone
   adds a matching `PackageReference` they silently pin an old version.
5. **Re-comment `Microsoft.WindowsAppSDK 1.8.260209005`, `Microsoft.Windows.SDK.BuildTools`,
   `CommunityToolkit.Mvvm 8.4.0`** as applying only to `src/tools/MZikmund.LegacyMigration`. They sit
   under a `<!-- Uno -->` header today, which is actively misleading. **Do not** force WinAppSDK 1.8
   into the Uno app — Uno 6.7 and 6.8 are both built against 1.7.250909003, and 1.8 split into
   `Microsoft.WindowsAppSDK.WinUI`.
6. `CommunityToolkit.WinUI.Media` — **Windows TFM condition only**.
   `CommunityToolkit.WinUI.Controls.SettingsControls` `8.2.251219`, matching the `8.2.251219` already
   used for `Converters`.
7. Remove `Navigation` from `<UnoFeatures>` — nothing consumes it and D5 rules it out. Verify nothing
   in the Extensions stack depends on it first.
8. Ship `Assets/Fonts/` (Inter, JetBrains Mono, subsetted Fluent System Icons) and fix or delete the two
   dangling `ms-appx:` font URIs.
9. Correct `CLAUDE.md`'s stale "Uno.Sdk 6.5.0-dev.31".

## 12. Sequencing

| Phase | Content |
|---|---|
| **0** | SDK bump + build hygiene (§11). Full multi-TFM build green before any design work. |
| **1** | `Theme/` tokens + `Styles/` library **+ `BlogView`** as the reference view. Locks the vocabulary. |
| **2** | Shell: title bar, breakpoints, page-header pattern, the seven navigation repairs. |
| **3** | *(parallel)* `HomeView`, `PostView`, `VideosView`, `SettingsView`, `ContactView`. |
| **4** | *(parallel)* Admin rebuild: Posts, Categories, Tags, MediaBrowser, PostEditor. |
| **5** | Dialogs, motion pass, accessibility pass, localization sweep. |

Phase 1 is the gate. Until the card style is proven in a real grid, everything downstream is guesswork.

## 13. Verification

- **Build:** four TFMs locally (Windows, Desktop, Android, iOS) after Phase 0 and at the end of each
  phase. **WASM is CI-verified only** — the dev machine reports `wasm-tools` as installed but only
  carries the .NET 7/8-era Emscripten packs (3.1.12, 3.1.56), so Uno's native-asset step cannot find a
  toolchain. This predates the redesign and does not affect CI, which installs workloads explicitly.
  Accepted risk: WASM-only regressions (font loading, acrylic, `ItemsRepeater` virtualization) will not
  surface until a CI run. Fix locally any time with `dotnet workload install wasm-tools`.
- **Visual:** Uno App MCP to drive the desktop head and screenshot each redesigned view in **both**
  light and dark.
- **Windows head:** verify shadows, `ConnectedAnimation`, and acrylic actually render.
- **Mobile smoke test (required by D7):** run `SettingsCard` / `SettingsExpander` on a physical Android
  device and an iOS device. Their `android35.0` / `ios18.0` assets were compiled against
  *native-renderer* Uno.WinUI 5.5.87 while this app ships the Skia Uno.UI — it builds clean and can
  fail at runtime.
- **Animation audit:** every VSM animation on a non-GPU property has `EnableDependentAnimation="True"`,
  or Uno drops it silently.
- **Contrast:** re-verify the dark accent-button fix and the HighContrast dictionary.
- `dotnet format MZikmund.slnx` at the end.

## 14. Risks

| Risk | Mitigation |
|---|---|
| `RadialGradientBrush` (Home hero glow) support on Uno unconfirmed | Verify in Phase 1; fall back to a layered `LinearGradientBrush` or a pre-rendered asset |
| Noise texture needs tiling; `ImageBrush` has no `TileMode` in WinUI | Pre-sized noise asset, or drop the grain and keep the glow |
| `SettingsControls` runtime failure on Android/iOS | Mobile smoke test is a gate, not a nicety; hand-rolled fallback is ~120 lines if it fails |
| Shell restructure breaks every page's ViewModel | Replace the `XamlRoot.Content is WindowShell` cast **first**, in Phase 2 |
| Minor-version SDK jump destabilises a head | Phase 0 is isolated and independently revertable; `6.7.0-dev.165` is the fallback |
| ~500 tags in a wrap layout | Virtualization is a requirement, verified with real data |

## Known doc drift

`docs/design-system.md` was audited against the SCSS. It is stale in these places and should be
corrected separately:

- Documents `.taxonomy-card`, which **no longer exists** — commit `bd42857` replaced it with
  `.taxonomy-list` / `.taxonomy-row` and `.tag-cloud`.
- Omits `.breadcrumb`, `.meta`, `.section-head*`, `.post-footer`, `.card__play`, `.tag__count`,
  `.tag--empty` and others.
- Omits tokens `--mz-radius-large`, `--mz-card-secondary`, `--mz-acrylic-blur`,
  `--mz-accent-fill-active`, `--mz-accent-subtle-strong`, `--mz-shimmer`, `--mz-measure`, the
  `--mz-leading-*` / `--mz-tracking-*` sets and the nine `--mz-code-*` pairs.
- Lists tokens that are dead in the SCSS (0 usages): `--mz-duration-faster`, `--mz-elev-control`,
  `--mz-layer`, `--mz-accent-light-3`. **In XAML these map 1:1 onto real WinUI resources and the app has
  surfaces the web does not — keep them.**
- States "never `!important`" (9 occurrences exist) and "never Bold" (`_code.scss:241` uses 700).
- Claims the 1280px breakpoint appears in two places; it appears in **three, with three different
  numbers**, leaving a 0.16px sliver where neither rule set applies. The XAML port uses exactly 768 and
  1280 and avoids the whole problem.
