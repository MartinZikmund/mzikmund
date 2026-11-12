# Uno/WinUI App Fluent Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the Fluent design language from the redesigned mzikmund.dev website to the Uno Platform / WinUI 3 app, and move the app to the latest public Uno.Sdk dev bits.

**Architecture:** A new `Theme/` ResourceDictionary set (colours, typography, spacing, radii, elevation, acrylic, motion, icons) plus a `Styles/` library defines the whole visual vocabulary; every view is then rebuilt against it. Windows is the reference platform — elevation shadows and connected animation are Windows-only and degrade to designed stroke-and-fill treatments on the four Skia heads. Navigation stays hand-rolled (no Uno.Extensions.Navigation) with its four real defects repaired in place.

**Tech Stack:** .NET 10, Uno.Sdk 6.8.0-dev.7, WinUI 3 / Uno.WinUI, CommunityToolkit.WinUI (SettingsControls all heads; Media Windows-only), Fluent System Icons + Bootstrap Icons, Inter + JetBrains Mono.

**Spec:** `docs/superpowers/specs/2026-08-11-app-fluent-redesign-design.md`

---

## Global Constraints

Every task's requirements implicitly include this section.

### Platform — verified, non-negotiable

- **`ScalarTransition`, `Vector3Transition`, `UIElement.Translation`, `UIElement.Scale` are `[NotImplemented]` on every Uno target.** `Translation` is not even a DependencyProperty, so it cannot be a Storyboard or binding target. All motion goes through `RenderTransform` + `CompositeTransform` Storyboards.
- **Any VSM animation on a non-GPU property** (`Width`, `Height`, `Margin`, brush `Color`) **needs `EnableDependentAnimation="True"`** or Uno silently skips it — no error, Debug-level log only.
- **`AcrylicBrush` needs `AlwaysUseFallback="False"`.** On Android, iOS and macOS it only works on elements with **no children** — author acrylic as a bare `Border` behind content, never as a container's background.
- **`GridView` does not virtualize on Skia** ([uno#21964](https://github.com/unoplatform/uno/issues/21964)). Use `ItemsRepeater` + `UniformGridLayout`.
- **`ConnectedAnimationService.GetForCurrentView()` throws on every Skia head.** Windows only, behind `#if WINDOWS`.
- **`CommunityToolkit.WinUI.Media` is Windows-only** — it fails to restore (`NU1202`) on the Uno TFMs. It may only be touched inside `#if WINDOWS`, and **no Windows-only type may ever be named in XAML**. The whole platform split lives in the `MzElevation` attached property.
- **Uno 6.8 exposes a root-level `WebView2` namespace.** Always write `Microsoft.UI.Xaml.Controls.WebView2` in full; a `using WebView2 = …` alias fails with `CS0576`.
- **Never bind to `SystemAccentColor*`** — that follows the user's Windows personalization accent and destroys every verified contrast ratio. The accent ramp is hard-coded.
- **`UISettings.AnimationsEnabled` is hardcoded `true` on iOS, Desktop and WASM.** Reduced motion needs an in-app setting.
- **WASM is CI-verified only** on this machine (missing .NET 10 Emscripten packs). Local build gate is Windows + Desktop + Android + iOS.

### Design system

- **Accent is `#0078D4`** with a theme-flipped applied ramp: **light steps *down* the ramp, dark steps *up*.** Light `MzAccentText` = `#FF005A9E`; dark = `#FF76B9ED`.
- **Weights 400 / 500 / 600 only. Never 700.** SemiBold, never Bold.
- **Each static `.ttf` carries one weight** — Regular/Medium/SemiBold each need their own `FontFamily` resource, or WinUI synthesizes a fake weight instead of selecting the real cut.
- **Strict 4px spacing grid:** `MzSpace1..10` = 4, 8, 12, 16, 24, 32, 36, 48, 64, 96.
- **Radii:** control 4, card 8, large 12, plus pill.
- **Breakpoints 768 and 1280.**
- **Tight tracking (`-0.022em` = `CharacterSpacing="-22"`) applies only at Title and above.** Card titles reset to normal tracking and use 1.3 line height.
- **Card hover rises `-2px`, not `-8px`** ("-8px reads as Material rather than Fluent"). **Button press is `translateY(+1px)` at 0ms** — Fluent presses down, never scales.
- **Motion budget: 167ms fill · 250ms depth/travel · 333ms page & image.**
- **Three disjoint surface families, never blended:** page ground (`MzBg*`, never an elevation); content surface (`MzCard` + `MzStrokeCard` + an elevation — always all three); material (acrylic + blur + flyout elevation). Interaction fills are separate: hover = `MzShimmer`, selected = `MzAccentSubtle`, filled = `MzAccentFill` + `MzTextOnAccent`.
- **Badges over photography are theme-invariant** — a theme-aware colour would be illegible on half the archive.
- **Dark-theme accent buttons flip their foreground to black on hover/pressed.** The website keeps white and lands at 2.11:1, which fails WCAG AA. Do not port that bug.

### Code style

- Tabs for indentation, CRLF line endings, file-scoped namespaces, Allman braces, nullable enabled.
- `Mz` prefix on every design resource.
- Target-typed `new()` with explicit type on the left; `var` only when the type is not on the left.
- Always braces on single-line `if`/`for`/`foreach`/`while`.
- Expression bodies for single-line members.
- `dotnet format MZikmund.slnx` at the end of each phase where code changed.

### Verification

Each phase ends green on: `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f <tfm>` for `net10.0-windows10.0.26100`, `net10.0-desktop`, `net10.0-android`, `net10.0-ios`. Visual checks via the Uno App MCP on the desktop head in **both light and dark theme**.

---

## Assembly reconciliation

This plan was authored by parallel agents, one per phase/view, then assembled and consistency-checked
mechanically. Two classes of drift were found and resolved:

**1. Resource keys renamed to Phase 1's canonical names** (36 references corrected). Later phases
invented alternative names for glyphs Phase 1 already defines. The canonical names win:

| Was written as | Canonical |
|---|---|
| `MzAcrylicMaterialBrush` | `MzAcrylicBrush` |
| `MzIconHomeGlyph` | `MzGlyphHome` |
| `MzIconBlogGlyph` | `MzGlyphNews` |
| `MzIconPostsGlyph` | `MzGlyphDocument` |
| `MzIconVideoGlyph` | `MzGlyphVideoClip` |
| `MzIconContactGlyph` | `MzGlyphMail` |
| `MzIconAdminGlyph` | `MzGlyphShield` |
| `MzIconCategoriesGlyph` | `MzGlyphFolder` |
| `MzIconTagsGlyph` | `MzGlyphTag` |
| `MzIconMediaGlyph` | `MzGlyphImageMultiple` |
| `MzIconLoginGlyph` | `MzGlyphPerson` |
| `MzIconLogoutGlyph` | `MzGlyphSignOut` |

**2. Three glyphs are referenced but genuinely absent from Phase 1's `Theme/Icons.xaml`.** Add them
when building that file, subsetting the corresponding Fluent System Icons codepoints alongside the
rest. Do not substitute a near-match silently — each has a distinct meaning:

| Key | Meaning | Suggested Fluent System Icon |
|---|---|---|
| `MzGlyphError` | Error severity in `StatusMessageDialog` — distinct from `MzGlyphWarning` and `MzGlyphInfo` | `ic_fluent_error_circle_20_filled` |
| `MzGlyphImage` | A single image (media tile placeholder) — distinct from `MzGlyphImageMultiple` and `MzGlyphImageOff` | `ic_fluent_image_20_regular` |
| `MzGlyphSignOut` | Sign out action in Settings — distinct from `MzGlyphPerson` (sign in) | `ic_fluent_sign_out_20_regular` |

**Verify after building `Theme/Icons.xaml`:** every `{StaticResource Mz…}` / `{ThemeResource Mz…}` in
this plan must resolve. Re-run the check with:

```bash
python -c "
import re
t=open('docs/superpowers/plans/2026-08-11-app-fluent-redesign.md',encoding='utf-8').read()
d=set(re.findall(r'x:Key=\"(Mz[A-Za-z0-9_]+)\"',t))
r=set(re.findall(r'\{(?:Static|Theme)Resource\s+(Mz[A-Za-z0-9_]+)\}',t))
print(sorted(r-d))"
```

## Phase status

| Phase | Content | Status |
|---|---|---|
| 0 | SDK bump + build hygiene | ✅ **Complete** — see note below |
| 1 | `Theme/` tokens + `Styles/` library + BlogView reference view | Planned |
| 2 | Shell: title bar, breakpoints, page header, navigation repairs | Planned |
| 3 | HomeView, PostView, VideosView, SettingsView, ContactView | Planned |
| 4 | Admin rebuild + shared row/tag styles | Planned |
| 5 | Dialogs, motion pass, accessibility pass, localization sweep | Planned |

> **Phase 0 was executed before this plan was written.** Its tasks are retained below for the record, but note two deviations from what the planner assumed:
> 1. **Fonts were deferred to Phase 1**, where `Typography.xaml` consumes them. Phase 0 shipped no font assets and left `Resources/Fonts.xaml` untouched.
> 2. The planner references a `scripts/Get-Fonts.ps1` that **does not exist** — there is no `scripts/` directory in this repo. Any font-fetch script must be created from scratch. The web's own fonts are **variable `.woff2`**, which SkiaSharp cannot parse, so static `.ttf` weights must be sourced separately.
>
> Additional work done in Phase 0 that the plan did not anticipate: `System.Security.Cryptography.Xml` 10.0.8 → 10.0.10 (Uno 6.8's implicit `Microsoft.Windows.Compatibility 10.0.10` requires it; also closes five high-severity advisories), and fully qualifying `WebView2` in `PostView.xaml.cs` and `PostEditorView.xaml.cs` for the Uno 6.8 namespace collision.

---


---

## Phase 0 — SDK bump and build hygiene (ALREADY EXECUTED — retained for record)

# Phase 0 — SDK bump and build hygiene (spec §11)

## File structure

## Files created

| Path | Responsibility |
|---|---|
| `NuGet.config` | Repo-root restore pin: `<clear />` + nuget.org only, so a local restore resolves exactly what CI resolves. Also clears `disabledPackageSources`. |
| `scripts/Get-Fonts.ps1` | Pinned, re-runnable download of Inter 4.1 and JetBrains Mono 2.304 into `Assets/Fonts/` + their OFL licences into `docs/licenses/`. |
| `src/app/MZikmund.App/Assets/Fonts/Inter-Regular.ttf` | UI font, weight 400. Auto-included as `Content` by `Uno.DefaultItems.targets` (`$(AssetsFolder)**`) — no csproj entry needed. |
| `src/app/MZikmund.App/Assets/Fonts/Inter-Medium.ttf` | UI font, weight 500. |
| `src/app/MZikmund.App/Assets/Fonts/Inter-SemiBold.ttf` | UI font, weight 600 (the ramp tops out here — never 700). |
| `src/app/MZikmund.App/Assets/Fonts/JetBrainsMono-Regular.ttf` | Code / markdown-editor font. Replaces the dangling `CascadiaCode.ttf` URI. |
| `docs/licenses/Inter-OFL.txt` | SIL OFL 1.1 text shipped with Inter 4.1. Kept out of `Assets/` so it is not packaged into the app. |
| `docs/licenses/JetBrainsMono-OFL.txt` | SIL OFL 1.1 text shipped with JetBrains Mono 2.304. |

## Files modified

| Path | Change |
|---|---|
| `global.json` | `Uno.Sdk` `6.7.0-dev.48` → `6.8.0-dev.7` (D11). |
| `src/Directory.Build.props` | Drop `NU1507` from `NoWarn` (single source now); delete the inert `UnoWasmBootstrapVersion` pin and the `UnoGenerateAssetsManifestDependsOn` workaround. |
| `src/Directory.Packages.props` | Delete 6 inert `PackageVersion` entries; regroup + re-comment the three `MZikmund.LegacyMigration`-only pins; add `CommunityToolkit.WinUI.Media` and `CommunityToolkit.WinUI.Controls.SettingsControls` at `8.2.251219`. |
| `src/app/MZikmund.App/MZikmund.App.csproj` | Remove `Navigation` from `<UnoFeatures>`; add `SettingsControls` (all TFMs) and `Media` (Windows platform identifier only). |
| `src/app/MZikmund.App.Core/MZikmund.App.Core.csproj` | Remove `Navigation` from `<UnoFeatures>` (the spec only names one project — this one has it too). |
| `src/app/MZikmund.App/Views/PostView.xaml.cs` | Fully qualify `Microsoft.UI.Xaml.Controls.WebView2` — Uno 6.8 adds a root-level `WebView2` namespace that shadows the simple name. **The SDK bump does not compile without this.** |
| `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs` | Same fully-qualification, 3 sites (field, ctor, `OnWebViewInitialized` signature). |
| `src/app/MZikmund.App/Resources/Fonts.xaml` | Replace the two dangling `ms-appx:` URIs and the 4 dead icomoon glyph keys with the four real `Mz*` `FontFamily` resources. |
| `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml` | `{StaticResource CodeFont}` → `{StaticResource MzFontFamilyMono}` (2 sites). |
| `.gitattributes` | `*.ttf binary` so git never line-ending-mangles the fonts. |
| `CLAUDE.md` | Stale "Uno.Sdk 6.5.0-dev.31" → 6.8.0-dev.7; document the new `NuGet.config`. |

## Files deleted

| Path | Reason |
|---|---|
| `src/app/MZikmund.App/Extensions/Xaml/MzFontIcon.cs` | Markup extension pointing at the non-existent `icomoon.ttf`. Verified zero XAML usages (`Select-String MzFontIcon` matches only its own declaration). Phase "Icons" re-adds it pointed at the subsetted Fluent System Icons font. |
| `src/app/MZikmund.App/Resources/FontResources.cs` | Its only member was `IconFont`, which resolved the deleted resource key. `ResourceAccessor.cs` and `ColorResoruces.cs` stay. |

## Interfaces

## Produced by Phase 0 — later phases consume these exact names

### XAML resource keys (in `src/app/MZikmund.App/Resources/Fonts.xaml`)

```
MzFontFamilyRegular   FontFamily  ms-appx:///Assets/Fonts/Inter-Regular.ttf#Inter
MzFontFamilyMedium    FontFamily  ms-appx:///Assets/Fonts/Inter-Medium.ttf#Inter Medium
MzFontFamilySemiBold  FontFamily  ms-appx:///Assets/Fonts/Inter-SemiBold.ttf#Inter SemiBold
MzFontFamilyMono      FontFamily  ms-appx:///Assets/Fonts/JetBrainsMono-Regular.ttf#JetBrains Mono
```

Family names after `#` were read out of the shipped files' `name` table (GDI family, name ID 1) — they are exact and case-sensitive: `Inter`, `Inter Medium`, `Inter SemiBold`, `JetBrains Mono`. Each static `.ttf` carries exactly one weight, so **each weight needs its own `FontFamily` resource**; setting `FontWeight="SemiBold"` against `MzFontFamilyRegular` will synthesize, not select.

**Phase 1 contract:** `Theme/Typography.xaml` MUST re-declare these four keys with the same names and the same URIs, and `Resources/Fonts.xaml` MUST then be deleted and its `<ResourceDictionary Source="ms-appx:///Resources/Fonts.xaml" />` entry removed from `App.xaml`. Do not invent a second set of family keys.

### Font asset paths (stable, referenced by the URIs above)

```
src/app/MZikmund.App/Assets/Fonts/Inter-Regular.ttf
src/app/MZikmund.App/Assets/Fonts/Inter-Medium.ttf
src/app/MZikmund.App/Assets/Fonts/Inter-SemiBold.ttf
src/app/MZikmund.App/Assets/Fonts/JetBrainsMono-Regular.ttf
```

The icon phase adds `src/app/MZikmund.App/Assets/Fonts/FluentSystemIcons-Resizable.ttf` next to them and a sibling `scripts/Get-Icons.ps1` modelled on `scripts/Get-Fonts.ps1`.

### Packages available to later phases

```
CommunityToolkit.WinUI.Media                     8.2.251219  — Windows head ONLY
CommunityToolkit.WinUI.Controls.SettingsControls 8.2.251219  — all five heads
CommunityToolkit.WinUI.Converters                8.2.251219  — all five heads (pre-existing)
```

`CommunityToolkit.WinUI.Media` is referenced under `Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'"`. Consequence for §5.6: the `MzElevation.Level` attached property may only touch `CommunityToolkit.WinUI.Media` types inside `#if WINDOWS`; that one file is the whole platform split, and no XAML may name a Windows-only type.

`CommunityToolkit.WinUI.Controls.SettingsControls` gives Phase 3 `SettingsCard` / `SettingsExpander` under `xmlns:tkcontrols="using:CommunityToolkit.WinUI.Controls"`. It restores and builds on all five heads; D7's Android + iOS *runtime* smoke test is still a gate.

### Deleted — do not reference

```
IconFont, HomeGlyph, BlogGlyph, ContactsGlyph, AdminGlyph   (XAML keys)
CodeFont                                                    (XAML key → use MzFontFamilyMono)
MZikmund.Extensions.Xaml.MzFontIcon                         (type)
MZikmund.Resources.FontResources                            (type)
```

`MZikmund.Resources.ResourceAccessor.GetResource<T>(string key)` and `MZikmund.Resources.ColorResoruces` survive untouched.

### Build facts later phases may rely on

- `Uno.Sdk` = `6.8.0-dev.7` (`global.json`), resolved from nuget.org — published 2026-08-09.
- `Uno.Wasm.Bootstrap` resolves to `10.1.0-dev.214` via the SDK's own `net10.0` version override; there is no repo-level pin any more.
- Uno 6.8 exposes a root-level `WebView2` namespace. Any new code-behind that news up a WebView **must** write `Microsoft.UI.Xaml.Controls.WebView2` in full.
- `Navigation` is gone from `<UnoFeatures>` in both app projects (D5). `Uno.Extensions.Navigation.WinUI` is not restored; `MZikmund.Services.Navigation.INavigationService` remains the only navigation abstraction.
- `NoWarn` no longer contains `NU1507`. A future second package source will make that warning real again.
- Anything dropped into `src/app/MZikmund.App/Assets/**` is auto-included as `Content` and lands at `bin/<config>/<tfm>/Assets/**` on every head. No csproj entry is ever needed for an asset.

## Tasks

## Phase 0 — SDK bump and build hygiene

**Goal:** a green multi-TFM build on `Uno.Sdk 6.8.0-dev.7` with a reproducible restore, before a single line of design work. This phase is isolated and independently revertable (see the rollback note in Task 2).

### Findings from reading the actual tree that change the spec's plan

These were verified empirically against this worktree, not assumed. Read them before starting; three of them contradict §11.

1. **The SDK bump does not compile as-is.** Uno 6.8 introduces a root-level `WebView2` namespace which shadows the simple type name. `PostView.xaml.cs` and `PostEditorView.xaml.cs` both fail until `Microsoft.UI.Xaml.Controls.WebView2` is written in full. §11 does not mention this; Task 2 covers it.
2. **`UnoWasmBootstrapVersion 10.1.0-dev.54` is already inert.** A forced restore resolves `Uno.Wasm.Bootstrap/10.1.0-dev.214` *with the pin still in place*, on both 6.7.0-dev.48 and 6.8.0-dev.7 — the SDK's per-TFM `versionOverride` (`"net10.0": "10.1.0-dev.214"`) beats the property. Deleting it is pure cleanup, not a version bump.
3. **The `UnoGenerateAssetsManifestDependsOn` workaround is redundant.** `Uno.Wasm.Bootstrap.targets` itself appends `GenerateUnoWasmAssets;` to that property (line 335 in 10.1.0-dev.214, line 266 in 10.1.0-dev.54), and `GenerateUnoWasmAssets` additionally declares `BeforeTargets="ResolveStaticWebAssetsInputs;…"`. The repo copy only duplicates an entry.
4. **The six "unreferenced" `PackageVersion` entries are not literally unreferenced** — `Uno.Sdk` adds implicit `PackageReference`s for `Microsoft.Windows.Compatibility`, `Microsoft.Extensions.Logging.Console`, `SkiaSharp.Skottie`, `SkiaSharp.Views.Uno.WinUI`, `Xamarin.Google.Android.Material` and `Xamarin.AndroidX.Annotation`. But the SDK supplies its own versions and **ignores the central ones** — resolved: `10.0.10`, `10.0.10`, `3.119.2`, `3.119.2`, `1.12.0.5`, `1.9.1.5` vs. pinned `10.0.3`, `10.0.3`, `3.119.2`, `3.119.2`, `1.13.0.1`, `1.9.1.6`. The conclusion holds — the entries are dead weight — but the reason is "overridden", not "unreferenced". Same mechanism protects the Uno app from the WinAppSDK 1.8 pin: the app resolves `Microsoft.WindowsAppSDK >= 1.7.250909003`.
5. **The WASM head does not build on this machine yet** and that is *not* the SDK bump's fault. `dotnet build -f net10.0-browserwasm` fails `error UNOWA0001` on 6.7.0-dev.48 and 6.8.0-dev.7 alike: the `wasm-tools` manifest is installed but the .NET 10 Emscripten packs are missing (`C:\Program Files\dotnet\packs` has only Emscripten 3.1.12 and 3.1.56). Task 1 fixes it.
6. **Baseline is otherwise green.** `net10.0-desktop`, `net10.0-windows10.0.26100`, `net10.0-android` and `net10.0-ios` all build with `0 Error(s)` on 6.8.0-dev.7.
7. **Nothing consumes `Uno.Extensions.Navigation`.** A repo-wide search for `Uno.Extensions.Navigation`, `uen:`, `Region.Attached`, `IRouteNotifier`, `RouteMap`, `ViewMap` returns zero hits. The single `using Uno.Extensions;` in `App.xaml.cs` is Hosting (`CreateBuilder`, `Environments`), not Navigation.
8. **`Navigation` is in `<UnoFeatures>` twice** — `MZikmund.App.csproj:35` *and* `MZikmund.App.Core.csproj:29`. §11.7 only implies one.

> **Note on the working tree.** Some of these edits may already be present as uncommitted changes when you start. Every code step below is written as *final desired content*, so applying it twice is a no-op. If a block already matches, verify and move to the next step — do not skip the verification.

---

### Task 1: Pin restore to nuget.org and repair the local WebAssembly toolchain

**Files:**
- Create: `NuGet.config`

**Interfaces:**
- Consumes: nothing.
- Produces: a single-source restore (`nuget.org` only), which is what makes the `NU1507` suppression removable in Task 3.

- [ ] **Step 1: Install the .NET 10 WebAssembly build tools.** Run from an **elevated** PowerShell (it writes to `C:\Program Files\dotnet`).
```powershell
dotnet workload install wasm-tools
```
Expected: `Successfully installed workload(s) wasm-tools.` (or `Workload(s) 'wasm-tools' are already installed` **plus** freshly downloaded `Microsoft.NET.Runtime.Emscripten.*` packs). Verify the packs landed:
```powershell
Get-ChildItem "C:\Program Files\dotnet\packs" -Filter "Microsoft.NET.Runtime.Emscripten.*" | Select-Object -ExpandProperty Name
```
Expected: at least one Emscripten pack newer than `3.1.56`.

- [ ] **Step 2: Create `NuGet.config` at the repo root** with exactly this content:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <!--
    Restore is pinned to nuget.org only so a local build resolves exactly what CI resolves.
    Without this file NuGet falls back to the machine-level configuration and silently mixes
    in private feeds: Uno.Sdk 6.7.7 looks like a stable release locally because it resolves
    from the private "Uno Internal" Azure DevOps feed, and the same restore fails on a clean
    CI agent. There is no public 6.7.x stable.
  -->
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <!-- A machine-level <disabledPackageSources> entry must not be able to disable nuget.org. -->
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
</configuration>
```

- [ ] **Step 3: Run a forced restore of the whole solution against the pinned source.**
```powershell
dotnet restore MZikmund.slnx --force
```
Expected: every project reports `Restored …`, and **no** `NU1101` (package not found) and **no** `NU1301` (source unreachable). If `NU1101` names a package, that package was coming from a private feed and must be dealt with before continuing — do not re-add the feed silently.

- [ ] **Step 4: Confirm nuget.org is the only source in effect.**
```powershell
dotnet nuget list source
```
Expected: exactly one enabled source, `nuget.org [Enabled]` → `https://api.nuget.org/v3/index.json`.

- [ ] **Step 5: Confirm the WASM head now builds.**
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-browserwasm
```
Expected: `Build succeeded.` with `0 Error(s)`. Specifically, `error UNOWA0001: Native WebAssembly assets were detected, but the wasm-tools workload could not be located.` must be gone.

- [ ] **Step 6: Commit**
```bash
git add NuGet.config
git commit -m "build: Pin restore to nuget.org with a repo-root NuGet.config"
```

---

### Task 2: Bump Uno.Sdk to 6.8.0-dev.7 and fix the WebView2 name collision it introduces

**Files:**
- Modify: `global.json:7`
- Modify: `src/app/MZikmund.App/Views/PostView.xaml.cs:9-16`
- Modify: `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs:9,18,56`

**Interfaces:**
- Consumes: the pinned nuget.org source from Task 1 (`Uno.Sdk 6.8.0-dev.7` was published to nuget.org on 2026-08-09).
- Produces: `Uno.Sdk 6.8.0-dev.7` for every later phase; the rule that WebView types must be written fully qualified.

> **Rollback note.** This task is the one destabilising change in the phase and is revertable on its own:
> ```bash
> git revert <this task's commit>      # restores Uno.Sdk 6.7.0-dev.48 and the simple-name WebView2
> dotnet restore MZikmund.slnx --force
> ```
> If `6.8.0-dev.7` is green everywhere except one head, the sanctioned intermediate fallback is `6.7.0-dev.165` (set it in `global.json` instead of reverting; the `WebView2` qualification is harmless on 6.7 and can stay). **Never `6.7.7`** — it exists only on the private Uno Internal feed and will fail CI, which is exactly what Task 1's `NuGet.config` now prevents you from accidentally resolving.

- [ ] **Step 1: Set the SDK version.** Replace the whole of `global.json` with:
```json
{
  "sdk": {
    "version": "10.0.100",
    "allowPrerelease": false
  },
  "msbuild-sdks": {
    "Uno.Sdk": "6.8.0-dev.7"
  }
}
```

- [ ] **Step 2: Restore against the new SDK.**
```powershell
dotnet restore MZikmund.slnx --force
```
Expected: all projects `Restored …`, no `NU1101`. Confirm the SDK actually moved:
```powershell
Select-String -Path src/app/MZikmund.App/obj/project.assets.json -Pattern '"Uno.UI.Adapter.Microsoft.Extensions.Logging >= 6\.8' | Select-Object -First 1
```
Expected: one match.

- [ ] **Step 3: Fully qualify `WebView2` in `PostView.xaml.cs`.** Replace the field declaration and constructor in `src/app/MZikmund.App/Views/PostView.xaml.cs` with:
```csharp
public sealed partial class PostView : PostViewBase
{
	// Fully qualified: Uno 6.8 adds a root-level WebView2 namespace that shadows the simple name.
	private readonly Microsoft.UI.Xaml.Controls.WebView2 _previewWebView;

	public PostView()
	{
		InitializeComponent();
		PreviewWebViewContainer.Content = _previewWebView = new Microsoft.UI.Xaml.Controls.WebView2();
		Loaded += PostView_Loaded;
		Unloaded += PostView_Unloaded;
	}
```

- [ ] **Step 4: Fully qualify `WebView2` in `PostEditorView.xaml.cs` — field and constructor.** Replace the field declaration and constructor in `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs` with:
```csharp
public sealed partial class PostEditorView : PostEditorViewBase
{
	// Fully qualified: Uno 6.8 adds a root-level WebView2 namespace that shadows the simple name.
	private readonly Microsoft.UI.Xaml.Controls.WebView2 _previewWebView;

	private string _postPreviewTemplate;

	public PostEditorView()
	{
		InitializeComponent();
		// Read the template from the embedded resource
		_postPreviewTemplate = typeof(PostEditorView).Assembly.GetManifestResourceStream("MZikmund.App.Templates.PostPreviewTemplate.html")!.ReadToEnd()!;
		PreviewWebViewContainer.Content = _previewWebView = new Microsoft.UI.Xaml.Controls.WebView2();
		this.Loaded += PostEditorView_Loaded;
		this.Unloaded += PostEditorView_Unloaded;
	}
```

- [ ] **Step 5: Fully qualify the third site — the `OnWebViewInitialized` signature** in the same file. Replace:
```csharp
	private void OnWebViewInitialized(WebView2 sender, CoreWebView2InitializedEventArgs args) => UpdatePreview();
```
with:
```csharp
	private void OnWebViewInitialized(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2InitializedEventArgs args) => UpdatePreview();
```

- [ ] **Step 6: Build all five TFMs.** Run each and confirm `Build succeeded.` / `0 Error(s)`:
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-browserwasm
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
```
Expected: five × `0 Error(s)`. Warnings are expected and unchanged (`CS8618` in `Platforms/Windows/Program.cs`, `NETSDK1198` about `win-AnyCPU.pubxml`, `APT2000` resource warnings on Android, `Uno0001` for `ItemsWrapGrid` in `MediaBrowserDialog`, `IL2026` trimming warnings). If you see `CS0118: 'WebView2' is a namespace but is used like a type`, a WebView site was missed.

- [ ] **Step 7: Commit**
```bash
git add global.json src/app/MZikmund.App/Views/PostView.xaml.cs src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs
git commit -m "build: Move the app to Uno.Sdk 6.8.0-dev.7"
```

---

### Task 3: Drop the NU1507 suppression

**Files:**
- Modify: `src/Directory.Build.props:17-24`

**Interfaces:**
- Consumes: the single-source `NuGet.config` from Task 1 (NU1507 only fires with CPM + more than one source and no source mapping).
- Produces: `NoWarn` without `NU1507`, so a future second feed surfaces the warning instead of hiding it.

- [ ] **Step 1: Replace the `NoWarn` comment block and property** in `src/Directory.Build.props`. Replace:
```xml
		<!--
			Adding NoWarn to remove build warnings
			NU1507: Warning when there are multiple package sources when using CPM with no source mapping
			NETSDK1201: Warning that specifying RID won't create self containing app
			PRI257: Ignore default language (en) not being one of the included resources (eg en-us, en-uk)
			CA1873: .NET 10 analyzer rule for structured logging - existing code pattern
		-->
		<NoWarn>$(NoWarn);NU1507;NETSDK1201;PRI257;XAOBS001;CA1873</NoWarn>
```
with:
```xml
		<!--
			Adding NoWarn to remove build warnings.
			NU1507 is deliberately absent: the repo-root NuGet.config pins restore to a single
			source, so the warning cannot fire - and if a second feed is ever added it should
			be seen, not suppressed.
			NETSDK1201: Warning that specifying RID won't create self containing app
			PRI257: Ignore default language (en) not being one of the included resources (eg en-us, en-uk)
			XAOBS001: .NET for Android obsolete-API diagnostic
			CA1873: .NET 10 analyzer rule for structured logging - existing code pattern
		-->
		<NoWarn>$(NoWarn);NETSDK1201;PRI257;XAOBS001;CA1873</NoWarn>
```

- [ ] **Step 2: Prove NU1507 does not come back.**
```powershell
dotnet restore MZikmund.slnx --force 2>&1 | Select-String -Pattern "NU1507"
```
Expected: **no output at all**. Any hit means a second package source is still in effect — recheck `dotnet nuget list source`.

- [ ] **Step 3: Confirm the desktop head still builds.**
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```
Expected: `Build succeeded.` `0 Error(s)`.

- [ ] **Step 4: Commit**
```bash
git add src/Directory.Build.props
git commit -m "build: Drop the NU1507 suppression now that restore has one source"
```

---

### Task 4: Delete the inert UnoWasmBootstrapVersion pin and the assets-manifest workaround

**Files:**
- Modify: `src/Directory.Build.props:30-36`

**Interfaces:**
- Consumes: `Uno.Sdk 6.8.0-dev.7` from Task 2.
- Produces: `Uno.Wasm.Bootstrap` resolved solely by the SDK (`10.1.0-dev.214`), with no repo-level MSBuild overrides on the WASM asset pipeline.

- [ ] **Step 1: Record the *before* state — this proves the pin is already inert.**
```powershell
Select-String -Path src/app/MZikmund.App/obj/project.assets.json -Pattern '"Uno\.Wasm\.Bootstrap/[^"]*"' -AllMatches |
  ForEach-Object { $_.Matches.Value } | Sort-Object -Unique
```
Expected: `"Uno.Wasm.Bootstrap/10.1.0-dev.214"` — **not** `10.1.0-dev.54`, even though the pin is still in the file. Write the value down; Step 4 compares against it.

- [ ] **Step 2: Delete both blocks** from `src/Directory.Build.props`. Remove exactly:
```xml
		<UnoWasmBootstrapVersion>10.1.0-dev.54</UnoWasmBootstrapVersion>

		<!-- Temporary workaround for Uno.Wasm.Bootstrap -->
		<UnoGenerateAssetsManifestDependsOn>
			$(UnoGenerateAssetsManifestDependsOn);
			GenerateUnoWasmAssets;
		</UnoGenerateAssetsManifestDependsOn>
```
so the `PropertyGroup` ends:
```xml
		<!-- Temporary workaround for https://github.com/unoplatform/uno/issues/18464 -->
		<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
	</PropertyGroup>
```

- [ ] **Step 3: Re-restore.**
```powershell
dotnet restore MZikmund.slnx --force
```
Expected: `Restored …` for every project, no errors.

- [ ] **Step 4: Confirm the resolved bootstrap version is unchanged.**
```powershell
Select-String -Path src/app/MZikmund.App/obj/project.assets.json -Pattern '"Uno\.Wasm\.Bootstrap/[^"]*"' -AllMatches |
  ForEach-Object { $_.Matches.Value } | Sort-Object -Unique
```
Expected: `"Uno.Wasm.Bootstrap/10.1.0-dev.214"` — identical to Step 1. Deleting the pin changed nothing, which is the point.

- [ ] **Step 5: Build WASM and prove asset staging still works.**
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-browserwasm
Get-ChildItem -Path src/app/MZikmund.App/bin/Debug/net10.0-browserwasm/wwwroot -Recurse -Filter "Blog.scale-100.png" |
  Select-Object -First 1 -ExpandProperty FullName
```
Expected: `Build succeeded.` `0 Error(s)`, and one path printed under `wwwroot`. That PNG only reaches `wwwroot` through the `UnoGenerateAssetsManifest` → `DefineStaticWebAssets` chain, so its presence is the functional proof the workaround was redundant.
**If nothing is printed**, restore only the workaround (keep the version pin deleted) by putting this back into the `PropertyGroup`, then re-run this step and leave it in place with a comment naming the Uno.Wasm.Bootstrap version that still needs it:
```xml
		<!-- Temporary workaround for Uno.Wasm.Bootstrap -->
		<UnoGenerateAssetsManifestDependsOn>
			$(UnoGenerateAssetsManifestDependsOn);
			GenerateUnoWasmAssets;
		</UnoGenerateAssetsManifestDependsOn>
```

- [ ] **Step 6: Commit**
```bash
git add src/Directory.Build.props
git commit -m "build: Remove the inert Uno.Wasm.Bootstrap pin and its manifest workaround"
```

---

### Task 5: Delete the six inert PackageVersion entries

**Files:**
- Modify: `src/Directory.Packages.props:39-40,70-71,76-78`

**Interfaces:**
- Consumes: nothing.
- Produces: a `Directory.Packages.props` where every `PackageVersion` corresponds to a real, honoured `PackageReference`.

- [ ] **Step 1: Prove no project declares a `PackageReference` for any of the six.**
```powershell
Select-String -Path (Get-ChildItem -Recurse -Include *.csproj -Path src) `
  -Pattern 'PackageReference Include="(Xamarin\.Google\.Android\.Material|Microsoft\.Extensions\.Logging\.Console|Microsoft\.Windows\.Compatibility|SkiaSharp\.Skottie|SkiaSharp\.Views\.Uno\.WinUI|Xamarin\.AndroidX\.Annotation)"'
```
Expected: **no output.** (Verified while writing this plan — the only hits repo-wide are the `PackageVersion` lines themselves and the spec text.)

- [ ] **Step 2: Prove Uno.Sdk overrides the pinned versions anyway**, i.e. deleting them cannot change resolution.
```powershell
foreach ($p in "Microsoft.Windows.Compatibility","Microsoft.Extensions.Logging.Console","SkiaSharp.Skottie","SkiaSharp.Views.Uno.WinUI","Xamarin.Google.Android.Material","Xamarin.AndroidX.Annotation") {
  $v = Select-String -Path src/app/MZikmund.App/obj/project.assets.json -Pattern "`"$p/[^`"]*`"" -AllMatches |
       ForEach-Object { $_.Matches.Value } | Sort-Object -Unique
  "{0,-40} {1}" -f $p, ($v -join ' ')
}
```
Expected (SDK-chosen versions, four of six differ from the central pins):
```
Microsoft.Windows.Compatibility          "Microsoft.Windows.Compatibility/10.0.10"
Microsoft.Extensions.Logging.Console     "Microsoft.Extensions.Logging.Console/10.0.10"
SkiaSharp.Skottie                        "SkiaSharp.Skottie/3.119.2"
SkiaSharp.Views.Uno.WinUI                "SkiaSharp.Views.Uno.WinUI/3.119.2"
Xamarin.Google.Android.Material          "Xamarin.Google.Android.Material/1.12.0.5"
Xamarin.AndroidX.Annotation              "Xamarin.AndroidX.Annotation/1.9.1.5"
```

- [ ] **Step 3: Delete the Android annotation entry and its stale comment.** Remove exactly:
```xml
    <!-- Required to avoid warnings in 1.9.0.1 of Android.Material - https://github.com/xamarin/AndroidX/issues/727 -->
    <PackageVersion Include="Xamarin.AndroidX.Annotation" Version="1.9.1.6" />
```

- [ ] **Step 4: Delete the remaining five entries.** Remove exactly these five lines from the `<!-- Uno -->` block:
```xml
    <PackageVersion Include="Microsoft.Extensions.Logging.Console" Version="10.0.3" />
    <PackageVersion Include="Microsoft.Windows.Compatibility" Version="10.0.3" />
    <PackageVersion Include="SkiaSharp.Skottie" Version="3.119.2" />
    <PackageVersion Include="SkiaSharp.Views.Uno.WinUI" Version="3.119.2" />
    <PackageVersion Include="Xamarin.Google.Android.Material" Version="1.13.0.1" />
```

- [ ] **Step 5: Restore and re-check the same six versions.**
```powershell
dotnet restore MZikmund.slnx --force
```
Expected: `Restored …` for every project, **no `NU1008`/`NU1010`** (CPM version-missing errors), and re-running Step 2's snippet prints the identical six versions.

- [ ] **Step 6: Build the two heads that pull those packages** (Android for Material/Annotation, Windows for Compatibility).
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```
Expected: both `Build succeeded.` `0 Error(s)`.

- [ ] **Step 7: Commit**
```bash
git add src/Directory.Packages.props
git commit -m "build: Delete six PackageVersion entries Uno.Sdk already overrides"
```

---

### Task 6: Re-group and re-comment the LegacyMigration-only pins

**Files:**
- Modify: `src/Directory.Packages.props:44,69-75`

**Interfaces:**
- Consumes: nothing.
- Produces: an unambiguous split between "the WinUI 3 migration tool needs this" and "the Uno app needs this". No version changes.

- [ ] **Step 1: Remove `CommunityToolkit.Mvvm` from the web block.** Delete this line from its current position (between `Microsoft.NET.Test.Sdk` and `xunit`):
```xml
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
```

- [ ] **Step 2: Replace the misleading `<!-- Uno -->` block** with two correctly labelled groups. Replace:
```xml
    <!-- Uno -->
    <PackageVersion Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.7705" />
    <PackageVersion Include="Microsoft.WindowsAppSDK" Version="1.8.260209005" />
    <PackageVersion Include="System.Security.Cryptography.Xml" Version="10.0.8" />
    <PackageVersion Include="Tmds.DBus.Protocol" Version="0.93.0" />
```
with:
```xml
    <!--
      WinUI 3 legacy migration tool (src/tools/MZikmund.LegacyMigration) ONLY.
      These do not reach the Uno app: Uno.Sdk supplies its own implicit versions and
      ignores the central ones - the app resolves Microsoft.WindowsAppSDK 1.7.250909003
      and CommunityToolkit.Mvvm 8.4.2 regardless of what is pinned here. Do not "align"
      the app onto WinAppSDK 1.8: Uno 6.7 and 6.8 are built against 1.7.250909003, and
      1.8 split WinUI out into Microsoft.WindowsAppSDK.WinUI.
    -->
    <PackageVersion Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.7705" />
    <PackageVersion Include="Microsoft.WindowsAppSDK" Version="1.8.260209005" />
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />

    <!-- Uno app (src/app/MZikmund.App) -->
    <PackageVersion Include="System.Security.Cryptography.Xml" Version="10.0.8" />
    <PackageVersion Include="Tmds.DBus.Protocol" Version="0.93.0" />
```

- [ ] **Step 3: Restore and build the migration tool** — it is the only consumer of the three re-grouped pins.
```powershell
dotnet restore MZikmund.slnx --force
dotnet build src/tools/MZikmund.LegacyMigration/MZikmund.LegacyMigration.csproj
```
Expected: both `Build succeeded.` / `Restored …` with `0 Error(s)`, and no `NU1008` (a missing central version would surface here).

- [ ] **Step 4: Commit**
```bash
git add src/Directory.Packages.props
git commit -m "build: Label the WinAppSDK and Mvvm pins as migration-tool-only"
```

---

### Task 7: Add CommunityToolkit.WinUI.Media (Windows head only) and SettingsControls

**Files:**
- Modify: `src/Directory.Packages.props` (Uno app group, after `Tmds.DBus.Protocol`)
- Modify: `src/app/MZikmund.App/MZikmund.App.csproj:54-58`

**Interfaces:**
- Consumes: the Uno-app package group created in Task 6.
- Produces: `CommunityToolkit.WinUI.Media 8.2.251219` on the Windows head only (for `MzElevation`'s `#if WINDOWS` branch, §5.6/D6) and `CommunityToolkit.WinUI.Controls.SettingsControls 8.2.251219` on all five heads (for `SettingsCard`/`SettingsExpander`, D7). Both versions confirmed present on nuget.org and matching the `8.2.251219` already used for `Converters`.

- [ ] **Step 1: Add the two central versions.** In `src/Directory.Packages.props`, extend the `<!-- Uno app (src/app/MZikmund.App) -->` group to:
```xml
    <!-- Uno app (src/app/MZikmund.App) -->
    <PackageVersion Include="System.Security.Cryptography.Xml" Version="10.0.8" />
    <PackageVersion Include="Tmds.DBus.Protocol" Version="0.93.0" />
    <PackageVersion Include="CommunityToolkit.WinUI.Controls.SettingsControls" Version="8.2.251219" />
    <!-- Windows head only - see the conditioned PackageReference in MZikmund.App.csproj -->
    <PackageVersion Include="CommunityToolkit.WinUI.Media" Version="8.2.251219" />
```
Central Package Management note: the `PackageVersion` items stay **unconditional**. `Directory.Packages.props` is evaluated once per inner build, but conditioning a `PackageVersion` on `$(TargetFramework)` is fragile during the outer build — condition the `PackageReference` instead, as the next step does.

- [ ] **Step 2: Reference the packages from the app.** In `src/app/MZikmund.App/MZikmund.App.csproj`, replace:
```xml
	<ItemGroup>
	  <PackageReference Include="CommunityToolkit.WinUI.Converters" />
	  <PackageReference Include="System.Security.Cryptography.Xml" />
	  <PackageReference Include="Tmds.DBus.Protocol" />
	</ItemGroup>
```
with:
```xml
	<ItemGroup>
		<PackageReference Include="CommunityToolkit.WinUI.Converters" />
		<PackageReference Include="CommunityToolkit.WinUI.Controls.SettingsControls" />
		<PackageReference Include="System.Security.Cryptography.Xml" />
		<PackageReference Include="Tmds.DBus.Protocol" />
	</ItemGroup>

	<!--
		Elevation shadows are Windows-only. CommunityToolkit.WinUI.Media is bound to
		WindowsAppSDK + Win2D and fails to restore (NU1202) on the Uno TFMs, and Uno's
		compositor has no CreateDropShadow. The other heads fall back to the paired 1px
		stroke, applied through the MzElevation attached property.
	-->
	<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
		<PackageReference Include="CommunityToolkit.WinUI.Media" />
	</ItemGroup>
```

- [ ] **Step 3: Restore and prove the Windows-only condition actually held.**
```powershell
dotnet restore MZikmund.slnx --force
python -c "import json;d=json.load(open(r'src/app/MZikmund.App/obj/MZikmund.App.csproj.nuget.dgspec.json'));[print(t,[k for k in f.get('dependencies',{}) if 'CommunityToolkit.WinUI.Media' in k]) for n,p in d['projects'].items() if n.endswith('MZikmund.App.csproj') for t,f in p['frameworks'].items()]"
```
Expected: `['CommunityToolkit.WinUI.Media']` on `net10.0-windows10.0.26100` and an empty list `[]` on `net10.0-android36.0`, `net10.0-browserwasm1.0`, `net10.0-desktop1.0`, `net10.0-ios26.5`.

- [ ] **Step 4: Build the two heads that would expose a mistake** — desktop must not see `NU1202`, Windows must resolve Media.
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```
Expected: both `Build succeeded.` `0 Error(s)`. Any `NU1202: Package CommunityToolkit.WinUI.Media … is not compatible with net10.0-desktop` means the condition is wrong.

- [ ] **Step 5: Build the remaining three heads** — `SettingsControls` ships `net9.0-android35.0` / `net9.0-ios18.0` assets and must at least restore and compile everywhere.
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-browserwasm
```
Expected: three × `0 Error(s)`. (D7's runtime smoke test on a physical Android and iOS device is a Phase 3 gate, not this one — the packages compile clean and can still fail at runtime.)

- [ ] **Step 6: Commit**
```bash
git add src/Directory.Packages.props src/app/MZikmund.App/MZikmund.App.csproj
git commit -m "build: Add CommunityToolkit SettingsControls and Windows-only Media"
```

---

### Task 8: Remove Navigation from UnoFeatures in both app projects

**Files:**
- Modify: `src/app/MZikmund.App/MZikmund.App.csproj:27-40`
- Modify: `src/app/MZikmund.App.Core/MZikmund.App.Core.csproj:22-33`

**Interfaces:**
- Consumes: nothing.
- Produces: no `Uno.Extensions.Navigation.WinUI` in the graph. `MZikmund.Services.Navigation.INavigationService` stays the sole navigation abstraction (D5 — navigation is fixed in place in Phase 2).

- [ ] **Step 1: Prove nothing consumes Uno.Extensions.Navigation.** Run **before** editing:
```powershell
Select-String -Path (Get-ChildItem -Recurse -Include *.cs,*.xaml -Path src/app) `
  -Pattern 'Uno\.Extensions\.Navigation|uen:|Region\.Attached|IRouteNotifier|RouteMap|ViewMap'
```
Expected: **no output.** The only `Uno.Extensions` usage in the app is `using Uno.Extensions;` in `App.xaml.cs`, which is the Hosting extension (`CreateBuilder`, `Environments.Development`) — leave it alone. In the SDK, the `navigation` feature adds exactly one package here (`Uno.Extensions.Navigation.WinUI`; the Markup and Toolkit variants are gated on `csharpmarkup`/`toolkit`, neither of which is enabled).

- [ ] **Step 2: Drop the feature from the head project.** In `src/app/MZikmund.App/MZikmund.App.csproj` replace:
```xml
		<UnoFeatures>
			Hosting;
			HttpRefit;
			Configuration;
			Serialization;
			Mvvm;
			Lottie;
			Logging;
			Navigation;
			Localization;
			ThemeService;
			SkiaRenderer;
			WebView;
		</UnoFeatures>
```
with:
```xml
		<UnoFeatures>
			Hosting;
			HttpRefit;
			Configuration;
			Serialization;
			Mvvm;
			Lottie;
			Logging;
			Localization;
			ThemeService;
			SkiaRenderer;
			WebView;
		</UnoFeatures>
```

- [ ] **Step 3: Drop the feature from the core library too.** In `src/app/MZikmund.App.Core/MZikmund.App.Core.csproj` replace:
```xml
		<UnoFeatures>
			Hosting;
			HttpRefit;
			Configuration;
			Serialization;
			Mvvm;
			Lottie;
			Navigation;
			Localization;
			ThemeService;
			SkiaRenderer;
		</UnoFeatures>
```
with:
```xml
		<UnoFeatures>
			Hosting;
			HttpRefit;
			Configuration;
			Serialization;
			Mvvm;
			Lottie;
			Localization;
			ThemeService;
			SkiaRenderer;
		</UnoFeatures>
```

- [ ] **Step 4: Restore and prove the package is gone from both graphs.**
```powershell
dotnet restore MZikmund.slnx --force
Select-String -Path src/app/MZikmund.App/obj/project.assets.json,src/app/MZikmund.App.Core/obj/project.assets.json `
  -Pattern 'Uno\.Extensions\.Navigation' | Measure-Object | Select-Object -ExpandProperty Count
```
Expected: `0`.

- [ ] **Step 5: Build all five TFMs.**
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-browserwasm
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
```
Expected: five × `Build succeeded.` `0 Error(s)`.

- [ ] **Step 6: Commit**
```bash
git add src/app/MZikmund.App/MZikmund.App.csproj src/app/MZikmund.App.Core/MZikmund.App.Core.csproj
git commit -m "build: Remove the unused Navigation UnoFeature from both app projects"
```

---

### Task 9: Ship Assets/Fonts and resolve the two dangling ms-appx font URIs

**Files:**
- Create: `scripts/Get-Fonts.ps1`
- Create: `src/app/MZikmund.App/Assets/Fonts/Inter-Regular.ttf`
- Create: `src/app/MZikmund.App/Assets/Fonts/Inter-Medium.ttf`
- Create: `src/app/MZikmund.App/Assets/Fonts/Inter-SemiBold.ttf`
- Create: `src/app/MZikmund.App/Assets/Fonts/JetBrainsMono-Regular.ttf`
- Create: `docs/licenses/Inter-OFL.txt`
- Create: `docs/licenses/JetBrainsMono-OFL.txt`
- Modify: `.gitattributes:1-2`
- Modify: `src/app/MZikmund.App/Resources/Fonts.xaml:1-8`
- Modify: `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml:60,93`
- Delete: `src/app/MZikmund.App/Extensions/Xaml/MzFontIcon.cs`
- Delete: `src/app/MZikmund.App/Resources/FontResources.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MzFontFamilyRegular`, `MzFontFamilyMedium`, `MzFontFamilySemiBold`, `MzFontFamilyMono`. Removes `IconFont`, `CodeFont`, `HomeGlyph`, `BlogGlyph`, `ContactsGlyph`, `AdminGlyph`, `MzFontIcon`, `FontResources`.

> Both `ms-appx:` URIs in `Resources/Fonts.xaml` point at files that are not in the repo, so the markdown editor is silently proportional today and `MzFontIcon` would render tofu if anything used it. `CodeFont` is *fixed* (JetBrains Mono ships here); `IconFont` is *deleted* — the subsetted `FluentSystemIcons-Resizable.ttf` needs the glyph list, which is not known until the views exist (§9). Ship `.ttf`, never `.woff2`: SkiaSharp cannot parse woff2 and it would break Desktop, Android and iOS.

- [ ] **Step 1: Create `scripts/Get-Fonts.ps1`** with exactly this content:
```powershell
#Requires -Version 7.0
<#
	.SYNOPSIS
		Downloads the app's bundled fonts into src/app/MZikmund.App/Assets/Fonts.
	.DESCRIPTION
		Versions are pinned so the checked-in binaries are reproducible. Only .ttf is
		shipped - SkiaSharp cannot parse .woff2, which would break the Desktop, Android
		and iOS heads. Licences land in docs/licenses so they are not packaged into the app.
#>
[CmdletBinding()]
param(
	[string]$InterVersion = '4.1',
	[string]$JetBrainsMonoVersion = '2.304'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$fontsDir = Join-Path $repoRoot 'src/app/MZikmund.App/Assets/Fonts'
$licenseDir = Join-Path $repoRoot 'docs/licenses'
$temp = Join-Path ([System.IO.Path]::GetTempPath()) "mz-fonts-$([System.Guid]::NewGuid().ToString('N'))"

New-Item -ItemType Directory -Force -Path $fontsDir, $licenseDir, $temp | Out-Null

function Expand-FromZip {
	param([string]$Zip, [string]$Entry, [string]$Destination)

	Add-Type -AssemblyName System.IO.Compression.FileSystem
	$archive = [System.IO.Compression.ZipFile]::OpenRead($Zip)
	try {
		$item = $archive.Entries | Where-Object { $_.FullName -eq $Entry }
		if ($null -eq $item) { throw "Entry '$Entry' not found in $Zip" }
		[System.IO.Compression.ZipFileExtensions]::ExtractToFile($item, $Destination, $true)
	}
	finally {
		$archive.Dispose()
	}
}

$interZip = Join-Path $temp "Inter-$InterVersion.zip"
Invoke-WebRequest -Uri "https://github.com/rsms/inter/releases/download/v$InterVersion/Inter-$InterVersion.zip" -OutFile $interZip
Expand-FromZip $interZip 'extras/ttf/Inter-Regular.ttf' (Join-Path $fontsDir 'Inter-Regular.ttf')
Expand-FromZip $interZip 'extras/ttf/Inter-Medium.ttf' (Join-Path $fontsDir 'Inter-Medium.ttf')
Expand-FromZip $interZip 'extras/ttf/Inter-SemiBold.ttf' (Join-Path $fontsDir 'Inter-SemiBold.ttf')
Expand-FromZip $interZip 'LICENSE.txt' (Join-Path $licenseDir 'Inter-OFL.txt')

$monoZip = Join-Path $temp "JetBrainsMono-$JetBrainsMonoVersion.zip"
Invoke-WebRequest -Uri "https://github.com/JetBrains/JetBrainsMono/releases/download/v$JetBrainsMonoVersion/JetBrainsMono-$JetBrainsMonoVersion.zip" -OutFile $monoZip
Expand-FromZip $monoZip 'fonts/ttf/JetBrainsMono-Regular.ttf' (Join-Path $fontsDir 'JetBrainsMono-Regular.ttf')
Expand-FromZip $monoZip 'OFL.txt' (Join-Path $licenseDir 'JetBrainsMono-OFL.txt')

Remove-Item -Recurse -Force $temp

Get-ChildItem $fontsDir -Filter *.ttf | Select-Object Name, Length
```

- [ ] **Step 2: Run it.**
```powershell
pwsh -File scripts/Get-Fonts.ps1
```
Expected output (byte counts are exact for Inter 4.1 / JetBrains Mono 2.304):
```
Name                      Length
----                      ------
Inter-Medium.ttf          417300
Inter-Regular.ttf         411640
Inter-SemiBold.ttf        419744
JetBrainsMono-Regular.ttf 273900
```

- [ ] **Step 3: Mark `.ttf` as binary** so git never rewrites line endings inside a font. Replace the whole of `.gitattributes` with:
```
#*.cs text
#*.xaml text

# Fonts must never be line-ending normalized.
*.ttf binary
```

- [ ] **Step 4: Rewrite `src/app/MZikmund.App/Resources/Fonts.xaml`** with exactly this content:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
	<!--
		Inter and JetBrains Mono ship as app assets (Assets/Fonts) and are regenerated by
		scripts/Get-Fonts.ps1. Each static .ttf carries exactly one weight, so every weight
		needs its own FontFamily resource - the family name after '#' is resolved inside that
		one file. Setting FontWeight against the wrong resource synthesizes instead of
		selecting. The ramp stops at SemiBold (600) by design: never Bold.
	-->
	<FontFamily x:Key="MzFontFamilyRegular">ms-appx:///Assets/Fonts/Inter-Regular.ttf#Inter</FontFamily>
	<FontFamily x:Key="MzFontFamilyMedium">ms-appx:///Assets/Fonts/Inter-Medium.ttf#Inter Medium</FontFamily>
	<FontFamily x:Key="MzFontFamilySemiBold">ms-appx:///Assets/Fonts/Inter-SemiBold.ttf#Inter SemiBold</FontFamily>
	<FontFamily x:Key="MzFontFamilyMono">ms-appx:///Assets/Fonts/JetBrainsMono-Regular.ttf#JetBrains Mono</FontFamily>
</ResourceDictionary>
```

- [ ] **Step 5: Repoint the markdown editor's two `CodeFont` usages.** In `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml`, in the Abstract `TextBox` replace:
```xml
            FontFamily="{StaticResource CodeFont}"
            Header="{xaml:Localize Key=Abstract}"
```
with:
```xml
            FontFamily="{StaticResource MzFontFamilyMono}"
            Header="{xaml:Localize Key=Abstract}"
```
and in the `ContentTextBox` replace:
```xml
                FontFamily="{StaticResource CodeFont}"
                SelectionChanged="ContentTextBox_SelectionChanged"
```
with:
```xml
                FontFamily="{StaticResource MzFontFamilyMono}"
                SelectionChanged="ContentTextBox_SelectionChanged"
```

- [ ] **Step 6: Delete the dead icomoon pipeline.** Both files exist only to serve the removed `IconFont` key; `MzFontIcon` has zero XAML usages (the `xmlns:extensions`/`xmlns:xaml` declarations in the views resolve `Localize`, which stays).
```bash
git rm src/app/MZikmund.App/Extensions/Xaml/MzFontIcon.cs src/app/MZikmund.App/Resources/FontResources.cs
```

- [ ] **Step 7: Confirm nothing still references the deleted keys or types.**
```powershell
Select-String -Path (Get-ChildItem -Recurse -Include *.cs,*.xaml -Path src/app) `
  -Pattern 'IconFont|CodeFont|HomeGlyph|BlogGlyph|ContactsGlyph|AdminGlyph|MzFontIcon|FontResources'
```
Expected: **no output.**

- [ ] **Step 8: Build and confirm the fonts are packaged.**
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
Get-ChildItem src/app/MZikmund.App/bin/Debug/net10.0-desktop/Assets/Fonts -Filter *.ttf | Select-Object Name
```
Expected: `Build succeeded.` `0 Error(s)`, then all four `.ttf` names listed. (`Uno.DefaultItems.targets` globs `$(AssetsFolder)**` into `Content` automatically — no csproj entry is needed, and none should be added.)

- [ ] **Step 9: Confirm the Windows head packages them too** (WinAppSDK uses its own `Content Update="Assets/**/*.*"` glob).
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
Get-ChildItem src/app/MZikmund.App/bin/Debug/net10.0-windows10.0.26100/Assets/Fonts -Filter *.ttf | Select-Object Name
```
Expected: `0 Error(s)` and the same four names.

- [ ] **Step 10: Commit**
```bash
git add scripts/Get-Fonts.ps1 src/app/MZikmund.App/Assets/Fonts docs/licenses .gitattributes src/app/MZikmund.App/Resources/Fonts.xaml src/app/MZikmund.App/Views/Admin/PostEditorView.xaml
git commit -m "feat: Ship Inter and JetBrains Mono and drop the dead icomoon font pipeline"
```

---

### Task 10: Fix CLAUDE.md's stale SDK line

**Files:**
- Modify: `CLAUDE.md:127`

**Interfaces:**
- Consumes: the version set in Task 2.
- Produces: nothing consumed by code.

- [ ] **Step 1: Correct the version and document the new package-source pin.** In `CLAUDE.md`, under `### Package and Build Configuration`, replace:
```markdown
- **SDK**: .NET 10.0.100 exact (`global.json`), Uno.Sdk 6.5.0-dev.31.
```
with:
```markdown
- **SDK**: .NET 10.0.100 exact (`global.json`), Uno.Sdk 6.8.0-dev.7 (`global.json` is the source of truth — update this line when it moves).
- **Package sources**: the repo-root `NuGet.config` pins restore to nuget.org only so local restore matches CI. Do not rely on machine-level feeds: Uno.Sdk 6.7.7 exists only on the private "Uno Internal" feed and fails on a clean agent.
```

- [ ] **Step 2: Verify no other stale version string survives.**
```powershell
Select-String -Path CLAUDE.md,README.md -Pattern '6\.5\.0-dev|6\.7\.0-dev|6\.7\.7'
```
Expected: **no output.**

- [ ] **Step 3: Commit**
```bash
git add CLAUDE.md
git commit -m "docs: Correct the Uno.Sdk version and document the NuGet.config pin"
```

---

### Task 11: Phase close — format, full solution build, five-TFM build, light/dark smoke test

**Files:** none created or modified beyond whatever `dotnet format` rewrites.

**Interfaces:**
- Consumes: everything above.
- Produces: the green baseline Phase 1 builds on.

- [ ] **Step 1: Format the solution.**
```powershell
dotnet format MZikmund.slnx
```
Expected: completes without error. Inspect what it touched with `git diff --stat`; the only expected changes are whitespace in the files edited this phase.

- [ ] **Step 2: Restore the web project's frontend dependencies** so the solution build does not trip on them (this is what CI does before building).
```powershell
cd src/web/MZikmund.Web
npm install
cd ../../..
```
Expected: `added N packages` or `up to date`.

- [ ] **Step 3: Build the whole solution.**
```powershell
dotnet build MZikmund.slnx
```
Expected: `Build succeeded.` with `0 Error(s)` across all projects — app, shared, web, tests and the migration tool.

- [ ] **Step 4: Run the test suite** to prove nothing in the web stack regressed from the central-package edits.
```powershell
dotnet test MZikmund.slnx --no-build
```
Expected: all tests pass, `0 Failed`.

- [ ] **Step 5: Build all five app TFMs one final time.**
```powershell
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-browserwasm
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
```
Expected: five × `Build succeeded.` `0 Error(s)`. **This is the phase gate — do not start Phase 1 until all five are green.**

- [ ] **Step 6: Launch the desktop head through the Uno App MCP and screenshot it in Light theme.** Call `mcp__uno-app__uno_health`, then `mcp__uno-app__uno_app_select_solution` with `D:/Personal/mzikmund-worktrees/dev-mazi-appredesign/MZikmund.slnx`, then `mcp__uno-app__uno_discover_tools` to list the run/screenshot tools, then `mcp__uno-app__uno_execute_tool` to launch `net10.0-desktop` and capture a screenshot of the shell with the Blog view showing. In the running app go to **Settings → Theme → Light**.
Expected: the app launches, the nav pane and Blog cards render, no blank/black regions, no unresolved-resource crash. The visuals are still the *old* design — Phase 0 changes nothing visible; this is a "did the SDK bump break rendering" check.

- [ ] **Step 7: Screenshot the same view in Dark theme.** In the running app go to **Settings → Theme → Dark** and capture again with `mcp__uno-app__uno_execute_tool`.
Expected: the app re-themes without exception; text stays legible. Compare against the Light capture — anything that renders in one theme and not the other is a regression to fix before Phase 1.

- [ ] **Step 8: Confirm the tree is clean apart from formatting.**
```bash
git status --porcelain
```
Expected: only files `dotnet format` touched, if any.

- [ ] **Step 9: Commit**
```bash
git add -A
git commit -m "style: Run dotnet format after the phase 0 build hygiene pass"
```

---

## Phase 1 — Theme tokens, Styles library, and BlogView

# Phase 1 — Theme/ token layer, Styles/ library, and BlogView as the reference view

## File structure

## Created

| Path | Responsibility |
|---|---|
| `src/app/MZikmund.App/Assets/Fonts/Inter-Regular.ttf` | UI face, weight 400 (`#Inter`) |
| `src/app/MZikmund.App/Assets/Fonts/Inter-Medium.ttf` | UI face, weight 500 (`#Inter Medium`) |
| `src/app/MZikmund.App/Assets/Fonts/Inter-SemiBold.ttf` | UI face, weight 600 (`#Inter SemiBold`) |
| `src/app/MZikmund.App/Assets/Fonts/JetBrainsMono-Regular.ttf` | Monospace face (`#JetBrains Mono`) |
| `src/app/MZikmund.App/Assets/Fonts/FluentSystemIcons-Resizable.ttf` | Subsetted icon font, 82 codepoints (`#FluentSystemIcons-Resizable`) |
| `src/app/MZikmund.App/Assets/Fonts/icons.txt` | Concept list driving the subset |
| `src/app/MZikmund.App/Assets/Fonts/subset-icons.ps1` | Regeneration script (pinned upstream SHA) |
| `THIRD-PARTY-NOTICES.txt` | OFL/MIT notices for Inter, JetBrains Mono, Fluent System Icons |
| `src/app/MZikmund.App/Theme/Colors.xaml` | Light / Default / HighContrast `Mz*` brushes + WinUI key redirects |
| `src/app/MZikmund.App/Theme/Spacing.xaml` | `MzSpace1..10` + Thickness combos |
| `src/app/MZikmund.App/Theme/Radii.xaml` | Four corner radii |
| `src/app/MZikmund.App/Theme/Motion.xaml` | Three KeySplines + three Durations |
| `src/app/MZikmund.App/Theme/Typography.xaml` | FontFamily resources + 7 ramp styles + 4 wide variants |
| `src/app/MZikmund.App/Theme/Icons.xaml` | Icon FontFamily + `MzGlyph*` constants |
| `src/app/MZikmund.App/Theme/Acrylic.xaml` | Per-theme `AcrylicBrush` |
| `src/app/MZikmund.App/Theme/Elevation.xaml` | `MzLayerSurfaceStyle` / `MzCardSurfaceStyle` (surface + stroke + elevation) |
| `src/app/MZikmund.App/Theme/MzElevation.cs` | Attached property; the entire Windows/non-Windows split |
| `src/app/MZikmund.App/Theme/Styles/TextBlocks.xaml` | Semantic text styles built on the ramp |
| `src/app/MZikmund.App/Theme/Styles/Badges.xaml` | 4 badge `ContentControl` styles |
| `src/app/MZikmund.App/Theme/Styles/Buttons.xaml` | 4 variants + Small/Large, press translate, focus ring |
| `src/app/MZikmund.App/Theme/Styles/Cards.xaml` | `MzContentCardStyle` + hover/press/focus VSM |
| `src/app/MZikmund.App/Theme/Styles/EmptyState.xaml` | Empty/error state panel, icon and text styles |
| `src/app/MZikmund.App.Core/ViewModels/Items/ICardItem.cs` | Contract the card template binds to |
| `src/app/MZikmund.App.Core/Collections/IncrementalLoadingCollection.cs` | `ISupportIncrementalLoading` paging collection |

## Modified

| Path | Change |
|---|---|
| `src/app/MZikmund.App/App.xaml` | Merges the whole `Theme/` tree; drops `Resources/Colors.xaml` + `Resources/Fonts.xaml` |
| `src/app/MZikmund.App/MZikmund.App.csproj` | Explicit `Content` include for `Assets/Fonts/*.ttf` (WinAppSDK head does not glob them) |
| `src/app/MZikmund.App/Resources/FontResources.cs` | `IconFont` now reads `MzIconFontFamily` |
| `src/app/MZikmund.App/Views/BlogView.xaml` | Rebuilt on the new vocabulary |
| `src/app/MZikmund.App/Views/BlogView.xaml.cs` | Click-to-navigate + incremental-loading trigger |
| `src/app/MZikmund.App.Core/ViewModels/BlogViewModel.cs` | Incremental loading, empty/error state |
| `src/app/MZikmund.App.Core/ViewModels/Items/PostListItemViewModel.cs` | Implements `ICardItem` |
| `src/app/MZikmund.App/Strings/en/Resources.resw` | New keys |
| `src/app/MZikmund.App/Strings/cs/Resources.resw` | New keys + missing `Blog` |

## Deleted

`src/app/MZikmund.App/Resources/Colors.xaml`, `src/app/MZikmund.App/Resources/Fonts.xaml`

## Interfaces

## Font family URIs (Task 1)
`ms-appx:///Assets/Fonts/Inter-Regular.ttf#Inter` · `Inter-Medium.ttf#Inter Medium` · `Inter-SemiBold.ttf#Inter SemiBold` · `JetBrainsMono-Regular.ttf#JetBrains Mono` · `FluentSystemIcons-Resizable.ttf#FluentSystemIcons-Resizable` (internal names verified against the shipped files).

## Theme/Icons.xaml
`MzIconFontFamily` (FontFamily). Glyph `x:String`s: `MzGlyphHome` `MzGlyphNews` `MzGlyphVideoClip` `MzGlyphPlayCircle` `MzGlyphMail` `MzGlyphSettings` `MzGlyphShield` `MzGlyphFolder` `MzGlyphTag` `MzGlyphImageMultiple` `MzGlyphImageOff` `MzGlyphCalendar` `MzGlyphChevronRight` `MzGlyphChevronDown` `MzGlyphArrowClockwise` `MzGlyphWarning` `MzGlyphSearch` `MzGlyphMore`. Later phases add keys to this file; they never redefine these.

## Theme/Colors.xaml — brushes (all three theme dictionaries)
`MzBgBaseBrush` `MzBgSecondaryBrush` `MzBgTertiaryBrush` `MzBgElevatedBrush` `MzLayerBrush` `MzCardBrush` `MzCardSecondaryBrush` `MzTextPrimaryBrush` `MzTextSecondaryBrush` `MzTextTertiaryBrush` `MzTextDisabledBrush` `MzTextOnAccentBrush` `MzTextOnAccentPressedBrush` `MzStrokeCardBrush` `MzStrokeControlBrush` `MzStrokeControlStrongBrush` `MzStrokeDividerBrush` `MzStrokeFocusBrush` `MzStrokeFocusInnerBrush` `MzAccentTextBrush` `MzAccentFillBrush` `MzAccentFillHoverBrush` `MzAccentFillActiveBrush` `MzAccentSubtleBrush` `MzAccentSubtleStrongBrush` `MzShimmerBrush` `MzScrimBrush` `MzOnScrimTextBrush` `MzControlElevationBorderBrush` `MzAccentControlElevationBorderBrush` `MzCardMediaEmptyBrush`.
Theme-invariant `Color`s: `MzAccentLight3` `MzAccentLight2` `MzAccentLight1` `MzAccent` `MzAccentDark1` `MzAccentDark2` `MzAccentDark3` `MzAcrylicTintColor` (per-theme) `BrandColor` (kept: `ColorResources.BrandColor` reads it).
Redirected WinUI keys: `ApplicationPageBackgroundThemeBrush` `SolidBackgroundFillColorBase/Secondary/Tertiary/QuarternaryBrush` `LayerFillColorDefaultBrush` `CardBackgroundFillColorDefault/SecondaryBrush` `CardStrokeColorDefaultBrush` `ControlStrokeColorDefault/SecondaryBrush` `ControlStrongStrokeColorDefaultBrush` `DividerStrokeColorDefaultBrush` `FocusStrokeColorOuter/InnerBrush` `TextFillColorPrimary/Secondary/Tertiary/DisabledBrush` `TextOnAccentFillColorPrimaryBrush` `AccentTextFillColorPrimary/Secondary/TertiaryBrush` `AccentFillColorDefault/Secondary/TertiaryBrush` `SubtleFillColorSecondary/TertiaryBrush` `ButtonBackground[PointerOver|Pressed]` `ButtonForeground[PointerOver|Pressed]` `ButtonBorderBrush[PointerOver|Pressed]` `AccentButtonBackground[PointerOver|Pressed]` `AccentButtonForeground[PointerOver|Pressed]` `AccentButtonBorderBrush`.

## Theme/Spacing.xaml
`x:Double`: `MzSpace1`=4 `MzSpace2`=8 `MzSpace3`=12 `MzSpace4`=16 `MzSpace5`=24 `MzSpace6`=32 `MzSpace7`=36 `MzSpace8`=48 `MzSpace9`=64 `MzSpace10`=96.
`Thickness`: `MzPagePaddingCompact` (16) `MzPagePaddingWide` (36,24,36,24) `MzCardPadding` (16) `MzBadgePadding` (8,3,8,3) `MzButtonPadding` (16,8,16,8) `MzButtonSmallPadding` (12,4,12,4) `MzButtonLargePadding` (24,12,24,12).

## Theme/Radii.xaml
`CornerRadius`: `MzControlCornerRadius`=4 `MzCardCornerRadius`=8 `MzLargeCornerRadius`=12 `MzPillCornerRadius`=999 `MzCardTopCornerRadius`=8,8,0,0. Redirects `ControlCornerRadius`, `OverlayCornerRadius`.

## Theme/Motion.xaml (`x:String`, WinUI-native form)
`MzEaseEntrance`=`0,0,0,1` `MzEaseExit`=`1,0,1,1` `MzEaseStandard`=`0.55,0.55,0,1` `MzDurationInstant`=`0` `MzDurationFast`=`00:00:00.167` `MzDurationNormal`=`00:00:00.250` `MzDurationSlow`=`00:00:00.333`. Consumed as `KeySpline="{StaticResource MzEaseStandard}"` / `KeyTime="{StaticResource MzDurationNormal}"`.

## Theme/Typography.xaml
`FontFamily`: `MzFontFamily` `MzFontFamilyMedium` `MzFontFamilySemiBold` `MzMonoFontFamily` (+ `CodeFont` alias kept for `PostEditorView`). Redirects `ContentControlThemeFontFamily`.
`Style TargetType="TextBlock"`: `MzCaptionTextStyle` `MzBodyTextStyle` `MzBodyLargeTextStyle` `MzSubtitleTextStyle` `MzTitleTextStyle` `MzTitleLargeTextStyle` `MzDisplayTextStyle` + wide tier `MzSubtitleWideTextStyle` `MzTitleWideTextStyle` `MzTitleLargeWideTextStyle` `MzDisplayWideTextStyle` (swapped by an `MzTypeScale` VisualStateGroup at ≥768).

## Theme/MzElevation.cs — `namespace MZikmund.Theme`
`public enum MzElevationLevel { None, Layer, Control, Card, Tooltip, Flyout, Dialog }`
`public static class MzElevation` · `public static readonly DependencyProperty LevelProperty` · `public static MzElevationLevel GetLevel(DependencyObject element)` · `public static void SetLevel(DependencyObject element, MzElevationLevel value)`.
XAML: `xmlns:theme="using:MZikmund.Theme"`, `theme:MzElevation.Level="Card"`.

## Theme/Elevation.xaml
`Style TargetType="Border"`: `MzLayerSurfaceStyle`, `MzCardSurfaceStyle`.

## Theme/Acrylic.xaml
`MzAcrylicBrush` (per theme, `AlwaysUseFallback="False"`, `TintOpacity="0.85"`). Authored behind content — never on a container with children.

## Theme/Styles/TextBlocks.xaml
`MzCardTitleTextStyle` `MzCardExcerptTextStyle` `MzMetaTextStyle` `MzLeadTextStyle` `MzSectionHeadingTextStyle`.

## Theme/Styles/Badges.xaml (`Style TargetType="ContentControl"`)
`MzBadgeAccentStyle` `MzBadgeSubtleStyle` `MzBadgeNeutralStyle` `MzBadgeOnPhotoStyle`.

## Theme/Styles/Buttons.xaml (`Style TargetType="Button"`)
`MzAccentButtonStyle` `MzStandardButtonStyle` `MzSubtleButtonStyle` `MzAccentSubtleButtonStyle`; sizes `MzAccentButtonSmallStyle` `MzAccentButtonLargeStyle` `MzStandardButtonSmallStyle` `MzStandardButtonLargeStyle` `MzSubtleButtonSmallStyle` `MzAccentSubtleButtonSmallStyle`.

## Theme/Styles/Cards.xaml
`MzContentCardStyle` (`Style TargetType="Button"`). Its template binds through **DataContext**, so any item bound to it must be an `ICardItem`.

## Theme/Styles/EmptyState.xaml
`MzEmptyStatePanelStyle` (StackPanel) `MzEmptyStateIconStyle` (FontIcon) `MzEmptyStateTitleTextStyle` `MzEmptyStateBodyTextStyle`.

## C# contracts
```csharp
namespace MZikmund.ViewModels.Items;
public interface ICardItem
{
	ImageSource? CardImage { get; }
	bool HasCardImage { get; }
	string CardTitle { get; }
	string CardSummary { get; }
	string CardMeta { get; }
	IReadOnlyList<string> CardBadges { get; }
}
```
```csharp
namespace MZikmund.Collections;
public readonly record struct IncrementalPage<T>(IReadOnlyList<T> Items, bool HasMoreItems);
public sealed class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
{
	public IncrementalLoadingCollection(Func<int, CancellationToken, Task<IncrementalPage<T>>> pageLoader);
	public bool HasMoreItems { get; }
	public void Reset();
	public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count);
}
```
`BlogViewModel`: `IncrementalLoadingCollection<PostListItemViewModel> Posts`, `string Lead`, `bool IsLoading`, `bool IsLoadingMore`, `bool HasError`, `bool IsEmpty`, `ICommand RetryCommand`, `void NavigateToPost(Guid postId)`.

## Resource strings (en + cs)
`Blog` (cs only) `Videos` `ReadMore` `Retry` `Refresh` `NoPostsFound` `NoPostsFoundDescription` `BlogLead` `WatchOnYouTube` `Id`.

## Tasks

### Task 12: Ship the app fonts and the icon vocabulary

**Files:**
- Create: `src/app/MZikmund.App/Assets/Fonts/{Inter-Regular,Inter-Medium,Inter-SemiBold,JetBrainsMono-Regular,FluentSystemIcons-Resizable}.ttf`
- Create: `src/app/MZikmund.App/Assets/Fonts/icons.txt`, `src/app/MZikmund.App/Assets/Fonts/subset-icons.ps1`
- Create: `src/app/MZikmund.App/Theme/Icons.xaml`
- Create: `THIRD-PARTY-NOTICES.txt`
- Modify: `src/app/MZikmund.App/MZikmund.App.csproj:50-52`
- Modify: `src/app/MZikmund.App/Resources/FontResources.cs:5`

**Interfaces:**
- Consumes: nothing.
- Produces: the five font URIs, `MzIconFontFamily`, the `MzGlyph*` string keys.

- [ ] **Step 1: Download Inter 4.1 and extract the three static weights**
```bash
mkdir -p src/app/MZikmund.App/Assets/Fonts
curl -sSL -o /tmp/inter.zip https://github.com/rsms/inter/releases/download/v4.1/Inter-4.1.zip
unzip -o -j /tmp/inter.zip "extras/ttf/Inter-Regular.ttf" "extras/ttf/Inter-Medium.ttf" "extras/ttf/Inter-SemiBold.ttf" -d src/app/MZikmund.App/Assets/Fonts
```
Expected: three files, ~411 KB / ~417 KB / ~419 KB. Their internal family names are `Inter`, `Inter Medium`, `Inter SemiBold` — the `#` suffix in every `FontFamily` URI must match exactly.

- [ ] **Step 2: Download JetBrains Mono 2.304 and extract Regular**
```bash
curl -sSL -o /tmp/jbm.zip https://github.com/JetBrains/JetBrainsMono/releases/download/v2.304/JetBrainsMono-2.304.zip
unzip -o -j /tmp/jbm.zip "fonts/ttf/JetBrainsMono-Regular.ttf" -d src/app/MZikmund.App/Assets/Fonts
```
Expected: `JetBrainsMono-Regular.ttf`, 273 900 bytes, internal family `JetBrains Mono`.

- [ ] **Step 3: Write the icon concept list**

`src/app/MZikmund.App/Assets/Fonts/icons.txt`:
```
home ECAA/EA32
news ECAA
video_clip F50B
play_circle EE8F
mail EBBC
settings F00F
shield_keyhole F04B
folder E8EB
tag F283
image_multiple EA6A
add E00D
save EF99
delete E66D
edit E837
search EFD7
filter E8A7
arrow_sort E14F
calendar_ltr E35E
clock E508
arrow_left E115
arrow_up E1A7
chevron_right E48E
chevron_down E488
open ECF4
rss EF91
weather_sunny F597
weather_moon F57D
circle_half_fill E49A
image_off EA6E
document_pdf E783
text_bullet_list_ltr F318
link EB68
checkmark E460
more_horizontal EC72
arrow_clockwise E0BF
arrow_upload E1B1
copy E5D7
person EDB3
dismiss E6D3
warning F561
info EA88
```
Each concept ships as a Regular/Filled pair; the filled codepoint is always the Regular codepoint minus one.

- [ ] **Step 4: Write the subset regeneration script**

`src/app/MZikmund.App/Assets/Fonts/subset-icons.ps1`:
```powershell
# Regenerates FluentSystemIcons-Resizable.ttf from the pinned upstream commit.
# Requires: pip install fonttools
$ErrorActionPreference = 'Stop'
$sha = 'b02c6d621e02c05b890d9f03f3e98f888d39a80e'
$url = "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/$sha/fonts/FluentSystemIcons-Resizable.ttf"
$full = Join-Path $env:TEMP 'FluentSystemIcons-Resizable-full.ttf'
Invoke-WebRequest -Uri $url -OutFile $full

# Regular + Filled pairs for the concepts listed in icons.txt.
$unicodes = 'E00C,E00D,E0BE,E0BF,E114,E115,E14E,E14F,E1A6,E1A7,E1B0,E1B1,E35D,E35E,E45F,E460,' +
	'E487,E488,E48D,E48E,E499,E49A,E507,E508,E5D6,E5D7,E66C,E66D,E6D2,E6D3,E782,E783,' +
	'E836,E837,E8A6,E8A7,E8EA,E8EB,EA31,EA32,EA69,EA6A,EA6D,EA6E,EA87,EA88,EB67,EB68,' +
	'EBBB,EBBC,EC71,EC72,ECA9,ECAA,ECF3,ECF4,EDB2,EDB3,EE8E,EE8F,EF90,EF91,EF98,EF99,' +
	'EFD6,EFD7,F00E,F00F,F04A,F04B,F282,F283,F317,F318,F50A,F50B,F560,F561,F57C,F57D,F596,F597'

python -m fontTools.subset $full --unicodes="$unicodes" `
	--output-file="$PSScriptRoot/FluentSystemIcons-Resizable.ttf" `
	--no-hinting --desubroutinize --name-IDs='*' --name-legacy --name-languages='*'

Write-Host "Subset written: $((Get-Item "$PSScriptRoot/FluentSystemIcons-Resizable.ttf").Length) bytes"
```

- [ ] **Step 5: Run the subset script / Expected: `Subset written: ~13800 bytes`**
```bash
pwsh -File src/app/MZikmund.App/Assets/Fonts/subset-icons.ps1
```
Expected: `src/app/MZikmund.App/Assets/Fonts/FluentSystemIcons-Resizable.ttf` exists, roughly 13.5–14 KB (down from 1 535 KB), internal family name `FluentSystemIcons-Resizable`.

- [ ] **Step 6: Write the third-party notices**

`THIRD-PARTY-NOTICES.txt`:
```
THIRD-PARTY SOFTWARE NOTICES AND INFORMATION

--------------------------------------------------------------------------------
Inter — https://github.com/rsms/inter — v4.1
SIL Open Font License 1.1
Copyright (c) 2016-2024 The Inter Project Authors.
Shipped as Assets/Fonts/Inter-Regular.ttf, Inter-Medium.ttf, Inter-SemiBold.ttf.

--------------------------------------------------------------------------------
JetBrains Mono — https://github.com/JetBrains/JetBrainsMono — v2.304
SIL Open Font License 1.1
Copyright (c) 2020 The JetBrains Mono Project Authors.
Shipped as Assets/Fonts/JetBrainsMono-Regular.ttf.

--------------------------------------------------------------------------------
Fluent System Icons — https://github.com/microsoft/fluentui-system-icons
Commit b02c6d621e02c05b890d9f03f3e98f888d39a80e
MIT License
Copyright (c) Microsoft Corporation.
Shipped subsetted as Assets/Fonts/FluentSystemIcons-Resizable.ttf; regenerate with
Assets/Fonts/subset-icons.ps1.
```

- [ ] **Step 7: Include the fonts as Content on every head**

The Uno SDK globs `Assets/**` into `Content` only when `!$(IsWinAppSdk)`, and the WindowsAppSDK glob covers images only — so `.ttf` needs an explicit item or the Windows head ships without fonts. In `src/app/MZikmund.App/MZikmund.App.csproj`, replace lines 50-52:
```xml
	<ItemGroup>
		<EmbeddedResource Include="Templates\PostPreviewTemplate.html" />
	</ItemGroup>
```
with:
```xml
	<ItemGroup>
		<EmbeddedResource Include="Templates\PostPreviewTemplate.html" />
	</ItemGroup>

	<!-- Uno globs Assets/** only on non-WinAppSdk heads, and the WinAppSdk glob is images-only. -->
	<ItemGroup>
		<Content Include="Assets\Fonts\*.ttf" />
	</ItemGroup>
```

- [ ] **Step 8: Create `src/app/MZikmund.App/Theme/Icons.xaml`**
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<FontFamily x:Key="MzIconFontFamily">ms-appx:///Assets/Fonts/FluentSystemIcons-Resizable.ttf#FluentSystemIcons-Resizable</FontFamily>

	<x:String x:Key="MzGlyphHome">&#xEA32;</x:String>
	<x:String x:Key="MzGlyphNews">&#xECAA;</x:String>
	<x:String x:Key="MzGlyphVideoClip">&#xF50B;</x:String>
	<x:String x:Key="MzGlyphPlayCircle">&#xEE8F;</x:String>
	<x:String x:Key="MzGlyphMail">&#xEBBC;</x:String>
	<x:String x:Key="MzGlyphSettings">&#xF00F;</x:String>
	<x:String x:Key="MzGlyphShield">&#xF04B;</x:String>
	<x:String x:Key="MzGlyphFolder">&#xE8EB;</x:String>
	<x:String x:Key="MzGlyphTag">&#xF283;</x:String>
	<x:String x:Key="MzGlyphImageMultiple">&#xEA6A;</x:String>
	<x:String x:Key="MzGlyphImageOff">&#xEA6E;</x:String>
	<x:String x:Key="MzGlyphCalendar">&#xE35E;</x:String>
	<x:String x:Key="MzGlyphChevronRight">&#xE48E;</x:String>
	<x:String x:Key="MzGlyphChevronDown">&#xE488;</x:String>
	<x:String x:Key="MzGlyphArrowClockwise">&#xE0BF;</x:String>
	<x:String x:Key="MzGlyphWarning">&#xF561;</x:String>
	<x:String x:Key="MzGlyphSearch">&#xEFD7;</x:String>
	<x:String x:Key="MzGlyphMore">&#xEC72;</x:String>

</ResourceDictionary>
```

- [ ] **Step 9: Repoint `FontResources`**

Replace `src/app/MZikmund.App/Resources/FontResources.cs` in full:
```csharp
namespace MZikmund.Resources;

public static class FontResources
{
	public static FontFamily IconFont => ResourceAccessor.GetResource<FontFamily>("MzIconFontFamily");
}
```

- [ ] **Step 10: Commit**
```bash
git add src/app/MZikmund.App/Assets/Fonts src/app/MZikmund.App/Theme/Icons.xaml src/app/MZikmund.App/MZikmund.App.csproj src/app/MZikmund.App/Resources/FontResources.cs THIRD-PARTY-NOTICES.txt
git commit -m "feat: Ship Inter, JetBrains Mono and a subsetted Fluent System Icons font"
```

---

### Task 13: `Theme/Colors.xaml` — the palette and the WinUI redirects

**Files:**
- Create: `src/app/MZikmund.App/Theme/Colors.xaml`

**Interfaces:**
- Consumes: nothing.
- Produces: every `Mz*Brush`, the accent ramp `Color`s, `MzAcrylicTintColor`, `BrandColor`, and the redirected WinUI brush keys.

- [ ] **Step 1: Create the file with the Light dictionary**
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<ResourceDictionary.ThemeDictionaries>

		<ResourceDictionary x:Key="Light">
			<Color x:Key="MzAcrylicTintColor">#FCFCFC</Color>

			<SolidColorBrush x:Key="MzBgBaseBrush" Color="#FFF3F3F3" />
			<SolidColorBrush x:Key="MzBgSecondaryBrush" Color="#FFEEEEEE" />
			<SolidColorBrush x:Key="MzBgTertiaryBrush" Color="#FFF9F9F9" />
			<SolidColorBrush x:Key="MzBgElevatedBrush" Color="#FFFFFFFF" />
			<SolidColorBrush x:Key="MzLayerBrush" Color="#80FFFFFF" />
			<SolidColorBrush x:Key="MzCardBrush" Color="#FFFFFFFF" />
			<SolidColorBrush x:Key="MzCardSecondaryBrush" Color="#80F6F6F6" />

			<SolidColorBrush x:Key="MzTextPrimaryBrush" Color="#E4000000" />
			<SolidColorBrush x:Key="MzTextSecondaryBrush" Color="#9B000000" />
			<SolidColorBrush x:Key="MzTextTertiaryBrush" Color="#72000000" />
			<SolidColorBrush x:Key="MzTextDisabledBrush" Color="#5C000000" />
			<SolidColorBrush x:Key="MzTextOnAccentBrush" Color="#FFFFFFFF" />
			<SolidColorBrush x:Key="MzTextOnAccentPressedBrush" Color="#FFFFFFFF" />

			<SolidColorBrush x:Key="MzStrokeCardBrush" Color="#0F000000" />
			<SolidColorBrush x:Key="MzStrokeControlBrush" Color="#0F000000" />
			<SolidColorBrush x:Key="MzStrokeControlStrongBrush" Color="#72000000" />
			<SolidColorBrush x:Key="MzStrokeDividerBrush" Color="#14000000" />
			<SolidColorBrush x:Key="MzStrokeFocusBrush" Color="#E4000000" />
			<SolidColorBrush x:Key="MzStrokeFocusInnerBrush" Color="#FFFFFFFF" />

			<SolidColorBrush x:Key="MzAccentTextBrush" Color="#FF005A9E" />
			<SolidColorBrush x:Key="MzAccentFillBrush" Color="#FF005A9E" />
			<SolidColorBrush x:Key="MzAccentFillHoverBrush" Color="#FF004275" />
			<SolidColorBrush x:Key="MzAccentFillActiveBrush" Color="#FF002642" />
			<SolidColorBrush x:Key="MzAccentSubtleBrush" Color="#140078D4" />
			<SolidColorBrush x:Key="MzAccentSubtleStrongBrush" Color="#290078D4" />
			<SolidColorBrush x:Key="MzShimmerBrush" Color="#0A000000" />

			<!--  Theme-invariant: badges over photography must survive any upload.  -->
			<SolidColorBrush x:Key="MzScrimBrush" Color="#B8000000" />
			<SolidColorBrush x:Key="MzOnScrimTextBrush" Color="#FFFFFFFF" />

			<!--  Fluent's ControlElevationBorderBrush: bottom edge darker than the sides.  -->
			<LinearGradientBrush x:Key="MzControlElevationBorderBrush" StartPoint="0,0" EndPoint="0,1">
				<GradientStop Offset="0.0" Color="#0F000000" />
				<GradientStop Offset="0.92" Color="#0F000000" />
				<GradientStop Offset="0.92" Color="#1B000000" />
				<GradientStop Offset="1.0" Color="#1B000000" />
			</LinearGradientBrush>
			<LinearGradientBrush x:Key="MzAccentControlElevationBorderBrush" StartPoint="0,0" EndPoint="0,1">
				<GradientStop Offset="0.0" Color="#14FFFFFF" />
				<GradientStop Offset="0.92" Color="#14FFFFFF" />
				<GradientStop Offset="0.92" Color="#24000000" />
				<GradientStop Offset="1.0" Color="#24000000" />
			</LinearGradientBrush>

			<LinearGradientBrush x:Key="MzCardMediaEmptyBrush" StartPoint="0,0" EndPoint="1,1">
				<GradientStop Offset="0.0" Color="#140078D4" />
				<GradientStop Offset="0.7" Color="#000078D4" />
			</LinearGradientBrush>

			<StaticResource x:Key="ApplicationPageBackgroundThemeBrush" ResourceKey="MzBgBaseBrush" />
			<StaticResource x:Key="SolidBackgroundFillColorBaseBrush" ResourceKey="MzBgBaseBrush" />
			<StaticResource x:Key="SolidBackgroundFillColorSecondaryBrush" ResourceKey="MzBgSecondaryBrush" />
			<StaticResource x:Key="SolidBackgroundFillColorTertiaryBrush" ResourceKey="MzBgTertiaryBrush" />
			<StaticResource x:Key="SolidBackgroundFillColorQuarternaryBrush" ResourceKey="MzBgElevatedBrush" />
			<StaticResource x:Key="LayerFillColorDefaultBrush" ResourceKey="MzLayerBrush" />
			<StaticResource x:Key="CardBackgroundFillColorDefaultBrush" ResourceKey="MzCardBrush" />
			<StaticResource x:Key="CardBackgroundFillColorSecondaryBrush" ResourceKey="MzCardSecondaryBrush" />
			<StaticResource x:Key="CardStrokeColorDefaultBrush" ResourceKey="MzStrokeCardBrush" />
			<StaticResource x:Key="ControlStrokeColorDefaultBrush" ResourceKey="MzStrokeControlBrush" />
			<StaticResource x:Key="ControlStrokeColorSecondaryBrush" ResourceKey="MzStrokeControlBrush" />
			<StaticResource x:Key="ControlStrongStrokeColorDefaultBrush" ResourceKey="MzStrokeControlStrongBrush" />
			<StaticResource x:Key="DividerStrokeColorDefaultBrush" ResourceKey="MzStrokeDividerBrush" />
			<StaticResource x:Key="FocusStrokeColorOuterBrush" ResourceKey="MzStrokeFocusBrush" />
			<StaticResource x:Key="FocusStrokeColorInnerBrush" ResourceKey="MzStrokeFocusInnerBrush" />
			<StaticResource x:Key="TextFillColorPrimaryBrush" ResourceKey="MzTextPrimaryBrush" />
			<StaticResource x:Key="TextFillColorSecondaryBrush" ResourceKey="MzTextSecondaryBrush" />
			<StaticResource x:Key="TextFillColorTertiaryBrush" ResourceKey="MzTextTertiaryBrush" />
			<StaticResource x:Key="TextFillColorDisabledBrush" ResourceKey="MzTextDisabledBrush" />
			<StaticResource x:Key="TextOnAccentFillColorPrimaryBrush" ResourceKey="MzTextOnAccentBrush" />
			<StaticResource x:Key="AccentTextFillColorPrimaryBrush" ResourceKey="MzAccentTextBrush" />
			<StaticResource x:Key="AccentTextFillColorSecondaryBrush" ResourceKey="MzAccentTextBrush" />
			<StaticResource x:Key="AccentTextFillColorTertiaryBrush" ResourceKey="MzAccentTextBrush" />
			<StaticResource x:Key="AccentFillColorDefaultBrush" ResourceKey="MzAccentFillBrush" />
			<StaticResource x:Key="AccentFillColorSecondaryBrush" ResourceKey="MzAccentFillHoverBrush" />
			<StaticResource x:Key="AccentFillColorTertiaryBrush" ResourceKey="MzAccentFillActiveBrush" />
			<StaticResource x:Key="SubtleFillColorSecondaryBrush" ResourceKey="MzShimmerBrush" />
			<StaticResource x:Key="SubtleFillColorTertiaryBrush" ResourceKey="MzShimmerBrush" />
			<StaticResource x:Key="ButtonBackground" ResourceKey="MzCardBrush" />
			<StaticResource x:Key="ButtonBackgroundPointerOver" ResourceKey="MzBgSecondaryBrush" />
			<StaticResource x:Key="ButtonBackgroundPressed" ResourceKey="MzBgSecondaryBrush" />
			<StaticResource x:Key="ButtonForeground" ResourceKey="MzTextPrimaryBrush" />
			<StaticResource x:Key="ButtonForegroundPointerOver" ResourceKey="MzTextPrimaryBrush" />
			<StaticResource x:Key="ButtonForegroundPressed" ResourceKey="MzTextSecondaryBrush" />
			<StaticResource x:Key="ButtonBorderBrush" ResourceKey="MzControlElevationBorderBrush" />
			<StaticResource x:Key="ButtonBorderBrushPointerOver" ResourceKey="MzControlElevationBorderBrush" />
			<StaticResource x:Key="ButtonBorderBrushPressed" ResourceKey="MzStrokeControlBrush" />
			<StaticResource x:Key="AccentButtonBackground" ResourceKey="MzAccentFillBrush" />
			<StaticResource x:Key="AccentButtonBackgroundPointerOver" ResourceKey="MzAccentFillHoverBrush" />
			<StaticResource x:Key="AccentButtonBackgroundPressed" ResourceKey="MzAccentFillActiveBrush" />
			<StaticResource x:Key="AccentButtonForeground" ResourceKey="MzTextOnAccentBrush" />
			<StaticResource x:Key="AccentButtonForegroundPointerOver" ResourceKey="MzTextOnAccentPressedBrush" />
			<StaticResource x:Key="AccentButtonForegroundPressed" ResourceKey="MzTextOnAccentPressedBrush" />
			<StaticResource x:Key="AccentButtonBorderBrush" ResourceKey="MzAccentControlElevationBorderBrush" />
		</ResourceDictionary>
```

- [ ] **Step 2: Append the Default (dark) dictionary**

Immediately after `</ResourceDictionary>` of the Light block, still inside `ThemeDictionaries`:
```xml
		<ResourceDictionary x:Key="Default">
			<Color x:Key="MzAcrylicTintColor">#2C2C2C</Color>

			<SolidColorBrush x:Key="MzBgBaseBrush" Color="#FF202020" />
			<SolidColorBrush x:Key="MzBgSecondaryBrush" Color="#FF1C1C1C" />
			<SolidColorBrush x:Key="MzBgTertiaryBrush" Color="#FF282828" />
			<SolidColorBrush x:Key="MzBgElevatedBrush" Color="#FF2C2C2C" />
			<SolidColorBrush x:Key="MzLayerBrush" Color="#4C3A3A3A" />
			<SolidColorBrush x:Key="MzCardBrush" Color="#FF2B2B2B" />
			<SolidColorBrush x:Key="MzCardSecondaryBrush" Color="#08FFFFFF" />

			<SolidColorBrush x:Key="MzTextPrimaryBrush" Color="#FFFFFFFF" />
			<SolidColorBrush x:Key="MzTextSecondaryBrush" Color="#C8FFFFFF" />
			<SolidColorBrush x:Key="MzTextTertiaryBrush" Color="#8BFFFFFF" />
			<SolidColorBrush x:Key="MzTextDisabledBrush" Color="#5DFFFFFF" />
			<SolidColorBrush x:Key="MzTextOnAccentBrush" Color="#FFFFFFFF" />
			<!--  Dark hover/active fills are light; white on them fails AA. WinUI flips to black.  -->
			<SolidColorBrush x:Key="MzTextOnAccentPressedBrush" Color="#FF000000" />

			<SolidColorBrush x:Key="MzStrokeCardBrush" Color="#1A000000" />
			<SolidColorBrush x:Key="MzStrokeControlBrush" Color="#12FFFFFF" />
			<SolidColorBrush x:Key="MzStrokeControlStrongBrush" Color="#8BFFFFFF" />
			<SolidColorBrush x:Key="MzStrokeDividerBrush" Color="#15FFFFFF" />
			<SolidColorBrush x:Key="MzStrokeFocusBrush" Color="#FFFFFFFF" />
			<SolidColorBrush x:Key="MzStrokeFocusInnerBrush" Color="#B3000000" />

			<SolidColorBrush x:Key="MzAccentTextBrush" Color="#FF76B9ED" />
			<SolidColorBrush x:Key="MzAccentFillBrush" Color="#FF0078D4" />
			<SolidColorBrush x:Key="MzAccentFillHoverBrush" Color="#FF429CE3" />
			<SolidColorBrush x:Key="MzAccentFillActiveBrush" Color="#FF76B9ED" />
			<SolidColorBrush x:Key="MzAccentSubtleBrush" Color="#1A76B9ED" />
			<SolidColorBrush x:Key="MzAccentSubtleStrongBrush" Color="#3376B9ED" />
			<SolidColorBrush x:Key="MzShimmerBrush" Color="#0DFFFFFF" />

			<SolidColorBrush x:Key="MzScrimBrush" Color="#B8000000" />
			<SolidColorBrush x:Key="MzOnScrimTextBrush" Color="#FFFFFFFF" />

			<LinearGradientBrush x:Key="MzControlElevationBorderBrush" StartPoint="0,0" EndPoint="0,1">
				<GradientStop Offset="0.0" Color="#12FFFFFF" />
				<GradientStop Offset="0.92" Color="#12FFFFFF" />
				<GradientStop Offset="0.92" Color="#24000000" />
				<GradientStop Offset="1.0" Color="#24000000" />
			</LinearGradientBrush>
			<LinearGradientBrush x:Key="MzAccentControlElevationBorderBrush" StartPoint="0,0" EndPoint="0,1">
				<GradientStop Offset="0.0" Color="#14FFFFFF" />
				<GradientStop Offset="0.92" Color="#14FFFFFF" />
				<GradientStop Offset="0.92" Color="#24000000" />
				<GradientStop Offset="1.0" Color="#24000000" />
			</LinearGradientBrush>

			<LinearGradientBrush x:Key="MzCardMediaEmptyBrush" StartPoint="0,0" EndPoint="1,1">
				<GradientStop Offset="0.0" Color="#1A76B9ED" />
				<GradientStop Offset="0.7" Color="#0076B9ED" />
			</LinearGradientBrush>

			<StaticResource x:Key="ApplicationPageBackgroundThemeBrush" ResourceKey="MzBgBaseBrush" />
			<StaticResource x:Key="SolidBackgroundFillColorBaseBrush" ResourceKey="MzBgBaseBrush" />
			<StaticResource x:Key="SolidBackgroundFillColorSecondaryBrush" ResourceKey="MzBgSecondaryBrush" />
			<StaticResource x:Key="SolidBackgroundFillColorTertiaryBrush" ResourceKey="MzBgTertiaryBrush" />
			<StaticResource x:Key="SolidBackgroundFillColorQuarternaryBrush" ResourceKey="MzBgElevatedBrush" />
			<StaticResource x:Key="LayerFillColorDefaultBrush" ResourceKey="MzLayerBrush" />
			<StaticResource x:Key="CardBackgroundFillColorDefaultBrush" ResourceKey="MzCardBrush" />
			<StaticResource x:Key="CardBackgroundFillColorSecondaryBrush" ResourceKey="MzCardSecondaryBrush" />
			<StaticResource x:Key="CardStrokeColorDefaultBrush" ResourceKey="MzStrokeCardBrush" />
			<StaticResource x:Key="ControlStrokeColorDefaultBrush" ResourceKey="MzStrokeControlBrush" />
			<StaticResource x:Key="ControlStrokeColorSecondaryBrush" ResourceKey="MzStrokeControlBrush" />
			<StaticResource x:Key="ControlStrongStrokeColorDefaultBrush" ResourceKey="MzStrokeControlStrongBrush" />
			<StaticResource x:Key="DividerStrokeColorDefaultBrush" ResourceKey="MzStrokeDividerBrush" />
			<StaticResource x:Key="FocusStrokeColorOuterBrush" ResourceKey="MzStrokeFocusBrush" />
			<StaticResource x:Key="FocusStrokeColorInnerBrush" ResourceKey="MzStrokeFocusInnerBrush" />
			<StaticResource x:Key="TextFillColorPrimaryBrush" ResourceKey="MzTextPrimaryBrush" />
			<StaticResource x:Key="TextFillColorSecondaryBrush" ResourceKey="MzTextSecondaryBrush" />
			<StaticResource x:Key="TextFillColorTertiaryBrush" ResourceKey="MzTextTertiaryBrush" />
			<StaticResource x:Key="TextFillColorDisabledBrush" ResourceKey="MzTextDisabledBrush" />
			<StaticResource x:Key="TextOnAccentFillColorPrimaryBrush" ResourceKey="MzTextOnAccentBrush" />
			<StaticResource x:Key="AccentTextFillColorPrimaryBrush" ResourceKey="MzAccentTextBrush" />
			<StaticResource x:Key="AccentTextFillColorSecondaryBrush" ResourceKey="MzAccentTextBrush" />
			<StaticResource x:Key="AccentTextFillColorTertiaryBrush" ResourceKey="MzAccentTextBrush" />
			<StaticResource x:Key="AccentFillColorDefaultBrush" ResourceKey="MzAccentFillBrush" />
			<StaticResource x:Key="AccentFillColorSecondaryBrush" ResourceKey="MzAccentFillHoverBrush" />
			<StaticResource x:Key="AccentFillColorTertiaryBrush" ResourceKey="MzAccentFillActiveBrush" />
			<StaticResource x:Key="SubtleFillColorSecondaryBrush" ResourceKey="MzShimmerBrush" />
			<StaticResource x:Key="SubtleFillColorTertiaryBrush" ResourceKey="MzShimmerBrush" />
			<StaticResource x:Key="ButtonBackground" ResourceKey="MzCardBrush" />
			<StaticResource x:Key="ButtonBackgroundPointerOver" ResourceKey="MzBgTertiaryBrush" />
			<StaticResource x:Key="ButtonBackgroundPressed" ResourceKey="MzBgTertiaryBrush" />
			<StaticResource x:Key="ButtonForeground" ResourceKey="MzTextPrimaryBrush" />
			<StaticResource x:Key="ButtonForegroundPointerOver" ResourceKey="MzTextPrimaryBrush" />
			<StaticResource x:Key="ButtonForegroundPressed" ResourceKey="MzTextSecondaryBrush" />
			<StaticResource x:Key="ButtonBorderBrush" ResourceKey="MzControlElevationBorderBrush" />
			<StaticResource x:Key="ButtonBorderBrushPointerOver" ResourceKey="MzControlElevationBorderBrush" />
			<StaticResource x:Key="ButtonBorderBrushPressed" ResourceKey="MzStrokeControlBrush" />
			<StaticResource x:Key="AccentButtonBackground" ResourceKey="MzAccentFillBrush" />
			<StaticResource x:Key="AccentButtonBackgroundPointerOver" ResourceKey="MzAccentFillHoverBrush" />
			<StaticResource x:Key="AccentButtonBackgroundPressed" ResourceKey="MzAccentFillActiveBrush" />
			<StaticResource x:Key="AccentButtonForeground" ResourceKey="MzTextOnAccentBrush" />
			<StaticResource x:Key="AccentButtonForegroundPointerOver" ResourceKey="MzTextOnAccentPressedBrush" />
			<StaticResource x:Key="AccentButtonForegroundPressed" ResourceKey="MzTextOnAccentPressedBrush" />
			<StaticResource x:Key="AccentButtonBorderBrush" ResourceKey="MzAccentControlElevationBorderBrush" />
		</ResourceDictionary>
```

- [ ] **Step 3: Append the HighContrast dictionary and close the file**

Only the eight permitted `SystemColor*Color` resources may appear here, and no elevation. The WinUI keys are deliberately *not* redirected in HighContrast — WinUI's own forced-colours mapping is already correct, and overriding it would only add risk.
```xml
		<ResourceDictionary x:Key="HighContrast">
			<Color x:Key="MzAcrylicTintColor">#FFFFFF</Color>

			<SolidColorBrush x:Key="MzBgBaseBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzBgSecondaryBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzBgTertiaryBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzBgElevatedBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzLayerBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzCardBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzCardSecondaryBrush" Color="{StaticResource SystemColorWindowColor}" />

			<SolidColorBrush x:Key="MzTextPrimaryBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzTextSecondaryBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzTextTertiaryBrush" Color="{StaticResource SystemColorGrayTextColor}" />
			<SolidColorBrush x:Key="MzTextDisabledBrush" Color="{StaticResource SystemColorGrayTextColor}" />
			<SolidColorBrush x:Key="MzTextOnAccentBrush" Color="{StaticResource SystemColorHighlightTextColor}" />
			<SolidColorBrush x:Key="MzTextOnAccentPressedBrush" Color="{StaticResource SystemColorHighlightTextColor}" />

			<SolidColorBrush x:Key="MzStrokeCardBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzStrokeControlBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzStrokeControlStrongBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzStrokeDividerBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzStrokeFocusBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzStrokeFocusInnerBrush" Color="{StaticResource SystemColorWindowColor}" />

			<SolidColorBrush x:Key="MzAccentTextBrush" Color="{StaticResource SystemColorHotlightColor}" />
			<SolidColorBrush x:Key="MzAccentFillBrush" Color="{StaticResource SystemColorHighlightColor}" />
			<SolidColorBrush x:Key="MzAccentFillHoverBrush" Color="{StaticResource SystemColorHighlightColor}" />
			<SolidColorBrush x:Key="MzAccentFillActiveBrush" Color="{StaticResource SystemColorHighlightColor}" />
			<SolidColorBrush x:Key="MzAccentSubtleBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzAccentSubtleStrongBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzShimmerBrush" Color="{StaticResource SystemColorWindowColor}" />

			<SolidColorBrush x:Key="MzScrimBrush" Color="{StaticResource SystemColorWindowColor}" />
			<SolidColorBrush x:Key="MzOnScrimTextBrush" Color="{StaticResource SystemColorWindowTextColor}" />

			<SolidColorBrush x:Key="MzControlElevationBorderBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzAccentControlElevationBorderBrush" Color="{StaticResource SystemColorWindowTextColor}" />
			<SolidColorBrush x:Key="MzCardMediaEmptyBrush" Color="{StaticResource SystemColorWindowColor}" />
		</ResourceDictionary>

	</ResourceDictionary.ThemeDictionaries>

	<!--  Accent ramp: theme-invariant and hard-coded. Never bind to SystemAccentColor*.  -->
	<Color x:Key="MzAccentLight3">#FFA6D8FF</Color>
	<Color x:Key="MzAccentLight2">#FF76B9ED</Color>
	<Color x:Key="MzAccentLight1">#FF429CE3</Color>
	<Color x:Key="MzAccent">#FF0078D4</Color>
	<Color x:Key="MzAccentDark1">#FF005A9E</Color>
	<Color x:Key="MzAccentDark2">#FF004275</Color>
	<Color x:Key="MzAccentDark3">#FF002642</Color>

	<!--  Replaces the personalization accent so no stock style can drag in the user's colour.  -->
	<StaticResource x:Key="SystemAccentColor" ResourceKey="MzAccent" />
	<StaticResource x:Key="SystemAccentColorLight1" ResourceKey="MzAccentLight1" />
	<StaticResource x:Key="SystemAccentColorLight2" ResourceKey="MzAccentLight2" />
	<StaticResource x:Key="SystemAccentColorLight3" ResourceKey="MzAccentLight3" />
	<StaticResource x:Key="SystemAccentColorDark1" ResourceKey="MzAccentDark1" />
	<StaticResource x:Key="SystemAccentColorDark2" ResourceKey="MzAccentDark2" />
	<StaticResource x:Key="SystemAccentColorDark3" ResourceKey="MzAccentDark3" />

	<!--  Kept: Resources/ColorResoruces.cs reads this key by name.  -->
	<StaticResource x:Key="BrandColor" ResourceKey="MzAccent" />
	<SolidColorBrush x:Key="BrandBrush" Color="{StaticResource MzAccent}" />

</ResourceDictionary>
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Theme/Colors.xaml
git commit -m "feat: Add the Mz colour token layer with light, dark and high-contrast dictionaries"
```

---

### Task 14: `Theme/Spacing.xaml`, `Theme/Radii.xaml`, `Theme/Motion.xaml`

**Files:**
- Create: `src/app/MZikmund.App/Theme/Spacing.xaml`, `src/app/MZikmund.App/Theme/Radii.xaml`, `src/app/MZikmund.App/Theme/Motion.xaml`

**Interfaces:**
- Consumes: nothing.
- Produces: `MzSpace1..10`, the Thickness combos, the four radii, the three eases and three durations.

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/Spacing.xaml`**
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<!--  Strict Fluent 4px grid.  -->
	<x:Double x:Key="MzSpace1">4</x:Double>
	<x:Double x:Key="MzSpace2">8</x:Double>
	<x:Double x:Key="MzSpace3">12</x:Double>
	<x:Double x:Key="MzSpace4">16</x:Double>
	<x:Double x:Key="MzSpace5">24</x:Double>
	<x:Double x:Key="MzSpace6">32</x:Double>
	<x:Double x:Key="MzSpace7">36</x:Double>
	<x:Double x:Key="MzSpace8">48</x:Double>
	<x:Double x:Key="MzSpace9">64</x:Double>
	<x:Double x:Key="MzSpace10">96</x:Double>

	<Thickness x:Key="MzPagePaddingCompact">16</Thickness>
	<Thickness x:Key="MzPagePaddingWide">36,24,36,24</Thickness>
	<Thickness x:Key="MzCardPadding">16</Thickness>
	<Thickness x:Key="MzBadgePadding">8,3,8,3</Thickness>
	<Thickness x:Key="MzButtonPadding">16,8,16,8</Thickness>
	<Thickness x:Key="MzButtonSmallPadding">12,4,12,4</Thickness>
	<Thickness x:Key="MzButtonLargePadding">24,12,24,12</Thickness>

</ResourceDictionary>
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Theme/Radii.xaml`**
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<CornerRadius x:Key="MzControlCornerRadius">4</CornerRadius>
	<CornerRadius x:Key="MzCardCornerRadius">8</CornerRadius>
	<CornerRadius x:Key="MzLargeCornerRadius">12</CornerRadius>
	<CornerRadius x:Key="MzPillCornerRadius">999</CornerRadius>
	<CornerRadius x:Key="MzCardTopCornerRadius">8,8,0,0</CornerRadius>

	<StaticResource x:Key="ControlCornerRadius" ResourceKey="MzControlCornerRadius" />
	<StaticResource x:Key="OverlayCornerRadius" ResourceKey="MzCardCornerRadius" />

</ResourceDictionary>
```

- [ ] **Step 3: Create `src/app/MZikmund.App/Theme/Motion.xaml`**

`KeySpline` and `KeyTime` both parse from strings; this is the form WinUI's own theme resources use (`ControlFastOutSlowInKeySpline`, `ControlNormalAnimationDuration`), so it is the only form guaranteed to work on every head.
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<!--  Decelerate: anything arriving.  -->
	<x:String x:Key="MzEaseEntrance">0,0,0,1</x:String>
	<!--  Accelerate: anything leaving.  -->
	<x:String x:Key="MzEaseExit">1,0,1,1</x:String>
	<!--  Point-to-point.  -->
	<x:String x:Key="MzEaseStandard">0.55,0.55,0,1</x:String>

	<!--  167 fill · 250 depth and travel · 333 page and image.  -->
	<x:String x:Key="MzDurationInstant">0</x:String>
	<x:String x:Key="MzDurationFast">00:00:00.167</x:String>
	<x:String x:Key="MzDurationNormal">00:00:00.250</x:String>
	<x:String x:Key="MzDurationSlow">00:00:00.333</x:String>

</ResourceDictionary>
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Theme/Spacing.xaml src/app/MZikmund.App/Theme/Radii.xaml src/app/MZikmund.App/Theme/Motion.xaml
git commit -m "feat: Add spacing, radius and motion tokens"
```

---

### Task 15: `Theme/Typography.xaml`

**Files:**
- Create: `src/app/MZikmund.App/Theme/Typography.xaml`

**Interfaces:**
- Consumes: `MzTextPrimaryBrush` (Task 2).
- Produces: the four `FontFamily` resources, the `CodeFont` alias, the seven ramp styles and the four wide-tier styles.

- [ ] **Step 1: Create the file**

`LineHeight` is absolute px, so every style pairs it with `LineStackingStrategy="BlockLineHeight"`. `CharacterSpacing` is 1/1000 em: `-0.022em` is `-22`. Uno's Skia renderer does not support variable fonts, so each weight is its own file and `FontFamily` + `FontWeight` are always set together.
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<FontFamily x:Key="MzFontFamily">ms-appx:///Assets/Fonts/Inter-Regular.ttf#Inter</FontFamily>
	<FontFamily x:Key="MzFontFamilyMedium">ms-appx:///Assets/Fonts/Inter-Medium.ttf#Inter Medium</FontFamily>
	<FontFamily x:Key="MzFontFamilySemiBold">ms-appx:///Assets/Fonts/Inter-SemiBold.ttf#Inter SemiBold</FontFamily>
	<FontFamily x:Key="MzMonoFontFamily">ms-appx:///Assets/Fonts/JetBrainsMono-Regular.ttf#JetBrains Mono</FontFamily>

	<!--  PostEditorView still asks for CodeFont; it now resolves to a font that exists.  -->
	<StaticResource x:Key="CodeFont" ResourceKey="MzMonoFontFamily" />

	<!--  Every stock control style reads this key.  -->
	<StaticResource x:Key="ContentControlThemeFontFamily" ResourceKey="MzFontFamily" />

	<Style x:Key="MzCaptionTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilyMedium}" />
		<Setter Property="FontSize" Value="13" />
		<Setter Property="FontWeight" Value="Medium" />
		<Setter Property="LineHeight" Value="17" />
		<Setter Property="LineStackingStrategy" Value="BlockLineHeight" />
		<Setter Property="CharacterSpacing" Value="0" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="TextWrapping" Value="Wrap" />
	</Style>

	<Style x:Key="MzBodyTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamily}" />
		<Setter Property="FontSize" Value="17" />
		<Setter Property="FontWeight" Value="Normal" />
		<Setter Property="LineHeight" Value="26" />
		<Setter Property="LineStackingStrategy" Value="BlockLineHeight" />
		<Setter Property="CharacterSpacing" Value="0" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="TextWrapping" Value="Wrap" />
	</Style>

	<Style x:Key="MzBodyLargeTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamily}" />
		<Setter Property="FontSize" Value="19" />
		<Setter Property="FontWeight" Value="Normal" />
		<Setter Property="LineHeight" Value="29" />
		<Setter Property="LineStackingStrategy" Value="BlockLineHeight" />
		<Setter Property="CharacterSpacing" Value="0" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="TextWrapping" Value="Wrap" />
	</Style>

	<!--  Subtitle and above carry the compact value; the wide tier swaps in at 768.  -->
	<Style x:Key="MzSubtitleTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilySemiBold}" />
		<Setter Property="FontSize" Value="20" />
		<Setter Property="FontWeight" Value="SemiBold" />
		<Setter Property="LineHeight" Value="23" />
		<Setter Property="LineStackingStrategy" Value="BlockLineHeight" />
		<Setter Property="CharacterSpacing" Value="0" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="TextWrapping" Value="Wrap" />
	</Style>

	<Style x:Key="MzSubtitleWideTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzSubtitleTextStyle}">
		<Setter Property="FontSize" Value="22" />
		<Setter Property="LineHeight" Value="25" />
	</Style>

	<Style x:Key="MzTitleTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilySemiBold}" />
		<Setter Property="FontSize" Value="24" />
		<Setter Property="FontWeight" Value="SemiBold" />
		<Setter Property="LineHeight" Value="28" />
		<Setter Property="LineStackingStrategy" Value="BlockLineHeight" />
		<Setter Property="CharacterSpacing" Value="-22" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="TextWrapping" Value="Wrap" />
	</Style>

	<Style x:Key="MzTitleWideTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzTitleTextStyle}">
		<Setter Property="FontSize" Value="30" />
		<Setter Property="LineHeight" Value="35" />
	</Style>

	<Style x:Key="MzTitleLargeTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilySemiBold}" />
		<Setter Property="FontSize" Value="30" />
		<Setter Property="FontWeight" Value="SemiBold" />
		<Setter Property="LineHeight" Value="35" />
		<Setter Property="LineStackingStrategy" Value="BlockLineHeight" />
		<Setter Property="CharacterSpacing" Value="-22" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="TextWrapping" Value="Wrap" />
	</Style>

	<Style x:Key="MzTitleLargeWideTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzTitleLargeTextStyle}">
		<Setter Property="FontSize" Value="44" />
		<Setter Property="LineHeight" Value="51" />
	</Style>

	<Style x:Key="MzDisplayTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilySemiBold}" />
		<Setter Property="FontSize" Value="40" />
		<Setter Property="FontWeight" Value="SemiBold" />
		<Setter Property="LineHeight" Value="46" />
		<Setter Property="LineStackingStrategy" Value="BlockLineHeight" />
		<Setter Property="CharacterSpacing" Value="-22" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="TextWrapping" Value="Wrap" />
	</Style>

	<Style x:Key="MzDisplayWideTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzDisplayTextStyle}">
		<Setter Property="FontSize" Value="72" />
		<Setter Property="LineHeight" Value="83" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 2: Commit**
```bash
git add src/app/MZikmund.App/Theme/Typography.xaml
git commit -m "feat: Add the Inter and JetBrains Mono type ramp"
```

---

### Task 16: `MzElevation` attached property and `Theme/Elevation.xaml`

**Files:**
- Create: `src/app/MZikmund.App/Theme/MzElevation.cs`
- Create: `src/app/MZikmund.App/Theme/Elevation.xaml`

**Interfaces:**
- Consumes: `MzCardBrush`, `MzStrokeCardBrush` (Task 2); `MzCardCornerRadius` (Task 3).
- Produces: `MzElevationLevel`, `MzElevation.Level`, `MzLayerSurfaceStyle`, `MzCardSurfaceStyle`.

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/MzElevation.cs`**

The whole platform split lives in this one file: XAML never names a Windows-only type. Off Windows the paired 1px stroke carries the hierarchy.
```csharp
#if WINDOWS
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Media;
using Windows.UI;
#endif

namespace MZikmund.Theme;

/// <summary>
/// Fluent elevation levels, named after what a surface is rather than how deep its shadow is.
/// </summary>
public enum MzElevationLevel
{
	None,
	Layer,
	Control,
	Card,
	Tooltip,
	Flyout,
	Dialog,
}

public static class MzElevation
{
	public static readonly DependencyProperty LevelProperty =
		DependencyProperty.RegisterAttached(
			"Level",
			typeof(MzElevationLevel),
			typeof(MzElevation),
			new PropertyMetadata(MzElevationLevel.None, OnLevelChanged));

	public static MzElevationLevel GetLevel(DependencyObject element) =>
		(MzElevationLevel)element.GetValue(LevelProperty);

	public static void SetLevel(DependencyObject element, MzElevationLevel value) =>
		element.SetValue(LevelProperty, value);

	private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
#if WINDOWS
		if (d is not FrameworkElement element)
		{
			return;
		}

		element.ActualThemeChanged -= OnActualThemeChanged;
		element.ActualThemeChanged += OnActualThemeChanged;
		Apply(element);
#endif
	}

#if WINDOWS
	private static void OnActualThemeChanged(FrameworkElement sender, object args) => Apply(sender);

	private static void Apply(FrameworkElement element)
	{
		var level = GetLevel(element);
		if (level == MzElevationLevel.None)
		{
			Effects.SetShadow(element, null!);
			return;
		}

		var isDark = element.ActualTheme == ElementTheme.Dark;

		// Composition gives one shadow per element, so each level uses its directional
		// shadow; the ambient companion is carried by the paired 1px stroke.
		var (offsetY, blur, opacity) = level switch
		{
			MzElevationLevel.Layer => (0d, 2d, isDark ? 0.24 : 0.12),
			MzElevationLevel.Control => (1d, 2d, isDark ? 0.28 : 0.14),
			MzElevationLevel.Card => (4d, 8d, isDark ? 0.28 : 0.14),
			MzElevationLevel.Tooltip => (8d, 16d, isDark ? 0.32 : 0.14),
			MzElevationLevel.Flyout => (14d, 28d, isDark ? 0.44 : 0.24),
			MzElevationLevel.Dialog => (32d, 64d, isDark ? 0.48 : 0.24),
			_ => (0d, 0d, 0d),
		};

		AttachedCardShadow shadow = new()
		{
			Color = Colors.Black,
			Offset = $"0,{offsetY.ToString(System.Globalization.CultureInfo.InvariantCulture)},0",
			BlurRadius = blur,
			Opacity = opacity,
			CornerRadius = element.CornerRadius.TopLeft,
		};

		Effects.SetShadow(element, shadow);
	}
#endif
}
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Theme/Elevation.xaml`**

A content surface is always all three: `MzCard` + `MzStrokeCard` + an elevation.
```xml
<ResourceDictionary
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:theme="using:MZikmund.Theme">

	<Style x:Key="MzLayerSurfaceStyle" TargetType="Border">
		<Setter Property="Background" Value="{ThemeResource MzCardBrush}" />
		<Setter Property="BorderBrush" Value="{ThemeResource MzStrokeCardBrush}" />
		<Setter Property="BorderThickness" Value="1" />
		<Setter Property="CornerRadius" Value="{StaticResource MzCardCornerRadius}" />
		<Setter Property="theme:MzElevation.Level" Value="Layer" />
	</Style>

	<Style x:Key="MzCardSurfaceStyle" TargetType="Border">
		<Setter Property="Background" Value="{ThemeResource MzCardBrush}" />
		<Setter Property="BorderBrush" Value="{ThemeResource MzStrokeCardBrush}" />
		<Setter Property="BorderThickness" Value="1" />
		<Setter Property="CornerRadius" Value="{StaticResource MzCardCornerRadius}" />
		<Setter Property="theme:MzElevation.Level" Value="Card" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Theme/MzElevation.cs src/app/MZikmund.App/Theme/Elevation.xaml
git commit -m "feat: Add the MzElevation attached property and the two content surfaces"
```

---

### Task 17: `Theme/Acrylic.xaml`

**Files:**
- Create: `src/app/MZikmund.App/Theme/Acrylic.xaml`

**Interfaces:**
- Consumes: `MzAcrylicTintColor`, `MzBgTertiaryBrush` (Task 2).
- Produces: `MzAcrylicBrush`.

- [ ] **Step 1: Create the file**

`AlwaysUseFallback="False"` is mandatory or the backdrop never renders. On Android, iOS and macOS an acrylic element must have **no children** — author it as a bare `Border` behind content, never as the background of a container that holds the content.
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<ResourceDictionary.ThemeDictionaries>

		<ResourceDictionary x:Key="Light">
			<AcrylicBrush
				x:Key="MzAcrylicBrush"
				AlwaysUseFallback="False"
				FallbackColor="#FFF9F9F9"
				TintColor="#FCFCFC"
				TintOpacity="0.85" />
		</ResourceDictionary>

		<ResourceDictionary x:Key="Default">
			<AcrylicBrush
				x:Key="MzAcrylicBrush"
				AlwaysUseFallback="False"
				FallbackColor="#FF282828"
				TintColor="#2C2C2C"
				TintOpacity="0.85" />
		</ResourceDictionary>

		<ResourceDictionary x:Key="HighContrast">
			<SolidColorBrush x:Key="MzAcrylicBrush" Color="{StaticResource SystemColorWindowColor}" />
		</ResourceDictionary>

	</ResourceDictionary.ThemeDictionaries>

</ResourceDictionary>
```

- [ ] **Step 2: Commit**
```bash
git add src/app/MZikmund.App/Theme/Acrylic.xaml
git commit -m "feat: Add the per-theme acrylic material brush"
```

---

### Task 18: `Theme/Styles/TextBlocks.xaml` and `Theme/Styles/Badges.xaml`

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/TextBlocks.xaml`
- Create: `src/app/MZikmund.App/Theme/Styles/Badges.xaml`

**Interfaces:**
- Consumes: the ramp styles (Task 4), `Mz*Brush` (Task 2), `MzBadgePadding` (Task 3), `MzControlCornerRadius` (Task 3).
- Produces: `MzCardTitleTextStyle` `MzCardExcerptTextStyle` `MzMetaTextStyle` `MzLeadTextStyle` `MzSectionHeadingTextStyle`; `MzBadgeAccentStyle` `MzBadgeSubtleStyle` `MzBadgeNeutralStyle` `MzBadgeOnPhotoStyle`.

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/Styles/TextBlocks.xaml`**

Card titles reset the tight tracking and take the 1.3 line height; tight tracking is for Title and above only.
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<Style x:Key="MzCardTitleTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzSubtitleTextStyle}">
		<Setter Property="LineHeight" Value="26" />
		<Setter Property="CharacterSpacing" Value="0" />
		<Setter Property="MaxLines" Value="2" />
		<Setter Property="TextTrimming" Value="CharacterEllipsis" />
	</Style>

	<Style x:Key="MzCardExcerptTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzBodyTextStyle}">
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
		<Setter Property="MaxLines" Value="3" />
		<Setter Property="TextTrimming" Value="CharacterEllipsis" />
	</Style>

	<Style x:Key="MzMetaTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzCaptionTextStyle}">
		<Setter Property="Foreground" Value="{ThemeResource MzTextTertiaryBrush}" />
		<Setter Property="TextWrapping" Value="NoWrap" />
		<Setter Property="TextTrimming" Value="CharacterEllipsis" />
	</Style>

	<Style x:Key="MzLeadTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzBodyLargeTextStyle}">
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
		<Setter Property="MaxWidth" Value="680" />
		<Setter Property="HorizontalAlignment" Value="Left" />
	</Style>

	<Style x:Key="MzSectionHeadingTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzSubtitleTextStyle}">
		<Setter Property="CharacterSpacing" Value="0" />
		<Setter Property="Margin" Value="0,0,0,12" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Theme/Styles/Badges.xaml`**

Badges are `ContentControl`s so `Foreground` is a real control property and reaches the text — a `Border` would let the card's hover accent bleed into the badge label. Radius 4, never a pill.
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<Style x:Key="MzBadgeBaseStyle" TargetType="ContentControl">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilySemiBold}" />
		<Setter Property="FontSize" Value="13" />
		<Setter Property="FontWeight" Value="SemiBold" />
		<Setter Property="CharacterSpacing" Value="20" />
		<Setter Property="Padding" Value="{StaticResource MzBadgePadding}" />
		<Setter Property="CornerRadius" Value="{StaticResource MzControlCornerRadius}" />
		<Setter Property="HorizontalAlignment" Value="Left" />
		<Setter Property="VerticalAlignment" Value="Top" />
		<Setter Property="IsTabStop" Value="False" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="ContentControl">
					<Border
						Background="{TemplateBinding Background}"
						BorderBrush="{TemplateBinding BorderBrush}"
						BorderThickness="{TemplateBinding BorderThickness}"
						CornerRadius="{TemplateBinding CornerRadius}">
						<ContentPresenter
							Padding="{TemplateBinding Padding}"
							HorizontalAlignment="Center"
							VerticalAlignment="Center"
							Content="{TemplateBinding Content}"
							ContentTemplate="{TemplateBinding ContentTemplate}" />
					</Border>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<Style x:Key="MzBadgeAccentStyle" TargetType="ContentControl" BasedOn="{StaticResource MzBadgeBaseStyle}">
		<Setter Property="Background" Value="{ThemeResource MzAccentFillBrush}" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextOnAccentBrush}" />
	</Style>

	<Style x:Key="MzBadgeSubtleStyle" TargetType="ContentControl" BasedOn="{StaticResource MzBadgeBaseStyle}">
		<Setter Property="Background" Value="{ThemeResource MzAccentSubtleStrongBrush}" />
		<Setter Property="Foreground" Value="{ThemeResource MzAccentTextBrush}" />
	</Style>

	<Style x:Key="MzBadgeNeutralStyle" TargetType="ContentControl" BasedOn="{StaticResource MzBadgeBaseStyle}">
		<Setter Property="Background" Value="{ThemeResource MzShimmerBrush}" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
	</Style>

	<!--  Over photography: theme-invariant scrim and white text, or half the archive is illegible.  -->
	<Style x:Key="MzBadgeOnPhotoStyle" TargetType="ContentControl" BasedOn="{StaticResource MzBadgeBaseStyle}">
		<Setter Property="Background" Value="{ThemeResource MzScrimBrush}" />
		<Setter Property="Foreground" Value="{ThemeResource MzOnScrimTextBrush}" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Theme/Styles/TextBlocks.xaml src/app/MZikmund.App/Theme/Styles/Badges.xaml
git commit -m "feat: Add semantic text styles and the badge family"
```

---

### Task 19: `Theme/Styles/Buttons.xaml`

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/Buttons.xaml`

**Interfaces:**
- Consumes: `Mz*Brush` (Task 2), padding/radius tokens (Task 3), font families (Task 4).
- Produces: the four variants and their six size variants.

- [ ] **Step 1: Create the file with the standard and accent variants**

Fluent presses down by a hair and never scales: the press translate is `TranslateY = 1` at **0 ms**. Uno has no `ScalarTransition`/`UIElement.Translation`, so this is a `CompositeTransform` in a `Storyboard`.
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<Style x:Key="MzStandardButtonStyle" TargetType="Button">
		<Setter Property="Background" Value="{ThemeResource MzCardBrush}" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="BorderBrush" Value="{ThemeResource MzControlElevationBorderBrush}" />
		<Setter Property="BorderThickness" Value="1" />
		<Setter Property="CornerRadius" Value="{StaticResource MzControlCornerRadius}" />
		<Setter Property="Padding" Value="{StaticResource MzButtonPadding}" />
		<Setter Property="MinHeight" Value="32" />
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilySemiBold}" />
		<Setter Property="FontSize" Value="17" />
		<Setter Property="FontWeight" Value="SemiBold" />
		<Setter Property="HorizontalContentAlignment" Value="Center" />
		<Setter Property="VerticalContentAlignment" Value="Center" />
		<Setter Property="UseSystemFocusVisuals" Value="False" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid x:Name="ButtonRoot">
						<Grid.RenderTransform>
							<CompositeTransform x:Name="ButtonTransform" />
						</Grid.RenderTransform>
						<Border
							x:Name="ButtonFill"
							Background="{TemplateBinding Background}"
							BorderBrush="{TemplateBinding BorderBrush}"
							BorderThickness="{TemplateBinding BorderThickness}"
							CornerRadius="{TemplateBinding CornerRadius}" />
						<ContentPresenter
							x:Name="ButtonContent"
							Padding="{TemplateBinding Padding}"
							HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
							VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
							Content="{TemplateBinding Content}"
							ContentTemplate="{TemplateBinding ContentTemplate}"
							Foreground="{TemplateBinding Foreground}" />
						<Border
							x:Name="FocusRing"
							Margin="-3"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="7"
							IsHitTestVisible="False"
							Visibility="Collapsed">
							<Border BorderBrush="{ThemeResource MzStrokeFocusInnerBrush}" BorderThickness="1" CornerRadius="5" />
						</Border>

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzBgSecondaryBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzBgSecondaryBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzStrokeControlBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextSecondaryBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<DoubleAnimation
											Storyboard.TargetName="ButtonTransform"
											Storyboard.TargetProperty="TranslateY"
											To="1"
											Duration="{StaticResource MzDurationInstant}" />
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzCardSecondaryBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Focused">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="FocusRing" Storyboard.TargetProperty="Visibility">
											<DiscreteObjectKeyFrame KeyTime="0" Value="Visible" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Unfocused" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>
```

- [ ] **Step 2: Append the accent variant**

The dark hover and active fills are light, so the foreground flips to black there — white on `#429CE3` is 2.96:1 and fails AA. `MzTextOnAccentPressedBrush` is white in light and black in dark.
```xml
	<Style x:Key="MzAccentButtonStyle" TargetType="Button" BasedOn="{StaticResource MzStandardButtonStyle}">
		<Setter Property="Background" Value="{ThemeResource MzAccentFillBrush}" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextOnAccentBrush}" />
		<Setter Property="BorderBrush" Value="{ThemeResource MzAccentControlElevationBorderBrush}" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid x:Name="ButtonRoot">
						<Grid.RenderTransform>
							<CompositeTransform x:Name="ButtonTransform" />
						</Grid.RenderTransform>
						<Border
							x:Name="ButtonFill"
							Background="{TemplateBinding Background}"
							BorderBrush="{TemplateBinding BorderBrush}"
							BorderThickness="{TemplateBinding BorderThickness}"
							CornerRadius="{TemplateBinding CornerRadius}" />
						<ContentPresenter
							x:Name="ButtonContent"
							Padding="{TemplateBinding Padding}"
							HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
							VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
							Content="{TemplateBinding Content}"
							ContentTemplate="{TemplateBinding ContentTemplate}"
							Foreground="{TemplateBinding Foreground}" />
						<Border
							x:Name="FocusRing"
							Margin="-3"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="7"
							IsHitTestVisible="False"
							Visibility="Collapsed">
							<Border BorderBrush="{ThemeResource MzStrokeFocusInnerBrush}" BorderThickness="1" CornerRadius="5" />
						</Border>

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentFillHoverBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextOnAccentPressedBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentFillActiveBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentFillActiveBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextOnAccentPressedBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<DoubleAnimation
											Storyboard.TargetName="ButtonTransform"
											Storyboard.TargetProperty="TranslateY"
											To="1"
											Duration="{StaticResource MzDurationInstant}" />
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzCardSecondaryBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Focused">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="FocusRing" Storyboard.TargetProperty="Visibility">
											<DiscreteObjectKeyFrame KeyTime="0" Value="Visible" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Unfocused" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>
```

- [ ] **Step 3: Append the subtle and accent-subtle variants and the size variants, then close the file**

Both reuse the standard template and only restate the hover fill through the `Background` setters, so no third and fourth template is needed: `MzSubtleButtonStyle` starts transparent and the template's `PointerOver` state paints `MzBgSecondaryBrush`, which for a subtle button is replaced below by an explicit template-free override on `Background`.
```xml
	<Style x:Key="MzSubtleButtonStyle" TargetType="Button" BasedOn="{StaticResource MzStandardButtonStyle}">
		<Setter Property="Background" Value="Transparent" />
		<Setter Property="BorderBrush" Value="Transparent" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid x:Name="ButtonRoot">
						<Grid.RenderTransform>
							<CompositeTransform x:Name="ButtonTransform" />
						</Grid.RenderTransform>
						<Border
							x:Name="ButtonFill"
							Background="{TemplateBinding Background}"
							BorderBrush="{TemplateBinding BorderBrush}"
							BorderThickness="{TemplateBinding BorderThickness}"
							CornerRadius="{TemplateBinding CornerRadius}" />
						<ContentPresenter
							x:Name="ButtonContent"
							Padding="{TemplateBinding Padding}"
							HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
							VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
							Content="{TemplateBinding Content}"
							ContentTemplate="{TemplateBinding ContentTemplate}"
							Foreground="{TemplateBinding Foreground}" />
						<Border
							x:Name="FocusRing"
							Margin="-3"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="7"
							IsHitTestVisible="False"
							Visibility="Collapsed">
							<Border BorderBrush="{ThemeResource MzStrokeFocusInnerBrush}" BorderThickness="1" CornerRadius="5" />
						</Border>

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzShimmerBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzShimmerBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextSecondaryBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<DoubleAnimation
											Storyboard.TargetName="ButtonTransform"
											Storyboard.TargetProperty="TranslateY"
											To="1"
											Duration="{StaticResource MzDurationInstant}" />
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="ButtonContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Focused">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="FocusRing" Storyboard.TargetProperty="Visibility">
											<DiscreteObjectKeyFrame KeyTime="0" Value="Visible" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Unfocused" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<Style x:Key="MzAccentSubtleButtonStyle" TargetType="Button" BasedOn="{StaticResource MzSubtleButtonStyle}">
		<Setter Property="Background" Value="{ThemeResource MzAccentSubtleBrush}" />
		<Setter Property="Foreground" Value="{ThemeResource MzAccentTextBrush}" />
	</Style>

	<Style x:Key="MzStandardButtonSmallStyle" TargetType="Button" BasedOn="{StaticResource MzStandardButtonStyle}">
		<Setter Property="Padding" Value="{StaticResource MzButtonSmallPadding}" />
		<Setter Property="MinHeight" Value="28" />
		<Setter Property="FontSize" Value="13" />
	</Style>

	<Style x:Key="MzStandardButtonLargeStyle" TargetType="Button" BasedOn="{StaticResource MzStandardButtonStyle}">
		<Setter Property="Padding" Value="{StaticResource MzButtonLargePadding}" />
		<Setter Property="MinHeight" Value="44" />
		<Setter Property="FontSize" Value="19" />
	</Style>

	<Style x:Key="MzAccentButtonSmallStyle" TargetType="Button" BasedOn="{StaticResource MzAccentButtonStyle}">
		<Setter Property="Padding" Value="{StaticResource MzButtonSmallPadding}" />
		<Setter Property="MinHeight" Value="28" />
		<Setter Property="FontSize" Value="13" />
	</Style>

	<Style x:Key="MzAccentButtonLargeStyle" TargetType="Button" BasedOn="{StaticResource MzAccentButtonStyle}">
		<Setter Property="Padding" Value="{StaticResource MzButtonLargePadding}" />
		<Setter Property="MinHeight" Value="44" />
		<Setter Property="FontSize" Value="19" />
	</Style>

	<Style x:Key="MzSubtleButtonSmallStyle" TargetType="Button" BasedOn="{StaticResource MzSubtleButtonStyle}">
		<Setter Property="Padding" Value="{StaticResource MzButtonSmallPadding}" />
		<Setter Property="MinHeight" Value="28" />
		<Setter Property="FontSize" Value="13" />
	</Style>

	<Style x:Key="MzAccentSubtleButtonSmallStyle" TargetType="Button" BasedOn="{StaticResource MzAccentSubtleButtonStyle}">
		<Setter Property="Padding" Value="{StaticResource MzButtonSmallPadding}" />
		<Setter Property="MinHeight" Value="28" />
		<Setter Property="FontSize" Value="13" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Theme/Styles/Buttons.xaml
git commit -m "feat: Add the Mz button family with the Fluent elevation border and press"
```

---

### Task 20: `ICardItem` and `Theme/Styles/Cards.xaml`

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/ICardItem.cs`
- Create: `src/app/MZikmund.App/Theme/Styles/Cards.xaml`

**Interfaces:**
- Consumes: `Mz*Brush` (Task 2), radii/spacing (Task 3), `MzCardTitleTextStyle` / `MzCardExcerptTextStyle` / `MzMetaTextStyle` (Task 7), `MzBadgeOnPhotoStyle` / `MzBadgeSubtleStyle` (Task 7), motion tokens (Task 3), `MzGlyph*` (Task 1), `InverseNullToVisibilityConverter` / `NullToVisibilityConverter` (existing `Resources/Converters.xaml`).
- Produces: `ICardItem`, `MzContentCardStyle`.

- [ ] **Step 1: Create `src/app/MZikmund.App.Core/ViewModels/Items/ICardItem.cs`**

The card's hover storyboard has to reach the title and the image by name, so the anatomy lives in the `ControlTemplate` and binds through `DataContext`. This interface is that contract; `VideosView` adopts it in Phase 3 so both grids share one style.
```csharp
namespace MZikmund.ViewModels.Items;

public interface ICardItem
{
	ImageSource? CardImage { get; }

	bool HasCardImage { get; }

	string CardTitle { get; }

	string CardSummary { get; }

	string CardMeta { get; }

	IReadOnlyList<string> CardBadges { get; }
}
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Theme/Styles/Cards.xaml` — the style header and card chrome**

The media is 180px tall, which is 16:9 at the 320px minimum card width. The title sits below the image on a solid surface, never overlaid.
```xml
<ResourceDictionary
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:theme="using:MZikmund.Theme">

	<Style x:Key="MzContentCardStyle" TargetType="Button">
		<Setter Property="Background" Value="{ThemeResource MzCardBrush}" />
		<Setter Property="BorderBrush" Value="{ThemeResource MzStrokeCardBrush}" />
		<Setter Property="BorderThickness" Value="1" />
		<Setter Property="CornerRadius" Value="{StaticResource MzCardCornerRadius}" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="Padding" Value="0" />
		<Setter Property="HorizontalAlignment" Value="Stretch" />
		<Setter Property="VerticalAlignment" Value="Stretch" />
		<Setter Property="HorizontalContentAlignment" Value="Stretch" />
		<Setter Property="VerticalContentAlignment" Value="Stretch" />
		<Setter Property="UseSystemFocusVisuals" Value="False" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid x:Name="CardRoot">
						<Grid.RenderTransform>
							<CompositeTransform x:Name="CardTransform" />
						</Grid.RenderTransform>
						<Border
							x:Name="CardBorder"
							Background="{TemplateBinding Background}"
							BorderBrush="{TemplateBinding BorderBrush}"
							BorderThickness="{TemplateBinding BorderThickness}"
							CornerRadius="{TemplateBinding CornerRadius}"
							theme:MzElevation.Level="Card">
							<Grid>
								<Grid.RowDefinitions>
									<RowDefinition Height="Auto" />
									<RowDefinition Height="*" />
									<RowDefinition Height="Auto" />
								</Grid.RowDefinitions>

								<!--  Media: 16:9 at the 320px minimum card width.  -->
								<Border
									Grid.Row="0"
									Height="180"
									Background="{ThemeResource MzBgSecondaryBrush}"
									CornerRadius="{StaticResource MzCardTopCornerRadius}">
									<Grid>
										<Border Background="{ThemeResource MzCardMediaEmptyBrush}" Visibility="{Binding HasCardImage, Converter={StaticResource InverseBoolToVisibilityConverter}}">
											<FontIcon
												FontFamily="{StaticResource MzIconFontFamily}"
												FontSize="32"
												Foreground="{ThemeResource MzTextTertiaryBrush}"
												Glyph="{StaticResource MzGlyphImageOff}" />
										</Border>
										<Image
											x:Name="CardImage"
											RenderTransformOrigin="0.5,0.5"
											Source="{Binding CardImage}"
											Stretch="UniformToFill"
											Visibility="{Binding HasCardImage, Converter={StaticResource BoolToVisibilityConverter}}">
											<Image.RenderTransform>
												<CompositeTransform x:Name="CardImageTransform" />
											</Image.RenderTransform>
										</Image>

										<!--  Over a photo the badge carries its own scrim; over the tinted placeholder it does not.  -->
										<ItemsControl
											Margin="12"
											HorizontalAlignment="Left"
											VerticalAlignment="Top"
											IsHitTestVisible="False"
											ItemsSource="{Binding CardBadges}"
											Visibility="{Binding HasCardImage, Converter={StaticResource BoolToVisibilityConverter}}">
											<ItemsControl.ItemsPanel>
												<ItemsPanelTemplate>
													<StackPanel Orientation="Horizontal" Spacing="4" />
												</ItemsPanelTemplate>
											</ItemsControl.ItemsPanel>
											<ItemsControl.ItemTemplate>
												<DataTemplate>
													<ContentControl Content="{Binding}" Style="{StaticResource MzBadgeOnPhotoStyle}" />
												</DataTemplate>
											</ItemsControl.ItemTemplate>
										</ItemsControl>
										<ItemsControl
											Margin="12"
											HorizontalAlignment="Left"
											VerticalAlignment="Top"
											IsHitTestVisible="False"
											ItemsSource="{Binding CardBadges}"
											Visibility="{Binding HasCardImage, Converter={StaticResource InverseBoolToVisibilityConverter}}">
											<ItemsControl.ItemsPanel>
												<ItemsPanelTemplate>
													<StackPanel Orientation="Horizontal" Spacing="4" />
												</ItemsPanelTemplate>
											</ItemsControl.ItemsPanel>
											<ItemsControl.ItemTemplate>
												<DataTemplate>
													<ContentControl Content="{Binding}" Style="{StaticResource MzBadgeSubtleStyle}" />
												</DataTemplate>
											</ItemsControl.ItemTemplate>
										</ItemsControl>
									</Grid>
								</Border>
```

- [ ] **Step 3: Append the card body, footer and focus ring**
```xml
								<StackPanel
									Grid.Row="1"
									Padding="{StaticResource MzCardPadding}"
									Spacing="8">
									<TextBlock
										x:Name="CardTitle"
										Foreground="{ThemeResource MzTextPrimaryBrush}"
										Style="{StaticResource MzCardTitleTextStyle}"
										Text="{Binding CardTitle}" />
									<StackPanel Orientation="Horizontal" Spacing="4">
										<FontIcon
											VerticalAlignment="Center"
											FontFamily="{StaticResource MzIconFontFamily}"
											FontSize="13"
											Foreground="{ThemeResource MzTextTertiaryBrush}"
											Glyph="{StaticResource MzGlyphCalendar}" />
										<TextBlock Style="{StaticResource MzMetaTextStyle}" Text="{Binding CardMeta}" />
									</StackPanel>
									<TextBlock Style="{StaticResource MzCardExcerptTextStyle}" Text="{Binding CardSummary}" />
								</StackPanel>

								<!--  Decorative: the whole card is the button, so this must not be a nested Button.  -->
								<Border
									Grid.Row="2"
									Margin="16,0,16,16"
									Padding="{StaticResource MzButtonSmallPadding}"
									HorizontalAlignment="Left"
									Background="{ThemeResource MzAccentSubtleBrush}"
									CornerRadius="{StaticResource MzControlCornerRadius}">
									<StackPanel Orientation="Horizontal" Spacing="4">
										<TextBlock
											VerticalAlignment="Center"
											FontFamily="{StaticResource MzFontFamilySemiBold}"
											FontSize="13"
											FontWeight="SemiBold"
											Foreground="{ThemeResource MzAccentTextBrush}"
											Text="{Binding ReadMoreText, RelativeSource={RelativeSource TemplatedParent}, FallbackValue=''}" />
										<FontIcon
											x:Name="ReadMoreArrow"
											VerticalAlignment="Center"
											FontFamily="{StaticResource MzIconFontFamily}"
											FontSize="13"
											Foreground="{ThemeResource MzAccentTextBrush}"
											Glyph="{StaticResource MzGlyphChevronRight}">
											<FontIcon.RenderTransform>
												<CompositeTransform x:Name="ReadMoreArrowTransform" />
											</FontIcon.RenderTransform>
										</FontIcon>
									</StackPanel>
								</Border>
							</Grid>
						</Border>

						<Border
							x:Name="FocusRing"
							Margin="-3"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="11"
							IsHitTestVisible="False"
							Visibility="Collapsed">
							<Border BorderBrush="{ThemeResource MzStrokeFocusInnerBrush}" BorderThickness="1" CornerRadius="9" />
						</Border>
```
Then replace the read-more `TextBlock`'s `Text` binding with the localized literal — `Tag` carries it so the style stays view-agnostic:
```xml
										<TextBlock
											VerticalAlignment="Center"
											FontFamily="{StaticResource MzFontFamilySemiBold}"
											FontSize="13"
											FontWeight="SemiBold"
											Foreground="{ThemeResource MzAccentTextBrush}"
											Text="{TemplateBinding Tag}" />
```
The call site sets `Tag="{extensions:Localize Key=ReadMore}"` on the card `Button`.

- [ ] **Step 4: Append the VisualStateManager and close the file**

Rise `-2px` (not `-8px` — that reads as Material), border to `MzStrokeControlStrong`, title to `MzAccentText`, image `scale 1.03` over 333 ms, elevation Card → Tooltip.
```xml
						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<VisualState.Setters>
										<Setter Target="CardBorder.(theme:MzElevation.Level)" Value="Tooltip" />
									</VisualState.Setters>
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="CardTransform" Storyboard.TargetProperty="TranslateY">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationNormal}"
												Value="-2" />
										</DoubleAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="CardBorder" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzStrokeControlStrongBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="CardTitle" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="CardImageTransform" Storyboard.TargetProperty="ScaleX">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationSlow}"
												Value="1.03" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="CardImageTransform" Storyboard.TargetProperty="ScaleY">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationSlow}"
												Value="1.03" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="ReadMoreArrowTransform" Storyboard.TargetProperty="TranslateX">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseEntrance}"
												KeyTime="{StaticResource MzDurationNormal}"
												Value="3" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<DoubleAnimation
											Storyboard.TargetName="CardTransform"
											Storyboard.TargetProperty="TranslateY"
											To="1"
											Duration="{StaticResource MzDurationInstant}" />
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="CardBorder" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzStrokeControlStrongBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="CardTitle" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="CardRoot" Storyboard.TargetProperty="Opacity">
											<DiscreteObjectKeyFrame KeyTime="0" Value="0.55" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Focused">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="FocusRing" Storyboard.TargetProperty="Visibility">
											<DiscreteObjectKeyFrame KeyTime="0" Value="Visible" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Unfocused" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

</ResourceDictionary>
```

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Items/ICardItem.cs src/app/MZikmund.App/Theme/Styles/Cards.xaml
git commit -m "feat: Add the shared content card style with its hover, press and focus states"
```

---

### Task 21: `Theme/Styles/EmptyState.xaml` and App.xaml wiring

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/EmptyState.xaml`
- Modify: `src/app/MZikmund.App/App.xaml:1-21`
- Delete: `src/app/MZikmund.App/Resources/Colors.xaml`, `src/app/MZikmund.App/Resources/Fonts.xaml`

**Interfaces:**
- Consumes: everything above.
- Produces: `MzEmptyStatePanelStyle` `MzEmptyStateIconStyle` `MzEmptyStateTitleTextStyle` `MzEmptyStateBodyTextStyle`; the merged application resource graph.

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/Styles/EmptyState.xaml`**
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<Style x:Key="MzEmptyStatePanelStyle" TargetType="StackPanel">
		<Setter Property="Padding" Value="16,64,16,64" />
		<Setter Property="Spacing" Value="12" />
		<Setter Property="HorizontalAlignment" Value="Center" />
		<Setter Property="VerticalAlignment" Value="Center" />
	</Style>

	<Style x:Key="MzEmptyStateIconStyle" TargetType="FontIcon">
		<Setter Property="FontFamily" Value="{StaticResource MzIconFontFamily}" />
		<Setter Property="FontSize" Value="40" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextTertiaryBrush}" />
		<Setter Property="HorizontalAlignment" Value="Center" />
	</Style>

	<Style x:Key="MzEmptyStateTitleTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzBodyLargeTextStyle}">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilySemiBold}" />
		<Setter Property="FontWeight" Value="SemiBold" />
		<Setter Property="TextAlignment" Value="Center" />
		<Setter Property="HorizontalAlignment" Value="Center" />
	</Style>

	<Style x:Key="MzEmptyStateBodyTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzBodyTextStyle}">
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
		<Setter Property="MaxWidth" Value="420" />
		<Setter Property="TextAlignment" Value="Center" />
		<Setter Property="HorizontalAlignment" Value="Center" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 2: Replace `src/app/MZikmund.App/App.xaml` in full**

Merge order matters: tokens before styles, and `Converters.xaml` before `Cards.xaml`, which resolves its converters by `StaticResource`.
```xml
<Application
    x:Class="MZikmund.App.MZikmundApp"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!--  Load WinUI resources  -->
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />

                <!--  Tokens  -->
                <ResourceDictionary Source="ms-appx:///Theme/Colors.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Spacing.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Radii.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Motion.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Icons.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Typography.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Acrylic.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Elevation.xaml" />

                <ResourceDictionary Source="ms-appx:///Resources/Converters.xaml" />

                <!--  Styles  -->
                <ResourceDictionary Source="ms-appx:///Theme/Styles/TextBlocks.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Badges.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Buttons.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Cards.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Styles/EmptyState.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>

</Application>
```

- [ ] **Step 3: Delete the superseded dictionaries**
```bash
git rm src/app/MZikmund.App/Resources/Colors.xaml src/app/MZikmund.App/Resources/Fonts.xaml
```
`BrandColor`, `BrandBrush` and `CodeFont` all survive as keys in `Theme/Colors.xaml` and `Theme/Typography.xaml`, so `Resources/ColorResoruces.cs` and `Views/Admin/PostEditorView.xaml` keep resolving.

- [ ] **Step 4: Build the desktop head / Expected: `Build succeeded`**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```
If a `StaticResource` fails to resolve, the XAML compiler reports the key by name — check that its defining dictionary is merged earlier in `App.xaml`.

- [ ] **Step 5: Build the Windows head / Expected: `Build succeeded`**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```
This is the only head that compiles the `#if WINDOWS` branch of `MzElevation`, so it is the one that proves `Effects.SetShadow` and `AttachedCardShadow` resolve.

- [ ] **Step 6: Commit**
```bash
git add src/app/MZikmund.App/Theme/Styles/EmptyState.xaml src/app/MZikmund.App/App.xaml src/app/MZikmund.App/Resources
git commit -m "feat: Merge the Theme token and style library into App.xaml"
```

---

### Task 22: Localized strings for `en` and `cs`

**Files:**
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw:314` (before `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (before `</root>`)

**Interfaces:**
- Produces: `Blog` (cs), `Videos`, `ReadMore`, `Retry`, `Refresh`, `NoPostsFound`, `NoPostsFoundDescription`, `BlogLead`, `WatchOnYouTube`, `Id`.

- [ ] **Step 1: Add the keys to `src/app/MZikmund.App/Strings/en/Resources.resw`**

Insert immediately before the closing `</root>`:
```xml
  <data name="Videos" xml:space="preserve">
    <value>Videos</value>
  </data>
  <data name="ReadMore" xml:space="preserve">
    <value>Read more</value>
  </data>
  <data name="Retry" xml:space="preserve">
    <value>Try again</value>
  </data>
  <data name="Refresh" xml:space="preserve">
    <value>Refresh</value>
  </data>
  <data name="NoPostsFound" xml:space="preserve">
    <value>No posts yet</value>
  </data>
  <data name="NoPostsFoundDescription" xml:space="preserve">
    <value>There is nothing published here at the moment. Check back soon.</value>
  </data>
  <data name="BlogLead" xml:space="preserve">
    <value>Articles on .NET, Windows and cross-platform development.</value>
  </data>
  <data name="WatchOnYouTube" xml:space="preserve">
    <value>Watch on YouTube</value>
  </data>
  <data name="Id" xml:space="preserve">
    <value>Id</value>
  </data>
```

- [ ] **Step 2: Add the keys to `src/app/MZikmund.App/Strings/cs/Resources.resw`**

`Blog` is missing from the Czech file today, so `BlogViewModel.Title` renders `???Blog???`. Insert before the closing `</root>`:
```xml
  <data name="Blog" xml:space="preserve">
    <value>Blog</value>
  </data>
  <data name="Videos" xml:space="preserve">
    <value>Videa</value>
  </data>
  <data name="ReadMore" xml:space="preserve">
    <value>Číst dál</value>
  </data>
  <data name="Retry" xml:space="preserve">
    <value>Zkusit znovu</value>
  </data>
  <data name="Refresh" xml:space="preserve">
    <value>Obnovit</value>
  </data>
  <data name="NoPostsFound" xml:space="preserve">
    <value>Zatím žádné příspěvky</value>
  </data>
  <data name="NoPostsFoundDescription" xml:space="preserve">
    <value>Momentálně tu není nic publikováno. Zkuste to prosím později.</value>
  </data>
  <data name="BlogLead" xml:space="preserve">
    <value>Články o .NET, Windows a multiplatformním vývoji.</value>
  </data>
  <data name="WatchOnYouTube" xml:space="preserve">
    <value>Přehrát na YouTube</value>
  </data>
  <data name="Id" xml:space="preserve">
    <value>Id</value>
  </data>
```
The Czech file is still missing `AddCategory`, `AddPost`, `AddTag`, `Admin`, `Cancel`, `Contact`, `DisplayName`, `EditCategory`, `EditTag`, `Home`, `Localizations`, `NoCategoriesSelected`, `Ok`, `Posts`, `RouteName`, `Settings`, `Login` and `Logout` — those belong to the Phase 5 localization sweep, not here.

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Strings
git commit -m "feat: Add en and cs strings for the blog view and the missing Videos key"
```

---

### Task 23: `IncrementalLoadingCollection` and the BlogViewModel rewrite

**Files:**
- Create: `src/app/MZikmund.App.Core/Collections/IncrementalLoadingCollection.cs`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Items/PostListItemViewModel.cs:1-24`
- Modify: `src/app/MZikmund.App.Core/ViewModels/BlogViewModel.cs:1-137`

**Interfaces:**
- Consumes: `ICardItem` (Task 9).
- Produces: `IncrementalPage<T>`, `IncrementalLoadingCollection<T>`, the new `BlogViewModel` surface.

- [ ] **Step 1: Create `src/app/MZikmund.App.Core/Collections/IncrementalLoadingCollection.cs`**

`AsyncInfo.Run` is implemented on every Uno head — Uno's own `ListView` samples use exactly this shape. `ItemsRepeater` does not consume `ISupportIncrementalLoading` on its own, so the view calls `LoadMoreItemsAsync` when the scroller nears the end; the interface keeps one paging code path instead of the infinite-scroll-*and*-Load-More duplication.
```csharp
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace MZikmund.Collections;

public readonly record struct IncrementalPage<T>(IReadOnlyList<T> Items, bool HasMoreItems);

public sealed class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
{
	private readonly Func<int, CancellationToken, Task<IncrementalPage<T>>> _pageLoader;
	private int _nextPage = 1;
	private bool _hasMoreItems = true;
	private bool _isLoading;

	public IncrementalLoadingCollection(Func<int, CancellationToken, Task<IncrementalPage<T>>> pageLoader)
	{
		_pageLoader = pageLoader ?? throw new ArgumentNullException(nameof(pageLoader));
	}

	public bool HasMoreItems => _hasMoreItems && !_isLoading;

	public void Reset()
	{
		Clear();
		_nextPage = 1;
		_hasMoreItems = true;
	}

	public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) =>
		AsyncInfo.Run(LoadMoreCoreAsync);

	private async Task<LoadMoreItemsResult> LoadMoreCoreAsync(CancellationToken cancellationToken)
	{
		if (_isLoading || !_hasMoreItems)
		{
			return new LoadMoreItemsResult { Count = 0 };
		}

		_isLoading = true;
		try
		{
			var page = await _pageLoader(_nextPage, cancellationToken);
			foreach (var item in page.Items)
			{
				Add(item);
			}

			_nextPage++;
			_hasMoreItems = page.HasMoreItems;
			return new LoadMoreItemsResult { Count = (uint)page.Items.Count };
		}
		finally
		{
			_isLoading = false;
		}
	}
}
```

- [ ] **Step 2: Replace `src/app/MZikmund.App.Core/ViewModels/Items/PostListItemViewModel.cs` in full**
```csharp
using Humanizer;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels.Items;

public sealed class PostListItemViewModel : ObservableObject, ICardItem
{
	private readonly IMarkdownConverter _markdownConverter;
	private ImageSource? _cardImage;

	public PostListItemViewModel(PostListItem item, IMarkdownConverter markdownConverter)
	{
		Item = item;
		_markdownConverter = markdownConverter;
	}

	public PostListItem Item { get; }

	public Uri? HeroImageUri => Item?.HeroImageUrl is not null ? new Uri(Item.HeroImageUrl) : null;

	public ImageSource? CardImage => _cardImage ??= HeroImageUri is { } uri ? new BitmapImage(uri) : null;

	public bool HasCardImage => HeroImageUri is not null;

	public string CardTitle => Item.Title;

	public string CardSummary => _markdownConverter.ToPlainText(Item.Abstract);

	public string CardMeta => (Item.PublishedDate ?? Item.LastModifiedDate)?.Humanize() ?? "";

	public IReadOnlyList<string> CardBadges => Item.Categories.Select(category => category.DisplayName).ToArray();
}
```

- [ ] **Step 3: Replace `src/app/MZikmund.App.Core/ViewModels/BlogViewModel.cs` in full**
```csharp
using System.Windows.Input;
using MZikmund.Api.Client;
using MZikmund.Collections;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public class BlogViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly INavigationService _navigationService;
	private readonly ILogger<BlogViewModel> _logger;
	private bool _isLoading;
	private bool _isLoadingMore;
	private bool _hasError;

	public BlogViewModel(
		IMZikmundApi api,
		IMarkdownConverter markdownConverter,
		INavigationService navigationService,
		ILogger<BlogViewModel> logger)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_markdownConverter = markdownConverter;
		_navigationService = navigationService;
		_logger = logger;
		Posts = new IncrementalLoadingCollection<PostListItemViewModel>(LoadPageAsync);
		RetryCommand = new AsyncRelayCommand(ReloadAsync);
	}

	public override string Title => Localizer.Instance.GetString("Blog");

	public string Lead => Localizer.Instance.GetString("BlogLead");

	public IncrementalLoadingCollection<PostListItemViewModel> Posts { get; }

	public ICommand RetryCommand { get; }

	public bool IsLoading
	{
		get => _isLoading;
		private set
		{
			SetProperty(ref _isLoading, value);
			OnPropertyChanged(nameof(IsEmpty));
		}
	}

	public bool IsLoadingMore
	{
		get => _isLoadingMore;
		private set => SetProperty(ref _isLoadingMore, value);
	}

	public bool HasError
	{
		get => _hasError;
		private set
		{
			SetProperty(ref _hasError, value);
			OnPropertyChanged(nameof(IsEmpty));
		}
	}

	public bool IsEmpty => !IsLoading && !HasError && Posts.Count == 0;

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		await ReloadAsync();
	}

	public void NavigateToPost(Guid postId) => _navigationService.Navigate<PostViewModel>(postId);

	private async Task ReloadAsync()
	{
		HasError = false;
		IsLoading = true;
		Posts.Reset();
		try
		{
			await Posts.LoadMoreItemsAsync(0);
		}
		finally
		{
			IsLoading = false;
			OnPropertyChanged(nameof(IsEmpty));
		}
	}

	private async Task<IncrementalPage<PostListItemViewModel>> LoadPageAsync(int pageNumber, CancellationToken cancellationToken)
	{
		IsLoadingMore = pageNumber > 1;
		try
		{
			var response = await _api.GetPostsAsync(pageNumber);
			if (response.Content is not { } content)
			{
				HasError = true;
				return new IncrementalPage<PostListItemViewModel>(Array.Empty<PostListItemViewModel>(), false);
			}

			var items = content.Data.Select(post => new PostListItemViewModel(post, _markdownConverter)).ToArray();
			var totalPages = content.PageSize > 0
				? (int)Math.Ceiling((double)content.TotalCount / content.PageSize)
				: 1;

			return new IncrementalPage<PostListItemViewModel>(items, content.PageNumber < totalPages);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading posts page {PageNumber} failed", pageNumber);
			HasError = true;
			return new IncrementalPage<PostListItemViewModel>(Array.Empty<PostListItemViewModel>(), false);
		}
		finally
		{
			IsLoadingMore = false;
			OnPropertyChanged(nameof(IsEmpty));
		}
	}
}
```
`ILoadingIndicator` is deliberately gone from this ViewModel: the page now owns its loading, empty and error states instead of blanking the whole shell.

- [ ] **Step 4: Build the desktop head / Expected: `Build succeeded`**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```
This will fail on `BlogView.xaml` until Task 13 lands — expect errors only in `Views/BlogView.xaml` (`PublishedDate`, `AbstractPlain`, `LoadMoreCommand`, `CanLoadMore`, `IsLoadingMore`). Any error inside `MZikmund.App.Core` must be fixed before moving on.

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App.Core
git commit -m "refactor: Move blog paging onto an ISupportIncrementalLoading collection"
```

---

### Task 24: Rebuild `BlogView`

**Files:**
- Modify: `src/app/MZikmund.App/Views/BlogView.xaml:1-117` (full replacement)
- Modify: `src/app/MZikmund.App/Views/BlogView.xaml.cs:1-43` (full replacement)

**Interfaces:**
- Consumes: `MzContentCardStyle`, `MzEmptyState*`, `MzStandardButtonStyle`, `MzTitleTextStyle` / `MzTitleWideTextStyle`, `MzLeadTextStyle`, `MzPagePadding*`, `MzGlyph*`, the new `BlogViewModel` surface.
- Produces: nothing downstream.

- [ ] **Step 1: Replace `src/app/MZikmund.App/Views/BlogView.xaml` in full**

`GridView` does not virtualize on Skia, so the grid is `ItemsRepeater` + `UniformGridLayout` with `MinItemWidth="320"`, 24px spacing on both axes and `ItemsStretch="Fill"`. The `MzTypeScale` group swaps the page title to the wide tier at 768.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<local:BlogViewBase
    x:Class="MZikmund.Views.BlogView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:extensions="using:MZikmund.Extensions.Xaml"
    xmlns:local="using:MZikmund.Views"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:vmitems="using:MZikmund.ViewModels.Items"
    Background="{ThemeResource MzBgBaseBrush}"
    mc:Ignorable="d">

    <Grid x:Name="PageRoot" Padding="{StaticResource MzPagePaddingCompact}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!--  Page header: title then lead. The shell no longer draws the title.  -->
        <StackPanel
            Grid.Row="0"
            Margin="0,0,0,24"
            Spacing="8">
            <TextBlock
                x:Name="PageTitle"
                Style="{StaticResource MzTitleTextStyle}"
                Text="{x:Bind ViewModel.Title}" />
            <TextBlock Style="{StaticResource MzLeadTextStyle}" Text="{x:Bind ViewModel.Lead}" />
        </StackPanel>

        <ScrollViewer
            x:Name="Scroller"
            Grid.Row="1"
            Padding="0,0,0,24"
            ViewChanged="Scroller_ViewChanged">
            <StackPanel Spacing="24">
                <ItemsRepeater ItemsSource="{x:Bind ViewModel.Posts, Mode=OneWay}">
                    <ItemsRepeater.Layout>
                        <UniformGridLayout
                            ItemsStretch="Fill"
                            MinColumnSpacing="24"
                            MinItemHeight="420"
                            MinItemWidth="320"
                            MinRowSpacing="24" />
                    </ItemsRepeater.Layout>
                    <ItemsRepeater.ItemTemplate>
                        <DataTemplate x:DataType="vmitems:PostListItemViewModel">
                            <Button
                                AutomationProperties.Name="{x:Bind CardTitle}"
                                Click="PostCard_Click"
                                Style="{StaticResource MzContentCardStyle}"
                                Tag="{extensions:Localize Key=ReadMore}" />
                        </DataTemplate>
                    </ItemsRepeater.ItemTemplate>
                </ItemsRepeater>

                <ProgressRing
                    HorizontalAlignment="Center"
                    IsActive="{x:Bind ViewModel.IsLoadingMore, Mode=OneWay}"
                    Visibility="{x:Bind ViewModel.IsLoadingMore, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />
            </StackPanel>
        </ScrollViewer>

        <ProgressRing
            Grid.Row="1"
            Width="48"
            Height="48"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
            Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />

        <StackPanel
            Grid.Row="1"
            Style="{StaticResource MzEmptyStatePanelStyle}"
            Visibility="{x:Bind ViewModel.IsEmpty, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
            <FontIcon Glyph="{StaticResource MzGlyphNews}" Style="{StaticResource MzEmptyStateIconStyle}" />
            <TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{extensions:Localize Key=NoPostsFound}" />
            <TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{extensions:Localize Key=NoPostsFoundDescription}" />
        </StackPanel>

        <StackPanel
            Grid.Row="1"
            Style="{StaticResource MzEmptyStatePanelStyle}"
            Visibility="{x:Bind ViewModel.HasError, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
            <FontIcon Glyph="{StaticResource MzGlyphWarning}" Style="{StaticResource MzEmptyStateIconStyle}" />
            <TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{extensions:Localize Key=CouldNotLoadData}" />
            <TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{extensions:Localize Key=ErrorLoadingData}" />
            <Button
                HorizontalAlignment="Center"
                Command="{x:Bind ViewModel.RetryCommand, Mode=OneWay}"
                Content="{extensions:Localize Key=Retry}"
                Style="{StaticResource MzStandardButtonStyle}" />
        </StackPanel>

        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="MzTypeScale">
                <VisualState x:Name="MzTypeScaleWide">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="768" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="PageTitle.Style" Value="{StaticResource MzTitleWideTextStyle}" />
                        <Setter Target="PageRoot.Padding" Value="{StaticResource MzPagePaddingWide}" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="MzTypeScaleCompact" />
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    </Grid>
</local:BlogViewBase>
```

- [ ] **Step 2: Replace `src/app/MZikmund.App/Views/BlogView.xaml.cs` in full**

The card is a real `Button`, so keyboard activation and focus visuals come for free — the old code-behind `Tapped` handler had neither.
```csharp
using MZikmund.ViewModels;
using MZikmund.ViewModels.Items;

namespace MZikmund.Views;

public sealed partial class BlogView : BlogViewBase
{
	private const double LoadMoreThreshold = 400;

	public BlogView()
	{
		this.InitializeComponent();
	}

	private void PostCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PostListItemViewModel item })
		{
			ViewModel?.NavigateToPost(item.Item.Id);
		}
	}

	private async void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
	{
		if (ViewModel is not { } viewModel || !viewModel.Posts.HasMoreItems)
		{
			return;
		}

		if (Scroller.ScrollableHeight - Scroller.VerticalOffset > LoadMoreThreshold)
		{
			return;
		}

		await viewModel.Posts.LoadMoreItemsAsync(0);
	}
}

public partial class BlogViewBase : PageBase<BlogViewModel>
{
}
```

- [ ] **Step 3: Build the desktop head / Expected: `Build succeeded`**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Views/BlogView.xaml src/app/MZikmund.App/Views/BlogView.xaml.cs
git commit -m "feat: Rebuild BlogView on the Mz card vocabulary"
```

---

### Task 25: Verify every head, verify both themes, format

**Files:**
- Modify: none unless verification finds defects.

**Interfaces:**
- Consumes: everything.
- Produces: a green Phase 1.

- [ ] **Step 1: Build the four local heads / Expected: four × `Build succeeded`**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
```
WASM is CI-verified only — this machine has no usable Emscripten pack.

- [ ] **Step 2: Launch the desktop head**

Uno App MCP: `uno_app_start` with `projectPath = D:\Personal\mzikmund-worktrees\dev-mazi-appredesign\src\app\MZikmund.App\MZikmund.App.csproj`, `targetFramework = net10.0-desktop`, `connectionTimeoutSeconds = 120`.
Expected: the app connects and `uno_app_get_runtime_info` reports a PID.

- [ ] **Step 3: Screenshot the blog grid in the current theme**

`uno_app_get_screenshot` with `fileType = png`. Check, in order: Inter is rendering (not the Open Sans default — the `t` and `a` shapes differ clearly); cards are 320px-plus wide in a stretched uniform grid with 24px gutters; each card has 180px media, badges top-left of the media, a two-line title, calendar icon plus humanized date, a three-line excerpt, and the accent-subtle "Read more" chip with a chevron.

- [ ] **Step 4: Verify hover and focus**

`uno_app_visualtree_snapshot` at `detail = full` to get a card's bounds, then `uno_app_pointer_click` at its centre-minus-100px to hover-and-not-activate, and screenshot. Expected: the card rises 2px, its border goes to `MzStrokeControlStrong`, the title turns `MzAccentText`, the image scales to 1.03 and the chevron nudges right. Then press <kbd>Tab</kbd> via `uno_app_key_press` (`virtualKey = Tab`) and screenshot: a two-tone focus ring, dark inner and light outer, 3px outside the card.

- [ ] **Step 5: Switch the theme and repeat**

Navigate to Settings from the nav pane (`uno_app_visualtree_snapshot`, then `uno_app_element_peer_default_action` on the settings item), flip the theme picker to the opposite of the current theme, navigate back to Blog and screenshot. Expected in dark: page ground `#202020`, cards `#2B2B2B`, accent text `#76B9ED`, meta text at 54% white, and the accent-subtle chip visibly tinted rather than grey. Nothing may keep a light-theme colour.

- [ ] **Step 6: Close the app**

`uno_app_close`.

- [ ] **Step 7: Audit the dependent animations**
```bash
grep -rn "Storyboard.TargetProperty=\"\(Width\|Height\|Margin\|Padding\|MaxWidth\|MaxHeight\)\"" src/app/MZikmund.App/Theme
```
Expected: no output. Every animation added in this phase targets `RenderTransform` sub-properties or swaps whole brush objects, so none needs `EnableDependentAnimation="True"`. Any hit must have the flag added.

- [ ] **Step 8: Format / Expected: no unformatted files reported**
```bash
dotnet format MZikmund.slnx
```

- [ ] **Step 9: Commit**
```bash
git add -A
git commit -m "style: Run dotnet format after the Phase 1 theme layer"
```

---

## Phase 2 — Shell and navigation

# Phase 2 — Shell and navigation (spec §7)

## File structure

## Created

| Path | Responsibility |
|---|---|
| `src/app/MZikmund.App.Core/Infrastructure/WindowShellLocator.cs` | Static registry of live `IWindowShell` instances + `FindForXamlRoot(XamlRoot?)`. Replaces every `XamlRoot.Content is WindowShell` cast so the shell's concrete type can change freely. |
| `src/app/MZikmund.App/Controls/MzPageHeader.xaml` | Reusable page-header layout: breadcrumb → Title-step title → body-large lead → forward actions. Carries the `MzTypeScale` VSG that swaps the title to the wide ramp at ≥768. |
| `src/app/MZikmund.App/Controls/MzPageHeader.xaml.cs` | `Title` / `Lead` / `BreadcrumbItems` / `BreadcrumbCommand` / `Actions` dependency properties; collapses each part when unset. |
| `src/app/MZikmund.App/Infrastructure/MzPageTransition.cs` | The tuned page-entrance storyboard (opacity 0→1 + `CompositeTransform.TranslateY` 16→0, 333 ms, `MzEaseEntrance`) plus the `AnimationsEnabled` kill switch Phase 5's reduced-motion toggle drives. |
| `src/app/MZikmund.App/Assets/NavigationViewLogo.scale-100.png` (+ `.scale-150`, `.scale-200`) | Scale-qualified app logo for the 56px title bar. Replaces the single unqualified 48×48 PNG. |

## Modified

| Path | Change |
|---|---|
| `src/app/MZikmund.App/Views/PageBase.cs` | `EnsureViewModel` uses `WindowShellLocator`; `PageLoaded`/`PageUnloaded` drive the entrance storyboard. |
| `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs` | Second `XamlRoot.Content is WindowShell` cast replaced by the locator. |
| `src/app/MZikmund.App.Core/Services/Navigation/WindowShellProvider.cs` | `XamlRoot` falls back to the window content and throws a descriptive error instead of returning null. |
| `src/app/MZikmund.App.Core/Services/Dialogs/DialogCoordinator.cs` | Per-dialog try/catch so a XamlRoot failure faults the awaiting task instead of hanging it. |
| `src/app/MZikmund.App.Core/Services/Navigation/INavigationService.cs` | Adds `bool TryNavigate(string viewModelTypeName)`. |
| `src/app/MZikmund.App.Core/Services/Navigation/NavigationService.cs` | Implements `TryNavigate`; `Initialize()` guarded off Windows; back request marked `Handled`. |
| `src/app/MZikmund.App.Core/ViewModels/WindowShellViewModel.cs` | Adds observable `CanGoBack` (driven by `Frame.CanGoBackProperty`) and observable `HasCustomTitleBar`. |
| `src/app/MZikmund.App/WindowShell.xaml` | Full rewrite: 56px acrylic title bar, scale-qualified logo, body/500 title, `Tag`s on every nav item, Home + Contact items, Fluent icon glyphs, 768/1280 breakpoints, `ContentHeader` deleted, transparent `Frame` with cleared `ContentTransitions`. |
| `src/app/MZikmund.App/WindowShell.xaml.cs` | Locator registration, `Initialize()` call, `Tag` dispatch, reverse selection sync, `#if WINDOWS` Mica/ground split, `SetTitlebarColors` + `ColorValuesChanged` + `UISettings` deleted. |
| `src/app/MZikmund.App/Resources/Colors.xaml` | Opaque `ApplicationPageBackgroundThemeBrush` overrides and the `SystemAccentColor` alias removed. |
| `src/app/MZikmund.App/Views/*.xaml`, `Views/Admin/*.xaml` | `Background` / `d:Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"` attributes stripped. |
| `src/app/MZikmund.App/Views/SettingsView.xaml` | Raw title `TextBlock` replaced by `MzPageHeader`; gutter to 16. |
| `src/app/MZikmund.App/Strings/en/Resources.resw`, `Strings/cs/Resources.resw` | `Videos` key added; the nine missing Czech nav strings added. |


## Interfaces

## Produced by this phase (later phases consume these)

**Types**
- `MZikmund.App.Core.Infrastructure.WindowShellLocator` (static)
  - `static void Register(IWindowShell shell)`
  - `static IWindowShell? FindForXamlRoot(XamlRoot? xamlRoot)`
- `MZikmund.Controls.MzPageHeader : UserControl`
  - `string Title` / `TitleProperty`
  - `string Lead` / `LeadProperty`
  - `object? BreadcrumbItems` / `BreadcrumbItemsProperty`
  - `System.Windows.Input.ICommand? BreadcrumbCommand` / `BreadcrumbCommandProperty` (executed with the clicked `BreadcrumbBarItemClickedEventArgs.Item`)
  - `object? Actions` / `ActionsProperty`
  - XAML namespace: `xmlns:controls="using:MZikmund.Controls"`
- `MZikmund.Infrastructure.MzPageTransition` (static)
  - `static bool AnimationsEnabled { get; set; }` — Phase 5's reduced-motion toggle sets this
  - `static Storyboard? PlayEntrance(FrameworkElement target)`

**Members added to existing types**
- `INavigationService.TryNavigate(string viewModelTypeName) : bool` — resolves `"FooViewModel"` → registered page `FooView`; returns `false` when no view is registered.
- `WindowShellViewModel.CanGoBack : bool` (observable)
- `WindowShellViewModel.HasCustomTitleBar : bool` (observable)
- Removed: `WindowShell.HasCustomTitleBar`, `WindowShell.SetTitlebarColors()`, `WindowShell.ColorValuesChanged`, `WindowShell.WindowShell_Loaded`

**Named XAML elements in `WindowShell.xaml`**
`PageGround`, `TitleBarGrid`, `TitleBarMaterial`, `AppNavigation`, `InnerFrame`, `HomeNavigationViewItem`, `BlogNavigationViewItem`, `VideosNavigationViewItem`, `ContactNavigationViewItem`, `AdminNavigationViewItem`, `AdminPostsNavigationViewItem`, `AdminMediaNavigationViewItem`, `AdminTagsNavigationViewItem`, `AdminCategoriesNavigationViewItem`, `LoginNavigationViewItem`, `LogoutNavigationViewItem`; visual states `Wide` (≥1280), `Mid` (≥768), `Narrow` in group `MzShellLayoutStates`.

**Navigation `Tag` contract** — a `NavigationViewItem.Tag` is the ViewModel type name; `"Login"` / `"Logout"` are the two command tags.
`HomeViewModel`, `BlogViewModel`, `VideosViewModel`, `ContactViewModel`, `PostsManagerViewModel`, `MediaBrowserViewModel`, `TagsManagerViewModel`, `CategoriesManagerViewModel`, `SettingsViewModel` (via `IsSettingsInvoked`).
**Phase 3 note:** Home and Contact nav items already ship with `Tag="HomeViewModel"` / `Tag="ContactViewModel"`. Phase 3 only has to add `HomeView.xaml` / `ContactView.xaml` (classes `MZikmund.Views.HomeView` / `MZikmund.Views.ContactView` deriving from `PageBase<HomeViewModel>` / `PageBase<ContactViewModel>`) and register the ViewModels in `App.xaml.cs`. **No shell edit is required** — `RegisterViewsFromAssembly` picks them up by convention and `TryNavigate` starts succeeding.

**Localization keys added** — `Videos` (en + cs); Czech values for `Blog`, `Home`, `Contact`, `Admin`, `Posts`, `Login`, `Logout`, `Settings`, `Videos`.

**Asset path** — `ms-appx:///Assets/NavigationViewLogo.png` (resolved from `scale-100` / `scale-150` / `scale-200` qualifiers; the unqualified file no longer exists).

## Consumed from Phase 1 (must exist before Task 11 runs)

**Brushes:** `MzBgBaseBrush`, `MzCardBrush`, `MzLayerBrush`, `MzTextPrimaryBrush`, `MzTextSecondaryBrush`, `MzStrokeCardBrush`, `MzStrokeDividerBrush`
**Acrylic:** `MzAcrylicBrush` (`AcrylicBrush`, `AlwaysUseFallback="False"`)
**Radii:** `MzCardCornerRadius`
**Type styles:** `MzBodyTextStyle`, `MzBodyLargeTextStyle`, `MzTitleTextStyle`, `MzTitleWideTextStyle`
**Motion:** `MzEaseEntrance` (a `KeySpline` resource, `0,0,0,1`)
**Icons:** `MzIconFontFamily` (`FontFamily`) and the `x:String` glyph keys `MzGlyphHome`, `MzGlyphNews`, `MzGlyphVideoClip`, `MzGlyphMail`, `MzGlyphShield`, `MzGlyphDocument`, `MzGlyphImageMultiple`, `MzGlyphTag`, `MzGlyphFolder`, `MzGlyphPerson`, `MzGlyphSignOut`

**Kept from the existing app:** `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter` (`Resources/Converters.xaml`), `NavigationViewContentBackground`, `BrandColor`, `BrandBrush` (`Resources/Colors.xaml` — `ColorResources.BrandColor` still reads `BrandColor`).


## Tasks

> **Run order is not negotiable.** Task 1 must land before anything else in this phase. `PageBase.EnsureViewModel` currently resolves every page's ViewModel by casting `XamlRoot.Content` to the concrete `WindowShell` type. Task 11 rewrites that shell. If Task 1 has not landed, every page silently gets `ViewModel == null` — no exception, no log, just empty pages.
>
> **Phase 1 contract.** Tasks 11–15 reference `Mz*` resources produced by Phase 1 (full list in the Interfaces section). If a key is missing, the app throws `XamlParseException` on shell load — loud, not silent.
>
> All C# uses tabs, CRLF, file-scoped namespaces, Allman braces. All XAML uses tabs.

---

### Task 26: Replace the `WindowShell` cast with an `IWindowShell` locator

**Files:**
- Create: `src/app/MZikmund.App.Core/Infrastructure/WindowShellLocator.cs`
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs:24-46`
- Modify: `src/app/MZikmund.App/Views/PageBase.cs:1-8`, `src/app/MZikmund.App/Views/PageBase.cs:63-74`
- Modify: `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs:1-5`, `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs:40-47`

**Interfaces:**
- Consumes: `MZikmund.App.Core.Infrastructure.IWindowShell` (existing — `WindowShellViewModel ViewModel`, `XamlRoot? XamlRoot`, `IServiceProvider ServiceProvider`, `DispatcherQueue DispatcherQueue`, `Frame RootFrame`)
- Produces: `MZikmund.App.Core.Infrastructure.WindowShellLocator.Register(IWindowShell)`, `WindowShellLocator.FindForXamlRoot(XamlRoot?) : IWindowShell?`

- [ ] **Step 1: Create the locator**

Create `src/app/MZikmund.App.Core/Infrastructure/WindowShellLocator.cs`:

```csharp
namespace MZikmund.App.Core.Infrastructure;

/// <summary>
/// Resolves the shell that owns a given <see cref="XamlRoot"/> without knowing the shell's concrete type.
/// </summary>
public static class WindowShellLocator
{
	private static readonly object Gate = new();
	private static readonly List<WeakReference<IWindowShell>> Shells = new();

	public static void Register(IWindowShell shell)
	{
		ArgumentNullException.ThrowIfNull(shell);

		lock (Gate)
		{
			Prune();

			foreach (var reference in Shells)
			{
				if (reference.TryGetTarget(out var existing) && ReferenceEquals(existing, shell))
				{
					return;
				}
			}

			Shells.Add(new WeakReference<IWindowShell>(shell));
		}
	}

	public static IWindowShell? FindForXamlRoot(XamlRoot? xamlRoot)
	{
		lock (Gate)
		{
			Prune();

			if (xamlRoot is not null)
			{
				foreach (var reference in Shells)
				{
					if (reference.TryGetTarget(out var shell) && ReferenceEquals(shell.XamlRoot, xamlRoot))
					{
						return shell;
					}
				}
			}

			// Single-window app: the only registered shell is the right one even before it has a XamlRoot.
			return Shells.Count == 1 && Shells[0].TryGetTarget(out var only) ? only : null;
		}
	}

	private static void Prune() => Shells.RemoveAll(reference => !reference.TryGetTarget(out _));
}
```

- [ ] **Step 2: Register the shell**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, replace:

```csharp
		InitializeComponent();

		_windowScope = serviceProvider.CreateScope();
```

with:

```csharp
		InitializeComponent();

		WindowShellLocator.Register(this);

		_windowScope = serviceProvider.CreateScope();
```

- [ ] **Step 3: Point `PageBase` at the locator**

In `src/app/MZikmund.App/Views/PageBase.cs`, replace the whole using block:

```csharp
using System.Runtime.InteropServices.ObjectiveC;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;
using MZikmund.App;
using MZikmund.ViewModels;
using Windows.ApplicationModel;
```

with:

```csharp
using Microsoft.UI.Xaml.Navigation;
using MZikmund.App.Core.Infrastructure;
using MZikmund.ViewModels;
```

and replace `EnsureViewModel`:

```csharp
	private void EnsureViewModel()
	{
		if (ViewModel is not null)
		{
			return;
		}

		if (XamlRoot?.Content is WindowShell windowShell)
		{
			ViewModel = windowShell.ServiceProvider.GetRequiredService<TViewModel>();
		}
	}
```

with:

```csharp
	private void EnsureViewModel()
	{
		if (ViewModel is not null)
		{
			return;
		}

		if (WindowShellLocator.FindForXamlRoot(XamlRoot) is { } windowShell)
		{
			ViewModel = windowShell.ServiceProvider.GetRequiredService<TViewModel>();
		}
	}
```

- [ ] **Step 4: Point the second cast in `PostEditorView` at the locator**

In `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs`, replace:

```csharp
using System.Globalization;
using MZikmund.ViewModels.Admin;
using Windows.Foundation;
```

with:

```csharp
using System.Globalization;
using MZikmund.App.Core.Infrastructure;
using MZikmund.ViewModels.Admin;
using Windows.Foundation;
```

and replace:

```csharp
			if (XamlRoot?.Content is WindowShell windowShell)
			{
				var logger = windowShell.ServiceProvider.GetRequiredService<ILogger<PostEditorView>>();
				logger.LogError(ex, "Failed to initialize WebView2 in PostEditorView");
			}
```

with:

```csharp
			if (WindowShellLocator.FindForXamlRoot(XamlRoot) is { } windowShell)
			{
				var logger = windowShell.ServiceProvider.GetRequiredService<ILogger<PostEditorView>>();
				logger.LogError(ex, "Failed to initialize WebView2 in PostEditorView");
			}
```

- [ ] **Step 5: Verify no cast survives**

```bash
grep -rn "Content is WindowShell" src/app
```

Expected: no output.

- [ ] **Step 6: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 7: Run and confirm ViewModels still resolve**

```bash
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: the Blog grid renders post cards (not an empty page). An empty page means `ViewModel` is null and the locator is not resolving.

- [ ] **Step 8: Commit**

```bash
git add src/app/MZikmund.App.Core/Infrastructure/WindowShellLocator.cs src/app/MZikmund.App/WindowShell.xaml.cs src/app/MZikmund.App/Views/PageBase.cs src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs
git commit -m "refactor: Resolve the window shell through an interface locator"
```

---

### Task 27: Harden the shell `XamlRoot` lookup used by `DialogCoordinator`

**Files:**
- Modify: `src/app/MZikmund.App.Core/Services/Navigation/WindowShellProvider.cs:49-56`
- Modify: `src/app/MZikmund.App.Core/Services/Dialogs/DialogCoordinator.cs:30-50`

**Interfaces:**
- Consumes: `IWindowShellProvider.Window`, `IWindowShell.XamlRoot`, `QueuedDialog.CompletionSource : TaskCompletionSource<ContentDialogResult>`
- Produces: `WindowShellProvider.XamlRoot` never returns null — it falls back to `Window.Content.XamlRoot` and otherwise throws `InvalidOperationException`

- [ ] **Step 1: Make the provider's `XamlRoot` resilient**

In `src/app/MZikmund.App.Core/Services/Navigation/WindowShellProvider.cs`, replace:

```csharp
	public XamlRoot XamlRoot
	{
		get
		{
			EnsureInitialized();
			return _shell.XamlRoot!;
		}
	}
```

with:

```csharp
	public XamlRoot XamlRoot
	{
		get
		{
			EnsureInitialized();
			return _shell.XamlRoot
				?? _window.Content?.XamlRoot
				?? throw new InvalidOperationException("The window shell is not attached to a XamlRoot yet.");
		}
	}
```

- [ ] **Step 2: Stop a failed dialog from hanging its caller**

In `src/app/MZikmund.App.Core/Services/Dialogs/DialogCoordinator.cs`, replace:

```csharp
				while (_dialogQueue.Count > 0)
				{
					var queuedDialog = _dialogQueue.Dequeue();
					queuedDialog.Dialog.XamlRoot = _windowShellProvider.XamlRoot;
					var result = await queuedDialog.Dialog.ShowAsync();
					queuedDialog.CompletionSource.SetResult(result);
				}
```

with:

```csharp
				while (_dialogQueue.Count > 0)
				{
					var queuedDialog = _dialogQueue.Dequeue();
					try
					{
						queuedDialog.Dialog.XamlRoot = _windowShellProvider.XamlRoot;
						var result = await queuedDialog.Dialog.ShowAsync();
						queuedDialog.CompletionSource.SetResult(result);
					}
					catch (Exception ex)
					{
						queuedDialog.CompletionSource.SetException(ex);
					}
				}
```

- [ ] **Step 3: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/app/MZikmund.App.Core/Services/Navigation/WindowShellProvider.cs src/app/MZikmund.App.Core/Services/Dialogs/DialogCoordinator.cs
git commit -m "fix: Harden the shell XamlRoot lookup used by the dialog coordinator"
```

---

### Task 28: Nav repair 1 — call `NavigationService.Initialize()` and mark system back handled

**Files:**
- Modify: `src/app/MZikmund.App.Core/Services/Navigation/NavigationService.cs:62-66`
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs:31-33`

**Interfaces:**
- Consumes: `INavigationService.Initialize()`, `INavigationService.GoBack()`
- Produces: hardware/system back is wired on Android, iOS, WASM and Desktop; `BackRequestedEventArgs.Handled` is set when a page was actually popped

- [ ] **Step 1: Guard `Initialize` and set `Handled`**

In `src/app/MZikmund.App.Core/Services/Navigation/NavigationService.cs`, replace:

```csharp
	public void Initialize() =>
		SystemNavigationManager.GetForCurrentView().BackRequested += NavigationManagerBackRequested;

	private void NavigationManagerBackRequested(object? sender, BackRequestedEventArgs? e) => GoBack();
	public void ClearBackStack() => Frame.BackStack.Clear();
```

with:

```csharp
	public void Initialize()
	{
		// SystemNavigationManager.GetForCurrentView() needs a CoreWindow, which WinUI 3 on Windows does not have.
#if !WINDOWS
		SystemNavigationManager.GetForCurrentView().BackRequested += NavigationManagerBackRequested;
#endif
	}

#if !WINDOWS
	private void NavigationManagerBackRequested(object? sender, BackRequestedEventArgs? e)
	{
		if (GoBack() && e is not null)
		{
			e.Handled = true;
		}
	}
#endif

	public void ClearBackStack() => Frame.BackStack.Clear();
```

- [ ] **Step 2: Call it from the shell**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, replace:

```csharp
		var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
		navigationService.RegisterViewsFromAssembly(typeof(MZikmundApp).Assembly);
```

with:

```csharp
		var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
		navigationService.RegisterViewsFromAssembly(typeof(MZikmundApp).Assembly);
		navigationService.Initialize();
```

- [ ] **Step 3: Build both a Skia head and the Windows head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

Expected: `Build succeeded`, 0 errors on both (the Windows build proves the `#if !WINDOWS` block is not orphaning a symbol).

- [ ] **Step 4: Build the Android head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/app/MZikmund.App.Core/Services/Navigation/NavigationService.cs src/app/MZikmund.App/WindowShell.xaml.cs
git commit -m "fix: Wire system back navigation by initializing the navigation service"
```

---

### Task 29: Nav repair 2 — observable `CanGoBack`

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/WindowShellViewModel.cs:10-34`
- Modify: `src/app/MZikmund.App/WindowShell.xaml:45`

**Interfaces:**
- Consumes: `IWindowShellProvider.RootFrame : Frame`
- Produces: `WindowShellViewModel.CanGoBack : bool` (observable, `[ObservableProperty]`)

- [ ] **Step 1: Track `Frame.CanGoBackProperty` in the ViewModel**

In `src/app/MZikmund.App.Core/ViewModels/WindowShellViewModel.cs`, replace:

```csharp
	private readonly IWindowShellProvider _provider;
	private readonly INavigationService _navigationService;
	private readonly IUserService _userService;
	private RefCountDisposable? _refCountDisposable;

	public WindowShellViewModel(IWindowShellProvider provider, INavigationService navigationService, IUserService userService)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
		_userService = userService ?? throw new ArgumentNullException(nameof(userService));

		// Initialize from cached authentication state
		UpdateAuthenticationState();
	}

	public string Title { get; set; } = "Martin Zikmund";

	[ObservableProperty]
	public partial bool IsLoading { get; set; }
```

with:

```csharp
	private readonly IWindowShellProvider _provider;
	private readonly INavigationService _navigationService;
	private readonly IUserService _userService;
	private readonly Frame _rootFrame;
	private RefCountDisposable? _refCountDisposable;

	public WindowShellViewModel(IWindowShellProvider provider, INavigationService navigationService, IUserService userService)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
		_userService = userService ?? throw new ArgumentNullException(nameof(userService));

		// Frame.CanGoBack is a DependencyProperty but raises no INotifyPropertyChanged, so observe it directly.
		// This also covers ClearBackStack(), which changes CanGoBack without a Navigated event.
		_rootFrame = provider.RootFrame;
		_rootFrame.RegisterPropertyChangedCallback(Frame.CanGoBackProperty, OnRootFrameCanGoBackChanged);
		CanGoBack = _rootFrame.CanGoBack;

		// Initialize from cached authentication state
		UpdateAuthenticationState();
	}

	public string Title { get; set; } = "Martin Zikmund";

	[ObservableProperty]
	public partial bool CanGoBack { get; set; }

	[ObservableProperty]
	public partial bool IsLoading { get; set; }
```

- [ ] **Step 2: Add the callback**

In the same file, replace:

```csharp
	private void UpdateAuthenticationState()
	{
		IsLoggedIn = _userService.IsLoggedIn;
	}
```

with:

```csharp
	private void OnRootFrameCanGoBackChanged(DependencyObject sender, DependencyProperty dp) =>
		CanGoBack = _rootFrame.CanGoBack;

	private void UpdateAuthenticationState()
	{
		IsLoggedIn = _userService.IsLoggedIn;
	}
```

- [ ] **Step 3: Bind the back button to it**

In `src/app/MZikmund.App/WindowShell.xaml`, replace:

```xml
            IsBackEnabled="{Binding ElementName=InnerFrame, Path=CanGoBack}"
```

with:

```xml
            IsBackEnabled="{x:Bind ViewModel.CanGoBack, Mode=OneWay}"
```

- [ ] **Step 4: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Verify the back button wakes up**

```bash
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: the back chevron is dimmed on Blog, becomes enabled the moment a post is opened, and dims again after going back. Previously it stayed dim until an unrelated layout pass.

- [ ] **Step 6: Commit**

```bash
git add src/app/MZikmund.App.Core/ViewModels/WindowShellViewModel.cs src/app/MZikmund.App/WindowShell.xaml
git commit -m "fix: Surface CanGoBack as an observable shell property"
```

---

### Task 30: Nav repair 3 — `Tag`-based dispatch replacing the if/else chain

**Files:**
- Modify: `src/app/MZikmund.App.Core/Services/Navigation/INavigationService.cs:5-18`
- Modify: `src/app/MZikmund.App.Core/Services/Navigation/NavigationService.cs:40-49`
- Modify: `src/app/MZikmund.App/WindowShell.xaml:57-111`
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs:117-166`

**Interfaces:**
- Consumes: `NavigationService._views` (populated by `RegisterViewsFromAssembly`), `WindowShellViewModel.LoginCommand`, `WindowShellViewModel.LogoutCommand`
- Produces: `INavigationService.TryNavigate(string viewModelTypeName) : bool`; the `Tag` contract on nav items (`"<Name>ViewModel"`, or `"Login"` / `"Logout"`)

- [ ] **Step 1: Add `TryNavigate` to the interface**

In `src/app/MZikmund.App.Core/Services/Navigation/INavigationService.cs`, replace:

```csharp
	void Navigate<TViewModel>(object parameter);

	bool GoBack();
```

with:

```csharp
	void Navigate<TViewModel>(object parameter);

	/// <summary>
	/// Navigates to the view registered for a ViewModel named <paramref name="viewModelTypeName"/>
	/// (for example "BlogViewModel"). Returns false when no such view is registered.
	/// </summary>
	bool TryNavigate(string viewModelTypeName);

	bool GoBack();
```

- [ ] **Step 2: Implement `TryNavigate`**

In `src/app/MZikmund.App.Core/Services/Navigation/NavigationService.cs`, replace:

```csharp
	private bool TryFindViewForViewModel(Type viewModelType, out Type? viewType)
```

with:

```csharp
	public bool TryNavigate(string viewModelTypeName)
	{
		if (string.IsNullOrEmpty(viewModelTypeName) ||
			!viewModelTypeName.EndsWith("ViewModel", StringComparison.Ordinal))
		{
			return false;
		}

		var viewName = viewModelTypeName.Substring(0, viewModelTypeName.Length - "Model".Length);
		if (!_views.TryGetValue(viewName, out var viewType) || viewType is null)
		{
			return false;
		}

		Frame.Navigate(viewType, null);
		return true;
	}

	private bool TryFindViewForViewModel(Type viewModelType, out Type? viewType)
```

- [ ] **Step 3: Tag every nav item**

In `src/app/MZikmund.App/WindowShell.xaml`, apply these five replacements one after another.

Replace `<mux:NavigationViewItem x:Name="BlogNavigationViewItem" Content="{extensions:Localize Key=Blog}">` with `<mux:NavigationViewItem x:Name="BlogNavigationViewItem" Content="{extensions:Localize Key=Blog}" Tag="BlogViewModel">`

Replace `<mux:NavigationViewItem x:Name="VideosNavigationViewItem" Content="Videos">` with `<mux:NavigationViewItem x:Name="VideosNavigationViewItem" Content="Videos" Tag="VideosViewModel">`

Replace `<mux:NavigationViewItem x:Name="ContactNavigationViewItem" Content="{extensions:Localize Key=Contact}">` with `<mux:NavigationViewItem x:Name="ContactNavigationViewItem" Content="{extensions:Localize Key=Contact}" Tag="ContactViewModel">`

Replace this block:

```xml
                        <mux:NavigationViewItem x:Name="AdminPostsNavigationViewItem" Content="{extensions:Localize Key=Posts}">
```

with:

```xml
                        <mux:NavigationViewItem x:Name="AdminPostsNavigationViewItem" Content="{extensions:Localize Key=Posts}" Tag="PostsManagerViewModel">
```

and likewise:

- `<mux:NavigationViewItem x:Name="AdminMediaNavigationViewItem" Content="{extensions:Localize Key=Media}">` → add `Tag="MediaBrowserViewModel"`
- `<mux:NavigationViewItem x:Name="AdminTagsNavigationViewItem" Content="{extensions:Localize Key=Tags}">` → add `Tag="TagsManagerViewModel"`
- `<mux:NavigationViewItem x:Name="AdminCategoriesNavigationViewItem" Content="{extensions:Localize Key=Categories}">` → add `Tag="CategoriesManagerViewModel"`
- `x:Name="LoginNavigationViewItem"` element → add `Tag="Login"`
- `x:Name="LogoutNavigationViewItem"` element → add `Tag="Logout"`

- [ ] **Step 4: Replace `MenuItemInvoked` with tag dispatch**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, replace the entire `MenuItemInvoked` method (from `private async void MenuItemInvoked` to its closing brace) with:

```csharp
	private async void MenuItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
	{
		if (args.IsSettingsInvoked)
		{
			NavigateToTag("SettingsViewModel");
			return;
		}

		if (args.InvokedItemContainer?.Tag is not string tag || tag.Length == 0)
		{
			// Group headers such as Admin carry no tag; invoking them only expands the group.
			return;
		}

		switch (tag)
		{
			case "Login":
				await ViewModel.LoginCommand.ExecuteAsync(null);
				return;
			case "Logout":
				await ViewModel.LogoutCommand.ExecuteAsync(null);
				return;
			default:
				NavigateToTag(tag);
				return;
		}
	}

	private void NavigateToTag(string viewModelTypeName)
	{
		var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
		if (!navigationService.TryNavigate(viewModelTypeName))
		{
			ServiceProvider
				.GetRequiredService<ILogger<WindowShell>>()
				.LogWarning("No view is registered for '{ViewModelTypeName}'; navigation was skipped.", viewModelTypeName);
			return;
		}

		navigationService.ClearBackStack();
	}
```

- [ ] **Step 5: Drop the now-unused admin ViewModel using**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, delete the line:

```csharp
using MZikmund.ViewModels.Admin;
```

- [ ] **Step 6: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 7: Verify every destination still navigates**

```bash
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: Blog, Videos, Settings all navigate. Contact logs `No view is registered for 'ContactViewModel'` and does nothing (Phase 3 builds it). Log in, then confirm Admin → Posts / Media / Tags / Categories all navigate.

- [ ] **Step 8: Commit**

```bash
git add src/app/MZikmund.App.Core/Services/Navigation/INavigationService.cs src/app/MZikmund.App.Core/Services/Navigation/NavigationService.cs src/app/MZikmund.App/WindowShell.xaml src/app/MZikmund.App/WindowShell.xaml.cs
git commit -m "refactor: Dispatch navigation from item tags instead of an if/else chain"
```

---

### Task 31: Nav repair 4 — reverse selection sync

**Files:**
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs:1-16` (usings)
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs:41-46` (constructor tail)
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs` (new methods)

**Interfaces:**
- Consumes: the `Tag` contract from Task 5, `AppNavigation.SettingsItem`, `MZikmund.Views.SettingsView`
- Produces: `AppNavigation.SelectedItem` follows every navigation, including programmatic ones such as `LogoutAsync`'s `Navigate<BlogViewModel>()` and `BlogViewModel.NavigateToPost`

- [ ] **Step 1: Add the usings**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, replace:

```csharp
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using MZikmund.App.Core.Infrastructure;
```

with:

```csharp
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MZikmund.App.Core.Infrastructure;
```

and add, immediately after `using MZikmund.ViewModels;`:

```csharp
using MZikmund.Views;
```

- [ ] **Step 2: Subscribe and drop the manual initial selection**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, replace:

```csharp
		Loaded += WindowShell_Loaded;

		// Navigate to Blog as the default page
		navigationService.Navigate<BlogViewModel>();
		AppNavigation.SelectedItem = BlogNavigationViewItem;
	}
```

with:

```csharp
		Loaded += WindowShell_Loaded;

		InnerFrame.Navigated += InnerFrameNavigated;

		// Navigate to Blog as the default page; InnerFrameNavigated selects the matching nav item.
		navigationService.Navigate<BlogViewModel>();
	}
```

- [ ] **Step 3: Add the sync handler**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, add these two methods immediately above `private void NavigateToTag(string viewModelTypeName)`:

```csharp
	private void InnerFrameNavigated(object sender, NavigationEventArgs e)
	{
		if (e.SourcePageType == typeof(SettingsView))
		{
			AppNavigation.SelectedItem = AppNavigation.SettingsItem;
			return;
		}

		// A page named FooView is owned by the item tagged FooViewModel.
		var expectedTag = e.SourcePageType.Name + "Model";

		if (FindNavigationItem(AppNavigation.MenuItems, expectedTag) is { } menuItem)
		{
			AppNavigation.SelectedItem = menuItem;
			return;
		}

		if (FindNavigationItem(AppNavigation.FooterMenuItems, expectedTag) is { } footerItem)
		{
			AppNavigation.SelectedItem = footerItem;
		}

		// Detail pages such as PostView have no item; the parent stays selected.
	}

	private static NavigationViewItem? FindNavigationItem(IList<object> items, string tag)
	{
		foreach (var item in items)
		{
			if (item is not NavigationViewItem navigationViewItem)
			{
				continue;
			}

			if (navigationViewItem.Tag as string == tag)
			{
				return navigationViewItem;
			}

			if (FindNavigationItem(navigationViewItem.MenuItems, tag) is { } nested)
			{
				return nested;
			}
		}

		return null;
	}
```

- [ ] **Step 4: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Verify the selection follows programmatic navigation**

```bash
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: on launch Blog is highlighted without the shell setting it manually. Navigate to Settings → Settings highlights. Log in, go to Admin → Tags (nested item highlights), then log out → the app lands on Blog and Blog highlights.

- [ ] **Step 6: Commit**

```bash
git add src/app/MZikmund.App/WindowShell.xaml.cs
git commit -m "fix: Sync the navigation pane selection back from the frame"
```

---

### Task 32: Nav repair 6a — replace the non-observable `HasCustomTitleBar`

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/WindowShellViewModel.cs` (new observable property)
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs:59` (delete), `:61-75` (`CustomizeWindow`)
- Modify: `src/app/MZikmund.App/WindowShell.xaml:24`

**Interfaces:**
- Consumes: `BoolToVisibilityConverter` (`Resources/Converters.xaml`)
- Produces: `WindowShellViewModel.HasCustomTitleBar : bool` (observable). `WindowShell.HasCustomTitleBar` is deleted.

- [ ] **Step 1: Add the observable property**

In `src/app/MZikmund.App.Core/ViewModels/WindowShellViewModel.cs`, replace:

```csharp
	[ObservableProperty]
	public partial bool IsLoggedIn { get; set; }
```

with:

```csharp
	[ObservableProperty]
	public partial bool IsLoggedIn { get; set; }

	[ObservableProperty]
	public partial bool HasCustomTitleBar { get; set; }
```

- [ ] **Step 2: Delete the stale shell property**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, delete:

```csharp
	public bool HasCustomTitleBar { get; private set; }
```

- [ ] **Step 3: Set it from `CustomizeWindow`**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, replace:

```csharp
			_associatedWindow.ExtendsContentIntoTitleBar = true;
			_associatedWindow.SetTitleBar(TitleBarGrid);
			HasCustomTitleBar = true;
```

with:

```csharp
			_associatedWindow.ExtendsContentIntoTitleBar = true;
			_associatedWindow.SetTitleBar(TitleBarGrid);
			ViewModel.HasCustomTitleBar = true;
```

- [ ] **Step 4: Rebind the title bar visibility**

In `src/app/MZikmund.App/WindowShell.xaml`, replace:

```xml
            Visibility="{x:Bind HasCustomTitleBar}">
```

with:

```xml
            Visibility="{x:Bind ViewModel.HasCustomTitleBar, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
```

- [ ] **Step 5: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/app/MZikmund.App.Core/ViewModels/WindowShellViewModel.cs src/app/MZikmund.App/WindowShell.xaml.cs src/app/MZikmund.App/WindowShell.xaml
git commit -m "fix: Make HasCustomTitleBar an observable shell view model property"
```

---

### Task 33: Nav repair 7 — delete `SetTitlebarColors` and its dead `ColorValuesChanged` subscription

The commented-out body targets `ApplicationView.GetForCurrentView().TitleBar`, which does not exist in WinUI 3. The Fluent title bar in Task 11 is drawn in XAML instead, so the whole path goes.

**Files:**
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs:1-14` (usings), `:20` (field), `:37` (subscription), `:41` (`Loaded`), `:50-53` (`WindowShell_Loaded`), `:77-115` (`ColorValuesChanged` + `SetTitlebarColors`)

**Interfaces:**
- Consumes: nothing new
- Produces: `WindowShell` no longer holds a `UISettings`, no longer handles `Loaded`, and no longer defines `SetTitlebarColors` or `ColorValuesChanged`

- [ ] **Step 1: Delete the field and the subscription**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, delete:

```csharp
	private readonly UISettings _uiSettings = new UISettings();
```

and delete:

```csharp
		_uiSettings.ColorValuesChanged += ColorValuesChanged;
```

- [ ] **Step 2: Delete the `Loaded` hook**

In the same file, delete:

```csharp
		Loaded += WindowShell_Loaded;

```

and delete the handler:

```csharp
	private void WindowShell_Loaded(object sender, RoutedEventArgs e)
	{
		SetTitlebarColors();
	}

```

- [ ] **Step 3: Delete the two dead methods**

In the same file, delete the whole `ColorValuesChanged` method and the whole `SetTitlebarColors` method — everything from:

```csharp
	private async void ColorValuesChanged(UISettings sender, object args)
```

down to and including the closing brace of `SetTitlebarColors` (the block ending with `//#pragma warning restore CS8618` followed by `	}`).

- [ ] **Step 4: Trim the usings that only existed for that code**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, delete these lines:

```csharp
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using MZikmund.Resources;
using MZikmund.Services.Theming;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
```

- [ ] **Step 5: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors. (`Windows.Foundation.Metadata` must remain — `ApiInformation` still uses it.)

- [ ] **Step 6: Commit**

```bash
git add src/app/MZikmund.App/WindowShell.xaml.cs
git commit -m "refactor: Delete the dead WinUI 2 title bar colouring path"
```

---

### Task 34: Localization keys for the nav pane

**Files:**
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw` (before the closing `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (before the closing `</root>`)

**Interfaces:**
- Consumes: `{extensions:Localize Key=…}` markup extension
- Produces: `Videos` (en + cs); Czech values for `Blog`, `Home`, `Contact`, `Admin`, `Posts`, `Login`, `Logout`, `Settings`

- [ ] **Step 1: Add `Videos` to English**

In `src/app/MZikmund.App/Strings/en/Resources.resw`, replace:

```xml
  <data name="OpenPreview" xml:space="preserve">
    <value>Open preview</value>
  </data>
</root>
```

with:

```xml
  <data name="OpenPreview" xml:space="preserve">
    <value>Open preview</value>
  </data>
  <data name="Videos" xml:space="preserve">
    <value>Videos</value>
  </data>
</root>
```

- [ ] **Step 2: Add the missing Czech nav strings**

In `src/app/MZikmund.App/Strings/cs/Resources.resw`, replace:

```xml
  <data name="OpenPreview" xml:space="preserve">
    <value>Otevřít náhled</value>
  </data>
</root>
```

with:

```xml
  <data name="OpenPreview" xml:space="preserve">
    <value>Otevřít náhled</value>
  </data>
  <data name="Home" xml:space="preserve">
    <value>Domů</value>
  </data>
  <data name="Blog" xml:space="preserve">
    <value>Blog</value>
  </data>
  <data name="Videos" xml:space="preserve">
    <value>Videa</value>
  </data>
  <data name="Contact" xml:space="preserve">
    <value>Kontakt</value>
  </data>
  <data name="Admin" xml:space="preserve">
    <value>Správa</value>
  </data>
  <data name="Posts" xml:space="preserve">
    <value>Příspěvky</value>
  </data>
  <data name="Login" xml:space="preserve">
    <value>Přihlásit</value>
  </data>
  <data name="Logout" xml:space="preserve">
    <value>Odhlásit</value>
  </data>
  <data name="Settings" xml:space="preserve">
    <value>Nastavení</value>
  </data>
</root>
```

- [ ] **Step 3: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/app/MZikmund.App/Strings/en/Resources.resw src/app/MZikmund.App/Strings/cs/Resources.resw
git commit -m "feat: Add the missing navigation strings for English and Czech"
```

---

### Task 35: Scale-qualified app logo assets

The title bar shows the logo at 24 effective pixels. The source is a single unqualified 48×48 PNG, so on a 100% display it is downscaled at runtime every frame and on 200% it is exact by accident.

**Files:**
- Create: `src/app/MZikmund.App/Assets/NavigationViewLogo.scale-100.png` (24×24)
- Create: `src/app/MZikmund.App/Assets/NavigationViewLogo.scale-150.png` (36×36)
- Create: `src/app/MZikmund.App/Assets/NavigationViewLogo.scale-200.png` (48×48)
- Delete: `src/app/MZikmund.App/Assets/NavigationViewLogo.png`

**Interfaces:**
- Consumes: nothing
- Produces: `ms-appx:///Assets/NavigationViewLogo.png` now resolves through scale qualifiers (Task 11's `<Image Source="…" Width="24" Height="24" />` depends on this)

- [ ] **Step 1: Generate the three qualified variants and remove the unqualified file**

```powershell
Add-Type -AssemblyName System.Drawing
$root = "D:\Personal\mzikmund-worktrees\dev-mazi-appredesign\src\app\MZikmund.App\Assets"
$source = Join-Path $root "NavigationViewLogo.png"
$image = [System.Drawing.Image]::FromFile($source)
foreach ($pair in @(@(100, 24), @(150, 36), @(200, 48))) {
	$size = $pair[1]
	$bitmap = New-Object System.Drawing.Bitmap $size, $size
	$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
	$graphics.InterpolationMode = 'HighQualityBicubic'
	$graphics.PixelOffsetMode = 'HighQuality'
	$graphics.SmoothingMode = 'HighQuality'
	$graphics.Clear([System.Drawing.Color]::Transparent)
	$graphics.DrawImage($image, 0, 0, $size, $size)
	$graphics.Dispose()
	$bitmap.Save((Join-Path $root ("NavigationViewLogo.scale-{0}.png" -f $pair[0])), [System.Drawing.Imaging.ImageFormat]::Png)
	$bitmap.Dispose()
}
$image.Dispose()
Remove-Item -LiteralPath $source
Get-ChildItem -LiteralPath $root -Filter "NavigationViewLogo*"
```

Expected output: exactly three files — `NavigationViewLogo.scale-100.png`, `NavigationViewLogo.scale-150.png`, `NavigationViewLogo.scale-200.png`.

- [ ] **Step 2: Build so the asset manifest regenerates**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors (the old `BitmapIcon UriSource="ms-appx:///Assets/NavigationViewLogo.png"` in the title bar still resolves, now via the qualifiers).

- [ ] **Step 3: Commit**

```bash
git add src/app/MZikmund.App/Assets/NavigationViewLogo.scale-100.png src/app/MZikmund.App/Assets/NavigationViewLogo.scale-150.png src/app/MZikmund.App/Assets/NavigationViewLogo.scale-200.png
git add -u src/app/MZikmund.App/Assets
git commit -m "feat: Ship the app logo with scale qualifiers"
```

---

### Task 36: Shell rewrite — 56px acrylic title bar, 768/1280 breakpoints, no `ContentHeader`, Home + Contact

This is the structural task. `ContentHeader` renders `ViewModel.Title` while five pages also draw their own heading, so the word appears twice below 768px — it goes, and pages own their header from Task 13 onward. The Wide/Mid states stop forcing `TitleBarGrid` visible on heads with no custom title bar (nav repair 6b). `Wide` stays declared before `Mid`: with two `AdaptiveTrigger`s scoring equally, the **first** declared active state wins.

**Files:**
- Modify: `src/app/MZikmund.App/WindowShell.xaml` (full rewrite)
- Modify: `src/app/MZikmund.App/WindowShell.xaml.cs` — `CustomizeWindow`

**Interfaces:**
- Consumes (Phase 1): `MzBgBaseBrush`, `MzCardBrush`, `MzLayerBrush`, `MzTextPrimaryBrush`, `MzTextSecondaryBrush`, `MzStrokeCardBrush`, `MzStrokeDividerBrush`, `MzAcrylicBrush`, `MzCardCornerRadius`, `MzBodyTextStyle`, `MzIconFontFamily`, `MzGlyphHome`, `MzGlyphNews`, `MzGlyphVideoClip`, `MzGlyphMail`, `MzGlyphShield`, `MzGlyphDocument`, `MzGlyphImageMultiple`, `MzGlyphTag`, `MzGlyphFolder`, `MzGlyphPerson`, `MzGlyphSignOut`
- Consumes (this phase): `WindowShellViewModel.CanGoBack`, `.HasCustomTitleBar`, `.IsLoading`, `.LoadingStatusMessage`, `.IsLoggedIn`, `.Title`, `.BackRequested`; `MenuItemInvoked`; `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter`; `ms-appx:///Assets/NavigationViewLogo.png`
- Produces: element names `PageGround`, `TitleBarGrid`, `TitleBarMaterial`, `AppNavigation`, `InnerFrame`, `HomeNavigationViewItem`; visual states `Wide` / `Mid` / `Narrow` in `MzShellLayoutStates`

- [ ] **Step 1: Rewrite `WindowShell.xaml`**

Replace the entire contents of `src/app/MZikmund.App/WindowShell.xaml` with:

```xml
<Page
	x:Class="MZikmund.App.WindowShell"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:extensions="using:MZikmund.Extensions.Xaml"
	xmlns:local="using:MZikmund.App"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	xmlns:mux="using:Microsoft.UI.Xaml.Controls"
	xmlns:toolkit="using:Uno.UI.Toolkit"
	Background="Transparent"
	d:RequestedTheme="Light"
	mc:Ignorable="d">

	<Grid toolkit:VisibleBoundsPadding.PaddingMask="All">
		<Grid.RowDefinitions>
			<RowDefinition Height="Auto" />
			<RowDefinition Height="*" />
		</Grid.RowDefinitions>

		<!--  Page ground. Collapsed on Windows, where the Mica backdrop paints it instead.  -->
		<Border
			x:Name="PageGround"
			Grid.RowSpan="2"
			Background="{ThemeResource MzBgBaseBrush}" />

		<Grid
			x:Name="TitleBarGrid"
			Height="56"
			Visibility="{x:Bind ViewModel.HasCustomTitleBar, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
			<!--  Acrylic must have no children on Android, iOS and macOS: bare Border behind the content.  -->
			<Border x:Name="TitleBarMaterial" Background="{ThemeResource MzAcrylicBrush}" />
			<Border
				VerticalAlignment="Bottom"
				BorderBrush="{ThemeResource MzStrokeDividerBrush}"
				BorderThickness="0,0,0,1" />
			<Grid Padding="16,0" ColumnSpacing="12">
				<Grid.ColumnDefinitions>
					<ColumnDefinition Width="Auto" />
					<ColumnDefinition Width="*" />
				</Grid.ColumnDefinitions>
				<Image
					Width="24"
					Height="24"
					VerticalAlignment="Center"
					AutomationProperties.AccessibilityView="Raw"
					Source="ms-appx:///Assets/NavigationViewLogo.png" />
				<TextBlock
					Grid.Column="1"
					VerticalAlignment="Center"
					FontWeight="Medium"
					Foreground="{ThemeResource MzTextPrimaryBrush}"
					Style="{ThemeResource MzBodyTextStyle}"
					Text="{x:Bind ViewModel.Title}"
					TextTrimming="CharacterEllipsis" />
			</Grid>
		</Grid>

		<mux:NavigationView
			x:Name="AppNavigation"
			Grid.Row="1"
			Background="Transparent"
			BackRequested="{x:Bind ViewModel.BackRequested}"
			IsBackButtonVisible="Auto"
			IsBackEnabled="{x:Bind ViewModel.CanGoBack, Mode=OneWay}"
			IsSettingsVisible="True"
			IsTitleBarAutoPaddingEnabled="False"
			ItemInvoked="MenuItemInvoked"
			PaneDisplayMode="LeftMinimal">
			<mux:NavigationView.MenuItems>
				<mux:NavigationViewItem
					x:Name="HomeNavigationViewItem"
					Content="{extensions:Localize Key=Home}"
					Tag="HomeViewModel">
					<mux:NavigationViewItem.Icon>
						<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphHome}" />
					</mux:NavigationViewItem.Icon>
				</mux:NavigationViewItem>
				<mux:NavigationViewItem
					x:Name="BlogNavigationViewItem"
					Content="{extensions:Localize Key=Blog}"
					Tag="BlogViewModel">
					<mux:NavigationViewItem.Icon>
						<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphNews}" />
					</mux:NavigationViewItem.Icon>
				</mux:NavigationViewItem>
				<mux:NavigationViewItem
					x:Name="VideosNavigationViewItem"
					Content="{extensions:Localize Key=Videos}"
					Tag="VideosViewModel">
					<mux:NavigationViewItem.Icon>
						<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphVideoClip}" />
					</mux:NavigationViewItem.Icon>
				</mux:NavigationViewItem>
				<mux:NavigationViewItem
					x:Name="ContactNavigationViewItem"
					Content="{extensions:Localize Key=Contact}"
					Tag="ContactViewModel">
					<mux:NavigationViewItem.Icon>
						<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphMail}" />
					</mux:NavigationViewItem.Icon>
				</mux:NavigationViewItem>
				<mux:NavigationViewItem
					x:Name="AdminNavigationViewItem"
					Content="{extensions:Localize Key=Admin}"
					Visibility="{x:Bind ViewModel.IsLoggedIn, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
					<mux:NavigationViewItem.Icon>
						<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphShield}" />
					</mux:NavigationViewItem.Icon>
					<mux:NavigationViewItem.MenuItems>
						<mux:NavigationViewItem
							x:Name="AdminPostsNavigationViewItem"
							Content="{extensions:Localize Key=Posts}"
							Tag="PostsManagerViewModel">
							<mux:NavigationViewItem.Icon>
								<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphDocument}" />
							</mux:NavigationViewItem.Icon>
						</mux:NavigationViewItem>
						<mux:NavigationViewItem
							x:Name="AdminMediaNavigationViewItem"
							Content="{extensions:Localize Key=Media}"
							Tag="MediaBrowserViewModel">
							<mux:NavigationViewItem.Icon>
								<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphImageMultiple}" />
							</mux:NavigationViewItem.Icon>
						</mux:NavigationViewItem>
						<mux:NavigationViewItem
							x:Name="AdminTagsNavigationViewItem"
							Content="{extensions:Localize Key=Tags}"
							Tag="TagsManagerViewModel">
							<mux:NavigationViewItem.Icon>
								<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphTag}" />
							</mux:NavigationViewItem.Icon>
						</mux:NavigationViewItem>
						<mux:NavigationViewItem
							x:Name="AdminCategoriesNavigationViewItem"
							Content="{extensions:Localize Key=Categories}"
							Tag="CategoriesManagerViewModel">
							<mux:NavigationViewItem.Icon>
								<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphFolder}" />
							</mux:NavigationViewItem.Icon>
						</mux:NavigationViewItem>
					</mux:NavigationViewItem.MenuItems>
				</mux:NavigationViewItem>
			</mux:NavigationView.MenuItems>
			<mux:NavigationView.FooterMenuItems>
				<mux:NavigationViewItem
					x:Name="LoginNavigationViewItem"
					Content="{extensions:Localize Key=Login}"
					Tag="Login"
					Visibility="{x:Bind ViewModel.IsLoggedIn, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}">
					<mux:NavigationViewItem.Icon>
						<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphPerson}" />
					</mux:NavigationViewItem.Icon>
				</mux:NavigationViewItem>
				<mux:NavigationViewItem
					x:Name="LogoutNavigationViewItem"
					Content="{extensions:Localize Key=Logout}"
					Tag="Logout"
					Visibility="{x:Bind ViewModel.IsLoggedIn, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
					<mux:NavigationViewItem.Icon>
						<FontIcon FontFamily="{ThemeResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphSignOut}" />
					</mux:NavigationViewItem.Icon>
				</mux:NavigationViewItem>
			</mux:NavigationView.FooterMenuItems>
			<mux:NavigationView.Content>
				<Frame x:Name="InnerFrame" Background="Transparent">
					<!--  The stock NavigationThemeTransition is not tunable; MzPageTransition replaces it.  -->
					<Frame.ContentTransitions>
						<TransitionCollection />
					</Frame.ContentTransitions>
				</Frame>
			</mux:NavigationView.Content>
		</mux:NavigationView>

		<Grid
			Grid.RowSpan="2"
			Background="{ThemeResource MzLayerBrush}"
			Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
			<StackPanel
				Padding="24"
				HorizontalAlignment="Center"
				VerticalAlignment="Center"
				Background="{ThemeResource MzCardBrush}"
				BorderBrush="{ThemeResource MzStrokeCardBrush}"
				BorderThickness="1"
				CornerRadius="{ThemeResource MzCardCornerRadius}"
				Spacing="12">
				<mux:ProgressRing
					Width="48"
					Height="48"
					HorizontalAlignment="Center"
					IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />
				<TextBlock
					HorizontalAlignment="Center"
					Foreground="{ThemeResource MzTextSecondaryBrush}"
					Style="{ThemeResource MzBodyTextStyle}"
					Text="{x:Bind ViewModel.LoadingStatusMessage, Mode=OneWay}"
					TextAlignment="Center"
					TextWrapping="Wrap" />
			</StackPanel>
		</Grid>

		<VisualStateManager.VisualStateGroups>
			<VisualStateGroup x:Name="MzShellLayoutStates">
				<!--  Wide is declared first on purpose: equal-scoring AdaptiveTriggers resolve to the first active state.  -->
				<VisualState x:Name="Wide">
					<VisualState.StateTriggers>
						<AdaptiveTrigger MinWindowWidth="1280" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="AppNavigation.PaneDisplayMode" Value="Top" />
					</VisualState.Setters>
				</VisualState>
				<VisualState x:Name="Mid">
					<VisualState.StateTriggers>
						<AdaptiveTrigger MinWindowWidth="768" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="AppNavigation.PaneDisplayMode" Value="LeftCompact" />
					</VisualState.Setters>
				</VisualState>
				<VisualState x:Name="Narrow">
					<VisualState.StateTriggers>
						<AdaptiveTrigger MinWindowWidth="0" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="AppNavigation.PaneDisplayMode" Value="LeftMinimal" />
					</VisualState.Setters>
				</VisualState>
			</VisualStateGroup>
		</VisualStateManager.VisualStateGroups>
	</Grid>
</Page>
```

- [ ] **Step 2: Split the backdrop and the ground in `CustomizeWindow`**

In `src/app/MZikmund.App/WindowShell.xaml.cs`, replace the whole `CustomizeWindow` method with:

```csharp
	private void CustomizeWindow()
	{
		if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Window", "ExtendsContentIntoTitleBar"))
		{
			_associatedWindow.ExtendsContentIntoTitleBar = true;
			_associatedWindow.SetTitleBar(TitleBarGrid);
			ViewModel.HasCustomTitleBar = true;
		}

		_associatedWindow.AppWindow.Title = "Martin Zikmund";

#if WINDOWS
		// Mica is the page ground on Windows; every other head paints MzBgBaseBrush instead.
		_associatedWindow.SystemBackdrop = new MicaBackdrop();
		PageGround.Visibility = Visibility.Collapsed;
#endif
	}
```

- [ ] **Step 3: Build the Skia head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors. A `XamlParseException` at runtime naming an `Mz*` key means Phase 1 has not produced it — see the consumed list above.

- [ ] **Step 4: Build the Windows head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Visual check — light theme, three widths**

Drive the desktop head with the Uno App MCP: `uno_health`, then `uno_app_select_solution` with `D:/Personal/mzikmund-worktrees/dev-mazi-appredesign/MZikmund.slnx`, then `uno_discover_tools` to find the launch / resize / screenshot tools, then `uno_execute_tool` to launch `net10.0-desktop` and screenshot at widths **700**, **1000** and **1400**.

Expected: title bar 56px tall with acrylic, logo and "Martin Zikmund" at body size / Medium weight; **no** second title band under it; nav pane is `LeftMinimal` at 700, `LeftCompact` at 1000, `Top` at 1400; Home and Contact appear in the pane.

- [ ] **Step 6: Visual check — dark theme**

Switch to Dark in Settings and repeat the three screenshots.

Expected: the acrylic title bar tints dark, the divider is visible, and page content sits on the dark ground with no light seam.

- [ ] **Step 7: Commit**

```bash
git add src/app/MZikmund.App/WindowShell.xaml src/app/MZikmund.App/WindowShell.xaml.cs
git commit -m "feat: Rebuild the shell with a 56px acrylic title bar and 768/1280 breakpoints"
```

---

### Task 37: Remove the opaque `ApplicationPageBackgroundThemeBrush` overrides

The two overrides paint a solid `#1F1F1F` / `#F1F1F1` over the whole window, which is what hides `MicaBackdrop` today. The `SystemAccentColor` alias goes with them — the spec's accent ramp is hard-coded and must not be reachable through the system accent.

**Files:**
- Modify: `src/app/MZikmund.App/Resources/Colors.xaml:1-14`
- Modify: every `.xaml` under `src/app/MZikmund.App/Views/`

**Interfaces:**
- Consumes: `PageGround` (Task 11) as the replacement ground
- Produces: `Resources/Colors.xaml` keeps only `BrandColor`, `BrandBrush` (read by `ColorResources.BrandColor`) and `NavigationViewContentBackground`

- [ ] **Step 1: Strip the overrides and the accent alias**

Replace the entire contents of `src/app/MZikmund.App/Resources/Colors.xaml` with:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Color x:Key="BrandColor">#1592e5</Color>
    <SolidColorBrush x:Key="BrandBrush" Color="{StaticResource BrandColor}" />
    <SolidColorBrush x:Key="NavigationViewContentBackground" Color="Transparent" />
</ResourceDictionary>
```

- [ ] **Step 2: Strip the page-level references**

```powershell
$views = "D:\Personal\mzikmund-worktrees\dev-mazi-appredesign\src\app\MZikmund.App\Views"
$pattern = '\r?\n[ \t]*(?:d:)?Background="\{ThemeResource ApplicationPageBackgroundThemeBrush\}"'
foreach ($file in Get-ChildItem -LiteralPath $views -Recurse -Filter *.xaml) {
	$text = Get-Content -Raw -LiteralPath $file.FullName
	$updated = [System.Text.RegularExpressions.Regex]::Replace($text, $pattern, '')
	if ($updated -ne $text) {
		Set-Content -NoNewline -LiteralPath $file.FullName -Value $updated
		Write-Output ("stripped: " + $file.Name)
	}
}
```

Expected output: one `stripped:` line per view that carried the attribute.

- [ ] **Step 3: Verify nothing references the key any more**

```bash
grep -rn "ApplicationPageBackgroundThemeBrush" src/app
```

Expected: no output.

- [ ] **Step 4: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Visual check — Mica shows on Windows**

```bash
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

Expected: the desktop wallpaper tints through the window behind the content in both light and dark. Then run the desktop head and confirm it is *not* transparent — it must show the flat `MzBgBaseBrush` ground.

- [ ] **Step 6: Commit**

```bash
git add src/app/MZikmund.App/Resources/Colors.xaml src/app/MZikmund.App/Views
git commit -m "fix: Stop the opaque page background from hiding the Mica backdrop"
```

---

### Task 38: `MzPageHeader` — the reusable page-header pattern

The shape is the web's: breadcrumb → Title-step title → body-large lead → **forward** actions ("Browse by category", "Subscribe"). Never the way back — the breadcrumb carries that. Page titles use the Title step; TitleLarge is reserved for hero and feature headings.

**Files:**
- Create: `src/app/MZikmund.App/Controls/MzPageHeader.xaml`
- Create: `src/app/MZikmund.App/Controls/MzPageHeader.xaml.cs`

**Interfaces:**
- Consumes (Phase 1): `MzTitleTextStyle`, `MzTitleWideTextStyle`, `MzBodyLargeTextStyle`, `MzTextPrimaryBrush`, `MzTextSecondaryBrush`
- Produces: `MZikmund.Controls.MzPageHeader` with `Title`, `Lead`, `BreadcrumbItems`, `BreadcrumbCommand`, `Actions` (and the matching `*Property` fields); XAML namespace `xmlns:controls="using:MZikmund.Controls"`

- [ ] **Step 1: Create the markup**

Create `src/app/MZikmund.App/Controls/MzPageHeader.xaml`:

```xml
<UserControl
	x:Class="MZikmund.Controls.MzPageHeader"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	xmlns:mux="using:Microsoft.UI.Xaml.Controls"
	HorizontalAlignment="Stretch"
	HorizontalContentAlignment="Stretch"
	mc:Ignorable="d">

	<Grid x:Name="LayoutRoot">
		<Grid.RowDefinitions>
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
		</Grid.RowDefinitions>

		<mux:BreadcrumbBar
			x:Name="BreadcrumbHost"
			Margin="0,0,0,12"
			HorizontalAlignment="Left"
			ItemClicked="OnBreadcrumbItemClicked"
			Visibility="Collapsed" />

		<TextBlock
			x:Name="TitleText"
			Grid.Row="1"
			Foreground="{ThemeResource MzTextPrimaryBrush}"
			Style="{StaticResource MzTitleTextStyle}"
			TextWrapping="Wrap"
			Visibility="Collapsed" />

		<TextBlock
			x:Name="LeadText"
			Grid.Row="2"
			MaxWidth="680"
			Margin="0,8,0,0"
			HorizontalAlignment="Left"
			Foreground="{ThemeResource MzTextSecondaryBrush}"
			Style="{StaticResource MzBodyLargeTextStyle}"
			TextWrapping="Wrap"
			Visibility="Collapsed" />

		<ContentPresenter
			x:Name="ActionsHost"
			Grid.Row="3"
			Margin="0,16,0,0"
			HorizontalAlignment="Left"
			Visibility="Collapsed" />

		<VisualStateManager.VisualStateGroups>
			<VisualStateGroup x:Name="MzTypeScale">
				<VisualState x:Name="Wide">
					<VisualState.StateTriggers>
						<AdaptiveTrigger MinWindowWidth="768" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="TitleText.Style" Value="{StaticResource MzTitleWideTextStyle}" />
					</VisualState.Setters>
				</VisualState>
				<VisualState x:Name="Compact">
					<VisualState.StateTriggers>
						<AdaptiveTrigger MinWindowWidth="0" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="TitleText.Style" Value="{StaticResource MzTitleTextStyle}" />
					</VisualState.Setters>
				</VisualState>
			</VisualStateGroup>
		</VisualStateManager.VisualStateGroups>
	</Grid>
</UserControl>
```

- [ ] **Step 2: Create the code-behind**

Create `src/app/MZikmund.App/Controls/MzPageHeader.xaml.cs`:

```csharp
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;

namespace MZikmund.Controls;

public sealed partial class MzPageHeader : UserControl
{
	public static readonly DependencyProperty TitleProperty =
		DependencyProperty.Register(
			nameof(Title),
			typeof(string),
			typeof(MzPageHeader),
			new PropertyMetadata(string.Empty, OnTitleChanged));

	public static readonly DependencyProperty LeadProperty =
		DependencyProperty.Register(
			nameof(Lead),
			typeof(string),
			typeof(MzPageHeader),
			new PropertyMetadata(string.Empty, OnLeadChanged));

	public static readonly DependencyProperty BreadcrumbItemsProperty =
		DependencyProperty.Register(
			nameof(BreadcrumbItems),
			typeof(object),
			typeof(MzPageHeader),
			new PropertyMetadata(null, OnBreadcrumbItemsChanged));

	public static readonly DependencyProperty BreadcrumbCommandProperty =
		DependencyProperty.Register(
			nameof(BreadcrumbCommand),
			typeof(ICommand),
			typeof(MzPageHeader),
			new PropertyMetadata(null));

	public static readonly DependencyProperty ActionsProperty =
		DependencyProperty.Register(
			nameof(Actions),
			typeof(object),
			typeof(MzPageHeader),
			new PropertyMetadata(null, OnActionsChanged));

	public MzPageHeader()
	{
		InitializeComponent();
	}

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public string Lead
	{
		get => (string)GetValue(LeadProperty);
		set => SetValue(LeadProperty, value);
	}

	public object? BreadcrumbItems
	{
		get => GetValue(BreadcrumbItemsProperty);
		set => SetValue(BreadcrumbItemsProperty, value);
	}

	public ICommand? BreadcrumbCommand
	{
		get => (ICommand?)GetValue(BreadcrumbCommandProperty);
		set => SetValue(BreadcrumbCommandProperty, value);
	}

	/// <summary>
	/// Forward actions only ("Browse by category", "Subscribe"). The breadcrumb carries the way back.
	/// </summary>
	public object? Actions
	{
		get => GetValue(ActionsProperty);
		set => SetValue(ActionsProperty, value);
	}

	private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var header = (MzPageHeader)d;
		var text = e.NewValue as string ?? string.Empty;
		header.TitleText.Text = text;
		header.TitleText.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
	}

	private static void OnLeadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var header = (MzPageHeader)d;
		var text = e.NewValue as string ?? string.Empty;
		header.LeadText.Text = text;
		header.LeadText.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
	}

	private static void OnBreadcrumbItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var header = (MzPageHeader)d;
		header.BreadcrumbHost.ItemsSource = e.NewValue;
		header.BreadcrumbHost.Visibility = e.NewValue is null ? Visibility.Collapsed : Visibility.Visible;
	}

	private static void OnActionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var header = (MzPageHeader)d;
		header.ActionsHost.Content = e.NewValue;
		header.ActionsHost.Visibility = e.NewValue is null ? Visibility.Collapsed : Visibility.Visible;
	}

	private void OnBreadcrumbItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
	{
		if (BreadcrumbCommand is { } command && command.CanExecute(args.Item))
		{
			command.Execute(args.Item);
		}
	}
}
```

- [ ] **Step 3: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/app/MZikmund.App/Controls/MzPageHeader.xaml src/app/MZikmund.App/Controls/MzPageHeader.xaml.cs
git commit -m "feat: Add the reusable MzPageHeader page-header control"
```

---

### Task 39: `SettingsView` adopts `MzPageHeader`

`SettingsView` is the reference call site: it is one of the five pages that used to double up with the deleted `ContentHeader`. Phases 3 and 4 apply the same pattern to the rest.

**Files:**
- Modify: `src/app/MZikmund.App/Views/SettingsView.xaml:1-24`

**Interfaces:**
- Consumes: `MZikmund.Controls.MzPageHeader.Title`, `SettingsViewModel.Title` (already `=> Localizer.Instance.GetString("Settings")`)
- Produces: nothing new

- [ ] **Step 1: Rewrite the view**

Replace the entire contents of `src/app/MZikmund.App/Views/SettingsView.xaml` with:

```xml
<local:SettingsViewBase
	x:Class="MZikmund.Views.SettingsView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:controls="using:MZikmund.Controls"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:extensions="using:MZikmund.Extensions.Xaml"
	xmlns:local="using:MZikmund.Views"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	xmlns:mux="using:Microsoft.UI.Xaml.Controls"
	mc:Ignorable="d">

	<Grid Padding="16">
		<Grid.RowDefinitions>
			<RowDefinition Height="Auto" />
			<RowDefinition Height="*" />
			<RowDefinition Height="Auto" />
		</Grid.RowDefinitions>
		<controls:MzPageHeader Margin="0,0,0,24" Title="{x:Bind ViewModel.Title, Mode=OneWay}" />
		<mux:RadioButtons
			Grid.Row="1"
			ItemsSource="{x:Bind ViewModel.Themes, Mode=OneWay}"
			SelectedItem="{x:Bind ViewModel.SelectedTheme, Mode=TwoWay}" />
		<TextBlock
			Grid.Row="2"
			Foreground="{ThemeResource MzTextSecondaryBrush}"
			Style="{ThemeResource MzCaptionTextStyle}"
			Text="{x:Bind ViewModel.Version, Mode=OneWay}" />
	</Grid>
</local:SettingsViewBase>
```

- [ ] **Step 2: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 3: Visual check at both breakpoints**

Run the desktop head and open Settings at width **700**, then **1400**.

Expected: exactly one "Settings" heading (never two), rendered at the Title step at 700 and at the wider Title step at 1400.

- [ ] **Step 4: Commit**

```bash
git add src/app/MZikmund.App/Views/SettingsView.xaml
git commit -m "refactor: Give SettingsView its own MzPageHeader"
```

---

### Task 40: Tuned page transition — 333 ms `MzEaseEntrance`, opacity + 16px rise

`ScalarTransition`, `Vector3Transition`, `UIElement.Translation` and `UIElement.Scale` are `[NotImplemented]` in Uno, so this is a `Storyboard` over a `CompositeTransform`. The stock `NavigationThemeTransition` has a fixed duration and cannot be tuned, so Task 11 cleared `InnerFrame.ContentTransitions` and this replaces it. Only the page inside the `Frame` moves — the title bar and the nav pane are outside it and stay still.

**Files:**
- Create: `src/app/MZikmund.App/Infrastructure/MzPageTransition.cs`
- Modify: `src/app/MZikmund.App/Views/PageBase.cs:1-3` (usings), `:10-21` (fields/ctor), `:38-41` (`PageLoaded`), `:58-61` (`PageUnloaded`)

**Interfaces:**
- Consumes (Phase 1): `MzEaseEntrance` (a `KeySpline` application resource, `0,0,0,1`)
- Produces: `MZikmund.Infrastructure.MzPageTransition.AnimationsEnabled : bool`, `MzPageTransition.PlayEntrance(FrameworkElement) : Storyboard?`

- [ ] **Step 1: Create the transition**

Create `src/app/MZikmund.App/Infrastructure/MzPageTransition.cs`:

```csharp
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace MZikmund.Infrastructure;

/// <summary>
/// The tuned page entrance: opacity 0 -> 1 and a 16px rise over MzDurationSlow with MzEaseEntrance.
/// Uno has no ScalarTransition/Vector3Transition and no UIElement.Translation, so this uses a
/// Storyboard over a CompositeTransform.
/// </summary>
public static class MzPageTransition
{
	private const double EntranceOffsetY = 16d;
	private static readonly TimeSpan EntranceDuration = TimeSpan.FromMilliseconds(333);

	/// <summary>
	/// Set to false by the in-app reduced-motion preference.
	/// </summary>
	public static bool AnimationsEnabled { get; set; } = true;

	public static Storyboard? PlayEntrance(FrameworkElement target)
	{
		ArgumentNullException.ThrowIfNull(target);

		if (!AnimationsEnabled)
		{
			target.Opacity = 1d;
			return null;
		}

		if (target.RenderTransform is not CompositeTransform transform)
		{
			transform = new CompositeTransform();
			target.RenderTransform = transform;
		}

		transform.TranslateY = EntranceOffsetY;
		target.Opacity = 0d;

		var opacity = new DoubleAnimationUsingKeyFrames { EnableDependentAnimation = true };
		opacity.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0d });
		opacity.KeyFrames.Add(new SplineDoubleKeyFrame
		{
			KeyTime = EntranceDuration,
			Value = 1d,
			KeySpline = CreateEntranceEasing(),
		});
		Storyboard.SetTarget(opacity, target);
		Storyboard.SetTargetProperty(opacity, "Opacity");

		var rise = new DoubleAnimationUsingKeyFrames { EnableDependentAnimation = true };
		rise.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = EntranceOffsetY });
		rise.KeyFrames.Add(new SplineDoubleKeyFrame
		{
			KeyTime = EntranceDuration,
			Value = 0d,
			KeySpline = CreateEntranceEasing(),
		});
		Storyboard.SetTarget(rise, target);
		Storyboard.SetTargetProperty(rise, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");

		var storyboard = new Storyboard();
		storyboard.Children.Add(opacity);
		storyboard.Children.Add(rise);
		storyboard.Begin();
		return storyboard;
	}

	public static void Reset(FrameworkElement target)
	{
		ArgumentNullException.ThrowIfNull(target);

		target.Opacity = 1d;
		if (target.RenderTransform is CompositeTransform transform)
		{
			transform.TranslateY = 0d;
		}
	}

	// A fresh KeySpline per key frame: a single DependencyObject cannot be attached twice.
	private static KeySpline CreateEntranceEasing()
	{
		if (Application.Current.Resources.TryGetValue("MzEaseEntrance", out var value) && value is KeySpline source)
		{
			return new KeySpline
			{
				ControlPoint1 = source.ControlPoint1,
				ControlPoint2 = source.ControlPoint2,
			};
		}

		return new KeySpline
		{
			ControlPoint1 = new Point(0d, 0d),
			ControlPoint2 = new Point(0d, 1d),
		};
	}
}
```

- [ ] **Step 2: Add the using to `PageBase`**

In `src/app/MZikmund.App/Views/PageBase.cs`, replace:

```csharp
using Microsoft.UI.Xaml.Navigation;
using MZikmund.App.Core.Infrastructure;
using MZikmund.ViewModels;
```

with:

```csharp
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using MZikmund.App.Core.Infrastructure;
using MZikmund.Infrastructure;
using MZikmund.ViewModels;
```

- [ ] **Step 3: Hold the storyboard**

In `src/app/MZikmund.App/Views/PageBase.cs`, replace:

```csharp
	private object? _pendingParameter;
	private bool _isNavigationDelayed;
```

with:

```csharp
	private object? _pendingParameter;
	private bool _isNavigationDelayed;
	private Storyboard? _entranceStoryboard;
```

- [ ] **Step 4: Play it on load, clear it on unload**

In the same file, replace:

```csharp
	private void PageLoaded(object sender, RoutedEventArgs e)
	{
		ViewModel?.ViewLoaded();
	}
```

with:

```csharp
	private void PageLoaded(object sender, RoutedEventArgs e)
	{
		ViewModel?.ViewLoaded();
		_entranceStoryboard = MzPageTransition.PlayEntrance(this);
	}
```

and replace:

```csharp
	private void PageUnloaded(object sender, RoutedEventArgs e)
	{
		ViewModel?.ViewUnloaded();
	}
```

with:

```csharp
	private void PageUnloaded(object sender, RoutedEventArgs e)
	{
		_entranceStoryboard?.Stop();
		_entranceStoryboard = null;
		MzPageTransition.Reset(this);
		ViewModel?.ViewUnloaded();
	}
```

- [ ] **Step 5: Build**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 6: Visual check — the chrome must not move**

Run the desktop head and switch Blog → Videos → Settings repeatedly.

Expected: each page fades in while rising ~16px over a third of a second; the title bar, the nav pane and the back button stay perfectly still. Nothing jumps, and no page is left at partial opacity after a fast double-navigation.

- [ ] **Step 7: Commit**

```bash
git add src/app/MZikmund.App/Infrastructure/MzPageTransition.cs src/app/MZikmund.App/Views/PageBase.cs
git commit -m "feat: Replace the stock frame transition with a tuned 333ms page entrance"
```

---

### Task 41: Format, multi-TFM build, light/dark verification

**Files:**
- Modify: any file `dotnet format` rewrites

**Interfaces:**
- Consumes: everything above
- Produces: a formatted, four-TFM-green shell

- [ ] **Step 1: Format**

```bash
dotnet format MZikmund.slnx
```

Expected: exits 0.

- [ ] **Step 2: Build the Windows head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 3: Build the Desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Build the Android head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Build the iOS head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
```

Expected: `Build succeeded`, 0 errors. (WASM is CI-verified only — this machine has no usable Emscripten pack.)

- [ ] **Step 6: Audit every VSM animation added this phase**

```bash
grep -rn "DoubleAnimation\|ObjectAnimation\|ColorAnimation" src/app/MZikmund.App
```

Expected: the only hits are the two `DoubleAnimationUsingKeyFrames` in `MzPageTransition.cs`, and both carry `EnableDependentAnimation = true`. Anything else Uno drops silently.

- [ ] **Step 7: Full visual pass — light theme**

Drive the desktop head with the Uno App MCP and screenshot Blog, Videos and Settings at widths 700 / 1000 / 1400 in **Light**.

Expected: 56px acrylic title bar with logo and Medium-weight title on every screenshot; pane mode LeftMinimal / LeftCompact / Top; exactly one heading on Settings; no duplicate title band anywhere; the Frame content sits on the ground with no seam.

- [ ] **Step 8: Full visual pass — dark theme**

Switch to Dark in Settings and repeat all nine screenshots.

Expected: identical layout, acrylic tinted dark, `MzStrokeDividerBrush` under the title bar still visible, text at `MzTextPrimaryBrush` / `MzTextSecondaryBrush` with no washed-out or unreadable label.

- [ ] **Step 9: Windows head sanity check**

```bash
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

Expected: Mica visible behind the content, the acrylic title bar reads as a distinct material over it, the caption buttons sit in the top-right of the 56px band, and window dragging works anywhere on that band.

- [ ] **Step 10: Commit**

```bash
git add -A src/app
git commit -m "style: Run dotnet format after the shell and navigation rework"
```


---

## Phase 3 — Public views

## HomeView (new) — Phase 3

### Interfaces

PRODUCES (later work may consume):
- `MZikmund.ViewModels.HomeViewModel : PageViewModel` — `string Title` (Localizer "Home"), `string Eyebrow`, `string Headline`, `string Lead`, `string Topics`, `ObservableCollection<PostListItemViewModel> LatestPosts`, `bool IsLoading`, `bool HasError`, `ICommand BrowseBlogCommand`, `ICommand SubscribeCommand`, `ICommand RetryCommand`, `void NavigateToPost(Guid postId)`. Registered `services.AddScoped<HomeViewModel>()` in `App.xaml.cs`.
- `MZikmund.Views.HomeView : HomeViewBase`, `public partial class HomeViewBase : PageBase<HomeViewModel>` — satisfies the Phase 2 nav contract `Tag="HomeViewModel"` via `RegisterViewsFromAssembly` convention.
- Named XAML elements in `HomeView.xaml`: `Scroller`, `Hero`, `HeroGlow`, `HeroGrain`, `HeroInner`, `HeroEyebrow`, `HeroTitle`, `HeroRule`, `HeroLead`, `HeroActions`, `HeroTopics`, and transforms `HeroEyebrowTransform`, `HeroTitleTransform`, `HeroRuleTransform`, `HeroLeadTransform`, `HeroActionsTransform`, `HeroTopicsTransform`; `Strip`, `StripHeading`. Storyboard resource key `HeroEntrance`. Page-scoped themed brush key `MzHeroGlowBrush` (Light/Default/HighContrast).
- Asset `ms-appx:///Assets/HeroNoise.png` + generator `src/app/MZikmund.App/Assets/generate-hero-noise.py`.
- Localized keys added to en + cs: `HomeEyebrow`, `HomeHeadline`, `HomeLead`, `BrowseTheBlog`, `Subscribe`, `LatestPosts`, `ViewAll`.

CONSUMES from Phase 1: brushes `MzBgBaseBrush`, `MzBgTertiaryBrush`, `MzAccentSubtleBrush`, `MzAccentTextBrush`, `MzTextTertiaryBrush`; radii `MzPillCornerRadius`; motion `MzEaseEntrance`, `MzDurationSlow`; type `MzCaptionTextStyle`, `MzDisplayTextStyle`, `MzDisplayWideTextStyle`, `MzLeadTextStyle`, `MzSectionHeadingTextStyle`; styles `MzAccentButtonStyle`, `MzStandardButtonStyle`, `MzSubtleButtonSmallStyle`, `MzContentCardStyle`, `MzEmptyStatePanelStyle`, `MzEmptyStateIconStyle`, `MzEmptyStateTitleTextStyle`, `MzEmptyStateBodyTextStyle`; glyphs `MzGlyphWarning`; `ICardItem` via `PostListItemViewModel`. Existing: `BoolToVisibilityConverter`, `extensions:Localize`, keys `CouldNotLoadData`, `ErrorLoadingData`, `Retry`, `ReadMore`, `Home`.
CONSUMES from Phase 2: `MZikmund.Infrastructure.MzPageTransition.AnimationsEnabled`; nav item already ships `Tag="HomeViewModel"` (no shell edit).

### Task 42: Spike — prove the hero glow and the grain on Uno before designing around them

Two unknowns block the hero: `RadialGradientBrush` is unverified on Uno, and WinUI's `ImageBrush` has no `TileMode`. This task lands the real `HomeViewModel` shell, the real `HomeView` file and the noise asset, then paints all three candidates side by side so the verdict is observed, not assumed.

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/HomeViewModel.cs`
- Create: `src/app/MZikmund.App/Views/HomeView.xaml`, `src/app/MZikmund.App/Views/HomeView.xaml.cs`
- Create: `src/app/MZikmund.App/Assets/generate-hero-noise.py`, `src/app/MZikmund.App/Assets/HeroNoise.png`
- Modify: `src/app/MZikmund.App/App.xaml.cs:127`

**Interfaces:**
- Consumes: `PageBase<TViewModel>`, `PageViewModel`, `MzBgTertiaryBrush`, `MzCaptionTextStyle`.
- Produces: `HomeViewModel` (stub surface), `HomeView`/`HomeViewBase`, `Assets/HeroNoise.png`, the recorded glow verdict.

- [ ] **Step 1: Write the noise generator**

Pure stdlib — no Pillow. Colour type 4 (grey + alpha) halves the payload versus RGBA, and quantising alpha to four levels keeps the deflate stream small.

`src/app/MZikmund.App/Assets/generate-hero-noise.py`:
```python
# Regenerates Assets/HeroNoise.png — the Mica-like grain for the Home hero.
# WinUI ImageBrush has no TileMode, so this ships pre-sized and is drawn with
# Stretch="UniformToFill" rather than tiled.
import os
import random
import struct
import zlib

SIZE = 512
random.seed(20260811)

raw = bytearray()
for _ in range(SIZE):
	raw.append(0)  # PNG filter type 0 (None) per scanline
	for _ in range(SIZE):
		raw.append(255 if random.getrandbits(1) else 0)
		raw.append(random.choice((0, 10, 20, 30)))


def chunk(tag, data):
	return (struct.pack('>I', len(data)) + tag + data
		+ struct.pack('>I', zlib.crc32(tag + data) & 0xFFFFFFFF))


png = b'\x89PNG\r\n\x1a\n'
png += chunk(b'IHDR', struct.pack('>IIBBBBB', SIZE, SIZE, 8, 4, 0, 0, 0))
png += chunk(b'IDAT', zlib.compress(bytes(raw), 9))
png += chunk(b'IEND', b'')

path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'HeroNoise.png')
with open(path, 'wb') as handle:
	handle.write(png)

print('HeroNoise.png: %d bytes' % os.path.getsize(path))
```

- [ ] **Step 2: Generate the asset / Expected: `HeroNoise.png: <bytes>` and a file of roughly 150–330 KB**
```bash
python src/app/MZikmund.App/Assets/generate-hero-noise.py
```
If the file exceeds **340 KB**, lower `SIZE` to 384 and rerun. If it still exceeds 340 KB, the grain is not worth its weight — record that and take the drop-the-grain branch in Step 7.

- [ ] **Step 3: Create the ViewModel stub**

`src/app/MZikmund.App.Core/ViewModels/HomeViewModel.cs`:
```csharp
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels;

public class HomeViewModel : PageViewModel
{
	public override string Title => Localizer.Instance.GetString("Home");
}
```

- [ ] **Step 4: Create the probe view**

`src/app/MZikmund.App/Views/HomeView.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<local:HomeViewBase
	x:Class="MZikmund.Views.HomeView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:local="using:MZikmund.Views"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	Background="{ThemeResource MzBgTertiaryBrush}"
	mc:Ignorable="d">

	<StackPanel Padding="16" Spacing="8">
		<TextBlock Style="{StaticResource MzCaptionTextStyle}" Text="A — RadialGradientBrush" />
		<Border Height="220">
			<Border.Background>
				<RadialGradientBrush
					Center="0.5,0.5"
					GradientOrigin="0.5,0.5"
					RadiusX="0.5"
					RadiusY="0.5">
					<GradientStop Offset="0.0" Color="#FF0078D4" />
					<GradientStop Offset="0.62" Color="#000078D4" />
				</RadialGradientBrush>
			</Border.Background>
		</Border>

		<TextBlock Style="{StaticResource MzCaptionTextStyle}" Text="B — layered LinearGradientBrush fallback" />
		<Grid Height="220">
			<Border>
				<Border.Background>
					<LinearGradientBrush StartPoint="1,0" EndPoint="0,1">
						<GradientStop Offset="0.0" Color="#FF0078D4" />
						<GradientStop Offset="0.62" Color="#000078D4" />
					</LinearGradientBrush>
				</Border.Background>
			</Border>
			<Border>
				<Border.Background>
					<LinearGradientBrush StartPoint="1,1" EndPoint="0,0">
						<GradientStop Offset="0.0" Color="#990078D4" />
						<GradientStop Offset="0.55" Color="#000078D4" />
					</LinearGradientBrush>
				</Border.Background>
			</Border>
		</Grid>

		<TextBlock Style="{StaticResource MzCaptionTextStyle}" Text="C — noise asset, UniformToFill" />
		<Border Height="220" Opacity="0.07">
			<Border.Background>
				<ImageBrush ImageSource="ms-appx:///Assets/HeroNoise.png" Stretch="UniformToFill" />
			</Border.Background>
		</Border>
	</StackPanel>
</local:HomeViewBase>
```

`src/app/MZikmund.App/Views/HomeView.xaml.cs`:
```csharp
using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class HomeView : HomeViewBase
{
	public HomeView()
	{
		this.InitializeComponent();
	}
}

public partial class HomeViewBase : PageBase<HomeViewModel>
{
}
```

- [ ] **Step 5: Register the ViewModel**

In `src/app/MZikmund.App/App.xaml.cs`, replace:
```csharp
		services.AddScoped<BlogViewModel>();
```
with:
```csharp
		services.AddScoped<HomeViewModel>();
		services.AddScoped<BlogViewModel>();
```

- [ ] **Step 6: Build and run the desktop head / Expected: `Build succeeded`, then the app launches on Home**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```
Navigate to the Home nav item (Phase 2 already ships it with `Tag="HomeViewModel"`) and screenshot with the Uno App MCP in both light and dark.

- [ ] **Step 7: Record the verdict — this decides the hero markup**

Three observations, three consequences:

**(a) Band A.** *Expected pass:* a soft circular blue falloff, no banding, identical in both themes. → the hero keeps `RadialGradientBrush` in `MzHeroGlowBrush`; no change to the next task.
*Expected failure modes:* a flat `#FF0078D4` fill (brush not implemented, first stop wins), a transparent band, or a `XamlParseException` naming `RadialGradientBrush`. → in the next task, replace the `MzHeroGlowBrush` `RadialGradientBrush` in **all three** theme dictionaries with band B's two-`LinearGradientBrush` grid, keeping the same per-theme colours (`#290078D4`/`#000078D4` light, `#3376B9ED`/`#0076B9ED` dark, `Transparent` high-contrast); the `HeroGlow` `Border` becomes a `Grid` holding the two `Border`s. Do **not** pre-render a PNG glow — a 560px gradient asset costs more than the layered brush and cannot follow the theme.

**(b) Band C.** *Expected pass:* an even, fine speckle with no seam and no visible repeat. → keep `HeroGrain`.
*Expected failure modes:* nothing renders (asset not deployed), or the speckle reads as blotches/banding at 512→~1200px upscale. → delete the `HeroGrain` `Border` from the hero, delete `Assets/HeroNoise.png` and `Assets/generate-hero-noise.py`, and keep the glow alone. The spec sanctions exactly this ("drop the grain and keep only the glow"). There is **no** tiling option: WinUI's `ImageBrush` has no `TileMode`, and `Stretch="None"` paints a single 512px patch instead of repeating.

**(c) Asset weight.** If Step 2 reported over 340 KB, take the (b) drop branch regardless of how it looks.

- [ ] **Step 8: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/HomeViewModel.cs src/app/MZikmund.App/Views/HomeView.xaml src/app/MZikmund.App/Views/HomeView.xaml.cs src/app/MZikmund.App/Assets/generate-hero-noise.py src/app/MZikmund.App/Assets/HeroNoise.png src/app/MZikmund.App/App.xaml.cs
git commit -m "feat: Add the Home page shell and verify the hero glow and grain on Uno"
```

---

### Task 43: HomeViewModel — latest posts, topics and the two forward actions

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/HomeViewModel.cs:1-9` (full replacement)
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw` (append before `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (append before `</root>`)

**Interfaces:**
- Consumes: `IMZikmundApi.GetPostsAsync(int)`, `IMZikmundApi.GetCategoriesAsync()`, `IMarkdownConverter`, `INavigationService.Navigate<TViewModel>()`, `PostListItemViewModel` (`ICardItem`), `IOptions<AppConfig>`, `Localizer.Instance`.
- Produces: the full `HomeViewModel` surface and the seven new resource keys.

- [ ] **Step 1: Replace `src/app/MZikmund.App.Core/ViewModels/HomeViewModel.cs` in full**

Three posts, not a page: the strip is a taste of the blog, and the blog itself owns paging. Categories double as the hero's topic line, which is why "browse by category" needs no separate destination — the categories are on screen and the accent action carries the visitor into the blog.
```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public class HomeViewModel : PageViewModel
{
	private const int LatestPostCount = 3;
	private const int TopicCount = 6;

	private readonly IMZikmundApi _api;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly INavigationService _navigationService;
	private readonly IOptions<AppConfig> _appConfig;
	private readonly ILogger<HomeViewModel> _logger;
	private string _topics = "";
	private bool _isLoading;
	private bool _hasError;

	public HomeViewModel(
		IMZikmundApi api,
		IMarkdownConverter markdownConverter,
		INavigationService navigationService,
		IOptions<AppConfig> appConfig,
		ILogger<HomeViewModel> logger)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_markdownConverter = markdownConverter;
		_navigationService = navigationService;
		_appConfig = appConfig;
		_logger = logger;
		BrowseBlogCommand = new RelayCommand(() => _navigationService.Navigate<BlogViewModel>());
		SubscribeCommand = new AsyncRelayCommand(SubscribeAsync);
		RetryCommand = new AsyncRelayCommand(ReloadAsync);
	}

	public override string Title => Localizer.Instance.GetString("Home");

	public string Eyebrow => Localizer.Instance.GetString("HomeEyebrow");

	public string Headline => Localizer.Instance.GetString("HomeHeadline");

	public string Lead => Localizer.Instance.GetString("HomeLead");

	public ObservableCollection<PostListItemViewModel> LatestPosts { get; } = new();

	public ICommand BrowseBlogCommand { get; }

	public ICommand SubscribeCommand { get; }

	public ICommand RetryCommand { get; }

	/// <summary>
	/// Structured metadata rather than marketing copy: the live category names.
	/// </summary>
	public string Topics
	{
		get => _topics;
		private set => SetProperty(ref _topics, value);
	}

	public bool IsLoading
	{
		get => _isLoading;
		private set => SetProperty(ref _isLoading, value);
	}

	public bool HasError
	{
		get => _hasError;
		private set => SetProperty(ref _hasError, value);
	}

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);

		if (LatestPosts.Count == 0)
		{
			await ReloadAsync();
		}
	}

	public void NavigateToPost(Guid postId) => _navigationService.Navigate<PostViewModel>(postId);

	private async Task ReloadAsync()
	{
		HasError = false;
		IsLoading = true;
		try
		{
			await Task.WhenAll(LoadLatestPostsAsync(), LoadTopicsAsync());
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task LoadLatestPostsAsync()
	{
		try
		{
			var response = await _api.GetPostsAsync(1);
			if (response.Content is not { } content)
			{
				HasError = true;
				return;
			}

			LatestPosts.Clear();
			foreach (var post in content.Data.Take(LatestPostCount))
			{
				LatestPosts.Add(new PostListItemViewModel(post, _markdownConverter));
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading the latest posts failed");
			HasError = true;
		}
	}

	private async Task LoadTopicsAsync()
	{
		try
		{
			var response = await _api.GetCategoriesAsync();
			if (response.Content is not { } categories)
			{
				return;
			}

			Topics = string.Join("   ·   ", categories.Take(TopicCount).Select(category => category.DisplayName));
		}
		catch (Exception ex)
		{
			// The topic line is decoration; a failure here must not fault the page.
			_logger.LogWarning(ex, "Loading the hero topics failed");
		}
	}

	private async Task SubscribeAsync()
	{
		if (_appConfig.Value.WebUrl is not { } webUrl)
		{
			return;
		}

		try
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri($"{webUrl.TrimEnd('/')}/rss"));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Opening the feed failed");
		}
	}
}
```

- [ ] **Step 2: Append the English strings**

In `src/app/MZikmund.App/Strings/en/Resources.resw`, insert immediately before the closing `</root>`:
```xml
  <data name="HomeEyebrow" xml:space="preserve">
    <value>Microsoft MVP · Uno Platform</value>
  </data>
  <data name="HomeHeadline" xml:space="preserve">
    <value>Martin Zikmund</value>
  </data>
  <data name="HomeLead" xml:space="preserve">
    <value>I write about .NET, WinUI and Uno Platform — and about getting one codebase onto every screen.</value>
  </data>
  <data name="BrowseTheBlog" xml:space="preserve">
    <value>Browse the blog</value>
  </data>
  <data name="Subscribe" xml:space="preserve">
    <value>Subscribe</value>
  </data>
  <data name="LatestPosts" xml:space="preserve">
    <value>Latest posts</value>
  </data>
  <data name="ViewAll" xml:space="preserve">
    <value>View all</value>
  </data>
```

- [ ] **Step 3: Append the Czech strings**

In `src/app/MZikmund.App/Strings/cs/Resources.resw`, insert immediately before the closing `</root>`:
```xml
  <data name="HomeEyebrow" xml:space="preserve">
    <value>Microsoft MVP · Uno Platform</value>
  </data>
  <data name="HomeHeadline" xml:space="preserve">
    <value>Martin Zikmund</value>
  </data>
  <data name="HomeLead" xml:space="preserve">
    <value>Píšu o .NET, WinUI a Uno Platform — a o tom, jak dostat jednu kódovou základnu na všechny obrazovky.</value>
  </data>
  <data name="BrowseTheBlog" xml:space="preserve">
    <value>Procházet blog</value>
  </data>
  <data name="Subscribe" xml:space="preserve">
    <value>Odebírat</value>
  </data>
  <data name="LatestPosts" xml:space="preserve">
    <value>Nejnovější články</value>
  </data>
  <data name="ViewAll" xml:space="preserve">
    <value>Zobrazit vše</value>
  </data>
```

- [ ] **Step 4: Build / Expected: `Build succeeded`**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/HomeViewModel.cs src/app/MZikmund.App/Strings
git commit -m "feat: Give HomeViewModel latest posts, live topics and forward actions"
```

---

### Task 44: The hero and the latest-posts strip

Replaces the probe with the real page. The hero is **page-ground family** — `MzBg*` alone, no elevation, and **no bottom rule**: the tone step from the hero's `MzBgTertiaryBrush` down to the page's `MzBgBaseBrush` is the boundary, so a divider would be a second boundary doing the first one's job.

**Files:**
- Modify: `src/app/MZikmund.App/Views/HomeView.xaml:1-53` (full replacement)
- Modify: `src/app/MZikmund.App/Views/HomeView.xaml.cs:1-15` (full replacement)

**Interfaces:**
- Consumes: everything listed in the slice interfaces; `MzPageTransition.AnimationsEnabled` (Phase 2).
- Produces: `MzHeroGlowBrush`, `HeroEntrance`, and the named hero/strip elements.

- [ ] **Step 1: Replace `src/app/MZikmund.App/Views/HomeView.xaml` in full**

`ScalarTransition`, `UIElement.Translation` and `UIElement.Scale` are all unimplemented on Uno, so every entrance moves a `CompositeTransform` from a `Storyboard`, and each transform is named so the storyboard targets it directly instead of walking a `(UIElement.RenderTransform).(CompositeTransform.…)` property path. `EnableDependentAnimation="True"` is set on all of them — it is a no-op for the independent ones and the only thing that stops Uno silently dropping the rest.

If the spike's band A failed, swap the three `RadialGradientBrush` definitions for the layered fallback described there; nothing outside this resource block changes.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<local:HomeViewBase
	x:Class="MZikmund.Views.HomeView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:extensions="using:MZikmund.Extensions.Xaml"
	xmlns:local="using:MZikmund.Views"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	xmlns:vmitems="using:MZikmund.ViewModels.Items"
	Background="{ThemeResource MzBgBaseBrush}"
	mc:Ignorable="d">

	<local:HomeViewBase.Resources>
		<ResourceDictionary>
			<ResourceDictionary.ThemeDictionaries>
				<ResourceDictionary x:Key="Light">
					<RadialGradientBrush
						x:Key="MzHeroGlowBrush"
						Center="0.5,0.5"
						GradientOrigin="0.5,0.5"
						RadiusX="0.5"
						RadiusY="0.5">
						<GradientStop Offset="0.0" Color="#290078D4" />
						<GradientStop Offset="0.62" Color="#000078D4" />
					</RadialGradientBrush>
				</ResourceDictionary>
				<ResourceDictionary x:Key="Default">
					<RadialGradientBrush
						x:Key="MzHeroGlowBrush"
						Center="0.5,0.5"
						GradientOrigin="0.5,0.5"
						RadiusX="0.5"
						RadiusY="0.5">
						<GradientStop Offset="0.0" Color="#3376B9ED" />
						<GradientStop Offset="0.62" Color="#0076B9ED" />
					</RadialGradientBrush>
				</ResourceDictionary>
				<ResourceDictionary x:Key="HighContrast">
					<SolidColorBrush x:Key="MzHeroGlowBrush" Color="Transparent" />
				</ResourceDictionary>
			</ResourceDictionary.ThemeDictionaries>

			<!--  0 / 60 / 120 / 160 / 220 / 280 ms — the eye is led eyebrow → name → rule → lead → actions.  -->
			<Storyboard x:Key="HeroEntrance">
				<DoubleAnimationUsingKeyFrames
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroEyebrow"
					Storyboard.TargetProperty="Opacity">
					<LinearDoubleKeyFrame KeyTime="0" Value="0" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="1" />
				</DoubleAnimationUsingKeyFrames>
				<DoubleAnimationUsingKeyFrames
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroEyebrowTransform"
					Storyboard.TargetProperty="TranslateY">
					<LinearDoubleKeyFrame KeyTime="0" Value="12" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="0" />
				</DoubleAnimationUsingKeyFrames>

				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.06"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroTitle"
					Storyboard.TargetProperty="Opacity">
					<LinearDoubleKeyFrame KeyTime="0" Value="0" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="1" />
				</DoubleAnimationUsingKeyFrames>
				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.06"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroTitleTransform"
					Storyboard.TargetProperty="TranslateY">
					<LinearDoubleKeyFrame KeyTime="0" Value="12" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="0" />
				</DoubleAnimationUsingKeyFrames>

				<!--  The rule draws itself out from the left rather than fading.  -->
				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.12"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroRule"
					Storyboard.TargetProperty="Opacity">
					<LinearDoubleKeyFrame KeyTime="0" Value="0" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="1" />
				</DoubleAnimationUsingKeyFrames>
				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.12"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroRuleTransform"
					Storyboard.TargetProperty="ScaleX">
					<LinearDoubleKeyFrame KeyTime="0" Value="0" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="1" />
				</DoubleAnimationUsingKeyFrames>

				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.16"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroLead"
					Storyboard.TargetProperty="Opacity">
					<LinearDoubleKeyFrame KeyTime="0" Value="0" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="1" />
				</DoubleAnimationUsingKeyFrames>
				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.16"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroLeadTransform"
					Storyboard.TargetProperty="TranslateY">
					<LinearDoubleKeyFrame KeyTime="0" Value="12" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="0" />
				</DoubleAnimationUsingKeyFrames>

				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.22"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroActions"
					Storyboard.TargetProperty="Opacity">
					<LinearDoubleKeyFrame KeyTime="0" Value="0" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="1" />
				</DoubleAnimationUsingKeyFrames>
				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.22"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroActionsTransform"
					Storyboard.TargetProperty="TranslateY">
					<LinearDoubleKeyFrame KeyTime="0" Value="12" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="0" />
				</DoubleAnimationUsingKeyFrames>

				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.28"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroTopics"
					Storyboard.TargetProperty="Opacity">
					<LinearDoubleKeyFrame KeyTime="0" Value="0" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="1" />
				</DoubleAnimationUsingKeyFrames>
				<DoubleAnimationUsingKeyFrames
					BeginTime="0:0:0.28"
					EnableDependentAnimation="True"
					Storyboard.TargetName="HeroTopicsTransform"
					Storyboard.TargetProperty="TranslateY">
					<LinearDoubleKeyFrame KeyTime="0" Value="12" />
					<SplineDoubleKeyFrame
						KeySpline="{StaticResource MzEaseEntrance}"
						KeyTime="{StaticResource MzDurationSlow}"
						Value="0" />
				</DoubleAnimationUsingKeyFrames>
			</Storyboard>
		</ResourceDictionary>
	</local:HomeViewBase.Resources>

	<ScrollViewer x:Name="Scroller">
		<StackPanel>

			<!--  HERO — page ground: MzBg* alone, never an elevation, and no bottom rule.  -->
			<Grid
				x:Name="Hero"
				Padding="16,64,16,48"
				SizeChanged="Hero_SizeChanged">
				<!--  Ground layer: childless, so it stays legal if it ever becomes acrylic.  -->
				<Border Background="{ThemeResource MzBgTertiaryBrush}" />

				<!--  Accent glow, offset top-right so it never sits behind the text.  -->
				<Border
					x:Name="HeroGlow"
					Width="560"
					Height="560"
					Margin="0,-180,-96,0"
					HorizontalAlignment="Right"
					VerticalAlignment="Top"
					Background="{ThemeResource MzHeroGlowBrush}"
					IsHitTestVisible="False" />

				<!--  Mica-like grain. Pre-sized: WinUI ImageBrush has no TileMode.  -->
				<Border
					x:Name="HeroGrain"
					IsHitTestVisible="False"
					Opacity="0.07">
					<Border.Background>
						<ImageBrush ImageSource="ms-appx:///Assets/HeroNoise.png" Stretch="UniformToFill" />
					</Border.Background>
				</Border>

				<StackPanel
					x:Name="HeroInner"
					MaxWidth="736"
					HorizontalAlignment="Left">
					<Border
						x:Name="HeroEyebrow"
						Margin="0,0,0,16"
						Padding="12,4,12,4"
						HorizontalAlignment="Left"
						Background="{ThemeResource MzAccentSubtleBrush}"
						CornerRadius="{StaticResource MzPillCornerRadius}"
						Opacity="0">
						<Border.RenderTransform>
							<CompositeTransform x:Name="HeroEyebrowTransform" TranslateY="12" />
						</Border.RenderTransform>
						<TextBlock
							CharacterSpacing="20"
							FontFamily="{StaticResource MzFontFamilySemiBold}"
							FontWeight="SemiBold"
							Foreground="{ThemeResource MzAccentTextBrush}"
							Style="{StaticResource MzCaptionTextStyle}"
							Text="{x:Bind ViewModel.Eyebrow, Mode=OneWay}" />
					</Border>

					<TextBlock
						x:Name="HeroTitle"
						Margin="0,0,0,16"
						Opacity="0"
						Style="{StaticResource MzDisplayTextStyle}"
						Text="{x:Bind ViewModel.Headline, Mode=OneWay}">
						<TextBlock.RenderTransform>
							<CompositeTransform x:Name="HeroTitleTransform" TranslateY="12" />
						</TextBlock.RenderTransform>
					</TextBlock>

					<Border
						x:Name="HeroRule"
						Width="56"
						Height="4"
						Margin="0,0,0,24"
						HorizontalAlignment="Left"
						Background="{ThemeResource MzAccentTextBrush}"
						CornerRadius="{StaticResource MzPillCornerRadius}"
						Opacity="0"
						RenderTransformOrigin="0,0.5">
						<Border.RenderTransform>
							<CompositeTransform x:Name="HeroRuleTransform" ScaleX="0" />
						</Border.RenderTransform>
					</Border>

					<TextBlock
						x:Name="HeroLead"
						MaxWidth="560"
						Margin="0,0,0,32"
						HorizontalAlignment="Left"
						Opacity="0"
						Style="{StaticResource MzLeadTextStyle}"
						Text="{x:Bind ViewModel.Lead, Mode=OneWay}">
						<TextBlock.RenderTransform>
							<CompositeTransform x:Name="HeroLeadTransform" TranslateY="12" />
						</TextBlock.RenderTransform>
					</TextBlock>

					<!--  Forward actions only. The way back is the shell's job.  -->
					<StackPanel
						x:Name="HeroActions"
						Margin="0,0,0,36"
						Opacity="0"
						Orientation="Horizontal"
						Spacing="12">
						<StackPanel.RenderTransform>
							<CompositeTransform x:Name="HeroActionsTransform" TranslateY="12" />
						</StackPanel.RenderTransform>
						<Button
							Command="{x:Bind ViewModel.BrowseBlogCommand, Mode=OneWay}"
							Content="{extensions:Localize Key=BrowseTheBlog}"
							Style="{StaticResource MzAccentButtonStyle}" />
						<Button
							Command="{x:Bind ViewModel.SubscribeCommand, Mode=OneWay}"
							Content="{extensions:Localize Key=Subscribe}"
							Style="{StaticResource MzStandardButtonStyle}" />
					</StackPanel>

					<!--  Structured metadata rather than marketing copy.  -->
					<TextBlock
						x:Name="HeroTopics"
						CharacterSpacing="20"
						Foreground="{ThemeResource MzTextTertiaryBrush}"
						Opacity="0"
						Style="{StaticResource MzCaptionTextStyle}"
						Text="{x:Bind ViewModel.Topics, Mode=OneWay}">
						<TextBlock.RenderTransform>
							<CompositeTransform x:Name="HeroTopicsTransform" TranslateY="12" />
						</TextBlock.RenderTransform>
					</TextBlock>
				</StackPanel>
			</Grid>

			<!--  LATEST POSTS — sits on the page's own ground; the tone step is the boundary.  -->
			<Grid
				x:Name="Strip"
				Padding="16,36,16,48"
				RowSpacing="24">
				<Grid.RowDefinitions>
					<RowDefinition Height="Auto" />
					<RowDefinition Height="Auto" />
				</Grid.RowDefinitions>

				<Grid Grid.Row="0" ColumnSpacing="16">
					<Grid.ColumnDefinitions>
						<ColumnDefinition Width="*" />
						<ColumnDefinition Width="Auto" />
					</Grid.ColumnDefinitions>
					<TextBlock
						x:Name="StripHeading"
						VerticalAlignment="Center"
						Style="{StaticResource MzSectionHeadingTextStyle}"
						Text="{extensions:Localize Key=LatestPosts}" />
					<Button
						Grid.Column="1"
						VerticalAlignment="Center"
						Command="{x:Bind ViewModel.BrowseBlogCommand, Mode=OneWay}"
						Content="{extensions:Localize Key=ViewAll}"
						Style="{StaticResource MzSubtleButtonSmallStyle}" />
				</Grid>

				<ItemsRepeater Grid.Row="1" ItemsSource="{x:Bind ViewModel.LatestPosts, Mode=OneWay}">
					<ItemsRepeater.Layout>
						<UniformGridLayout
							ItemsStretch="Fill"
							MinColumnSpacing="24"
							MinItemHeight="420"
							MinItemWidth="320"
							MinRowSpacing="24" />
					</ItemsRepeater.Layout>
					<ItemsRepeater.ItemTemplate>
						<DataTemplate x:DataType="vmitems:PostListItemViewModel">
							<Button
								AutomationProperties.Name="{x:Bind CardTitle}"
								Click="PostCard_Click"
								Style="{StaticResource MzContentCardStyle}"
								Tag="{extensions:Localize Key=ReadMore}" />
						</DataTemplate>
					</ItemsRepeater.ItemTemplate>
				</ItemsRepeater>

				<ProgressRing
					Grid.Row="1"
					Width="48"
					Height="48"
					HorizontalAlignment="Center"
					IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
					Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />

				<StackPanel
					Grid.Row="1"
					Style="{StaticResource MzEmptyStatePanelStyle}"
					Visibility="{x:Bind ViewModel.HasError, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
					<FontIcon Glyph="{StaticResource MzGlyphWarning}" Style="{StaticResource MzEmptyStateIconStyle}" />
					<TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{extensions:Localize Key=CouldNotLoadData}" />
					<TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{extensions:Localize Key=ErrorLoadingData}" />
					<Button
						HorizontalAlignment="Center"
						Command="{x:Bind ViewModel.RetryCommand, Mode=OneWay}"
						Content="{extensions:Localize Key=Retry}"
						Style="{StaticResource MzStandardButtonStyle}" />
				</StackPanel>
			</Grid>

			<VisualStateManager.VisualStateGroups>
				<VisualStateGroup x:Name="MzTypeScale">
					<VisualState x:Name="MzTypeScaleWide">
						<VisualState.StateTriggers>
							<AdaptiveTrigger MinWindowWidth="768" />
						</VisualState.StateTriggers>
						<VisualState.Setters>
							<Setter Target="HeroTitle.Style" Value="{StaticResource MzDisplayWideTextStyle}" />
							<Setter Target="Hero.Padding" Value="36,96,36,64" />
							<Setter Target="Strip.Padding" Value="36,36,36,64" />
						</VisualState.Setters>
					</VisualState>
					<VisualState x:Name="MzTypeScaleCompact" />
				</VisualStateGroup>
			</VisualStateManager.VisualStateGroups>
		</StackPanel>
	</ScrollViewer>
</local:HomeViewBase>
```

- [ ] **Step 2: Replace `src/app/MZikmund.App/Views/HomeView.xaml.cs` in full**

A `Grid` does not clip its children, so the glow's negative margin would bleed over the strip below; the hero gets an explicit clip rectangle instead — the XAML equivalent of the web's `overflow: hidden`. The reduced-motion path must set the *final* values, or the hero stays at `Opacity="0"` forever.
```csharp
using Microsoft.UI.Xaml.Media.Animation;
using MZikmund.Infrastructure;
using MZikmund.ViewModels;
using MZikmund.ViewModels.Items;
using Windows.Foundation;

namespace MZikmund.Views;

public sealed partial class HomeView : HomeViewBase
{
	public HomeView()
	{
		this.InitializeComponent();
		Loaded += HomeView_Loaded;
	}

	private void HomeView_Loaded(object sender, RoutedEventArgs e)
	{
		if (MzPageTransition.AnimationsEnabled)
		{
			((Storyboard)Resources["HeroEntrance"]).Begin();
			return;
		}

		SettleHero();
	}

	private void SettleHero()
	{
		HeroEyebrow.Opacity = 1;
		HeroEyebrowTransform.TranslateY = 0;
		HeroTitle.Opacity = 1;
		HeroTitleTransform.TranslateY = 0;
		HeroRule.Opacity = 1;
		HeroRuleTransform.ScaleX = 1;
		HeroLead.Opacity = 1;
		HeroLeadTransform.TranslateY = 0;
		HeroActions.Opacity = 1;
		HeroActionsTransform.TranslateY = 0;
		HeroTopics.Opacity = 1;
		HeroTopicsTransform.TranslateY = 0;
	}

	private void Hero_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		Hero.Clip = new RectangleGeometry
		{
			Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height),
		};
	}

	private void PostCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PostListItemViewModel item })
		{
			ViewModel?.NavigateToPost(item.Item.Id);
		}
	}
}

public partial class HomeViewBase : PageBase<HomeViewModel>
{
}
```

- [ ] **Step 3: Build the desktop head / Expected: `Build succeeded`**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Views/HomeView.xaml src/app/MZikmund.App/Views/HomeView.xaml.cs
git commit -m "feat: Build the Home hero and the latest-posts strip"
```

---

### Task 45: Verify HomeView on the real heads and format

**Files:**
- Modify: none unless verification finds defects.

**Interfaces:**
- Consumes: everything above.
- Produces: nothing.

- [ ] **Step 1: Run the desktop head and check the hero in both themes**
```bash
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```
Drive it with the Uno App MCP and confirm, in **light and dark**:
1. The glow sits in the **top-right** and no part of it falls behind the title, eyebrow, lead or actions.
2. The glow is **clipped at the hero's edges** — nothing bleeds onto the latest-posts strip.
3. There is **no rule, line or divider** along the bottom of the hero; the only boundary is the tertiary→base tone step.
4. The entrance staggers visibly in the order eyebrow → name → rule → lead → actions → topics, and the rule **grows from its left edge**, not from the centre and not by fading.
5. Nothing is left at `Opacity 0` after the storyboard completes.
6. The three post cards match `BlogView`'s cards exactly — same style, same media height, same hover.

- [ ] **Step 2: Check the breakpoint**

Resize across 768px. Expected: the title swaps between `MzDisplayTextStyle` (40px) and `MzDisplayWideTextStyle` (72px), the hero padding goes `16,64,16,48` → `36,96,36,64`, and the card grid reflows without the glow re-entering the text column.

- [ ] **Step 3: Check reduced motion**

Set `MzPageTransition.AnimationsEnabled = false` (Phase 5 exposes the toggle in Settings; until then set it in the debugger or temporarily in `HomeView_Loaded`) and navigate to Home. Expected: the hero renders fully formed with no animation and nothing invisible.

- [ ] **Step 4: Build the remaining local heads / Expected: `Build succeeded` on each**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
```
WASM is CI-verified only. On the Windows head additionally confirm the glow and grain render — Windows is the reference platform and the only one where `MzElevation` produces real shadows on the strip's cards.

- [ ] **Step 5: Format / Expected: no diff after the run**
```bash
dotnet format MZikmund.slnx
```

- [ ] **Step 6: Commit**
```bash
git add -A
git commit -m "chore: Format after the Home view work"
```## PostView

### Interfaces

PRODUCES (later work may rely on these):

C# — `MZikmund.ViewModels.PostViewModel` (rebuilt): `override string Title`, `Post? Post`, `Uri? EmbedUrl`, `ImageSource? HeroImage`, `bool HasHeroImage`, `string HeroImageAlt`, `IReadOnlyList<string> CategoryNames`, `string PublishedDateText`, `string ReadTimeText`, `bool IsLoading`, `bool HasError`, `bool HasContent`, `ICommand RetryCommand`, `void ReportBodyLoadFailed(Exception? exception, string reason)`. Constructor signature is now `(IMZikmundApi, IOptions<AppConfig>, IMarkdownConverter, ILogger<PostViewModel>)` — resolved by the existing `services.AddScoped<PostViewModel>()` in `App.xaml.cs:136`, no registration change needed.

C# — `MZikmund.Infrastructure.MzHeroTransition` (static): `const string PostHeroKey = "MzPostHero"`, `static void PrepareFromCard(UIElement? source)`, `static bool TryStartOnHero(UIElement? destination)`. The entire `ConnectedAnimationService` platform split lives in this one file; both members are safe no-ops off Windows.

XAML — `Theme/Icons.xaml` gains `MzGlyphClock` (`&#xE508;`, already inside the Phase 1 subset). It does not redefine any Phase 1 key.

XAML — `Views/PostView.xaml` named elements: `PageRoot`, `Hero`, `HeroMediaFrame`, `HeroBanner`, `HeroBannerTransform`, `HeroTitle`, `BodyWebViewContainer`; resource `HeroEntranceStoryboard`; visual state group `MzTypeScale` with states `MzTypeScaleWide` / `MzTypeScaleCompact`.

Resource strings (en + cs): `ReadTimeMinutesFormat`, `PostUnavailable`, `PostUnavailableDescription`.

CONSUMES:

Phase 1 brushes: `MzBgBaseBrush`, `MzCardMediaEmptyBrush`, `MzTextTertiaryBrush`, `MzStrokeDividerBrush`.
Phase 1 type styles: `MzTitleTextStyle`, `MzTitleWideTextStyle`, `MzMetaTextStyle`.
Phase 1 component styles: `MzBadgeSubtleStyle`, `MzEmptyStatePanelStyle`, `MzEmptyStateIconStyle`, `MzEmptyStateTitleTextStyle`, `MzEmptyStateBodyTextStyle`, `MzStandardButtonStyle`.
Phase 1 tokens: `MzPagePaddingCompact`, `MzPagePaddingWide`, `MzSpace2`, `MzSpace3`, `MzSpace4`, `MzCardCornerRadius`, `MzEaseEntrance`, `MzDurationSlow`.
Phase 1 icons: `MzIconFontFamily`, `MzGlyphCalendar`, `MzGlyphImageOff`, `MzGlyphWarning`.
Phase 1 strings: `Retry`. Existing strings: `CouldNotLoadData`.
Phase 1 C#: `MZikmund.ViewModels.Items.PostListItemViewModel.Item` (unchanged by `ICardItem`).
Phase 2 C#: `MZikmund.Infrastructure.MzPageTransition.AnimationsEnabled`.
Phase 2 XAML: nothing — `PostView` owns its own hero header instead of `MzPageHeader`, because the hero *is* the header.
Existing converters (`Resources/Converters.xaml`, kept by Phase 2): `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter`.
Existing markup extension: `MZikmund.Extensions.Xaml.Localize`.

### Task 46: Rebuild `PostViewModel` with hero data, real states and real logging

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/PostViewModel.cs:1-38` (full replacement)
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw` (append before `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (append before `</root>`)

**Interfaces:**
- Consumes: `IMZikmundApi.GetPostAsync(Guid) : Task<ApiResponse<Post>>`, `IMarkdownConverter.ToPlainText(string)`, `AppConfig.WebUrl`, `Localizer.Instance.GetString`.
- Produces: `PostViewModel.Title` / `Post` / `EmbedUrl` / `HeroImage` / `HasHeroImage` / `HeroImageAlt` / `CategoryNames` / `PublishedDateText` / `ReadTimeText` / `IsLoading` / `HasError` / `HasContent` / `RetryCommand` / `ReportBodyLoadFailed`; strings `ReadTimeMinutesFormat`, `PostUnavailable`, `PostUnavailableDescription`.

- [ ] **Step 1: Replace `src/app/MZikmund.App.Core/ViewModels/PostViewModel.cs` in full**

The old file threw `InvalidOperationException` from `async void ViewNavigatedTo` (unobservable), blind-cast `parameter` to `Guid`, and ignored the HTTP status entirely. Every failure path now lands in `HasError` and in the log.

```csharp
using System.Globalization;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Localization;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public class PostViewModel : PageViewModel
{
	private const int WordsPerMinute = 200;

	private readonly IMZikmundApi _api;
	private readonly IOptions<AppConfig> _appConfig;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly ILogger<PostViewModel> _logger;

	private Guid _postId = Guid.Empty;
	private Post? _post;
	private Uri? _embedUrl;
	private ImageSource? _heroImage;
	private string _readTimeText = "";
	private bool _isLoading;
	private bool _hasError;

	public PostViewModel(
		IMZikmundApi api,
		IOptions<AppConfig> appConfig,
		IMarkdownConverter markdownConverter,
		ILogger<PostViewModel> logger)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
		_markdownConverter = markdownConverter ?? throw new ArgumentNullException(nameof(markdownConverter));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		RetryCommand = new AsyncRelayCommand(LoadAsync);
	}

	public override string Title => _post?.Title ?? "";

	public ICommand RetryCommand { get; }

	public Post? Post
	{
		get => _post;
		private set
		{
			if (SetProperty(ref _post, value))
			{
				OnPropertyChanged(nameof(Title));
				OnPropertyChanged(nameof(HasHeroImage));
				OnPropertyChanged(nameof(HeroImageAlt));
				OnPropertyChanged(nameof(CategoryNames));
				OnPropertyChanged(nameof(PublishedDateText));
				OnPropertyChanged(nameof(ReadTimeText));
				OnPropertyChanged(nameof(HasContent));
			}
		}
	}

	public Uri? EmbedUrl
	{
		get => _embedUrl;
		private set => SetProperty(ref _embedUrl, value);
	}

	/// <summary>
	/// Materialised here rather than bound as a <see cref="Uri"/>: x:Bind is strongly typed and will
	/// not convert Uri to ImageSource for us.
	/// </summary>
	public ImageSource? HeroImage
	{
		get => _heroImage;
		private set => SetProperty(ref _heroImage, value);
	}

	public bool HasHeroImage => _heroImage is not null;

	public string HeroImageAlt => _post?.HeroImageAlt ?? _post?.Title ?? "";

	public IReadOnlyList<string> CategoryNames =>
		_post?.Categories.Select(category => category.DisplayName).ToArray() ?? Array.Empty<string>();

	public string PublishedDateText
	{
		get
		{
			var date = _post?.PublishedDate ?? _post?.LastModifiedDate;
			return date is null
				? ""
				: date.Value.ToLocalTime().ToString("d MMMM yyyy", CultureInfo.CurrentCulture);
		}
	}

	public string ReadTimeText => _readTimeText;

	public bool IsLoading
	{
		get => _isLoading;
		private set
		{
			if (SetProperty(ref _isLoading, value))
			{
				OnPropertyChanged(nameof(HasContent));
			}
		}
	}

	public bool HasError
	{
		get => _hasError;
		private set
		{
			if (SetProperty(ref _hasError, value))
			{
				OnPropertyChanged(nameof(HasContent));
			}
		}
	}

	public bool HasContent => _post is not null && !_isLoading && !_hasError;

	public override void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);

		if (parameter is not Guid postId || postId == Guid.Empty)
		{
			_logger.LogError("PostView was navigated to with {Parameter}; a non-empty Guid is required.", parameter ?? "null");
			HasError = true;
			return;
		}

		_postId = postId;
		_ = LoadAsync();
	}

	/// <summary>
	/// Called by the view when the body web view itself fails; the hero may already be on screen.
	/// </summary>
	public void ReportBodyLoadFailed(Exception? exception, string reason)
	{
		_logger.LogError(exception, "The post body web view failed for {PostId}: {Reason}", _postId, reason);
		HasError = true;
	}

	private async Task LoadAsync()
	{
		if (_postId == Guid.Empty)
		{
			return;
		}

		IsLoading = true;
		HasError = false;

		try
		{
			var baseUrl = _appConfig.Value.WebUrl;
			if (string.IsNullOrWhiteSpace(baseUrl))
			{
				_logger.LogError("WebUrl is not set in configuration; the post body cannot be shown.");
				HasError = true;
				return;
			}

			var response = await _api.GetPostAsync(_postId);
			if (!response.IsSuccessful || response.Content is null)
			{
				_logger.LogError(response.Error, "Loading post {PostId} failed with status {StatusCode}.", _postId, response.StatusCode);
				HasError = true;
				return;
			}

			var post = response.Content;
			_readTimeText = BuildReadTimeText(post);
			HeroImage = BuildHeroImage(post);

			var root = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
			EmbedUrl = new Uri(new Uri(root, UriKind.Absolute), $"embed/post/{_postId}");

			Post = post;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading post {PostId} failed.", _postId);
			HasError = true;
		}
		finally
		{
			IsLoading = false;
		}
	}

	private ImageSource? BuildHeroImage(Post post)
	{
		if (string.IsNullOrWhiteSpace(post.HeroImageUrl) ||
			!Uri.TryCreate(post.HeroImageUrl, UriKind.Absolute, out var uri))
		{
			return null;
		}

		return new BitmapImage(uri);
	}

	private string BuildReadTimeText(Post post)
	{
		var plain = _markdownConverter.ToPlainText(post.Content);
		var words = plain.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
		var minutes = Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
		return string.Format(
			CultureInfo.CurrentCulture,
			Localizer.Instance.GetString("ReadTimeMinutesFormat"),
			minutes);
	}
}
```

- [ ] **Step 2: Append the English strings**

In `src/app/MZikmund.App/Strings/en/Resources.resw`, insert immediately before the closing `</root>`:
```xml
  <data name="ReadTimeMinutesFormat" xml:space="preserve">
    <value>{0} min read</value>
  </data>
  <data name="PostUnavailable" xml:space="preserve">
    <value>This post could not be loaded</value>
  </data>
  <data name="PostUnavailableDescription" xml:space="preserve">
    <value>Check your connection and try again.</value>
  </data>
```

- [ ] **Step 3: Append the Czech strings**

In `src/app/MZikmund.App/Strings/cs/Resources.resw`, insert immediately before the closing `</root>`:
```xml
  <data name="ReadTimeMinutesFormat" xml:space="preserve">
    <value>{0} min čtení</value>
  </data>
  <data name="PostUnavailable" xml:space="preserve">
    <value>Příspěvek se nepodařilo načíst</value>
  </data>
  <data name="PostUnavailableDescription" xml:space="preserve">
    <value>Zkontrolujte připojení a zkuste to znovu.</value>
  </data>
```

- [ ] **Step 4: Build the desktop head / Expected: Build succeeded**

`PostView.xaml.cs` still references the old `_previewWebView` field only, so the app compiles; the XAML rebuild lands in a later task.
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/PostViewModel.cs src/app/MZikmund.App/Strings/en/Resources.resw src/app/MZikmund.App/Strings/cs/Resources.resw
git commit -m "feat: Give PostViewModel hero data, loading and failure states and real logging"
```

---

### Task 47: `MzHeroTransition` — ConnectedAnimation on Windows, cross-fade everywhere else

**Files:**
- Create: `src/app/MZikmund.App/Infrastructure/MzHeroTransition.cs`
- Modify: `src/app/MZikmund.App/Views/BlogView.xaml.cs:1-3`, `src/app/MZikmund.App/Views/BlogView.xaml.cs:12-18` (the `PostCard_Click` handler produced by Phase 1)

**Interfaces:**
- Consumes: `MZikmund.Infrastructure.MzPageTransition.AnimationsEnabled` (Phase 2); `PostListItemViewModel.Item.Id` (Phase 1).
- Produces: `MZikmund.Infrastructure.MzHeroTransition.PostHeroKey` / `.PrepareFromCard(UIElement?)` / `.TryStartOnHero(UIElement?)`.

- [ ] **Step 1: Create `src/app/MZikmund.App/Infrastructure/MzHeroTransition.cs`**

`ConnectedAnimationService.GetForCurrentView()` throws on every Skia head, so the type is never named outside the `#if WINDOWS` block and both entry points degrade to no-ops that let the caller fall back to its own storyboard.
```csharp
#if WINDOWS
using Microsoft.UI.Xaml.Media.Animation;
#endif
using Microsoft.UI.Xaml;

namespace MZikmund.Infrastructure;

/// <summary>
/// The blog card to post hero transition. Windows gets a real ConnectedAnimation; every Skia head
/// falls back to the page's own cross-fade because GetForCurrentView() throws there.
/// </summary>
public static class MzHeroTransition
{
	public const string PostHeroKey = "MzPostHero";

	/// <summary>
	/// Registers the tapped card as the animation source. No-op unless this is the Windows head.
	/// </summary>
	public static void PrepareFromCard(UIElement? source)
	{
#if WINDOWS
		if (source is null || !MzPageTransition.AnimationsEnabled)
		{
			return;
		}

		var service = ConnectedAnimationService.GetForCurrentView();
		service.DefaultDuration = TimeSpan.FromMilliseconds(333);
		service.PrepareToAnimate(PostHeroKey, source);
#endif
	}

	/// <summary>
	/// Runs the pending card to hero animation.
	/// </summary>
	/// <returns><c>true</c> when the animation started and the caller must not cross-fade.</returns>
	public static bool TryStartOnHero(UIElement? destination)
	{
#if WINDOWS
		if (destination is null || !MzPageTransition.AnimationsEnabled)
		{
			return false;
		}

		var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation(PostHeroKey);
		if (animation is null)
		{
			return false;
		}

		animation.Configuration = new DirectConnectedAnimationConfiguration();
		return animation.TryStart(destination);
#else
		return false;
#endif
	}
}
```

- [ ] **Step 2: Register the card as the animation source**

In `src/app/MZikmund.App/Views/BlogView.xaml.cs`, add `using MZikmund.Infrastructure;` to the using block and replace:
```csharp
	private void PostCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PostListItemViewModel item })
		{
			ViewModel?.NavigateToPost(item.Item.Id);
		}
	}
```
with:
```csharp
	private void PostCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PostListItemViewModel item } card)
		{
			MzHeroTransition.PrepareFromCard(card);
			ViewModel?.NavigateToPost(item.Item.Id);
		}
	}
```

- [ ] **Step 3: Build the desktop head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 4: Build the Windows head / Expected: Build succeeded**

This is the only head that compiles the `#if WINDOWS` body, so it is the only one that proves `ConnectedAnimationService` and `DirectConnectedAnimationConfiguration` resolve.
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App/Infrastructure/MzHeroTransition.cs src/app/MZikmund.App/Views/BlogView.xaml.cs
git commit -m "feat: Add the card to hero transition with a Windows-only ConnectedAnimation"
```

---

### Task 48: Rebuild `PostView` as a native hero above the embed WebView

**Files:**
- Modify: `src/app/MZikmund.App/Theme/Icons.xaml` (append one glyph key)
- Modify: `src/app/MZikmund.App/Views/PostView.xaml:1-15` (full replacement)
- Modify: `src/app/MZikmund.App/Views/PostView.xaml.cs:1-59` (full replacement)

**Interfaces:**
- Consumes: `MzBgBaseBrush`, `MzCardMediaEmptyBrush`, `MzTextTertiaryBrush`, `MzStrokeDividerBrush`, `MzTitleTextStyle`, `MzTitleWideTextStyle`, `MzMetaTextStyle`, `MzBadgeSubtleStyle`, `MzEmptyStatePanelStyle`, `MzEmptyStateIconStyle`, `MzEmptyStateTitleTextStyle`, `MzEmptyStateBodyTextStyle`, `MzStandardButtonStyle`, `MzPagePaddingCompact`, `MzPagePaddingWide`, `MzSpace2`, `MzSpace3`, `MzSpace4`, `MzCardCornerRadius`, `MzEaseEntrance`, `MzDurationSlow`, `MzIconFontFamily`, `MzGlyphCalendar`, `MzGlyphImageOff`, `MzGlyphWarning`, `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter`, `MzHeroTransition`, `MzPageTransition.AnimationsEnabled`, the new `PostViewModel` surface, strings `Retry` / `CouldNotLoadData` / `PostUnavailable` / `PostUnavailableDescription`.
- Produces: `MzGlyphClock`; named elements `PageRoot`, `Hero`, `HeroMediaFrame`, `HeroBanner`, `HeroBannerTransform`, `HeroTitle`, `BodyWebViewContainer`; resource `HeroEntranceStoryboard`.

- [ ] **Step 1: Add the clock glyph to `src/app/MZikmund.App/Theme/Icons.xaml`**

`clock E508` is already inside the Phase 1 subset; only the key is missing. Insert immediately after the `MzGlyphCalendar` line (this adds a key, it does not redefine one):
```xml
	<x:String x:Key="MzGlyphClock">&#xE508;</x:String>
```

- [ ] **Step 2: Replace `src/app/MZikmund.App/Views/PostView.xaml` in full**

Fixes the live bug: the old root `Grid` had no `RowDefinitions` yet its only child asked for `Grid.Row="1"`. The title sits below the banner on the page ground, never over the photo. `Height` is only ever changed by a `VisualState.Setter` (instant, not an animation), so no `EnableDependentAnimation` is required; the storyboard touches `Opacity` and a `CompositeTransform` only, both GPU properties.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<local:PostViewBase
    x:Class="MZikmund.Views.PostView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:extensions="using:MZikmund.Extensions.Xaml"
    xmlns:local="using:MZikmund.Views"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    Background="{ThemeResource MzBgBaseBrush}"
    mc:Ignorable="d">

    <local:PostViewBase.Resources>
        <!--  Skia fallback for the Windows ConnectedAnimation: cross-fade plus a small settle.  -->
        <Storyboard x:Key="HeroEntranceStoryboard">
            <DoubleAnimationUsingKeyFrames Storyboard.TargetName="Hero" Storyboard.TargetProperty="Opacity">
                <DiscreteDoubleKeyFrame KeyTime="0" Value="0" />
                <SplineDoubleKeyFrame
                    KeySpline="{StaticResource MzEaseEntrance}"
                    KeyTime="{StaticResource MzDurationSlow}"
                    Value="1" />
            </DoubleAnimationUsingKeyFrames>
            <DoubleAnimationUsingKeyFrames Storyboard.TargetName="HeroBanner" Storyboard.TargetProperty="(UIElement.RenderTransform).(CompositeTransform.ScaleX)">
                <DiscreteDoubleKeyFrame KeyTime="0" Value="1.02" />
                <SplineDoubleKeyFrame
                    KeySpline="{StaticResource MzEaseEntrance}"
                    KeyTime="{StaticResource MzDurationSlow}"
                    Value="1" />
            </DoubleAnimationUsingKeyFrames>
            <DoubleAnimationUsingKeyFrames Storyboard.TargetName="HeroBanner" Storyboard.TargetProperty="(UIElement.RenderTransform).(CompositeTransform.ScaleY)">
                <DiscreteDoubleKeyFrame KeyTime="0" Value="1.02" />
                <SplineDoubleKeyFrame
                    KeySpline="{StaticResource MzEaseEntrance}"
                    KeyTime="{StaticResource MzDurationSlow}"
                    Value="1" />
            </DoubleAnimationUsingKeyFrames>
        </Storyboard>
    </local:PostViewBase.Resources>

    <Grid x:Name="PageRoot">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!--  Native hero: banner, then badges, title and meta on the solid page ground.  -->
        <StackPanel
            x:Name="Hero"
            Grid.Row="0"
            MaxWidth="780"
            Padding="{StaticResource MzPagePaddingCompact}"
            HorizontalAlignment="Center"
            Spacing="{StaticResource MzSpace3}"
            Visibility="{x:Bind ViewModel.HasContent, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">

            <Border
                x:Name="HeroMediaFrame"
                Height="220"
                Background="{ThemeResource MzCardMediaEmptyBrush}"
                CornerRadius="{StaticResource MzCardCornerRadius}">
                <Grid>
                    <FontIcon
                        FontFamily="{StaticResource MzIconFontFamily}"
                        FontSize="40"
                        Foreground="{ThemeResource MzTextTertiaryBrush}"
                        Glyph="{StaticResource MzGlyphImageOff}"
                        Visibility="{x:Bind ViewModel.HasHeroImage, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}" />
                    <Image
                        x:Name="HeroBanner"
                        AutomationProperties.Name="{x:Bind ViewModel.HeroImageAlt, Mode=OneWay}"
                        RenderTransformOrigin="0.5,0.5"
                        Source="{x:Bind ViewModel.HeroImage, Mode=OneWay}"
                        Stretch="UniformToFill"
                        Visibility="{x:Bind ViewModel.HasHeroImage, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
                        <Image.RenderTransform>
                            <CompositeTransform x:Name="HeroBannerTransform" />
                        </Image.RenderTransform>
                    </Image>
                </Grid>
            </Border>

            <ItemsRepeater ItemsSource="{x:Bind ViewModel.CategoryNames, Mode=OneWay}">
                <ItemsRepeater.Layout>
                    <StackLayout Orientation="Horizontal" Spacing="{StaticResource MzSpace2}" />
                </ItemsRepeater.Layout>
                <ItemsRepeater.ItemTemplate>
                    <DataTemplate x:DataType="x:String">
                        <ContentControl Content="{x:Bind}" Style="{StaticResource MzBadgeSubtleStyle}" />
                    </DataTemplate>
                </ItemsRepeater.ItemTemplate>
            </ItemsRepeater>

            <TextBlock
                x:Name="HeroTitle"
                Style="{StaticResource MzTitleTextStyle}"
                Text="{x:Bind ViewModel.Title, Mode=OneWay}" />

            <StackPanel Orientation="Horizontal" Spacing="{StaticResource MzSpace4}">
                <StackPanel Orientation="Horizontal" Spacing="{StaticResource MzSpace2}">
                    <FontIcon
                        FontFamily="{StaticResource MzIconFontFamily}"
                        FontSize="14"
                        Foreground="{ThemeResource MzTextTertiaryBrush}"
                        Glyph="{StaticResource MzGlyphCalendar}" />
                    <TextBlock Style="{StaticResource MzMetaTextStyle}" Text="{x:Bind ViewModel.PublishedDateText, Mode=OneWay}" />
                </StackPanel>
                <StackPanel Orientation="Horizontal" Spacing="{StaticResource MzSpace2}">
                    <FontIcon
                        FontFamily="{StaticResource MzIconFontFamily}"
                        FontSize="14"
                        Foreground="{ThemeResource MzTextTertiaryBrush}"
                        Glyph="{StaticResource MzGlyphClock}" />
                    <TextBlock Style="{StaticResource MzMetaTextStyle}" Text="{x:Bind ViewModel.ReadTimeText, Mode=OneWay}" />
                </StackPanel>
            </StackPanel>
        </StackPanel>

        <!--  Article body: the site's own /embed/post/{id} page, so the serif measure comes for free.  -->
        <Border
            Grid.Row="1"
            BorderBrush="{ThemeResource MzStrokeDividerBrush}"
            BorderThickness="0,1,0,0"
            Visibility="{x:Bind ViewModel.HasContent, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
            <ContentControl
                x:Name="BodyWebViewContainer"
                HorizontalAlignment="Stretch"
                VerticalAlignment="Stretch"
                HorizontalContentAlignment="Stretch"
                VerticalContentAlignment="Stretch" />
        </Border>

        <ProgressRing
            Grid.Row="0"
            Grid.RowSpan="2"
            Width="48"
            Height="48"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
            Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />

        <StackPanel
            Grid.Row="0"
            Grid.RowSpan="2"
            Style="{StaticResource MzEmptyStatePanelStyle}"
            Visibility="{x:Bind ViewModel.HasError, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
            <FontIcon Glyph="{StaticResource MzGlyphWarning}" Style="{StaticResource MzEmptyStateIconStyle}" />
            <TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{extensions:Localize Key=PostUnavailable}" />
            <TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{extensions:Localize Key=PostUnavailableDescription}" />
            <Button
                HorizontalAlignment="Center"
                Command="{x:Bind ViewModel.RetryCommand, Mode=OneWay}"
                Content="{extensions:Localize Key=Retry}"
                Style="{StaticResource MzStandardButtonStyle}" />
        </StackPanel>

        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="MzTypeScale">
                <VisualState x:Name="MzTypeScaleWide">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="768" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="HeroTitle.Style" Value="{StaticResource MzTitleWideTextStyle}" />
                        <Setter Target="Hero.Padding" Value="{StaticResource MzPagePaddingWide}" />
                        <Setter Target="HeroMediaFrame.Height" Value="320" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="MzTypeScaleCompact" />
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    </Grid>
</local:PostViewBase>
```

- [ ] **Step 3: Replace `src/app/MZikmund.App/Views/PostView.xaml.cs` in full**

Both entrance paths are here in full: `MzHeroTransition.TryStartOnHero` is the Windows ConnectedAnimation and returns `false` on every Skia head, which is exactly when `HeroEntranceStoryboard` runs instead. The old `catch (Exception) { // TODO: Log error }` is gone — every failure now reaches `ReportBodyLoadFailed`, which logs through `ILogger<PostViewModel>` and raises the failure state.
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using MZikmund.Infrastructure;
using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class PostView : PostViewBase
{
	// Fully qualified: Uno 6.8 added a root-level WebView2 namespace that shadows the simple name.
	private readonly Microsoft.UI.Xaml.Controls.WebView2 _bodyWebView;

	private bool _isWebViewReady;
	private bool _heroEntrancePlayed;

	public PostView()
	{
		InitializeComponent();

		_bodyWebView = new Microsoft.UI.Xaml.Controls.WebView2();
		_bodyWebView.NavigationCompleted += BodyWebView_NavigationCompleted;
		BodyWebViewContainer.Content = _bodyWebView;

		Loaded += PostView_Loaded;
		Unloaded += PostView_Unloaded;
	}

	private async void PostView_Loaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel is { } viewModel)
		{
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		PlayHeroEntrance();

		try
		{
			await _bodyWebView.EnsureCoreWebView2Async();
			_isWebViewReady = true;
			NavigateToBody();
		}
		catch (Exception ex)
		{
			ViewModel?.ReportBodyLoadFailed(ex, "EnsureCoreWebView2Async failed.");
		}
	}

	private void PostView_Unloaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel is { } viewModel)
		{
			viewModel.PropertyChanged -= ViewModel_PropertyChanged;
		}

		_bodyWebView.NavigationCompleted -= BodyWebView_NavigationCompleted;
	}

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(PostViewModel.EmbedUrl))
		{
			NavigateToBody();
		}
		else if (e.PropertyName == nameof(PostViewModel.HasContent))
		{
			PlayHeroEntrance();
		}
	}

	private void NavigateToBody()
	{
		if (!_isWebViewReady || ViewModel?.EmbedUrl is not { } embedUrl)
		{
			return;
		}

		_bodyWebView.Source = embedUrl;
	}

	private void BodyWebView_NavigationCompleted(
		Microsoft.UI.Xaml.Controls.WebView2 sender,
		Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
	{
		if (!args.IsSuccess)
		{
			ViewModel?.ReportBodyLoadFailed(null, $"Navigation to the embed page failed: {args.WebErrorStatus}.");
		}
	}

	/// <summary>
	/// Windows morphs the blog card into the banner; every Skia head cross-fades the hero in, because
	/// ConnectedAnimationService.GetForCurrentView() throws there.
	/// </summary>
	private void PlayHeroEntrance()
	{
		if (_heroEntrancePlayed || ViewModel is not { HasContent: true } viewModel)
		{
			return;
		}

		_heroEntrancePlayed = true;

		var destination = viewModel.HasHeroImage ? (UIElement)HeroBanner : HeroMediaFrame;
		if (MzHeroTransition.TryStartOnHero(destination))
		{
			return;
		}

		if (!MzPageTransition.AnimationsEnabled)
		{
			return;
		}

		((Storyboard)Resources["HeroEntranceStoryboard"]).Begin();
	}
}

public partial class PostViewBase : PageBase<PostViewModel>
{
}
```

- [ ] **Step 4: Build the desktop head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 5: Build the Windows head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

- [ ] **Step 6: Verify on the desktop head with the Uno App MCP**

Run the desktop app, navigate Blog then a post, and screenshot in **both** light and dark. Confirm: the banner renders with an 8px radius; the title sits below it on the page ground and is never over the photo; the badges, calendar date and clock read time line up; the wide title style and the 320px banner kick in above 768px and revert below; the body web view fills the remaining height and scrolls internally; the hero cross-fades in. Then kill the network and reload to confirm the failure panel and that **Retry** re-runs the load.

- [ ] **Step 7: Format**
```bash
dotnet format MZikmund.slnx
```

- [ ] **Step 8: Commit**
```bash
git add src/app/MZikmund.App/Theme/Icons.xaml src/app/MZikmund.App/Views/PostView.xaml src/app/MZikmund.App/Views/PostView.xaml.cs
git commit -m "feat: Rebuild PostView as a native hero above the embed WebView"
```## VideosView

### Interfaces

PRODUCES (later work may consume):
- `MZikmund.ViewModels.Items.IPlayableCardItem` — `bool HasPlayAffordance { get; }`
- `MZikmund.ViewModels.Items.VideoListItemViewModel` — `VideoDto Video`, `Uri? ThumbnailUri`, plus the full `ICardItem` + `IPlayableCardItem` surface
- `VideosViewModel` surface: `string Title`, `string Lead`, `ObservableCollection<VideoListItemViewModel> Videos`, `ICommand RefreshCommand`, `bool IsLoading`, `bool HasError`, `bool IsEmpty`, `Task OpenVideoAsync(VideoListItemViewModel item)`
- Theme keys added to Phase 1 files: `MzMediaScrimBrush` (#40000000), `MzMediaScrimShadowBrush` (#66000000) at the root of `Theme/Colors.xaml`
- Named parts added to `MzContentCardStyle`'s template: `PlayAffordance` (Grid), `PlayScrim` (Border) — opt-in via a loose `HasPlayAffordance` binding with `FallbackValue=False`, so every existing `ICardItem` keeps working unchanged
- Localization keys (en + cs): `VideosLead`, `NoVideosFound`, `NoVideosFoundDescription`

CONSUMES from Phase 1: `MzContentCardStyle`, `ICardItem`, `MzStandardButtonStyle`, `MzEmptyStatePanelStyle`, `MzEmptyStateIconStyle`, `MzEmptyStateTitleTextStyle`, `MzEmptyStateBodyTextStyle`, `MzPagePaddingCompact`, `MzPagePaddingWide`, `MzIconFontFamily`, `MzGlyphVideoClip`, `MzGlyphPlayCircle`, `MzGlyphWarning`, `MzGlyphArrowClockwise`, `MzOnScrimTextBrush`, `MzEaseStandard`, `MzDurationFast`, strings `Videos` / `Refresh` / `WatchOnYouTube` / `Retry`.
CONSUMES from Phase 2: `MZikmund.Controls.MzPageHeader` (`Title`, `Lead`, `Actions`), `xmlns:controls="using:MZikmund.Controls"`, the stripped page `Background` convention, nav `Tag="VideosViewModel"`.
CONSUMES pre-existing: `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter`, `{extensions:Localize Key=…}`, `CouldNotLoadData`, `ErrorLoadingData`.

### Task 49: Play-affordance brushes

Two theme-invariant brushes. The scrim is `#000` at 25% (`0x40`) and the icon's drop shadow is `#000` at 40% (`0x66`). Both sit over uploaded thumbnails, so like `MzScrimBrush` they never follow the theme.

**Files:**
- Modify: `src/app/MZikmund.App/Theme/Colors.xaml` (root-level block, after `BrandBrush`)

**Interfaces:**
- Consumes: nothing.
- Produces: `MzMediaScrimBrush`, `MzMediaScrimShadowBrush`.

- [ ] **Step 1: Append the two brushes**

In `src/app/MZikmund.App/Theme/Colors.xaml`, replace:
```xml
	<!--  Kept: Resources/ColorResoruces.cs reads this key by name.  -->
	<StaticResource x:Key="BrandColor" ResourceKey="MzAccent" />
	<SolidColorBrush x:Key="BrandBrush" Color="{StaticResource MzAccent}" />
```
with:
```xml
	<!--  Kept: Resources/ColorResoruces.cs reads this key by name.  -->
	<StaticResource x:Key="BrandColor" ResourceKey="MzAccent" />
	<SolidColorBrush x:Key="BrandBrush" Color="{StaticResource MzAccent}" />

	<!--  Over uploaded thumbnails, so theme-invariant like MzScrimBrush.  -->
	<SolidColorBrush x:Key="MzMediaScrimBrush" Color="#40000000" />
	<SolidColorBrush x:Key="MzMediaScrimShadowBrush" Color="#66000000" />
```

- [ ] **Step 2: Commit**
```bash
git add src/app/MZikmund.App/Theme/Colors.xaml
git commit -m "feat: Add the media scrim and icon shadow brushes"
```

---

### Task 50: Opt-in play affordance on the shared card style

The whole point of this phase for Videos is that both grids use **one** card style. Rather than fork a `MzVideoCardStyle`, the play layer goes into `MzContentCardStyle` itself, gated on a loose `HasPlayAffordance` binding with `FallbackValue=False` — blog items do not implement it and stay unchanged.

The icon is always visible (Android and iOS have no hover at all, per the degradation matrix) and carries a real drop shadow: a 40%-black copy offset 1×2px behind a white copy. WinUI has no text shadow and `Compositor.CreateDropShadow` is not implemented in Uno, so two stacked `FontIcon`s are the portable answer. The full-bleed 25% scrim is what fades in on hover and focus.

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/IPlayableCardItem.cs`
- Modify: `src/app/MZikmund.App/Theme/Styles/Cards.xaml` (media grid + `PointerOver` / `Pressed` / `Focused` storyboards)

**Interfaces:**
- Consumes: `MzMediaScrimBrush`, `MzMediaScrimShadowBrush`, `MzOnScrimTextBrush`, `MzIconFontFamily`, `MzGlyphPlayCircle`, `MzEaseStandard`, `MzDurationFast`, `BoolToVisibilityConverter`.
- Produces: `IPlayableCardItem`; template parts `PlayAffordance`, `PlayScrim`.

- [ ] **Step 1: Create `src/app/MZikmund.App.Core/ViewModels/Items/IPlayableCardItem.cs`**
```csharp
namespace MZikmund.ViewModels.Items;

/// <summary>
/// Opt-in play affordance for cards whose media is a video thumbnail.
/// The card template binds this loosely, so plain <see cref="ICardItem"/> items simply never show it.
/// </summary>
public interface IPlayableCardItem
{
	bool HasPlayAffordance { get; }
}
```

- [ ] **Step 2: Add the play layer to the media grid**

In `src/app/MZikmund.App/Theme/Styles/Cards.xaml`, the media `Border`'s inner `Grid` ends with the second badge `ItemsControl`. Insert the block below immediately **after** that second `</ItemsControl>` and **before** the `</Grid>` that closes it — so the affordance paints above the image and below nothing:
```xml
										<!--  Videos only: the icon survives light thumbnails on its own; the scrim is the hover reveal.  -->
										<Grid
											x:Name="PlayAffordance"
											AutomationProperties.AccessibilityView="Raw"
											IsHitTestVisible="False"
											Visibility="{Binding HasPlayAffordance, FallbackValue=False, Converter={StaticResource BoolToVisibilityConverter}}">
											<Border
												x:Name="PlayScrim"
												Background="{StaticResource MzMediaScrimBrush}"
												Opacity="0" />
											<Grid HorizontalAlignment="Center" VerticalAlignment="Center">
												<FontIcon
													FontFamily="{StaticResource MzIconFontFamily}"
													FontSize="48"
													Foreground="{StaticResource MzMediaScrimShadowBrush}"
													Glyph="{StaticResource MzGlyphPlayCircle}">
													<FontIcon.RenderTransform>
														<TranslateTransform X="1" Y="2" />
													</FontIcon.RenderTransform>
												</FontIcon>
												<FontIcon
													FontFamily="{StaticResource MzIconFontFamily}"
													FontSize="48"
													Foreground="{StaticResource MzOnScrimTextBrush}"
													Glyph="{StaticResource MzGlyphPlayCircle}" />
											</Grid>
										</Grid>
```

- [ ] **Step 3: Reveal the scrim in `PointerOver`**

Still in `Cards.xaml`, inside the `PointerOver` state's `<Storyboard>`, insert immediately after the `ReadMoreArrowTransform` `DoubleAnimationUsingKeyFrames` and before `</Storyboard>`:
```xml
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="PlayScrim" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
```
`Opacity` is an independently-animatable property, so this needs no `EnableDependentAnimation`.

- [ ] **Step 4: Keep it lit while pressed and while focused**

In the `Pressed` state's `<Storyboard>`, insert before `</Storyboard>` (after the `CardTitle` `Foreground` frames):
```xml
										<DoubleAnimation
											Storyboard.TargetName="PlayScrim"
											Storyboard.TargetProperty="Opacity"
											To="1"
											Duration="{StaticResource MzDurationInstant}" />
```
In the `Focused` state's `<Storyboard>`, insert after the `FocusRing` `ObjectAnimationUsingKeyFrames`:
```xml
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="PlayScrim" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
```

- [ ] **Step 5: Build the desktop head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 6: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Items/IPlayableCardItem.cs src/app/MZikmund.App/Theme/Styles/Cards.xaml
git commit -m "feat: Add an opt-in play affordance with a scrim to the shared card style"
```

---

### Task 51: `VideoListItemViewModel`

`VideosView` binds `VideoDto` directly today, which is why the grid could never share the blog card. This adapts the DTO onto `ICardItem` so the shared template's `DataContext` bindings resolve, and onto `IPlayableCardItem` so the play layer switches on.

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/VideoListItemViewModel.cs`

**Interfaces:**
- Consumes: `ICardItem`, `IPlayableCardItem`, `MZikmund.DataContracts.Videos.VideoDto`.
- Produces: `VideoListItemViewModel` with `VideoDto Video` and `Uri? ThumbnailUri`.

- [ ] **Step 1: Create the file**

`ThumbnailUrl` and `YouTubeUrl` are `required string` on the DTO but come off an RSS feed, so both are parsed defensively — a malformed URL degrades to the card's tinted media placeholder instead of throwing inside a template.
```csharp
using Humanizer;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.DataContracts.Videos;

namespace MZikmund.ViewModels.Items;

public sealed class VideoListItemViewModel : ObservableObject, ICardItem, IPlayableCardItem
{
	private static readonly IReadOnlyList<string> NoBadges = Array.Empty<string>();

	private ImageSource? _cardImage;

	public VideoListItemViewModel(VideoDto video)
	{
		Video = video ?? throw new ArgumentNullException(nameof(video));
	}

	public VideoDto Video { get; }

	public Uri? ThumbnailUri =>
		Uri.TryCreate(Video.ThumbnailUrl, UriKind.Absolute, out var uri) ? uri : null;

	public Uri? WatchUri =>
		Uri.TryCreate(Video.YouTubeUrl, UriKind.Absolute, out var uri) ? uri : null;

	public ImageSource? CardImage => _cardImage ??= ThumbnailUri is { } uri ? new BitmapImage(uri) : null;

	public bool HasCardImage => ThumbnailUri is not null;

	public string CardTitle => Video.Title;

	public string CardSummary => Video.Description;

	public string CardMeta => Video.PublishedDate.Humanize();

	// Videos carry no taxonomy, so the card's badge strip stays empty.
	public IReadOnlyList<string> CardBadges => NoBadges;

	public bool HasPlayAffordance => true;
}
```

- [ ] **Step 2: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Items/VideoListItemViewModel.cs
git commit -m "feat: Adapt VideoDto onto the shared card item contract"
```

---

### Task 52: Rewrite `VideosViewModel`

Adds the lead, the error and empty states the page has never had, and moves opening YouTube out of a `HyperlinkButton` into the ViewModel. `ILoadingIndicator` goes, for the same reason it went from `BlogViewModel`: the page owns its own states instead of blanking the whole shell.

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/VideosViewModel.cs:1-80` (full replacement)

**Interfaces:**
- Consumes: `VideoListItemViewModel`, `IMZikmundApi.GetVideosAsync()`, `Localizer`.
- Produces: `Lead`, `HasError`, `IsEmpty`, `Videos` as `ObservableCollection<VideoListItemViewModel>`, `OpenVideoAsync`.

- [ ] **Step 1: Replace the file in full**
```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using MZikmund.Api.Client;
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Items;
using Launcher = Windows.System.Launcher;

namespace MZikmund.ViewModels;

public class VideosViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILogger<VideosViewModel> _logger;
	private bool _isLoading;
	private bool _hasError;

	public VideosViewModel(
		IMZikmundApi api,
		ILogger<VideosViewModel> logger)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_logger = logger;
		RefreshCommand = new AsyncRelayCommand(LoadVideosAsync);
	}

	public override string Title => Localizer.Instance.GetString("Videos");

	public string Lead => Localizer.Instance.GetString("VideosLead");

	public ObservableCollection<VideoListItemViewModel> Videos { get; } = new();

	public ICommand RefreshCommand { get; }

	public bool IsLoading
	{
		get => _isLoading;
		private set
		{
			SetProperty(ref _isLoading, value);
			OnPropertyChanged(nameof(IsEmpty));
		}
	}

	public bool HasError
	{
		get => _hasError;
		private set
		{
			SetProperty(ref _hasError, value);
			OnPropertyChanged(nameof(IsEmpty));
		}
	}

	public bool IsEmpty => !IsLoading && !HasError && Videos.Count == 0;

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		await LoadVideosAsync();
	}

	public async Task OpenVideoAsync(VideoListItemViewModel item)
	{
		ArgumentNullException.ThrowIfNull(item);

		if (item.WatchUri is { } uri)
		{
			await Launcher.LaunchUriAsync(uri);
		}
		else
		{
			_logger.LogWarning("Video {VideoId} has an unusable watch URL", item.Video.VideoId);
		}
	}

	private async Task LoadVideosAsync()
	{
		HasError = false;
		IsLoading = true;
		try
		{
			var response = await _api.GetVideosAsync();
			if (!response.IsSuccessStatusCode || response.Content is not { } content)
			{
				_logger.LogWarning("Failed to load videos. Status: {StatusCode}", response.StatusCode);
				HasError = true;
				return;
			}

			Videos.Clear();
			foreach (var video in content)
			{
				Videos.Add(new VideoListItemViewModel(video));
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load videos");
			HasError = true;
		}
		finally
		{
			IsLoading = false;
			OnPropertyChanged(nameof(IsEmpty));
		}
	}
}
```

- [ ] **Step 2: Build `MZikmund.App.Core` / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App.Core/MZikmund.App.Core.csproj -f net10.0-desktop
```
`VideosView.xaml` still binds the old shape and is rebuilt in a later task, so build the Core project alone here.

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/VideosViewModel.cs
git commit -m "feat: Give VideosViewModel a lead, an error state and an empty state"
```

---

### Task 53: Localized strings for the videos page

`Videos`, `Refresh`, `WatchOnYouTube` and `Retry` are produced by Phase 1 Task 11 and Phase 2 Task 15 — a `.resw` cannot hold a duplicate `name`, so Step 1 verifies rather than re-adds them.

**Files:**
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw` (before the closing `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (before the closing `</root>`)

**Interfaces:**
- Produces: `VideosLead`, `NoVideosFound`, `NoVideosFoundDescription` (en + cs).

- [ ] **Step 1: Verify the inherited keys exist / Expected: 4 hits per file**
```bash
grep -c 'name="\(Videos\|Refresh\|WatchOnYouTube\|Retry\)"' src/app/MZikmund.App/Strings/en/Resources.resw src/app/MZikmund.App/Strings/cs/Resources.resw
```
Expected `4` for each file. If a count is lower, Phase 1 Task 11 has not landed — add only the missing entries, using exactly these values:
```xml
  <data name="Videos" xml:space="preserve">
    <value>Videos</value>
  </data>
  <data name="Refresh" xml:space="preserve">
    <value>Refresh</value>
  </data>
  <data name="WatchOnYouTube" xml:space="preserve">
    <value>Watch on YouTube</value>
  </data>
  <data name="Retry" xml:space="preserve">
    <value>Try again</value>
  </data>
```
and their Czech values `Videa`, `Obnovit`, `Přehrát na YouTube`, `Zkusit znovu`.

- [ ] **Step 2: Add the English keys**

Insert immediately before the closing `</root>` of `src/app/MZikmund.App/Strings/en/Resources.resw`:
```xml
  <data name="VideosLead" xml:space="preserve">
    <value>Talks, tutorials and screencasts from my YouTube channel.</value>
  </data>
  <data name="NoVideosFound" xml:space="preserve">
    <value>No videos yet</value>
  </data>
  <data name="NoVideosFoundDescription" xml:space="preserve">
    <value>There is nothing published on the channel at the moment. Check back soon.</value>
  </data>
```

- [ ] **Step 3: Add the Czech keys**

Insert immediately before the closing `</root>` of `src/app/MZikmund.App/Strings/cs/Resources.resw`:
```xml
  <data name="VideosLead" xml:space="preserve">
    <value>Přednášky, návody a screencasty z mého YouTube kanálu.</value>
  </data>
  <data name="NoVideosFound" xml:space="preserve">
    <value>Zatím žádná videa</value>
  </data>
  <data name="NoVideosFoundDescription" xml:space="preserve">
    <value>Na kanálu momentálně nic není. Zkuste to prosím později.</value>
  </data>
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Strings
git commit -m "feat: Add en and cs strings for the videos page"
```

---

### Task 54: Rebuild `VideosView`

The grid switches to the blog's exact geometry — `MinItemWidth="320"`, `MinItemHeight="420"`, 24px on both axes, `ItemsStretch="Fill"` — instead of 250×300 with 16px gutters and a 150px thumbnail. The card is `MzContentCardStyle`, so the media is 180px 16:9, the title sits below it on a solid surface, and hover/press/focus come from the style's own VSM. The naked right-aligned `Refresh` becomes an `MzPageHeader` action, and the `Padding="0"` `HyperlinkButton` disappears entirely: the whole card is the link, and the card's accent-subtle chip carries the localized "Watch on YouTube".

**Files:**
- Modify: `src/app/MZikmund.App/Views/VideosView.xaml:1-107` (full replacement)
- Modify: `src/app/MZikmund.App/Views/VideosView.xaml.cs:1-15` (full replacement)

**Interfaces:**
- Consumes: `MzPageHeader`, `MzContentCardStyle`, `MzStandardButtonStyle`, `MzEmptyState*`, `MzPagePadding*`, `MzIconFontFamily`, `MzGlyphArrowClockwise` / `MzGlyphVideoClip` / `MzGlyphWarning`, `VideosViewModel`, `VideoListItemViewModel`.
- Produces: nothing downstream.

- [ ] **Step 1: Replace `src/app/MZikmund.App/Views/VideosView.xaml` in full**

No `Background` attribute: Phase 2 strips those and the shell's `PageGround` paints the page.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<local:VideosViewBase
	x:Class="MZikmund.Views.VideosView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:controls="using:MZikmund.Controls"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:extensions="using:MZikmund.Extensions.Xaml"
	xmlns:local="using:MZikmund.Views"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	xmlns:vmitems="using:MZikmund.ViewModels.Items"
	mc:Ignorable="d">

	<Grid x:Name="PageRoot" Padding="{StaticResource MzPagePaddingCompact}">
		<Grid.RowDefinitions>
			<RowDefinition Height="Auto" />
			<RowDefinition Height="*" />
		</Grid.RowDefinitions>

		<controls:MzPageHeader
			Grid.Row="0"
			Margin="0,0,0,24"
			Lead="{x:Bind ViewModel.Lead, Mode=OneWay}"
			Title="{x:Bind ViewModel.Title, Mode=OneWay}">
			<controls:MzPageHeader.Actions>
				<Button
					AutomationProperties.Name="{extensions:Localize Key=Refresh}"
					Command="{x:Bind ViewModel.RefreshCommand, Mode=OneWay}"
					Style="{StaticResource MzStandardButtonStyle}">
					<StackPanel Orientation="Horizontal" Spacing="8">
						<FontIcon
							VerticalAlignment="Center"
							FontFamily="{StaticResource MzIconFontFamily}"
							FontSize="16"
							Glyph="{StaticResource MzGlyphArrowClockwise}" />
						<TextBlock VerticalAlignment="Center" Text="{extensions:Localize Key=Refresh}" />
					</StackPanel>
				</Button>
			</controls:MzPageHeader.Actions>
		</controls:MzPageHeader>

		<ScrollViewer
			x:Name="Scroller"
			Grid.Row="1"
			Padding="0,0,0,24">
			<ItemsRepeater ItemsSource="{x:Bind ViewModel.Videos, Mode=OneWay}">
				<ItemsRepeater.Layout>
					<UniformGridLayout
						ItemsStretch="Fill"
						MinColumnSpacing="24"
						MinItemHeight="420"
						MinItemWidth="320"
						MinRowSpacing="24" />
				</ItemsRepeater.Layout>
				<ItemsRepeater.ItemTemplate>
					<DataTemplate x:DataType="vmitems:VideoListItemViewModel">
						<Button
							AutomationProperties.HelpText="{extensions:Localize Key=WatchOnYouTube}"
							AutomationProperties.Name="{x:Bind CardTitle}"
							Click="VideoCard_Click"
							Style="{StaticResource MzContentCardStyle}"
							Tag="{extensions:Localize Key=WatchOnYouTube}" />
					</DataTemplate>
				</ItemsRepeater.ItemTemplate>
			</ItemsRepeater>
		</ScrollViewer>

		<ProgressRing
			Grid.Row="1"
			Width="48"
			Height="48"
			HorizontalAlignment="Center"
			VerticalAlignment="Center"
			IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
			Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />

		<StackPanel
			Grid.Row="1"
			Style="{StaticResource MzEmptyStatePanelStyle}"
			Visibility="{x:Bind ViewModel.IsEmpty, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
			<FontIcon Glyph="{StaticResource MzGlyphVideoClip}" Style="{StaticResource MzEmptyStateIconStyle}" />
			<TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{extensions:Localize Key=NoVideosFound}" />
			<TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{extensions:Localize Key=NoVideosFoundDescription}" />
		</StackPanel>

		<StackPanel
			Grid.Row="1"
			Style="{StaticResource MzEmptyStatePanelStyle}"
			Visibility="{x:Bind ViewModel.HasError, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
			<FontIcon Glyph="{StaticResource MzGlyphWarning}" Style="{StaticResource MzEmptyStateIconStyle}" />
			<TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{extensions:Localize Key=CouldNotLoadData}" />
			<TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{extensions:Localize Key=ErrorLoadingData}" />
			<Button
				HorizontalAlignment="Center"
				Command="{x:Bind ViewModel.RefreshCommand, Mode=OneWay}"
				Content="{extensions:Localize Key=Retry}"
				Style="{StaticResource MzStandardButtonStyle}" />
		</StackPanel>

		<VisualStateManager.VisualStateGroups>
			<VisualStateGroup x:Name="MzTypeScale">
				<VisualState x:Name="MzTypeScaleWide">
					<VisualState.StateTriggers>
						<AdaptiveTrigger MinWindowWidth="768" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="PageRoot.Padding" Value="{StaticResource MzPagePaddingWide}" />
					</VisualState.Setters>
				</VisualState>
				<VisualState x:Name="MzTypeScaleCompact" />
			</VisualStateGroup>
		</VisualStateManager.VisualStateGroups>
	</Grid>
</local:VideosViewBase>
```
`MzPageHeader` carries its own `MzTypeScale` group for the title ramp, so this page's group only moves the gutter.

- [ ] **Step 2: Replace `src/app/MZikmund.App/Views/VideosView.xaml.cs` in full**
```csharp
using MZikmund.ViewModels;
using MZikmund.ViewModels.Items;

namespace MZikmund.Views;

public sealed partial class VideosView : VideosViewBase
{
	public VideosView()
	{
		this.InitializeComponent();
	}

	private async void VideoCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: VideoListItemViewModel item } && ViewModel is { } viewModel)
		{
			await viewModel.OpenVideoAsync(item);
		}
	}
}

public partial class VideosViewBase : PageBase<VideosViewModel>
{
}
```

- [ ] **Step 3: Build the desktop head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Views/VideosView.xaml src/app/MZikmund.App/Views/VideosView.xaml.cs
git commit -m "feat: Rebuild VideosView on the shared card vocabulary"
```

---

### Task 55: Verify the videos grid

**Files:**
- Modify: none unless verification finds defects.

**Interfaces:**
- Consumes: everything above.
- Produces: a verified `VideosView`.

- [ ] **Step 1: Build the four local heads / Expected: four × Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
```
WASM is CI-verified only on this machine.

- [ ] **Step 2: Launch the desktop head**

Uno App MCP `uno_app_start` with `projectPath = D:\Personal\mzikmund-worktrees\dev-mazi-appredesign\src\app\MZikmund.App\MZikmund.App.csproj`, `targetFramework = net10.0-desktop`, `connectionTimeoutSeconds = 120`. Navigate to Videos from the nav pane (`uno_app_visualtree_snapshot`, then `uno_app_element_peer_default_action` on `VideosNavigationViewItem`).

- [ ] **Step 3: Screenshot and compare against Blog**

`uno_app_get_screenshot` on Videos, then navigate to Blog and screenshot again. The two grids must be indistinguishable in geometry: same card width at the same window size, same 24px gutters, same 180px media band, same title/meta/excerpt rhythm, same accent-subtle chip. Any difference in card size is a bug in this slice, not a design choice.

- [ ] **Step 4: Verify the play affordance on a light thumbnail**

Find a card whose thumbnail is bright (`uno_app_visualtree_snapshot` at `detail = full` for bounds). Expected at rest: the white 48px play glyph is legible because of the offset 40%-black copy behind it, with **no** full-bleed scrim. Then `uno_app_pointer_move` to the card centre and screenshot: the 25% scrim fades in over 167 ms, the card rises 2px, the border goes to `MzStrokeControlStrong` and the title to `MzAccentText`.

- [ ] **Step 5: Verify keyboard reach and that Blog is unaffected**

`uno_app_key_press` `virtualKey = Tab` until a card takes focus: the two-tone focus ring appears and the scrim lights. Press `Enter` — expected: the system browser opens the YouTube URL. Then go to Blog and confirm **no** play glyph and **no** scrim appear on any post card (the `FallbackValue=False` binding is what guarantees this).

- [ ] **Step 6: Verify both themes**

Flip the theme in Settings, return to Videos and screenshot. Expected: cards `#2B2B2B` on a `#202020` ground in dark, `#FFFFFF` on `#F3F3F3` in light, and the scrim plus play glyph identical in both — they are theme-invariant by design. Then `uno_app_close`.

- [ ] **Step 7: Confirm no English literal survives**
```bash
grep -n "Refresh\|Watch on YouTube\|Videos" src/app/MZikmund.App/Views/VideosView.xaml
```
Expected: only `extensions:Localize Key=` occurrences and the `x:Class` / namespace lines. No bare quoted English.

- [ ] **Step 8: Format / Expected: no unformatted files reported**
```bash
dotnet format MZikmund.slnx
```

- [ ] **Step 9: Commit**
```bash
git add -A
git commit -m "style: Run dotnet format after the videos view rebuild"
```## SettingsView

### Interfaces

CONSUMES (Phase 1): brushes `MzBgBaseBrush` `MzCardBrush` `MzStrokeCardBrush` `MzTextPrimaryBrush` `MzTextSecondaryBrush` `MzTextTertiaryBrush` `MzAccentTextBrush`; type styles `MzBodyTextStyle` `MzCaptionTextStyle` `MzSectionHeadingTextStyle`; button styles `MzStandardButtonStyle` `MzSubtleButtonStyle`; spacing `MzPagePaddingCompact` `MzPagePaddingWide`; radii `MzCardCornerRadius`; icons `MzIconFontFamily` + existing `MzGlyph*` keys; `Theme/Icons.xaml`, `Theme/Styles/*.xaml` and the `App.xaml` merge list.
CONSUMES (Phase 2): `MZikmund.Controls.MzPageHeader` (`Title`, `Lead`) with `xmlns:controls="using:MZikmund.Controls"`; `MZikmund.Infrastructure.MzPageTransition.AnimationsEnabled`; `WindowShell.PageGround` (the page must carry NO `Background`); `PageBase<T>.ViewModel`.
CONSUMES (existing): `Uno.Extensions.Toolkit.AppTheme` {System,Light,Dark}; `IAppPreferences`, `IPreferencesService`, `IThemeManager`, `IUserService`, `IOptions<AppConfig>`, `Localizer.Instance`, `MZikmund.Extensions.Xaml.Localize`, converters `BoolToVisibilityConverter` / `InverseBoolToVisibilityConverter`.
PRODUCES — resource keys (`Theme/Icons.xaml`): `MzGlyphWeatherSunny` `MzGlyphWeatherMoon` `MzGlyphCircleHalf` `MzGlyphRss` `MzGlyphOpen` `MzGlyphPerson` `MzGlyphInfo` `MzGlyphLink`.
PRODUCES — resource keys (`Theme/Styles/Settings.xaml`): `MzSettingsGroupHeaderTextStyle` (TextBlock), `MzSettingsIconStyle` (FontIcon, 20px), `MzThemeOptionIconStyle` (FontIcon, 16px), `MzSettingsValueTextStyle` (TextBlock).
PRODUCES — types: `MZikmund.Services.Motion.IMotionService` (`bool IsReducedMotionEnabled { get; set; }`, `bool AreAnimationsEnabled { get; }`, `void Initialize()`); `MZikmund.Infrastructure.MotionService : IMotionService`; `MZikmund.ViewModels.Items.ThemeOptionViewModel` (`AppTheme Theme`, `string Label`, `string Glyph`).
PRODUCES — members: `IAppPreferences.ReducedMotion : bool` (persisted under key `"ReducedMotion"`, roamed).
PRODUCES — `SettingsViewModel` (now `public partial`): `IReadOnlyList<ThemeOptionViewModel> ThemeOptions`, `int SelectedThemeIndex` (observable, TwoWay), `bool IsReducedMotionEnabled` (observable, TwoWay), `bool IsSignedIn`, `string AccountStatus`, `string Version`, `string WebsiteUrl`, `string RssUrl`, commands `OpenWebsiteCommand` `OpenRssFeedCommand` `SignInCommand` `SignOutCommand`. Removed: `Themes`, `SelectedTheme`.
PRODUCES — resw keys (en + cs): `SettingsLead` `Appearance` `Theme` `ThemeDescription` `ThemeSystem` `ThemeLight` `ThemeDark` `Animations` `ReducedMotion` `ReducedMotionDescription` `Account` `AccountSignedIn` `AccountSignedOut` `About` `Version` `Website` `WebsiteDescription` `RssFeed` `RssFeedDescription` `Open`.
PRODUCES — named XAML elements in `SettingsView.xaml`: `LayoutRoot`, `ContentScroll`, `ThemeOptionsRadioButtons`, `AboutExpander`; visual states `Wide` / `Compact` in group `MzSettingsLayoutStates`.

### Task 56: Localize the settings surface

**Files:**
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw` (insert before the closing `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (insert before the closing `</root>`)

**Interfaces:**
- Consumes: nothing.
- Produces: the 20 resw keys listed in the view Interfaces block.

- [ ] **Step 1: Append the English strings**

In `src/app/MZikmund.App/Strings/en/Resources.resw`, insert immediately before the final `</root>` line:

```xml
  <data name="SettingsLead" xml:space="preserve">
    <value>Personalize the app and manage your account.</value>
  </data>
  <data name="Appearance" xml:space="preserve">
    <value>Appearance</value>
  </data>
  <data name="Theme" xml:space="preserve">
    <value>Theme</value>
  </data>
  <data name="ThemeDescription" xml:space="preserve">
    <value>Choose how the app looks.</value>
  </data>
  <data name="ThemeSystem" xml:space="preserve">
    <value>Use system setting</value>
  </data>
  <data name="ThemeLight" xml:space="preserve">
    <value>Light</value>
  </data>
  <data name="ThemeDark" xml:space="preserve">
    <value>Dark</value>
  </data>
  <data name="Animations" xml:space="preserve">
    <value>Animations</value>
  </data>
  <data name="ReducedMotion" xml:space="preserve">
    <value>Reduce motion</value>
  </data>
  <data name="ReducedMotionDescription" xml:space="preserve">
    <value>Turn off page transitions and other in-app animations.</value>
  </data>
  <data name="Account" xml:space="preserve">
    <value>Account</value>
  </data>
  <data name="AccountSignedIn" xml:space="preserve">
    <value>Signed in</value>
  </data>
  <data name="AccountSignedOut" xml:space="preserve">
    <value>Not signed in</value>
  </data>
  <data name="About" xml:space="preserve">
    <value>About</value>
  </data>
  <data name="Version" xml:space="preserve">
    <value>Version</value>
  </data>
  <data name="Website" xml:space="preserve">
    <value>Website</value>
  </data>
  <data name="WebsiteDescription" xml:space="preserve">
    <value>Open the blog in your browser.</value>
  </data>
  <data name="RssFeed" xml:space="preserve">
    <value>RSS feed</value>
  </data>
  <data name="RssFeedDescription" xml:space="preserve">
    <value>Subscribe to new posts.</value>
  </data>
  <data name="Open" xml:space="preserve">
    <value>Open</value>
  </data>
```

- [ ] **Step 2: Append the Czech strings**

In `src/app/MZikmund.App/Strings/cs/Resources.resw`, insert immediately before the final `</root>` line:

```xml
  <data name="SettingsLead" xml:space="preserve">
    <value>Přizpůsobte si aplikaci a spravujte svůj účet.</value>
  </data>
  <data name="Appearance" xml:space="preserve">
    <value>Vzhled</value>
  </data>
  <data name="Theme" xml:space="preserve">
    <value>Motiv</value>
  </data>
  <data name="ThemeDescription" xml:space="preserve">
    <value>Zvolte, jak má aplikace vypadat.</value>
  </data>
  <data name="ThemeSystem" xml:space="preserve">
    <value>Podle systému</value>
  </data>
  <data name="ThemeLight" xml:space="preserve">
    <value>Světlý</value>
  </data>
  <data name="ThemeDark" xml:space="preserve">
    <value>Tmavý</value>
  </data>
  <data name="Animations" xml:space="preserve">
    <value>Animace</value>
  </data>
  <data name="ReducedMotion" xml:space="preserve">
    <value>Omezit pohyb</value>
  </data>
  <data name="ReducedMotionDescription" xml:space="preserve">
    <value>Vypne přechody stránek a další animace v aplikaci.</value>
  </data>
  <data name="Account" xml:space="preserve">
    <value>Účet</value>
  </data>
  <data name="AccountSignedIn" xml:space="preserve">
    <value>Přihlášen</value>
  </data>
  <data name="AccountSignedOut" xml:space="preserve">
    <value>Nepřihlášen</value>
  </data>
  <data name="About" xml:space="preserve">
    <value>O aplikaci</value>
  </data>
  <data name="Version" xml:space="preserve">
    <value>Verze</value>
  </data>
  <data name="Website" xml:space="preserve">
    <value>Web</value>
  </data>
  <data name="WebsiteDescription" xml:space="preserve">
    <value>Otevřít blog v prohlížeči.</value>
  </data>
  <data name="RssFeed" xml:space="preserve">
    <value>RSS kanál</value>
  </data>
  <data name="RssFeedDescription" xml:space="preserve">
    <value>Odebírat nové příspěvky.</value>
  </data>
  <data name="Open" xml:space="preserve">
    <value>Otevřít</value>
  </data>
```

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Strings/en/Resources.resw src/app/MZikmund.App/Strings/cs/Resources.resw
git commit -m "feat: Localize the settings surface in English and Czech"
```

---

### Task 57: Settings glyphs and the settings style sheet

The eight glyphs below are all already inside the Phase 1 subset (`icons.txt` ships `weather_sunny F597`, `weather_moon F57D`, `circle_half_fill E49A`, `rss EF91`, `open ECF4`, `person EDB3`, `info EA88`, `link EB68`), so `subset-icons.ps1` does **not** need rerunning. This task only names them.

`SettingsCard` / `SettingsExpander` are deliberately **not** restyled: their default templates resolve `CardBackgroundFillColorDefaultBrush`, `CardStrokeColorDefaultBrush`, `ControlCornerRadius`, `TextFillColorPrimaryBrush` and `TextFillColorSecondaryBrush`, all of which Phase 1 already redirects at the `Mz` palette. A `Style` without `BasedOn` would drop the `Template` setter and render the control invisible, and the toolkit's own `DefaultSettingsCardStyle` key is not part of any contract this plan controls.

**Files:**
- Modify: `src/app/MZikmund.App/Theme/Icons.xaml` (append before `</ResourceDictionary>`)
- Create: `src/app/MZikmund.App/Theme/Styles/Settings.xaml`
- Modify: `src/app/MZikmund.App/App.xaml`

**Interfaces:**
- Consumes: `MzIconFontFamily`, `MzSectionHeadingTextStyle`, `MzCaptionTextStyle`, `MzTextTertiaryBrush`
- Produces: `MzGlyphWeatherSunny` `MzGlyphWeatherMoon` `MzGlyphCircleHalf` `MzGlyphRss` `MzGlyphOpen` `MzGlyphPerson` `MzGlyphInfo` `MzGlyphLink`; `MzSettingsGroupHeaderTextStyle` `MzSettingsIconStyle` `MzThemeOptionIconStyle` `MzSettingsValueTextStyle`

- [ ] **Step 1: Add the glyph keys**

In `src/app/MZikmund.App/Theme/Icons.xaml`, insert directly above the closing `</ResourceDictionary>`:

```xml
	<x:String x:Key="MzGlyphWeatherSunny">&#xF597;</x:String>
	<x:String x:Key="MzGlyphWeatherMoon">&#xF57D;</x:String>
	<x:String x:Key="MzGlyphCircleHalf">&#xE49A;</x:String>
	<x:String x:Key="MzGlyphRss">&#xEF91;</x:String>
	<x:String x:Key="MzGlyphOpen">&#xECF4;</x:String>
	<x:String x:Key="MzGlyphPerson">&#xEDB3;</x:String>
	<x:String x:Key="MzGlyphInfo">&#xEA88;</x:String>
	<x:String x:Key="MzGlyphLink">&#xEB68;</x:String>
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Theme/Styles/Settings.xaml`**

No `Foreground` is set on either icon style, so the icons inherit `SettingsCard`'s (and the radio button's) foreground and grey out correctly when disabled.

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<Style x:Key="MzSettingsGroupHeaderTextStyle" BasedOn="{StaticResource MzSectionHeadingTextStyle}" TargetType="TextBlock">
		<Setter Property="Margin" Value="0,0,0,8" />
	</Style>

	<Style x:Key="MzSettingsIconStyle" TargetType="FontIcon">
		<Setter Property="FontFamily" Value="{StaticResource MzIconFontFamily}" />
		<Setter Property="FontSize" Value="20" />
	</Style>

	<Style x:Key="MzThemeOptionIconStyle" TargetType="FontIcon">
		<Setter Property="FontFamily" Value="{StaticResource MzIconFontFamily}" />
		<Setter Property="FontSize" Value="16" />
	</Style>

	<Style x:Key="MzSettingsValueTextStyle" BasedOn="{StaticResource MzCaptionTextStyle}" TargetType="TextBlock">
		<Setter Property="Foreground" Value="{ThemeResource MzTextTertiaryBrush}" />
		<Setter Property="IsTextSelectionEnabled" Value="True" />
		<Setter Property="VerticalAlignment" Value="Center" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 3: Merge it last in `src/app/MZikmund.App/App.xaml`**

`Settings.xaml` derives from `MzSectionHeadingTextStyle` (`TextBlocks.xaml`) and `MzIconFontFamily` (`Icons.xaml`), so it must come after both. Replace the line:

```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/EmptyState.xaml" />
```

with:

```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/EmptyState.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Settings.xaml" />
```

- [ ] **Step 4: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App/Theme/Icons.xaml src/app/MZikmund.App/Theme/Styles/Settings.xaml src/app/MZikmund.App/App.xaml
git commit -m "feat: Add the settings glyph keys and the settings style sheet"
```

---

### Task 58: Persisted reduced-motion preference and the motion kill switch

`UISettings.AnimationsEnabled` is hardcoded `true` on iOS, Desktop and WASM — only Android and the WinAppSDK head report it truthfully. So the persisted preference is the authority everywhere, and the system signal is only consulted on the two heads that tell the truth. The whole platform split lives in `MotionService`; no `#if` reaches XAML or the ViewModel.

**Files:**
- Modify: `src/app/MZikmund.App.Core/Services/Preferences/IAppPreferences.cs:5`
- Modify: `src/app/MZikmund.App.Core/Services/Preferences/AppPreferences.cs:5-23`
- Create: `src/app/MZikmund.App.Core/Services/Motion/IMotionService.cs`
- Create: `src/app/MZikmund.App/Infrastructure/MotionService.cs`
- Modify: `src/app/MZikmund.App/App.xaml.cs:17-19`, `:107-109`, `:142-143`

**Interfaces:**
- Consumes: `MZikmund.Infrastructure.MzPageTransition.AnimationsEnabled` (Phase 2), `IPreferencesService`
- Produces: `IAppPreferences.ReducedMotion`, `MZikmund.Services.Motion.IMotionService`, `MZikmund.Infrastructure.MotionService`

- [ ] **Step 1: Add the preference to the interface**

In `src/app/MZikmund.App.Core/Services/Preferences/IAppPreferences.cs`, replace:

```csharp
	AppTheme Theme { get; set; }
```

with:

```csharp
	AppTheme Theme { get; set; }

	/// <summary>
	/// Gets or sets the in-app "reduce motion" preference. Required because
	/// UISettings.AnimationsEnabled is hardcoded true on iOS, Desktop and WASM.
	/// </summary>
	bool ReducedMotion { get; set; }
```

- [ ] **Step 2: Implement it**

In `src/app/MZikmund.App.Core/Services/Preferences/AppPreferences.cs`, replace:

```csharp
	private const string AppThemeKey = "AppTheme";
```

with:

```csharp
	private const string AppThemeKey = "AppTheme";
	private const string ReducedMotionKey = "ReducedMotion";
```

and replace:

```csharp
	public AppTheme Theme
	{
		get => _preferences.GetComplex(AppThemeKey, () => AppTheme.System, true);
		set => _preferences.SetComplex(AppThemeKey, value, true);
	}
```

with:

```csharp
	public AppTheme Theme
	{
		get => _preferences.GetComplex(AppThemeKey, () => AppTheme.System, true);
		set => _preferences.SetComplex(AppThemeKey, value, true);
	}

	public bool ReducedMotion
	{
		get => _preferences.GetSetting(ReducedMotionKey, () => false, true);
		set => _preferences.SetSetting(ReducedMotionKey, value, true);
	}
```

- [ ] **Step 3: Create `src/app/MZikmund.App.Core/Services/Motion/IMotionService.cs`**

```csharp
namespace MZikmund.Services.Motion;

/// <summary>
/// The single gate every in-app animation asks before it runs.
/// </summary>
public interface IMotionService
{
	/// <summary>
	/// Gets or sets the persisted "reduce motion" preference. Setting it applies immediately.
	/// </summary>
	bool IsReducedMotionEnabled { get; set; }

	/// <summary>
	/// Gets a value indicating whether animations may run: the preference is off AND, on the
	/// heads that report it honestly, the system animation setting is on.
	/// </summary>
	bool AreAnimationsEnabled { get; }

	/// <summary>
	/// Pushes the persisted preference into the animation layer. Called once at startup.
	/// </summary>
	void Initialize();
}
```

- [ ] **Step 4: Create `src/app/MZikmund.App/Infrastructure/MotionService.cs`**

This is where the motion code reads the setting: `Apply()` writes `MzPageTransition.AnimationsEnabled`, and `MzPageTransition.PlayEntrance` (Phase 2) short-circuits to `Opacity = 1` when it is `false`. Every later animation asks `IMotionService.AreAnimationsEnabled` the same way.

```csharp
#if WINDOWS || __ANDROID__
using Windows.UI.ViewManagement;
#endif
using MZikmund.Services.Motion;
using MZikmund.Services.Preferences;

namespace MZikmund.Infrastructure;

/// <inheritdoc />
public sealed class MotionService : IMotionService
{
	private readonly IAppPreferences _appPreferences;

#if WINDOWS || __ANDROID__
	// Only these two heads report AnimationsEnabled truthfully; Uno hardcodes true elsewhere.
	private readonly UISettings _uiSettings = new();
#endif

	public MotionService(IAppPreferences appPreferences)
	{
		_appPreferences = appPreferences ?? throw new ArgumentNullException(nameof(appPreferences));
	}

	public bool IsReducedMotionEnabled
	{
		get => _appPreferences.ReducedMotion;
		set
		{
			if (value == _appPreferences.ReducedMotion)
			{
				return;
			}

			_appPreferences.ReducedMotion = value;
			Apply();
		}
	}

	public bool AreAnimationsEnabled => !_appPreferences.ReducedMotion && SystemAnimationsEnabled;

	public void Initialize() => Apply();

	private bool SystemAnimationsEnabled
	{
#if WINDOWS || __ANDROID__
		get => _uiSettings.AnimationsEnabled;
#else
		get => true;
#endif
	}

	private void Apply() => MzPageTransition.AnimationsEnabled = AreAnimationsEnabled;
}
```

- [ ] **Step 5: Register and initialize it in `src/app/MZikmund.App/App.xaml.cs`**

Add the using next to the other service usings — replace:

```csharp
using MZikmund.Services.Loading;
using MZikmund.Services.Navigation;
```

with:

```csharp
using MZikmund.Services.Loading;
using MZikmund.Services.Motion;
using MZikmund.Services.Navigation;
```

Register it as a singleton — replace:

```csharp
		services.AddSingleton<IAppPreferences, AppPreferences>();
```

with:

```csharp
		services.AddSingleton<IAppPreferences, AppPreferences>();
		services.AddSingleton<IMotionService, MotionService>();
```

Apply the stored preference before the shell (and therefore any page transition) exists — replace:

```csharp
		Host = builder.Build();
		Ioc.Default.ConfigureServices(Host.Services);
```

with:

```csharp
		Host = builder.Build();
		Ioc.Default.ConfigureServices(Host.Services);

		// Must run before the first page loads, or the first entrance animation ignores the preference.
		Host.Services.GetRequiredService<IMotionService>().Initialize();
```

- [ ] **Step 6: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 7: Build the Android head (exercises the `__ANDROID__` branch) / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
```

- [ ] **Step 8: Commit**
```bash
git add src/app/MZikmund.App.Core/Services/Preferences src/app/MZikmund.App.Core/Services/Motion src/app/MZikmund.App/Infrastructure/MotionService.cs src/app/MZikmund.App/App.xaml.cs
git commit -m "feat: Add a persisted reduced-motion preference and the motion kill switch"
```

---

### Task 59: Theme option item and the `SettingsViewModel` rebuild

Today `Themes` is a raw `AppTheme[]` and `SelectedTheme` is an untyped `object`, so the picker renders `System` / `Light` / `Dark` via `Enum.ToString()` — unlocalized and iconless. This replaces both with a real item type. `SelectedIndex` (an `int`) is bound rather than `SelectedItem` (an `object`) so no boxed-reference identity comparison is involved on any head.

Glyphs are read from `Theme/Icons.xaml` at construction, so `Icons.xaml` stays the single source of truth and no codepoint literal lands in the ViewModel.

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/ThemeOptionViewModel.cs`
- Modify: `src/app/MZikmund.App.Core/ViewModels/SettingsViewModel.cs:1-66` (full replacement)

**Interfaces:**
- Consumes: `AppTheme`, `IAppPreferences`, `IThemeManager`, `IMotionService`, `IUserService`, `IOptions<AppConfig>`, `Localizer.Instance`, `MzGlyphCircleHalf` / `MzGlyphWeatherSunny` / `MzGlyphWeatherMoon`
- Produces: `ThemeOptionViewModel`; `SettingsViewModel.ThemeOptions` / `SelectedThemeIndex` / `IsReducedMotionEnabled` / `IsSignedIn` / `AccountStatus` / `Version` / `WebsiteUrl` / `RssUrl` / `OpenWebsiteCommand` / `OpenRssFeedCommand` / `SignInCommand` / `SignOutCommand`

- [ ] **Step 1: Create `src/app/MZikmund.App.Core/ViewModels/Items/ThemeOptionViewModel.cs`**

```csharp
namespace MZikmund.ViewModels.Items;

/// <summary>
/// One entry in the theme picker: the enum value, a localized label and a Fluent glyph.
/// </summary>
public sealed class ThemeOptionViewModel
{
	public ThemeOptionViewModel(AppTheme theme, string label, string glyph)
	{
		Theme = theme;
		Label = label;
		Glyph = glyph;
	}

	public AppTheme Theme { get; }

	public string Label { get; }

	public string Glyph { get; }
}
```

- [ ] **Step 2: Replace `src/app/MZikmund.App.Core/ViewModels/SettingsViewModel.cs` in full**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MZikmund.Business.Models;
using MZikmund.Services.Account;
using MZikmund.Services.Localization;
using MZikmund.Services.Motion;
using MZikmund.Services.Preferences;
using MZikmund.Services.Theming;
using MZikmund.ViewModels.Items;

namespace MZikmund.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
	private const string DefaultWebsiteUrl = "https://mzikmund.dev";

	private readonly IAppPreferences _appPreferences;
	private readonly IThemeManager _themeManager;
	private readonly IMotionService _motionService;
	private readonly IUserService _userService;
	private readonly IOptions<AppConfig> _appConfig;
	private readonly ThemeOptionViewModel[] _themeOptions;
	private bool _isInitializing;

	public SettingsViewModel(
		IAppPreferences appPreferences,
		IThemeManager themeManager,
		IMotionService motionService,
		IUserService userService,
		IOptions<AppConfig> appConfig)
	{
		_appPreferences = appPreferences ?? throw new ArgumentNullException(nameof(appPreferences));
		_themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
		_motionService = motionService ?? throw new ArgumentNullException(nameof(motionService));
		_userService = userService ?? throw new ArgumentNullException(nameof(userService));
		_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

		_themeOptions =
		[
			new ThemeOptionViewModel(AppTheme.System, Localizer.Instance.GetString("ThemeSystem"), GetGlyph("MzGlyphCircleHalf")),
			new ThemeOptionViewModel(AppTheme.Light, Localizer.Instance.GetString("ThemeLight"), GetGlyph("MzGlyphWeatherSunny")),
			new ThemeOptionViewModel(AppTheme.Dark, Localizer.Instance.GetString("ThemeDark"), GetGlyph("MzGlyphWeatherMoon")),
		];

		_isInitializing = true;
		SelectedThemeIndex = Math.Max(0, Array.FindIndex(_themeOptions, option => option.Theme == _appPreferences.Theme));
		IsReducedMotionEnabled = _motionService.IsReducedMotionEnabled;
		_isInitializing = false;

		RefreshAccountState();
	}

	public override string Title => Localizer.Instance.GetString("Settings");

	public IReadOnlyList<ThemeOptionViewModel> ThemeOptions => _themeOptions;

	[ObservableProperty]
	public partial int SelectedThemeIndex { get; set; }

	[ObservableProperty]
	public partial bool IsReducedMotionEnabled { get; set; }

	[ObservableProperty]
	public partial bool IsSignedIn { get; set; }

	[ObservableProperty]
	public partial string AccountStatus { get; set; } = "";

	public string WebsiteUrl => string.IsNullOrWhiteSpace(_appConfig.Value.WebUrl)
		? DefaultWebsiteUrl
		: _appConfig.Value.WebUrl!.TrimEnd('/');

	public string RssUrl => $"{WebsiteUrl}/rss";

	public string Version
	{
		get
		{
			var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			if (!string.IsNullOrEmpty(informationalVersion))
			{
				return informationalVersion;
			}

			return assembly.GetName().Version?.ToString() ?? "1.0.0";
		}
	}

	public override void ViewLoaded() => RefreshAccountState();

	partial void OnSelectedThemeIndexChanged(int value)
	{
		if (_isInitializing || value < 0 || value >= _themeOptions.Length)
		{
			return;
		}

		var theme = _themeOptions[value].Theme;
		if (theme == _appPreferences.Theme)
		{
			return;
		}

		_appPreferences.Theme = theme;
		_themeManager.SetTheme(theme);
	}

	partial void OnIsReducedMotionEnabledChanged(bool value)
	{
		if (_isInitializing)
		{
			return;
		}

		_motionService.IsReducedMotionEnabled = value;
	}

	[RelayCommand]
	private async Task OpenWebsiteAsync() => await Windows.System.Launcher.LaunchUriAsync(new Uri(WebsiteUrl));

	[RelayCommand]
	private async Task OpenRssFeedAsync() => await Windows.System.Launcher.LaunchUriAsync(new Uri(RssUrl));

	[RelayCommand]
	private async Task SignInAsync()
	{
		await _userService.AuthenticateAsync();
		RefreshAccountState();
	}

	[RelayCommand]
	private async Task SignOutAsync()
	{
		await _userService.LogoutAsync();
		RefreshAccountState();
	}

	private void RefreshAccountState()
	{
		IsSignedIn = _userService.IsLoggedIn;
		AccountStatus = IsSignedIn
			? _userService.UserName ?? Localizer.Instance.GetString("AccountSignedIn")
			: Localizer.Instance.GetString("AccountSignedOut");
	}

	// Icons.xaml stays the single source of truth for glyphs; "?" matches MzFontIcon's fallback.
	private static string GetGlyph(string key) =>
		Application.Current.Resources.TryGetValue(key, out var value) && value is string glyph ? glyph : "?";
}
```

- [ ] **Step 3: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

If the compiler reports `SelectedTheme`/`Themes` as missing, a view still binds the removed members — the only call site is `SettingsView.xaml`, rewritten in the next task.

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Items/ThemeOptionViewModel.cs src/app/MZikmund.App.Core/ViewModels/SettingsViewModel.cs
git commit -m "feat: Give the theme picker localized, icon-bearing options"
```

---

### Task 60: Rebuild `SettingsView` on `SettingsCard` / `SettingsExpander`

Notes on the two defects named in the spec:

1. **The duplicate title is already gone** — Phase 2 replaced the raw `TextBlock` with `MzPageHeader` and deleted the shell's `ContentHeader` band. This task keeps exactly one heading, now with a lead line.
2. **`d:Background`** — Phase 2 stripped it and moved the runtime ground to `WindowShell`'s `PageGround` border (`MzBgBaseBrush`). The page therefore carries **no** `Background` of its own; re-adding one would paint over the shell's ground and defeat the title-bar acrylic. Step 3 verifies the ground actually renders.

Exactly one `SettingsExpander` is used (About), so the mobile smoke test covers both control types with a minimal blast radius.

**Files:**
- Modify: `src/app/MZikmund.App/Views/SettingsView.xaml:1-24` (full replacement)

**Interfaces:**
- Consumes: `MzPageHeader.Title` / `.Lead`; `MzSettingsGroupHeaderTextStyle` `MzSettingsIconStyle` `MzThemeOptionIconStyle` `MzSettingsValueTextStyle` `MzBodyTextStyle` `MzStandardButtonStyle` `MzPagePaddingCompact` `MzPagePaddingWide`; `MzGlyphCircleHalf` `MzGlyphPlayCircle` `MzGlyphPerson` `MzGlyphInfo` `MzGlyphLink` `MzGlyphRss`; `BoolToVisibilityConverter` `InverseBoolToVisibilityConverter`; every `SettingsViewModel` member listed above
- Produces: named elements `LayoutRoot` `ContentScroll` `ThemeOptionsRadioButtons` `AboutExpander`; states `Wide` / `Compact` in `MzSettingsLayoutStates`

- [ ] **Step 1: Replace `src/app/MZikmund.App/Views/SettingsView.xaml` in full**

```xml
<local:SettingsViewBase
	x:Class="MZikmund.Views.SettingsView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:controls="using:MZikmund.Controls"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:extensions="using:MZikmund.Extensions.Xaml"
	xmlns:items="using:MZikmund.ViewModels.Items"
	xmlns:local="using:MZikmund.Views"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	xmlns:mux="using:Microsoft.UI.Xaml.Controls"
	xmlns:tkcontrols="using:CommunityToolkit.WinUI.Controls"
	mc:Ignorable="d">

	<Grid x:Name="LayoutRoot">
		<ScrollViewer
			x:Name="ContentScroll"
			Padding="{StaticResource MzPagePaddingCompact}"
			HorizontalScrollMode="Disabled"
			VerticalScrollBarVisibility="Auto">

			<StackPanel
				MaxWidth="780"
				HorizontalAlignment="Left"
				Spacing="24">

				<controls:MzPageHeader
					Lead="{extensions:Localize Key=SettingsLead}"
					Title="{x:Bind ViewModel.Title, Mode=OneWay}" />

				<!--  Appearance  -->
				<StackPanel Spacing="4">
					<TextBlock Style="{StaticResource MzSettingsGroupHeaderTextStyle}" Text="{extensions:Localize Key=Appearance}" />

					<tkcontrols:SettingsCard
						ContentAlignment="Vertical"
						Description="{extensions:Localize Key=ThemeDescription}"
						Header="{extensions:Localize Key=Theme}">
						<tkcontrols:SettingsCard.HeaderIcon>
							<FontIcon Glyph="{StaticResource MzGlyphCircleHalf}" Style="{StaticResource MzSettingsIconStyle}" />
						</tkcontrols:SettingsCard.HeaderIcon>
						<mux:RadioButtons
							x:Name="ThemeOptionsRadioButtons"
							AutomationProperties.AutomationId="ThemeOptions"
							AutomationProperties.Name="{extensions:Localize Key=Theme}"
							ItemsSource="{x:Bind ViewModel.ThemeOptions}"
							MaxColumns="1"
							SelectedIndex="{x:Bind ViewModel.SelectedThemeIndex, Mode=TwoWay}">
							<mux:RadioButtons.ItemTemplate>
								<DataTemplate x:DataType="items:ThemeOptionViewModel">
									<StackPanel Orientation="Horizontal" Spacing="12">
										<FontIcon
											AutomationProperties.AccessibilityView="Raw"
											Glyph="{x:Bind Glyph}"
											Style="{StaticResource MzThemeOptionIconStyle}"
											VerticalAlignment="Center" />
										<TextBlock
											Style="{StaticResource MzBodyTextStyle}"
											Text="{x:Bind Label}"
											VerticalAlignment="Center" />
									</StackPanel>
								</DataTemplate>
							</mux:RadioButtons.ItemTemplate>
						</mux:RadioButtons>
					</tkcontrols:SettingsCard>

					<tkcontrols:SettingsCard
						Description="{extensions:Localize Key=ReducedMotionDescription}"
						Header="{extensions:Localize Key=ReducedMotion}">
						<tkcontrols:SettingsCard.HeaderIcon>
							<FontIcon Glyph="{StaticResource MzGlyphPlayCircle}" Style="{StaticResource MzSettingsIconStyle}" />
						</tkcontrols:SettingsCard.HeaderIcon>
						<ToggleSwitch
							AutomationProperties.AutomationId="ReducedMotionToggle"
							AutomationProperties.Name="{extensions:Localize Key=ReducedMotion}"
							IsOn="{x:Bind ViewModel.IsReducedMotionEnabled, Mode=TwoWay}"
							MinWidth="0"
							OffContent=""
							OnContent="" />
					</tkcontrols:SettingsCard>
				</StackPanel>

				<!--  Account  -->
				<StackPanel Spacing="4">
					<TextBlock Style="{StaticResource MzSettingsGroupHeaderTextStyle}" Text="{extensions:Localize Key=Account}" />

					<tkcontrols:SettingsCard Description="{x:Bind ViewModel.AccountStatus, Mode=OneWay}" Header="{extensions:Localize Key=Account}">
						<tkcontrols:SettingsCard.HeaderIcon>
							<FontIcon Glyph="{StaticResource MzGlyphPerson}" Style="{StaticResource MzSettingsIconStyle}" />
						</tkcontrols:SettingsCard.HeaderIcon>
						<StackPanel Orientation="Horizontal" Spacing="8">
							<Button
								AutomationProperties.AutomationId="SignInButton"
								Command="{x:Bind ViewModel.SignInCommand}"
								Content="{extensions:Localize Key=Login}"
								Style="{StaticResource MzStandardButtonStyle}"
								Visibility="{x:Bind ViewModel.IsSignedIn, Converter={StaticResource InverseBoolToVisibilityConverter}, Mode=OneWay}" />
							<Button
								AutomationProperties.AutomationId="SignOutButton"
								Command="{x:Bind ViewModel.SignOutCommand}"
								Content="{extensions:Localize Key=Logout}"
								Style="{StaticResource MzStandardButtonStyle}"
								Visibility="{x:Bind ViewModel.IsSignedIn, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}" />
						</StackPanel>
					</tkcontrols:SettingsCard>
				</StackPanel>

				<!--  About  -->
				<StackPanel Spacing="4">
					<TextBlock Style="{StaticResource MzSettingsGroupHeaderTextStyle}" Text="{extensions:Localize Key=About}" />

					<tkcontrols:SettingsExpander
						x:Name="AboutExpander"
						Description="{extensions:Localize Key=ApplicationName}"
						Header="{extensions:Localize Key=About}"
						IsExpanded="True">
						<tkcontrols:SettingsExpander.HeaderIcon>
							<FontIcon Glyph="{StaticResource MzGlyphInfo}" Style="{StaticResource MzSettingsIconStyle}" />
						</tkcontrols:SettingsExpander.HeaderIcon>
						<tkcontrols:SettingsExpander.Items>

							<tkcontrols:SettingsCard Header="{extensions:Localize Key=Version}">
								<TextBlock Style="{StaticResource MzSettingsValueTextStyle}" Text="{x:Bind ViewModel.Version}" />
							</tkcontrols:SettingsCard>

							<tkcontrols:SettingsCard
								Description="{x:Bind ViewModel.WebsiteUrl}"
								Header="{extensions:Localize Key=Website}">
								<tkcontrols:SettingsCard.HeaderIcon>
									<FontIcon Glyph="{StaticResource MzGlyphLink}" Style="{StaticResource MzSettingsIconStyle}" />
								</tkcontrols:SettingsCard.HeaderIcon>
								<Button
									AutomationProperties.AutomationId="OpenWebsiteButton"
									AutomationProperties.Name="{extensions:Localize Key=WebsiteDescription}"
									Command="{x:Bind ViewModel.OpenWebsiteCommand}"
									Content="{extensions:Localize Key=Open}"
									Style="{StaticResource MzStandardButtonStyle}" />
							</tkcontrols:SettingsCard>

							<tkcontrols:SettingsCard
								Description="{x:Bind ViewModel.RssUrl}"
								Header="{extensions:Localize Key=RssFeed}">
								<tkcontrols:SettingsCard.HeaderIcon>
									<FontIcon Glyph="{StaticResource MzGlyphRss}" Style="{StaticResource MzSettingsIconStyle}" />
								</tkcontrols:SettingsCard.HeaderIcon>
								<Button
									AutomationProperties.AutomationId="OpenRssButton"
									AutomationProperties.Name="{extensions:Localize Key=RssFeedDescription}"
									Command="{x:Bind ViewModel.OpenRssFeedCommand}"
									Content="{extensions:Localize Key=Open}"
									Style="{StaticResource MzStandardButtonStyle}" />
							</tkcontrols:SettingsCard>

						</tkcontrols:SettingsExpander.Items>
					</tkcontrols:SettingsExpander>
				</StackPanel>

			</StackPanel>
		</ScrollViewer>

		<VisualStateManager.VisualStateGroups>
			<VisualStateGroup x:Name="MzSettingsLayoutStates">
				<VisualState x:Name="Wide">
					<VisualState.StateTriggers>
						<AdaptiveTrigger MinWindowWidth="768" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="ContentScroll.Padding" Value="{StaticResource MzPagePaddingWide}" />
					</VisualState.Setters>
				</VisualState>
				<VisualState x:Name="Compact">
					<VisualState.StateTriggers>
						<AdaptiveTrigger MinWindowWidth="0" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="ContentScroll.Padding" Value="{StaticResource MzPagePaddingCompact}" />
					</VisualState.Setters>
				</VisualState>
			</VisualStateGroup>
		</VisualStateManager.VisualStateGroups>
	</Grid>
</local:SettingsViewBase>
```

`SettingsView.xaml.cs` is unchanged: it only calls `InitializeComponent()` and declares `SettingsViewBase : PageBase<SettingsViewModel>`.

- [ ] **Step 2: Build both reference heads / Expected: Build succeeded, 0 errors**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

- [ ] **Step 3: Verify on the desktop head with the Uno App MCP**

Run the desktop head, navigate to Settings and check:
1. **Exactly one** "Settings" heading, followed by the lead line — the shell renders no second one.
2. The page has a solid ground and no transparent gap: this comes from `WindowShell`'s `PageGround` (`MzBgBaseBrush`), not from the page. If the page looks transparent, `PageGround` is missing from the shell — fix it there, do **not** add `Background` to this page.
3. Theme picker shows **"Use system setting" / "Light" / "Dark"** with a half-circle, sun and moon glyph — never `System` / `Light` / `Dark` from `Enum.ToString()`, and never `?` (a `?` glyph means the codepoint is outside the subsetted font).
4. Selecting Light then Dark flips the whole app immediately; reopening Settings keeps the selection.
5. Toggle **Reduce motion** on, navigate Blog → Settings → Blog: pages appear instantly with no fade or rise. Toggle it off: the 333 ms entrance returns. Restart the app with it on and confirm the very first page also does not animate (that is `IMotionService.Initialize()`).
6. Expand/collapse About; Version, Website and RSS rows render with their glyphs.
7. Resize across 768: the page gutter goes 16 → 36 and the title steps up to the wide ramp.
8. Screenshot in **light and dark**.

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Views/SettingsView.xaml
git commit -m "feat: Rebuild SettingsView on SettingsCard and SettingsExpander"
```

---

### Task 61: MANDATORY mobile smoke test for `SettingsCard` / `SettingsExpander` (gate, spec D7)

**This is a gate, not a nicety.** `CommunityToolkit.WinUI.Controls.SettingsControls` ships `lib/net9.0-android35.0` and `lib/net9.0-ios18.0` assets that were compiled against **native-renderer Uno.WinUI 5.5.87**, while this app ships the **Skia** Uno.UI. The package restores and the project builds clean on both heads; the failure, if any, is at runtime — a `TypeLoadException`, `MissingMethodException`, `XamlParseException` on the generic template, or a control that lays out to zero height. It must be caught on real hardware, not in an emulator screenshot.

**Files:** none (verification only, unless the fallback triggers).

**Interfaces:**
- Consumes: the rebuilt `SettingsView.xaml`
- Produces: a go/no-go on `SettingsCard` / `SettingsExpander` for every later view

- [ ] **Step 1: Deploy to a physical Android device**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android -t:Run
```

- [ ] **Step 2: Watch the device log while opening Settings**
```bash
adb logcat -c
adb logcat *:E Mono:I MonoDroid:I Uno:I
```
Navigate to Settings and record the log.

- [ ] **Step 3: Check the seven runtime symptoms (Android)**

| # | What to look for | Failure signature |
|---|---|---|
| 1 | The page appears at all | `XamlParseException` / `TypeLoadException` / `MissingMethodException` naming `CommunityToolkit.WinUI.Controls` in logcat |
| 2 | Each `SettingsCard` has real height (~60–68px) with header, description, header icon and right-aligned content | cards collapse to a 0px line, or content overlaps the header |
| 3 | The theme `SettingsCard` stacks its `RadioButtons` **below** the header | `ContentAlignment="Vertical"` ignored — radios squeezed on the right edge |
| 4 | `AboutExpander` expands **and** collapses on tap, and all three `Items` render | chevron animates but the item host stays empty → `SettingsExpander.Items` did not materialize |
| 5 | Tapping the `ToggleSwitch` and the Open buttons hits the control, not the card | the tap is swallowed by the card and nothing happens |
| 6 | Card background/stroke/radius follow the theme in both light and dark | flat white or system-default grey → the redirected WinUI brush keys are not reaching the toolkit template |
| 7 | Header icons render as glyphs | `?` or tofu boxes → the subsetted font did not deploy to this head |

Also flip the theme and toggle Reduce motion on the device: on Android `IMotionService` additionally consults `UISettings.AnimationsEnabled`, so with **Developer options → Animator duration scale = off** animations must stay off even with the in-app toggle off.

- [ ] **Step 4: Deploy to a physical iOS device and repeat**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios -t:Run
```
Read the device log with Console.app (or `idevicesyslog`) filtered on the app process and run the same seven checks. iOS additionally: the in-app Reduce motion toggle is the **only** signal (`UISettings.AnimationsEnabled` is hardcoded `true` there), so verify page transitions actually stop when it is on.

- [ ] **Step 5: Record the outcome**

Both heads pass → the toolkit controls are cleared for the rest of the redesign; no code change.

Any check fails on either head → do **not** ship the toolkit control; execute the fallback in Step 6 and re-run Steps 1–4.

- [ ] **Step 6 (fallback, only if Step 5 fails): hand-rolled `MzSettingsCard`**

Roughly 120 lines, no new dependency:

1. Create `src/app/MZikmund.App/Controls/MzSettingsCard.cs` — `public sealed partial class MzSettingsCard : ContentControl` in namespace `MZikmund.Controls`, with `DependencyProperty`s `Header` (`object`), `Description` (`object`), `HeaderIcon` (`IconElement`) and `IsContentBelowHeader` (`bool`); `DefaultStyleKey = typeof(MzSettingsCard)` in the constructor.
2. Add `MzSettingsCardStyle` to `Theme/Styles/Settings.xaml` — a `Style TargetType="controls:MzSettingsCard"` carrying a full `ControlTemplate`: outer `Border` with `Background="{ThemeResource MzCardBrush}"`, `BorderBrush="{ThemeResource MzStrokeCardBrush}"`, `BorderThickness="1"`, `CornerRadius="{StaticResource MzCardCornerRadius}"`, `MinHeight="60"`, `Padding="{StaticResource MzCardPadding}"`, wrapping a 3-column `Grid` (`Auto` icon / `*` header+description / `Auto` content). Column 0 hosts a `ContentPresenter` bound to `HeaderIcon` with `Margin="0,0,16,0"`; column 1 a `StackPanel` with a `ContentPresenter` bound to `Header` styled `MzBodyTextStyle` over a `ContentPresenter` bound to `Description` styled `MzCaptionTextStyle` and `MzTextSecondaryBrush`, collapsed when null via `NullToVisibilityConverter`; column 2 the default `ContentPresenter`. Add a `CommonStates` `VisualStateGroup` whose `PointerOver` state sets `Background` to `MzShimmerBrush` — because `Background` is a brush property, that transition must use `VisualState.Setters`, or, if animated, `EnableDependentAnimation="True"`.
3. Add a second `VisualState` group `MzSettingsCardLayout` with a `Vertical` state that moves the content presenter to `Grid.Row="1" Grid.ColumnSpan="3"`, driven from `IsContentBelowHeader` in `OnApplyTemplate` via `VisualStateManager.GoToState`.
4. In `SettingsView.xaml` swap `xmlns:tkcontrols` usages: `tkcontrols:SettingsCard` → `controls:MzSettingsCard` (`ContentAlignment="Vertical"` → `IsContentBelowHeader="True"`), and `tkcontrols:SettingsExpander` → a stock `Expander` whose `Header` is the same header row and whose `Content` is a `StackPanel` of `MzSettingsCard`s.
5. Drop the `CommunityToolkit.WinUI.Controls.SettingsControls` `PackageReference` from `src/app/MZikmund.App/MZikmund.App.csproj` and its `PackageVersion` from `src/Directory.Packages.props`.

- [ ] **Step 7: Format and commit the verification outcome**
```bash
dotnet format MZikmund.slnx
git add -A
git commit -m "test: Smoke-test SettingsCard and SettingsExpander on Android and iOS hardware"
```
## ContactView (new) — Phase 3

### Interfaces

PRODUCES (later work may rely on these):
- `MZikmund.ViewModels.ContactViewModel : PageViewModel` (partial). Members: `override string Title` (localized `Contact`), `string Lead`, `string EmailAddress`, `string WebsiteUrl`, `string WebsiteDisplayName`, `string RssUrl`, `string TalkPdfUrl`, `string GitHubUrl`, `string YouTubeUrl`, `string XUrl`, `string FacebookUrl`, `string LinkedInUrl`, `bool IsEmailCopied` (observable), commands `OpenLinkCommand` (`IAsyncRelayCommand<string?>`), `SendEmailCommand`, `CopyEmailCommand`.
- `MZikmund.Views.ContactView : ContactViewBase`, `MZikmund.Views.ContactViewBase : PageBase<ContactViewModel>` — auto-registered by `NavigationService.RegisterViewsFromAssembly` (type name ends in `View`, key `"ContactView"`), so `Navigate<ContactViewModel>()` / `TryNavigate("ContactViewModel")` resolve it.
- DI: `services.AddScoped<ContactViewModel>()` in `App.xaml.cs`.
- New `Theme/Icons.xaml` glyph keys (appended, none redefined): `MzGlyphLink` (EB68), `MzGlyphRss` (EF91), `MzGlyphCopy` (E5D7), `MzGlyphOpen` (ECF4), `MzGlyphDocumentPdf` (E783). All five codepoints are already inside Phase 1's subset unicode list.
- New resw keys (en + cs): `ContactLead`, `GetInTouch`, `FollowMe`, `Email`, `SendEmail`, `CopyEmailAddress`, `CopiedToClipboard`, `Website`, `ContactWebsiteDescription`, `RssFeed`, `ContactRssDescription`, `ContactPdfTitle`, `ContactPdfDescription`.
- Page-local (not global) styles inside `ContactView.xaml`: `MzContactRowStyle`, `MzContactRowIconStyle`, `MzContactSocialButtonStyle`.

CONSUMES:
- Phase 1 — brushes `MzTextTertiaryBrush`, `MzAccentTextBrush`, `MzStrokeDividerBrush`; `MzIconFontFamily`; glyph `MzGlyphMail`; `Thickness` `MzPagePaddingCompact` / `MzPagePaddingWide`; `x:Double` `MzSpace2` / `MzSpace3` / `MzSpace6`; text styles `MzBodyTextStyle`, `MzMetaTextStyle`, `MzCaptionTextStyle`, `MzSectionHeadingTextStyle`; button styles `MzStandardButtonStyle`, `MzSubtleButtonStyle`, `MzSubtleButtonSmallStyle`; border style `MzLayerSurfaceStyle`.
- Phase 2 — `MZikmund.Controls.MzPageHeader` (`Title`, `Lead`), `xmlns:controls="using:MZikmund.Controls"`; the shell's `Tag="ContactViewModel"` nav item and `INavigationService.TryNavigate`.
- Existing app — `PageBase<T>`, `MZikmund.Extensions.Xaml.Localize`, `BoolToVisibilityConverter`, `MZikmund.Business.Models.AppConfig`.

### Task 62: Add the Contact icon glyphs to the theme icon vocabulary

**Files:**
- Modify: `src/app/MZikmund.App/Theme/Icons.xaml` (append inside the existing `<ResourceDictionary>`, after `MzGlyphMore`)

**Interfaces:**
- Consumes: `Theme/Icons.xaml` (Phase 1 Task 1).
- Produces: `MzGlyphLink`, `MzGlyphRss`, `MzGlyphCopy`, `MzGlyphOpen`, `MzGlyphDocumentPdf`.

- [ ] **Step 1: Append the five glyph keys**

All five codepoints are already in the Phase 1 subset (`EB68`, `EF91`, `E5D7`, `ECF4`, `E783` appear in `subset-icons.ps1`'s `$unicodes`), so no font regeneration is needed. Insert immediately before the closing `</ResourceDictionary>` of `src/app/MZikmund.App/Theme/Icons.xaml`:

```xml
	<!--  Added by ContactView.  -->
	<x:String x:Key="MzGlyphLink">&#xEB68;</x:String>
	<x:String x:Key="MzGlyphRss">&#xEF91;</x:String>
	<x:String x:Key="MzGlyphCopy">&#xE5D7;</x:String>
	<x:String x:Key="MzGlyphOpen">&#xECF4;</x:String>
	<x:String x:Key="MzGlyphDocumentPdf">&#xE783;</x:String>
```

- [ ] **Step 2: Commit**
```bash
git add src/app/MZikmund.App/Theme/Icons.xaml
git commit -m "feat: Add link, RSS, copy, open and PDF glyph keys"
```

---

### Task 63: Localized Contact strings (en + cs)

**Files:**
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw` (insert before `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (insert before `</root>`)

**Interfaces:**
- Consumes: nothing.
- Produces: the 13 resource keys listed in the view's Interfaces section. `Contact` itself already exists in `en` and is added to `cs` by Phase 2.

- [ ] **Step 1: Add the English strings**

Insert immediately before the final `</root>` in `src/app/MZikmund.App/Strings/en/Resources.resw`:

```xml
  <data name="ContactLead" xml:space="preserve">
    <value>Get in touch or find me on social media.</value>
  </data>
  <data name="GetInTouch" xml:space="preserve">
    <value>Get in touch</value>
  </data>
  <data name="FollowMe" xml:space="preserve">
    <value>Follow me</value>
  </data>
  <data name="Email" xml:space="preserve">
    <value>E-mail</value>
  </data>
  <data name="SendEmail" xml:space="preserve">
    <value>Send e-mail</value>
  </data>
  <data name="CopyEmailAddress" xml:space="preserve">
    <value>Copy e-mail address</value>
  </data>
  <data name="CopiedToClipboard" xml:space="preserve">
    <value>Copied to clipboard</value>
  </data>
  <data name="Website" xml:space="preserve">
    <value>Website</value>
  </data>
  <data name="ContactWebsiteDescription" xml:space="preserve">
    <value>Articles, videos and everything else.</value>
  </data>
  <data name="RssFeed" xml:space="preserve">
    <value>RSS feed</value>
  </data>
  <data name="ContactRssDescription" xml:space="preserve">
    <value>Subscribe to new posts in your reader.</value>
  </data>
  <data name="ContactPdfTitle" xml:space="preserve">
    <value>Elektromobilita</value>
  </data>
  <data name="ContactPdfDescription" xml:space="preserve">
    <value>A talk about electromobility (PDF)</value>
  </data>
```

- [ ] **Step 2: Add the Czech strings**

Insert immediately before the final `</root>` in `src/app/MZikmund.App/Strings/cs/Resources.resw`:

```xml
  <data name="ContactLead" xml:space="preserve">
    <value>Napište mi nebo mě najděte na sociálních sítích.</value>
  </data>
  <data name="GetInTouch" xml:space="preserve">
    <value>Napište mi</value>
  </data>
  <data name="FollowMe" xml:space="preserve">
    <value>Sledujte mě</value>
  </data>
  <data name="Email" xml:space="preserve">
    <value>E-mail</value>
  </data>
  <data name="SendEmail" xml:space="preserve">
    <value>Odeslat e-mail</value>
  </data>
  <data name="CopyEmailAddress" xml:space="preserve">
    <value>Kopírovat e-mailovou adresu</value>
  </data>
  <data name="CopiedToClipboard" xml:space="preserve">
    <value>Zkopírováno do schránky</value>
  </data>
  <data name="Website" xml:space="preserve">
    <value>Web</value>
  </data>
  <data name="ContactWebsiteDescription" xml:space="preserve">
    <value>Články, videa a všechno ostatní.</value>
  </data>
  <data name="RssFeed" xml:space="preserve">
    <value>RSS kanál</value>
  </data>
  <data name="ContactRssDescription" xml:space="preserve">
    <value>Odebírejte nové články ve své čtečce.</value>
  </data>
  <data name="ContactPdfTitle" xml:space="preserve">
    <value>Elektromobilita</value>
  </data>
  <data name="ContactPdfDescription" xml:space="preserve">
    <value>Přednáška o elektromobilitě (PDF)</value>
  </data>
```

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Strings/en/Resources.resw src/app/MZikmund.App/Strings/cs/Resources.resw
git commit -m "feat: Localize the Contact page strings in English and Czech"
```

---

### Task 64: `ContactViewModel`

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/ContactViewModel.cs`
- Modify: `src/app/MZikmund.App/App.xaml.cs:127-139`

**Interfaces:**
- Consumes: `PageViewModel`, `Localizer`, `IOptions<AppConfig>` (`MZikmund.Business.Models`), `Windows.System.Launcher`, `Windows.ApplicationModel.DataTransfer.Clipboard`.
- Produces: `MZikmund.ViewModels.ContactViewModel` and its DI registration.

- [ ] **Step 1: Confirm the LinkedIn vanity URL before writing the file**

The repo has no LinkedIn reference — `src/web/MZikmund.Web/Pages/Contact.cshtml` lists only GitHub, X, YouTube and Facebook. The other four URLs below are copied verbatim from that page and from `_Layout.cshtml`; the LinkedIn one (`https://www.linkedin.com/in/mzikmund/`) is the only value not verifiable from the source tree. Open it once in a browser and correct the constant if it 404s.

- [ ] **Step 2: Create `src/app/MZikmund.App.Core/ViewModels/ContactViewModel.cs`**

`Launcher.LaunchUriAsync` is the same call `PostEditorViewModel` already uses for external links, and `DataPackage`/`Clipboard` mirror `ImageVariantsDialogViewModel.CopyUrl`. Both work on every head.

```csharp
using Microsoft.Extensions.Options;
using MZikmund.Business.Models;
using MZikmund.Services.Localization;
using Windows.ApplicationModel.DataTransfer;
using Launcher = Windows.System.Launcher;

namespace MZikmund.ViewModels;

public partial class ContactViewModel : PageViewModel
{
	private const string FallbackWebUrl = "https://mzikmund.dev";

	private readonly ILogger<ContactViewModel> _logger;
	private readonly string _webUrl;

	public ContactViewModel(IOptions<AppConfig> appConfig, ILogger<ContactViewModel> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		var configuredUrl = appConfig?.Value?.WebUrl;
		_webUrl = (string.IsNullOrWhiteSpace(configuredUrl) ? FallbackWebUrl : configuredUrl!).TrimEnd('/');
	}

	public override string Title => Localizer.Instance.GetString("Contact");

	public string Lead => Localizer.Instance.GetString("ContactLead");

	public string EmailAddress => "martin@zikmund.dev";

	public string WebsiteUrl => _webUrl;

	public string WebsiteDisplayName => Uri.TryCreate(_webUrl, UriKind.Absolute, out var uri) ? uri.Host : _webUrl;

	public string RssUrl => _webUrl + "/rss";

	public string TalkPdfUrl => "https://cdn.mzikmund.dev/files/elektromobilitamz.pdf";

	public string GitHubUrl => "https://github.com/MartinZikmund/";

	public string YouTubeUrl => "https://www.youtube.com/@mzikmund";

	public string XUrl => "https://x.com/mzikmunddev/";

	public string FacebookUrl => "https://facebook.com/mzikmunddev/";

	public string LinkedInUrl => "https://www.linkedin.com/in/mzikmund/";

	[ObservableProperty]
	public partial bool IsEmailCopied { get; set; }

	public override void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		IsEmailCopied = false;
	}

	[RelayCommand]
	private async Task OpenLinkAsync(string? url)
	{
		if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			return;
		}

		try
		{
			await Launcher.LaunchUriAsync(uri);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to open {Uri}", uri);
		}
	}

	[RelayCommand]
	private async Task SendEmailAsync() => await OpenLinkAsync("mailto:" + EmailAddress);

	[RelayCommand]
	private void CopyEmail()
	{
		var dataPackage = new DataPackage();
		dataPackage.SetText(EmailAddress);
		Clipboard.SetContent(dataPackage);
		IsEmailCopied = true;
	}
}
```

- [ ] **Step 3: Register the ViewModel in DI**

In `src/app/MZikmund.App/App.xaml.cs`, replace:

```csharp
		services.AddScoped<VideosViewModel>();
		services.AddScoped<SettingsViewModel>();
```

with:

```csharp
		services.AddScoped<VideosViewModel>();
		services.AddScoped<ContactViewModel>();
		services.AddScoped<SettingsViewModel>();
```

- [ ] **Step 4: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/ContactViewModel.cs src/app/MZikmund.App/App.xaml.cs
git commit -m "feat: Add ContactViewModel with contact routes and social links"
```

---

### Task 65: `ContactView` — contact routes and Bootstrap brand marks

**Files:**
- Create: `src/app/MZikmund.App/Views/ContactView.xaml`
- Create: `src/app/MZikmund.App/Views/ContactView.xaml.cs`

**Interfaces:**
- Consumes: `controls:MzPageHeader` (`Title`, `Lead`); `MzLayerSurfaceStyle`; `MzStandardButtonStyle`, `MzSubtleButtonStyle`, `MzSubtleButtonSmallStyle`; `MzBodyTextStyle`, `MzMetaTextStyle`, `MzCaptionTextStyle`, `MzSectionHeadingTextStyle`; `MzIconFontFamily`, `MzGlyphMail`, `MzGlyphLink`, `MzGlyphRss`, `MzGlyphDocumentPdf`, `MzGlyphOpen`, `MzGlyphCopy`; `MzAccentTextBrush`, `MzTextTertiaryBrush`, `MzStrokeDividerBrush`; `MzPagePaddingCompact`, `MzPagePaddingWide`, `MzSpace2`, `MzSpace3`, `MzSpace6`; `BoolToVisibilityConverter`; `ContactViewModel`.
- Produces: `MZikmund.Views.ContactView`, `MZikmund.Views.ContactViewBase`.

- [ ] **Step 1: Create `src/app/MZikmund.App/Views/ContactView.xaml.cs`**

Same two-class shape as `VideosView` — the generic base cannot be the `x:Class` root directly.

```csharp
using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class ContactView : ContactViewBase
{
	public ContactView()
	{
		this.InitializeComponent();
	}
}

public partial class ContactViewBase : PageBase<ContactViewModel>
{
}
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Views/ContactView.xaml`**

Notes that matter:
- **No `Background`** — the Phase 2 shell paints `PageGround` behind the frame.
- The four contact routes live inside **one** `MzLayerSurfaceStyle` border (the `.taxonomy-list` shape), separated by 1px dividers, each row a real `Button` with a 44px minimum height.
- Brand marks are inline `PathIcon` geometries. Fluent System Icons has zero brand glyphs, so these come from Bootstrap Icons; GitHub / X / YouTube / Facebook are copied verbatim out of `src/web/MZikmund.Web/Pages/Shared/_IconSprite.cshtml`, LinkedIn from Bootstrap Icons `linkedin.svg`. Every one is prefixed **`F1 `** — XAML's path mini-language defaults to `EvenOdd`, SVG to nonzero, and without the prefix the X and Facebook marks lose their crossing/overlap fills.
- The social cluster wraps by flipping `StackPanel.Orientation` in the VSM. These are plain state setters, not animations, so no `EnableDependentAnimation` is involved.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<local:ContactViewBase
	x:Class="MZikmund.Views.ContactView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:controls="using:MZikmund.Controls"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:extensions="using:MZikmund.Extensions.Xaml"
	xmlns:local="using:MZikmund.Views"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	mc:Ignorable="d">

	<local:ContactViewBase.Resources>
		<Style
			x:Key="MzContactRowStyle"
			BasedOn="{StaticResource MzSubtleButtonStyle}"
			TargetType="Button">
			<Setter Property="MinHeight" Value="44" />
			<Setter Property="Padding" Value="16,10,16,10" />
			<Setter Property="CornerRadius" Value="0" />
			<Setter Property="HorizontalAlignment" Value="Stretch" />
			<Setter Property="HorizontalContentAlignment" Value="Stretch" />
		</Style>

		<Style x:Key="MzContactRowIconStyle" TargetType="FontIcon">
			<Setter Property="FontFamily" Value="{StaticResource MzIconFontFamily}" />
			<Setter Property="FontSize" Value="20" />
			<Setter Property="Foreground" Value="{ThemeResource MzAccentTextBrush}" />
			<Setter Property="VerticalAlignment" Value="Center" />
		</Style>

		<Style x:Key="MzContactRowTrailingIconStyle" TargetType="FontIcon">
			<Setter Property="FontFamily" Value="{StaticResource MzIconFontFamily}" />
			<Setter Property="FontSize" Value="16" />
			<Setter Property="Foreground" Value="{ThemeResource MzTextTertiaryBrush}" />
			<Setter Property="VerticalAlignment" Value="Center" />
		</Style>

		<Style
			x:Key="MzContactSocialButtonStyle"
			BasedOn="{StaticResource MzStandardButtonStyle}"
			TargetType="Button">
			<Setter Property="MinHeight" Value="44" />
			<Setter Property="HorizontalAlignment" Value="Left" />
		</Style>
	</local:ContactViewBase.Resources>

	<ScrollViewer>
		<Grid x:Name="ContentRoot" Padding="{StaticResource MzPagePaddingCompact}">
			<StackPanel
				MaxWidth="780"
				HorizontalAlignment="Left"
				Spacing="{StaticResource MzSpace6}">

				<controls:MzPageHeader
					Title="{x:Bind ViewModel.Title, Mode=OneWay}"
					Lead="{x:Bind ViewModel.Lead, Mode=OneWay}" />

				<!--  Contact routes: one Layer surface, 44px rows, dividers between.  -->
				<StackPanel Spacing="{StaticResource MzSpace3}">
					<TextBlock Style="{StaticResource MzSectionHeadingTextStyle}" Text="{extensions:Localize Key=GetInTouch}" />

					<Border Style="{StaticResource MzLayerSurfaceStyle}">
						<StackPanel>
							<Grid>
								<Grid.ColumnDefinitions>
									<ColumnDefinition Width="*" />
									<ColumnDefinition Width="Auto" />
								</Grid.ColumnDefinitions>
								<Button
									AutomationProperties.AutomationId="ContactSendEmailButton"
									AutomationProperties.Name="{extensions:Localize Key=SendEmail}"
									Command="{x:Bind ViewModel.SendEmailCommand, Mode=OneWay}"
									Style="{StaticResource MzContactRowStyle}">
									<Grid ColumnSpacing="{StaticResource MzSpace3}">
										<Grid.ColumnDefinitions>
											<ColumnDefinition Width="Auto" />
											<ColumnDefinition Width="*" />
										</Grid.ColumnDefinitions>
										<FontIcon Glyph="{StaticResource MzGlyphMail}" Style="{StaticResource MzContactRowIconStyle}" />
										<StackPanel Grid.Column="1" VerticalAlignment="Center">
											<TextBlock Style="{StaticResource MzBodyTextStyle}" Text="{extensions:Localize Key=Email}" />
											<TextBlock Style="{StaticResource MzMetaTextStyle}" Text="{x:Bind ViewModel.EmailAddress, Mode=OneWay}" />
										</StackPanel>
									</Grid>
								</Button>
								<Button
									Grid.Column="1"
									Margin="0,0,12,0"
									VerticalAlignment="Center"
									AutomationProperties.AutomationId="ContactCopyEmailButton"
									AutomationProperties.Name="{extensions:Localize Key=CopyEmailAddress}"
									Command="{x:Bind ViewModel.CopyEmailCommand, Mode=OneWay}"
									Style="{StaticResource MzSubtleButtonSmallStyle}"
									ToolTipService.ToolTip="{extensions:Localize Key=CopyEmailAddress}">
									<FontIcon Glyph="{StaticResource MzGlyphCopy}" Style="{StaticResource MzContactRowTrailingIconStyle}" />
								</Button>
							</Grid>

							<Border
								Height="1"
								Margin="16,0"
								Background="{ThemeResource MzStrokeDividerBrush}" />

							<Button
								AutomationProperties.AutomationId="ContactWebsiteButton"
								Command="{x:Bind ViewModel.OpenLinkCommand, Mode=OneWay}"
								CommandParameter="{x:Bind ViewModel.WebsiteUrl, Mode=OneWay}"
								Style="{StaticResource MzContactRowStyle}">
								<Grid ColumnSpacing="{StaticResource MzSpace3}">
									<Grid.ColumnDefinitions>
										<ColumnDefinition Width="Auto" />
										<ColumnDefinition Width="*" />
										<ColumnDefinition Width="Auto" />
									</Grid.ColumnDefinitions>
									<FontIcon Glyph="{StaticResource MzGlyphLink}" Style="{StaticResource MzContactRowIconStyle}" />
									<StackPanel Grid.Column="1" VerticalAlignment="Center">
										<TextBlock Style="{StaticResource MzBodyTextStyle}" Text="{extensions:Localize Key=Website}" />
										<TextBlock Style="{StaticResource MzMetaTextStyle}" Text="{x:Bind ViewModel.WebsiteDisplayName, Mode=OneWay}" />
									</StackPanel>
									<FontIcon
										Grid.Column="2"
										Glyph="{StaticResource MzGlyphOpen}"
										Style="{StaticResource MzContactRowTrailingIconStyle}" />
								</Grid>
							</Button>

							<Border
								Height="1"
								Margin="16,0"
								Background="{ThemeResource MzStrokeDividerBrush}" />

							<Button
								AutomationProperties.AutomationId="ContactRssButton"
								Command="{x:Bind ViewModel.OpenLinkCommand, Mode=OneWay}"
								CommandParameter="{x:Bind ViewModel.RssUrl, Mode=OneWay}"
								Style="{StaticResource MzContactRowStyle}">
								<Grid ColumnSpacing="{StaticResource MzSpace3}">
									<Grid.ColumnDefinitions>
										<ColumnDefinition Width="Auto" />
										<ColumnDefinition Width="*" />
										<ColumnDefinition Width="Auto" />
									</Grid.ColumnDefinitions>
									<FontIcon Glyph="{StaticResource MzGlyphRss}" Style="{StaticResource MzContactRowIconStyle}" />
									<StackPanel Grid.Column="1" VerticalAlignment="Center">
										<TextBlock Style="{StaticResource MzBodyTextStyle}" Text="{extensions:Localize Key=RssFeed}" />
										<TextBlock Style="{StaticResource MzMetaTextStyle}" Text="{extensions:Localize Key=ContactRssDescription}" />
									</StackPanel>
									<FontIcon
										Grid.Column="2"
										Glyph="{StaticResource MzGlyphOpen}"
										Style="{StaticResource MzContactRowTrailingIconStyle}" />
								</Grid>
							</Button>

							<Border
								Height="1"
								Margin="16,0"
								Background="{ThemeResource MzStrokeDividerBrush}" />

							<Button
								AutomationProperties.AutomationId="ContactTalkPdfButton"
								Command="{x:Bind ViewModel.OpenLinkCommand, Mode=OneWay}"
								CommandParameter="{x:Bind ViewModel.TalkPdfUrl, Mode=OneWay}"
								Style="{StaticResource MzContactRowStyle}">
								<Grid ColumnSpacing="{StaticResource MzSpace3}">
									<Grid.ColumnDefinitions>
										<ColumnDefinition Width="Auto" />
										<ColumnDefinition Width="*" />
										<ColumnDefinition Width="Auto" />
									</Grid.ColumnDefinitions>
									<FontIcon Glyph="{StaticResource MzGlyphDocumentPdf}" Style="{StaticResource MzContactRowIconStyle}" />
									<StackPanel Grid.Column="1" VerticalAlignment="Center">
										<TextBlock Style="{StaticResource MzBodyTextStyle}" Text="{extensions:Localize Key=ContactPdfTitle}" />
										<TextBlock Style="{StaticResource MzMetaTextStyle}" Text="{extensions:Localize Key=ContactPdfDescription}" />
									</StackPanel>
									<FontIcon
										Grid.Column="2"
										Glyph="{StaticResource MzGlyphOpen}"
										Style="{StaticResource MzContactRowTrailingIconStyle}" />
								</Grid>
							</Button>
						</StackPanel>
					</Border>

					<TextBlock
						Foreground="{ThemeResource MzAccentTextBrush}"
						Style="{StaticResource MzCaptionTextStyle}"
						Text="{extensions:Localize Key=CopiedToClipboard}"
						Visibility="{x:Bind ViewModel.IsEmailCopied, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}" />
				</StackPanel>

				<!--  Brand marks: Bootstrap Icons geometry, 16x16 viewBox maps 1:1 onto PathIcon.  -->
				<StackPanel Spacing="{StaticResource MzSpace3}">
					<TextBlock Style="{StaticResource MzSectionHeadingTextStyle}" Text="{extensions:Localize Key=FollowMe}" />

					<StackPanel
						x:Name="SocialCluster"
						Orientation="Vertical"
						Spacing="{StaticResource MzSpace2}">
						<Button
							AutomationProperties.AutomationId="ContactGitHubButton"
							AutomationProperties.Name="GitHub"
							Command="{x:Bind ViewModel.OpenLinkCommand, Mode=OneWay}"
							CommandParameter="{x:Bind ViewModel.GitHubUrl, Mode=OneWay}"
							Style="{StaticResource MzContactSocialButtonStyle}">
							<StackPanel Orientation="Horizontal" Spacing="{StaticResource MzSpace2}">
								<PathIcon
									Width="16"
									Height="16"
									VerticalAlignment="Center"
									Data="F1 M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27s1.36.09 2 .27c1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8" />
								<TextBlock VerticalAlignment="Center" Text="GitHub" />
							</StackPanel>
						</Button>

						<Button
							AutomationProperties.AutomationId="ContactYouTubeButton"
							AutomationProperties.Name="YouTube"
							Command="{x:Bind ViewModel.OpenLinkCommand, Mode=OneWay}"
							CommandParameter="{x:Bind ViewModel.YouTubeUrl, Mode=OneWay}"
							Style="{StaticResource MzContactSocialButtonStyle}">
							<StackPanel Orientation="Horizontal" Spacing="{StaticResource MzSpace2}">
								<PathIcon
									Width="16"
									Height="16"
									VerticalAlignment="Center"
									Data="F1 M8.051 1.999h.089c.822.003 4.987.033 6.11.335a2.01 2.01 0 0 1 1.415 1.42c.101.38.172.883.22 1.402l.01.104.022.26.008.104c.065.914.073 1.77.074 1.957v.075c-.001.194-.01 1.108-.082 2.06l-.008.105-.009.104c-.05.572-.124 1.14-.235 1.558a2.01 2.01 0 0 1-1.415 1.42c-1.16.312-5.569.334-6.18.335h-.142c-.309 0-1.587-.006-2.927-.052l-.17-.006-.087-.004-.171-.007-.171-.007c-1.11-.049-2.167-.128-2.654-.26a2.01 2.01 0 0 1-1.415-1.419c-.111-.417-.185-.986-.235-1.558L.09 9.82l-.008-.104A31 31 0 0 1 0 7.68v-.123c.002-.215.01-.958.064-1.778l.007-.103.003-.052.008-.104.022-.26.01-.104c.048-.519.119-1.023.22-1.402a2.01 2.01 0 0 1 1.415-1.42c.487-.13 1.544-.21 2.654-.26l.17-.007.172-.006.086-.003.171-.007A100 100 0 0 1 7.858 2zM6.4 5.209v4.818l4.157-2.408z" />
								<TextBlock VerticalAlignment="Center" Text="YouTube" />
							</StackPanel>
						</Button>

						<Button
							AutomationProperties.AutomationId="ContactXButton"
							AutomationProperties.Name="X"
							Command="{x:Bind ViewModel.OpenLinkCommand, Mode=OneWay}"
							CommandParameter="{x:Bind ViewModel.XUrl, Mode=OneWay}"
							Style="{StaticResource MzContactSocialButtonStyle}">
							<StackPanel Orientation="Horizontal" Spacing="{StaticResource MzSpace2}">
								<PathIcon
									Width="16"
									Height="16"
									VerticalAlignment="Center"
									Data="F1 M12.6.75h2.454l-5.36 6.142L16 15.25h-4.937l-3.867-5.07-4.425 5.07H.316l5.733-6.57L0 .75h5.063l3.495 4.633L12.601.75Zm-.86 13.028h1.36L4.323 2.145H2.865z" />
								<TextBlock VerticalAlignment="Center" Text="X" />
							</StackPanel>
						</Button>

						<Button
							AutomationProperties.AutomationId="ContactFacebookButton"
							AutomationProperties.Name="Facebook"
							Command="{x:Bind ViewModel.OpenLinkCommand, Mode=OneWay}"
							CommandParameter="{x:Bind ViewModel.FacebookUrl, Mode=OneWay}"
							Style="{StaticResource MzContactSocialButtonStyle}">
							<StackPanel Orientation="Horizontal" Spacing="{StaticResource MzSpace2}">
								<PathIcon
									Width="16"
									Height="16"
									VerticalAlignment="Center"
									Data="F1 M16 8.049c0-4.446-3.582-8.05-8-8.05C3.58 0-.002 3.603-.002 8.05c0 4.017 2.926 7.347 6.75 7.951v-5.625h-2.03V8.05H6.75V6.275c0-2.017 1.195-3.131 3.022-3.131.876 0 1.791.157 1.791.157v1.98h-1.009c-.993 0-1.303.621-1.303 1.258v1.51h2.218l-.354 2.326H9.25V16c3.824-.604 6.75-3.934 6.75-7.951" />
								<TextBlock VerticalAlignment="Center" Text="Facebook" />
							</StackPanel>
						</Button>

						<Button
							AutomationProperties.AutomationId="ContactLinkedInButton"
							AutomationProperties.Name="LinkedIn"
							Command="{x:Bind ViewModel.OpenLinkCommand, Mode=OneWay}"
							CommandParameter="{x:Bind ViewModel.LinkedInUrl, Mode=OneWay}"
							Style="{StaticResource MzContactSocialButtonStyle}">
							<StackPanel Orientation="Horizontal" Spacing="{StaticResource MzSpace2}">
								<PathIcon
									Width="16"
									Height="16"
									VerticalAlignment="Center"
									Data="F1 M0 1.146C0 .513.526 0 1.175 0h13.65C15.474 0 16 .513 16 1.146v13.708c0 .633-.526 1.146-1.175 1.146H1.175C.526 16 0 15.487 0 14.854zm4.943 12.248V6.169H2.542v7.225zm-1.2-8.212c.837 0 1.358-.554 1.358-1.248-.015-.709-.52-1.248-1.342-1.248S2.4 3.226 2.4 3.934c0 .694.521 1.248 1.327 1.248zm4.908 8.212V9.359c0-.216.016-.432.08-.586.173-.431.568-.878 1.232-.878.869 0 1.216.662 1.216 1.634v3.865h2.401V9.25c0-2.22-1.184-3.252-2.764-3.252-1.274 0-1.845.7-2.165 1.193v.025h-.016l.016-.025V6.169h-2.4c.03.678 0 7.225 0 7.225z" />
								<TextBlock VerticalAlignment="Center" Text="LinkedIn" />
							</StackPanel>
						</Button>
					</StackPanel>
				</StackPanel>
			</StackPanel>

			<VisualStateManager.VisualStateGroups>
				<VisualStateGroup x:Name="MzContactLayoutStates">
					<VisualState x:Name="Wide">
						<VisualState.StateTriggers>
							<AdaptiveTrigger MinWindowWidth="768" />
						</VisualState.StateTriggers>
						<VisualState.Setters>
							<Setter Target="ContentRoot.Padding" Value="{StaticResource MzPagePaddingWide}" />
							<Setter Target="SocialCluster.Orientation" Value="Horizontal" />
						</VisualState.Setters>
					</VisualState>
					<VisualState x:Name="Compact">
						<VisualState.StateTriggers>
							<AdaptiveTrigger MinWindowWidth="0" />
						</VisualState.StateTriggers>
					</VisualState>
				</VisualStateGroup>
			</VisualStateManager.VisualStateGroups>
		</Grid>
	</ScrollViewer>
</local:ContactViewBase>
```

- [ ] **Step 3: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 4: Verify the shell already dispatches Contact / Expected: one match**
```bash
grep -n 'Tag="ContactViewModel"' src/app/MZikmund.App/WindowShell.xaml
```
Phase 2 ships `ContactNavigationViewItem` with `Tag="ContactViewModel"` and a `Tag`-based dispatch through `INavigationService.TryNavigate`, so `ContactView` existing is the whole wiring — `RegisterViewsFromAssembly` keys it as `"ContactView"` and `TryFindViewForViewModel` strips `"Model"` off `ContactViewModel` to find it. If the grep returns nothing, Phase 2 did not land as planned: add `Tag="ContactViewModel"` to the `mux:NavigationViewItem x:Name="ContactNavigationViewItem"` opening tag in `WindowShell.xaml`, and in `WindowShell.xaml.cs` replace the empty branch

```csharp
		else if (args.InvokedItemContainer == ContactNavigationViewItem)
		{

		}
```
with

```csharp
		else if (args.InvokedItemContainer == ContactNavigationViewItem)
		{
			navigationService.Navigate<ContactViewModel>();
		}
```

- [ ] **Step 5: Run the desktop head and click Contact**

Launch with the Uno App MCP, invoke the Contact nav item, and confirm: the page navigates instead of no-opping, the five brand marks render as solid shapes (an X or Facebook mark showing a hollow centre means the `F1 ` fill-rule prefix was dropped), the four routes sit in one rounded surface with dividers, and the social cluster flips from a column to a row as the window crosses 768px. Screenshot in both light and dark.

- [ ] **Step 6: Commit**
```bash
git add src/app/MZikmund.App/Views/ContactView.xaml src/app/MZikmund.App/Views/ContactView.xaml.cs
git commit -m "feat: Add ContactView with contact routes and Bootstrap brand marks"
```

---

### Task 66: Format and close out the Contact slice

**Files:**
- Modify: any file `dotnet format` touches under `src/app/`

**Interfaces:**
- Consumes: everything above.
- Produces: nothing new.

- [ ] **Step 1: Format / Expected: no errors**
```bash
dotnet format MZikmund.slnx
```

- [ ] **Step 2: Build the Windows head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

- [ ] **Step 3: Commit any formatting fallout**
```bash
git add -A src/app
git commit -m "style: Run dotnet format after the ContactView slice"
```

---

## Phase 4 — Admin rebuild

## Phase 4 foundation — shared admin row and tag styles (`Theme/Styles/Rows.xaml`, `Theme/Styles/Tags.xaml`)

### Interfaces

PRODUCES — `src/app/MZikmund.App/Theme/Styles/Rows.xaml`:
- `MzTaxonomyRowStyle` — `Style TargetType="Button"`. Consumer contract: `Content` = display name (string, ellipsised, weight 500/17px); `Tag` = count (string, right-aligned, tertiary caption, MinWidth 24 gutter). `Command`/`CommandParameter` are free for the consumer. MinHeight 44, Padding 12,8,12,8, CornerRadius `MzControlCornerRadius`, 12px column spacing. Named template parts: `RowRoot`, `RowHoverFill`, `RowPressFill`, `RowBar`, `RowBarTransform` (CompositeTransform, CenterY 12), `RowIcon`, `RowName`, `RowCount`, `FocusRing`. VisualStateGroups `CommonStates` (Normal/PointerOver/Pressed/Disabled) and `FocusStates` (Focused/PointerFocused/Unfocused).
- `MzTaxonomyRowGlyph` — `x:String`, defaults to `{StaticResource MzGlyphFolder}`. Resolved from the template via `{ThemeResource}`, so a page overrides it in its own `Page.Resources` to change the leading icon without redefining the template.
- `MzTaxonomyListSurfaceStyle` — `Style TargetType="Border"`, `BasedOn="{StaticResource MzLayerSurfaceStyle}"`, Padding 8. The single r8 Layer surface all rows live inside.
- `MzTaxonomyRowSpacing` — `x:Double` 4 (gap between rows).
- `MzTaxonomyRowMinWidth` — `x:Double` 272 (UniformGridLayout `MinItemWidth` for a multi-column taxonomy list).

PRODUCES — `src/app/MZikmund.App/Theme/Styles/Tags.xaml`:
- `MzTagPillStyle` — `Style TargetType="Button"`. `Content` = label (string). Pill radius, Padding 12,4,12,4, MinHeight 28, 1px `MzStrokeControlStrongBrush`, 13px/Medium `MzTextSecondaryBrush`. Parts: `PillRoot`, `PillFill`, `PillLabel`, `FocusRing`.
- `MzTagPillWithCountStyle` — `BasedOn` `MzTagPillStyle`, own Template. `Content` = label, `Tag` = count (string). Adds parts `PillCount` (Border, `MzShimmerBrush`, pill radius) and `PillCountText` (11px Medium tertiary).
- `MzTagPillEmptyStyle` — `BasedOn` `MzTagPillWithCountStyle`, `Opacity` 0.55. For zero-post tags.
- `MzTagCloudSurfaceStyle` — `Style TargetType="Border"`, `BasedOn="{StaticResource MzLayerSurfaceStyle}"`, Padding 16.
- `MzTagCloudSpacing` — `x:Double` 8; `MzTagCloudItemMinWidth` — `x:Double` 120. Both feed the virtualizing `ItemsRepeater` + `UniformGridLayout` the ~500-tag view must use.

REQUIRES from Phase 1 (exact keys, must already exist): brushes `MzShimmerBrush`, `MzAccentSubtleBrush`, `MzAccentSubtleStrongBrush`, `MzAccentTextBrush`, `MzTextPrimaryBrush`, `MzTextSecondaryBrush`, `MzTextTertiaryBrush`, `MzTextDisabledBrush`, `MzStrokeControlBrush`, `MzStrokeControlStrongBrush`, `MzStrokeFocusBrush`, `MzStrokeFocusInnerBrush`; radii `MzControlCornerRadius`, `MzPillCornerRadius`; spacing `MzSpace2`, `MzSpace4`; type `MzFontFamilyMedium`, `MzCaptionTextStyle`; icons `MzIconFontFamily`, `MzGlyphFolder`; motion `MzDurationFast`, `MzEaseStandard` (both `x:String`, consumed as `KeyTime=` / `KeySpline=`); surfaces `MzLayerSurfaceStyle` (which carries `theme:MzElevation.Level="Layer"`).

REQUIRES from Phase 2: nothing. These two dictionaries are shell-independent.

APP WIRING: `src/app/MZikmund.App/App.xaml` merges `ms-appx:///Theme/Styles/Rows.xaml` then `ms-appx:///Theme/Styles/Tags.xaml`, immediately after `ms-appx:///Theme/Styles/EmptyState.xaml` — i.e. last, and strictly after `Theme/Elevation.xaml` and `Theme/Typography.xaml`, because both surface styles resolve `MzLayerSurfaceStyle` through a parse-time `BasedOn`.

PLATFORM NOTES the downstream views inherit: no `UIElement.Scale`/`Translation`/`ScalarTransition` anywhere — the centre-growing bar is `CompositeTransform.ScaleY` inside a `Storyboard`; every `DoubleAnimationUsingKeyFrames` sets `EnableDependentAnimation="True"`; all colour changes are `DiscreteObjectKeyFrame` swaps, never `ColorAnimation`, because the theme brushes are shared instances.

### Task 67: `Theme/Styles/Rows.xaml` — the taxonomy row and its Layer surface

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/Rows.xaml`

**Interfaces:**
- Consumes (Phase 1): `MzShimmerBrush`, `MzAccentSubtleBrush`, `MzAccentTextBrush`, `MzTextPrimaryBrush`, `MzTextTertiaryBrush`, `MzTextDisabledBrush`, `MzStrokeFocusBrush`, `MzStrokeFocusInnerBrush`, `MzControlCornerRadius`, `MzPillCornerRadius`, `MzSpace2`, `MzFontFamilyMedium`, `MzCaptionTextStyle`, `MzIconFontFamily`, `MzGlyphFolder`, `MzDurationFast`, `MzEaseStandard`, `MzLayerSurfaceStyle`.
- Produces: `MzTaxonomyRowGlyph` (`x:String`, page-overridable), `MzTaxonomyRowStyle` (`Style TargetType="Button"` — `Content` = display name string, `Tag` = count string), `MzTaxonomyListSurfaceStyle` (`Style TargetType="Border"`), `MzTaxonomyRowSpacing` (`x:Double` 4), `MzTaxonomyRowMinWidth` (`x:Double` 272).

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/Styles/Rows.xaml`**

Contract, so downstream views do not have to guess: `Content` is the display name (string), `Tag` is the count (string), the leading glyph comes from `MzTaxonomyRowGlyph` — a `{ThemeResource}` lookup, so a page that wants a different icon overrides the key in its own `Page.Resources` without redefining the template.

Three fidelity notes:
1. The hover fill is a dedicated `Border` whose **Opacity** animates 0→1 over 167 ms. Animating a `SolidColorBrush.Color` instead would mutate the *shared* `MzShimmerBrush` instance for the whole app, and would need `EnableDependentAnimation` on every head. Opacity is GPU-composited and safe.
2. The 3px bar grows from the centre — the web animates `inset-block: 50% → 22%`. `UIElement.Scale` is `[NotImplemented]` in Uno, so this is `CompositeTransform.ScaleY` 0→1 with `CenterY="12"` on a fixed 24px bar (56% of the 44px row, matching the 22% inset).
3. Every `DoubleAnimationUsingKeyFrames` carries `EnableDependentAnimation="True"` — harmless on independent properties, and it means nothing here is ever silently dropped by Uno.

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<!--  Override in a page's own Resources to change the leading icon; the template resolves it late.  -->
	<StaticResource x:Key="MzTaxonomyRowGlyph" ResourceKey="MzGlyphFolder" />

	<!--  Rows sit 4px apart inside one surface; 272px = the web's minmax(17rem, 1fr) column.  -->
	<x:Double x:Key="MzTaxonomyRowSpacing">4</x:Double>
	<x:Double x:Key="MzTaxonomyRowMinWidth">272</x:Double>

	<!--  One contiguous Layer surface holding every row. Compact rows on the bare page ground read as floating text.  -->
	<Style x:Key="MzTaxonomyListSurfaceStyle" TargetType="Border" BasedOn="{StaticResource MzLayerSurfaceStyle}">
		<Setter Property="Padding" Value="{StaticResource MzSpace2}" />
	</Style>

	<Style x:Key="MzTaxonomyRowStyle" TargetType="Button">
		<Setter Property="Background" Value="Transparent" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="BorderThickness" Value="0" />
		<Setter Property="CornerRadius" Value="{StaticResource MzControlCornerRadius}" />
		<Setter Property="Padding" Value="12,8,12,8" />
		<Setter Property="MinHeight" Value="44" />
		<Setter Property="HorizontalAlignment" Value="Stretch" />
		<Setter Property="HorizontalContentAlignment" Value="Stretch" />
		<Setter Property="VerticalContentAlignment" Value="Center" />
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilyMedium}" />
		<Setter Property="FontSize" Value="17" />
		<Setter Property="FontWeight" Value="Medium" />
		<Setter Property="UseSystemFocusVisuals" Value="False" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid
						x:Name="RowRoot"
						Background="{TemplateBinding Background}"
						CornerRadius="{TemplateBinding CornerRadius}">

						<Border
							x:Name="RowHoverFill"
							Background="{ThemeResource MzShimmerBrush}"
							CornerRadius="{TemplateBinding CornerRadius}"
							IsHitTestVisible="False"
							Opacity="0" />
						<Border
							x:Name="RowPressFill"
							Background="{ThemeResource MzAccentSubtleBrush}"
							CornerRadius="{TemplateBinding CornerRadius}"
							IsHitTestVisible="False"
							Opacity="0" />

						<!--  Leading 3px indicator, grown from the centre. Outside the padding, flush to the row edge.  -->
						<Border
							x:Name="RowBar"
							Width="3"
							Height="24"
							HorizontalAlignment="Left"
							VerticalAlignment="Center"
							Background="{ThemeResource MzAccentTextBrush}"
							CornerRadius="{StaticResource MzPillCornerRadius}"
							IsHitTestVisible="False"
							Opacity="0">
							<Border.RenderTransform>
								<CompositeTransform x:Name="RowBarTransform" CenterY="12" ScaleY="0" />
							</Border.RenderTransform>
						</Border>

						<Border Padding="{TemplateBinding Padding}">
							<Grid ColumnSpacing="12">
								<Grid.ColumnDefinitions>
									<ColumnDefinition Width="Auto" />
									<ColumnDefinition Width="*" />
									<ColumnDefinition Width="Auto" />
								</Grid.ColumnDefinitions>

								<FontIcon
									x:Name="RowIcon"
									Grid.Column="0"
									VerticalAlignment="Center"
									FontFamily="{StaticResource MzIconFontFamily}"
									FontSize="16"
									Foreground="{ThemeResource MzTextTertiaryBrush}"
									Glyph="{ThemeResource MzTaxonomyRowGlyph}" />

								<TextBlock
									x:Name="RowName"
									Grid.Column="1"
									VerticalAlignment="Center"
									FontFamily="{TemplateBinding FontFamily}"
									FontSize="{TemplateBinding FontSize}"
									FontWeight="{TemplateBinding FontWeight}"
									Foreground="{TemplateBinding Foreground}"
									Text="{Binding Content, RelativeSource={RelativeSource TemplatedParent}}"
									TextTrimming="CharacterEllipsis"
									TextWrapping="NoWrap" />

								<!--  Right-aligned with a fixed gutter so counts form a scannable column.  -->
								<TextBlock
									x:Name="RowCount"
									Grid.Column="2"
									MinWidth="24"
									VerticalAlignment="Center"
									Style="{StaticResource MzCaptionTextStyle}"
									Foreground="{ThemeResource MzTextTertiaryBrush}"
									Text="{Binding Tag, RelativeSource={RelativeSource TemplatedParent}}"
									TextAlignment="Right"
									TextWrapping="NoWrap" />
							</Grid>
						</Border>

						<!--  Inset two-tone ring: a negative margin would clip against the surface's 8px padding.  -->
						<Border
							x:Name="FocusRing"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="{TemplateBinding CornerRadius}"
							IsHitTestVisible="False"
							Visibility="Collapsed">
							<Border
								BorderBrush="{ThemeResource MzStrokeFocusInnerBrush}"
								BorderThickness="1"
								CornerRadius="{StaticResource MzControlCornerRadius}" />
						</Border>

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowHoverFill"
											Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowBar"
											Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowBarTransform"
											Storyboard.TargetProperty="ScaleY">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="RowName" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="RowIcon" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowHoverFill"
											Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowPressFill"
											Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowBar"
											Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowBarTransform"
											Storyboard.TargetProperty="ScaleY">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="RowName" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="RowIcon" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="RowName" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="RowIcon" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="RowCount" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Focused">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="FocusRing" Storyboard.TargetProperty="Visibility">
											<DiscreteObjectKeyFrame KeyTime="0" Value="Visible" />
										</ObjectAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowBar"
											Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="RowBarTransform"
											Storyboard.TargetProperty="ScaleY">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Unfocused" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

</ResourceDictionary>
```

- [ ] **Step 2: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

The dictionary is compiled even before it is merged, so this proves the XAML parses and every `TargetType`/`Storyboard.TargetProperty` pair is legal. Runtime key resolution is proven in the next task, once `App.xaml` merges it.

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Theme/Styles/Rows.xaml
git commit -m "feat: Add the taxonomy row style with a centre-growing accent indicator"
```

---

### Task 68: `Theme/Styles/Tags.xaml` — the tag pill, count chip and cloud surface, and the App.xaml wiring

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/Tags.xaml`
- Modify: `src/app/MZikmund.App/App.xaml:29` (the `EmptyState.xaml` merge line added by Phase 1 Task 10)

**Interfaces:**
- Consumes (Phase 1): `MzTextSecondaryBrush`, `MzTextTertiaryBrush`, `MzTextDisabledBrush`, `MzStrokeControlBrush`, `MzStrokeControlStrongBrush`, `MzAccentTextBrush`, `MzAccentSubtleBrush`, `MzAccentSubtleStrongBrush`, `MzShimmerBrush`, `MzStrokeFocusBrush`, `MzStrokeFocusInnerBrush`, `MzPillCornerRadius`, `MzControlCornerRadius`, `MzSpace4`, `MzFontFamilyMedium`, `MzLayerSurfaceStyle`.
- Produces: `MzTagPillStyle`, `MzTagPillWithCountStyle`, `MzTagPillEmptyStyle` (all `Style TargetType="Button"`), `MzTagCloudSurfaceStyle` (`Style TargetType="Border"`), `MzTagCloudSpacing` (`x:Double` 8), `MzTagCloudItemMinWidth` (`x:Double` 120).

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/Styles/Tags.xaml`**

Contract: `Content` is the tag label (string); on the `WithCount` variants `Tag` is the count (string). Three styles rather than one converter-driven template — the count chip has to be *inside* the pill's template so the pill's own VSM can retint it on hover (`.tag:hover .tag__count`), and neither `Button` nor any Phase 1/2 converter gives a declarative way to collapse it.

The pill is a `Button`, not a `HyperlinkButton`: `HyperlinkButton`'s default template owns its foreground states and would fight every state below, and a `Button` matches `MzTaxonomyRowStyle`, so both taxonomy pages share one interaction model. Colour swaps are discrete `DiscreteObjectKeyFrame`s — animating a `SolidColorBrush.Color` would mutate the shared theme brush instance app-wide.

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<!--  ~500 tags: the consuming view uses ItemsRepeater + UniformGridLayout, so it needs both numbers.  -->
	<x:Double x:Key="MzTagCloudSpacing">8</x:Double>
	<x:Double x:Key="MzTagCloudItemMinWidth">120</x:Double>

	<!--  Same Layer surface as the taxonomy list, so both taxonomy pages sit on a recognisable ground.  -->
	<Style x:Key="MzTagCloudSurfaceStyle" TargetType="Border" BasedOn="{StaticResource MzLayerSurfaceStyle}">
		<Setter Property="Padding" Value="{StaticResource MzSpace4}" />
	</Style>

	<Style x:Key="MzTagPillStyle" TargetType="Button">
		<Setter Property="Background" Value="Transparent" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
		<Setter Property="BorderBrush" Value="{ThemeResource MzStrokeControlStrongBrush}" />
		<Setter Property="BorderThickness" Value="1" />
		<Setter Property="CornerRadius" Value="{StaticResource MzPillCornerRadius}" />
		<Setter Property="Padding" Value="12,4,12,4" />
		<Setter Property="MinHeight" Value="28" />
		<Setter Property="HorizontalAlignment" Value="Left" />
		<Setter Property="VerticalAlignment" Value="Center" />
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilyMedium}" />
		<Setter Property="FontSize" Value="13" />
		<Setter Property="FontWeight" Value="Medium" />
		<Setter Property="UseSystemFocusVisuals" Value="False" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid x:Name="PillRoot">
						<Border
							x:Name="PillFill"
							Background="{TemplateBinding Background}"
							BorderBrush="{TemplateBinding BorderBrush}"
							BorderThickness="{TemplateBinding BorderThickness}"
							CornerRadius="{TemplateBinding CornerRadius}" />

						<TextBlock
							x:Name="PillLabel"
							Padding="{TemplateBinding Padding}"
							HorizontalAlignment="Center"
							VerticalAlignment="Center"
							FontFamily="{TemplateBinding FontFamily}"
							FontSize="{TemplateBinding FontSize}"
							FontWeight="{TemplateBinding FontWeight}"
							Foreground="{TemplateBinding Foreground}"
							Text="{Binding Content, RelativeSource={RelativeSource TemplatedParent}}"
							TextTrimming="CharacterEllipsis"
							TextWrapping="NoWrap" />

						<Border
							x:Name="FocusRing"
							Margin="-3"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="{StaticResource MzPillCornerRadius}"
							IsHitTestVisible="False"
							Visibility="Collapsed">
							<Border
								BorderBrush="{ThemeResource MzStrokeFocusInnerBrush}"
								BorderThickness="1"
								CornerRadius="{StaticResource MzPillCornerRadius}" />
						</Border>

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentSubtleBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillLabel" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentSubtleStrongBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillLabel" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzStrokeControlBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillLabel" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Focused">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="FocusRing" Storyboard.TargetProperty="Visibility">
											<DiscreteObjectKeyFrame KeyTime="0" Value="Visible" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Unfocused" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

</ResourceDictionary>
```

- [ ] **Step 2: Append the count-chip variant, before `</ResourceDictionary>`**

The chip is 11px on `MzShimmer`, and retints to `MzAccentSubtleStrong` + accent text with the pill.
```xml
	<Style x:Key="MzTagPillWithCountStyle" TargetType="Button" BasedOn="{StaticResource MzTagPillStyle}">
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid x:Name="PillRoot">
						<Border
							x:Name="PillFill"
							Background="{TemplateBinding Background}"
							BorderBrush="{TemplateBinding BorderBrush}"
							BorderThickness="{TemplateBinding BorderThickness}"
							CornerRadius="{TemplateBinding CornerRadius}" />

						<Border Padding="{TemplateBinding Padding}">
							<StackPanel Orientation="Horizontal" VerticalAlignment="Center">
								<TextBlock
									x:Name="PillLabel"
									VerticalAlignment="Center"
									FontFamily="{TemplateBinding FontFamily}"
									FontSize="{TemplateBinding FontSize}"
									FontWeight="{TemplateBinding FontWeight}"
									Foreground="{TemplateBinding Foreground}"
									Text="{Binding Content, RelativeSource={RelativeSource TemplatedParent}}"
									TextTrimming="CharacterEllipsis"
									TextWrapping="NoWrap" />
								<Border
									x:Name="PillCount"
									Margin="8,0,0,0"
									Padding="6,0,6,0"
									VerticalAlignment="Center"
									Background="{ThemeResource MzShimmerBrush}"
									CornerRadius="{StaticResource MzPillCornerRadius}">
									<TextBlock
										x:Name="PillCountText"
										MinWidth="12"
										FontFamily="{StaticResource MzFontFamilyMedium}"
										FontSize="11"
										FontWeight="Medium"
										Foreground="{ThemeResource MzTextTertiaryBrush}"
										Text="{Binding Tag, RelativeSource={RelativeSource TemplatedParent}}"
										TextAlignment="Right"
										TextWrapping="NoWrap" />
								</Border>
							</StackPanel>
						</Border>

						<Border
							x:Name="FocusRing"
							Margin="-3"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="{StaticResource MzPillCornerRadius}"
							IsHitTestVisible="False"
							Visibility="Collapsed">
							<Border
								BorderBrush="{ThemeResource MzStrokeFocusInnerBrush}"
								BorderThickness="1"
								CornerRadius="{StaticResource MzPillCornerRadius}" />
						</Border>

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentSubtleBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillLabel" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillCount" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentSubtleStrongBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillCountText" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentSubtleStrongBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillLabel" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillCount" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentSubtleStrongBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillCountText" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzStrokeControlBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillLabel" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillCountText" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzTextDisabledBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Focused">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="FocusRing" Storyboard.TargetProperty="Visibility">
											<DiscreteObjectKeyFrame KeyTime="0" Value="Visible" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Unfocused" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<!--  Zero-post tags lead to an empty listing: reachable, focusable, but no longer competing.  -->
	<Style x:Key="MzTagPillEmptyStyle" TargetType="Button" BasedOn="{StaticResource MzTagPillWithCountStyle}">
		<Setter Property="Opacity" Value="0.55" />
	</Style>
```

- [ ] **Step 3: Merge both dictionaries in `src/app/MZikmund.App/App.xaml`**

They must come after `Theme/Elevation.xaml` (both surface styles are `BasedOn="{StaticResource MzLayerSurfaceStyle}"`, and `StaticResource` in a `BasedOn` is resolved at parse time) and after `Theme/Typography.xaml` (`MzCaptionTextStyle`, `MzFontFamilyMedium`). Appending them last satisfies both. Replace:
```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/EmptyState.xaml" />
```
with:
```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/EmptyState.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Rows.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Tags.xaml" />
```

- [ ] **Step 4: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 5: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100` / Expected: Build succeeded**

Both surface styles carry `theme:MzElevation.Level="Layer"` through `MzLayerSurfaceStyle`, and this is the only head that compiles the `#if WINDOWS` branch that turns that into a real shadow.

- [ ] **Step 6: Commit**
```bash
git add src/app/MZikmund.App/Theme/Styles/Tags.xaml src/app/MZikmund.App/App.xaml
git commit -m "feat: Add the tag pill, count chip and tag cloud surface styles"
```

---

### Task 69: Prove both styles on a throwaway harness page

**Files:**
- Create: `src/app/MZikmund.App/Views/Admin/CategoriesManagerView.xaml` *(temporary harness content — the Categories slice replaces this body wholesale; do not build on it)*

**Interfaces:**
- Consumes: everything produced by the two tasks above.
- Produces: nothing. This task exists so the vocabulary is verified before four parallel Phase 4 views depend on it.

- [ ] **Step 1: Drop a static harness into the page body**

Replace the contents of the page's root `Grid` (keep the existing `<Page …>` element, `x:Class` and any `PageBase` generic argument exactly as they are) with:
```xml
	<ScrollViewer Padding="16">
		<StackPanel Spacing="24">

			<Border Style="{StaticResource MzTaxonomyListSurfaceStyle}">
				<StackPanel Spacing="{StaticResource MzTaxonomyRowSpacing}">
					<Button
						Content="Uno Platform"
						Style="{StaticResource MzTaxonomyRowStyle}"
						Tag="42" />
					<Button
						Content="A category name long enough that it has to be trimmed with an ellipsis somewhere around here"
						Style="{StaticResource MzTaxonomyRowStyle}"
						Tag="7" />
					<Button
						Content="Disabled"
						IsEnabled="False"
						Style="{StaticResource MzTaxonomyRowStyle}"
						Tag="0" />
				</StackPanel>
			</Border>

			<Border Style="{StaticResource MzTagCloudSurfaceStyle}">
				<StackPanel Orientation="Horizontal" Spacing="{StaticResource MzTagCloudSpacing}">
					<Button Content="dotnet" Style="{StaticResource MzTagPillStyle}" />
					<Button
						Content="winui"
						Style="{StaticResource MzTagPillWithCountStyle}"
						Tag="128" />
					<Button
						Content="never-used"
						Style="{StaticResource MzTagPillEmptyStyle}"
						Tag="0" />
				</StackPanel>
			</Border>

		</StackPanel>
	</ScrollViewer>
```

- [ ] **Step 2: Run the desktop head and check both themes**
```bash
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```
Navigate to Categories and confirm, in **light and dark**:
- the row bar grows out of the vertical centre of the row rather than dropping in from the top, and takes ~1/6 second;
- hover tints the row name **and** the folder icon accent while the count stays tertiary (this is deliberate — the web sets the count colour explicitly, so it does not follow the hover);
- counts line up in a column down the right edge;
- keyboard `Tab` shows the two-tone ring on both the rows and the pills, and moving focus onto a row also reveals its bar;
- the tag count chip flips from `MzShimmer` to accent-subtle-strong with its pill, and the zero-post pill is visibly recessive without being unreadable.

- [ ] **Step 3: Revert the harness**
```bash
git checkout -- src/app/MZikmund.App/Views/Admin/CategoriesManagerView.xaml
```
Nothing is committed from this task — the Categories slice owns that file.## PostsManagerView

### Interfaces

CONSUMES (Phase 1): brushes MzTextPrimaryBrush, MzTextSecondaryBrush, MzTextTertiaryBrush, MzShimmerBrush, MzAccentSubtleBrush, MzStrokeDividerBrush, MzStrokeFocusBrush; radii MzControlCornerRadius; spacing MzPagePaddingCompact, MzSpace2/3/4; type MzCaptionTextStyle, MzMetaTextStyle, MzFontFamilyMedium, MzMonoFontFamily; badges MzBadgeAccentStyle, MzBadgeSubtleStyle, MzBadgeNeutralStyle; surfaces MzLayerSurfaceStyle; empty state MzEmptyStatePanelStyle, MzEmptyStateIconStyle, MzEmptyStateTitleTextStyle, MzEmptyStateBodyTextStyle; icons MzIconFontFamily, MzGlyphSearch, MzGlyphMore, MzGlyphArrowClockwise; strings Refresh, NoPostsFound, NoPostsFoundDescription. CONSUMES (Phase 2): MZikmund.Controls.MzPageHeader (Title, Lead). CONSUMES (existing): BoolToVisibilityConverter, PageBase<TViewModel>, IDialogService, ILoadingIndicator, INavigationService, IMZikmundApi.GetAllPostsAsync/DeletePostAsync, MZikmund.Extensions.ObservableCollectionExtensions.AddRange, Localizer.Instance. PRODUCES: resource dictionary src/app/MZikmund.App/Theme/Styles/ListRows.xaml with keys MzListRowButtonStyle, MzListRowIconButtonStyle, MzListRowDividerStyle, MzListRowTitleTextStyle, MzListRowMetaTextStyle (CategoriesManagerView and TagsManagerView consume these as the shared dense-row vocabulary); glyph key MzGlyphAdd appended to Theme/Icons.xaml; types MZikmund.ViewModels.Items.PostRowStatus (enum Draft/Scheduled/Published) and MZikmund.ViewModels.Items.PostRowViewModel (Post, Id, Title, RouteText, DateText, SortDate, Status, StatusText, IsPublished, IsScheduled, IsDraft, Categories, HasCategories); PostsManagerViewModel members Posts (ObservableCollection&lt;PostRowViewModel&gt;), StatusFilters, SortOptions, SearchText, SelectedStatusFilterIndex, SelectedSortIndex, CountSummary, IsEmpty, RefreshCommand, DeletePostCommand (kept: AddPostCommand, UpdatePostCommand, LoadMoreCommand, CanLoadMore, IsLoadingMore, TotalCount); localization keys SearchPosts, PostStatusAll, PostStatusPublished, PostStatusScheduled, PostStatusDraft, NotPublished, SortNewestFirst, SortOldestFirst, SortTitleAscending, SortTitleDescending, FilterByStatus, SortOrder, MoreActions, Edit, CopyRoute, DeletePost, ConfirmDeletePost, DeletePostFailed, PostsCountFormat (en + cs).

### Task 70: Add the `MzGlyphAdd` glyph and the Posts manager strings

**Files:**
- Modify: `src/app/MZikmund.App/Theme/Icons.xaml` (insert before the closing `</ResourceDictionary>`)
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw` (insert before the closing `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (insert before the closing `</root>`)

**Interfaces:**
- Consumes: Phase 1 `Theme/Icons.xaml` (codepoint `E00D` is already inside the subset — it is listed in `Assets/Fonts/icons.txt` as `add E00D`).
- Produces: `MzGlyphAdd`; the 19 localization keys listed below.

- [ ] **Step 1: Append the glyph key**

In `src/app/MZikmund.App/Theme/Icons.xaml`, immediately after the `MzGlyphMore` line:
```xml
	<x:String x:Key="MzGlyphAdd">&#xE00D;</x:String>
```

- [ ] **Step 2: Append the English strings**

In `src/app/MZikmund.App/Strings/en/Resources.resw`, immediately before `</root>`:
```xml
  <data name="SearchPosts" xml:space="preserve">
    <value>Search posts</value>
  </data>
  <data name="PostStatusAll" xml:space="preserve">
    <value>All statuses</value>
  </data>
  <data name="PostStatusPublished" xml:space="preserve">
    <value>Published</value>
  </data>
  <data name="PostStatusScheduled" xml:space="preserve">
    <value>Scheduled</value>
  </data>
  <data name="PostStatusDraft" xml:space="preserve">
    <value>Draft</value>
  </data>
  <data name="NotPublished" xml:space="preserve">
    <value>Not published</value>
  </data>
  <data name="SortNewestFirst" xml:space="preserve">
    <value>Newest first</value>
  </data>
  <data name="SortOldestFirst" xml:space="preserve">
    <value>Oldest first</value>
  </data>
  <data name="SortTitleAscending" xml:space="preserve">
    <value>Title (A-Z)</value>
  </data>
  <data name="SortTitleDescending" xml:space="preserve">
    <value>Title (Z-A)</value>
  </data>
  <data name="FilterByStatus" xml:space="preserve">
    <value>Filter by status</value>
  </data>
  <data name="SortOrder" xml:space="preserve">
    <value>Sort order</value>
  </data>
  <data name="MoreActions" xml:space="preserve">
    <value>More actions</value>
  </data>
  <data name="Edit" xml:space="preserve">
    <value>Edit</value>
  </data>
  <data name="CopyRoute" xml:space="preserve">
    <value>Copy route</value>
  </data>
  <data name="DeletePost" xml:space="preserve">
    <value>Delete post</value>
  </data>
  <data name="ConfirmDeletePost" xml:space="preserve">
    <value>"{0}" will be permanently deleted. This cannot be undone.</value>
  </data>
  <data name="DeletePostFailed" xml:space="preserve">
    <value>Could not delete the post</value>
  </data>
  <data name="PostsCountFormat" xml:space="preserve">
    <value>Showing {0} of {1} posts</value>
  </data>
```

- [ ] **Step 3: Append the Czech strings**

In `src/app/MZikmund.App/Strings/cs/Resources.resw`, immediately before `</root>`:
```xml
  <data name="SearchPosts" xml:space="preserve">
    <value>Hledat prispevky</value>
  </data>
  <data name="PostStatusAll" xml:space="preserve">
    <value>Vsechny stavy</value>
  </data>
  <data name="PostStatusPublished" xml:space="preserve">
    <value>Publikovano</value>
  </data>
  <data name="PostStatusScheduled" xml:space="preserve">
    <value>Naplanovano</value>
  </data>
  <data name="PostStatusDraft" xml:space="preserve">
    <value>Koncept</value>
  </data>
  <data name="NotPublished" xml:space="preserve">
    <value>Nepublikovano</value>
  </data>
  <data name="SortNewestFirst" xml:space="preserve">
    <value>Nejnovejsi prvni</value>
  </data>
  <data name="SortOldestFirst" xml:space="preserve">
    <value>Nejstarsi prvni</value>
  </data>
  <data name="SortTitleAscending" xml:space="preserve">
    <value>Nazev (A-Z)</value>
  </data>
  <data name="SortTitleDescending" xml:space="preserve">
    <value>Nazev (Z-A)</value>
  </data>
  <data name="FilterByStatus" xml:space="preserve">
    <value>Filtrovat podle stavu</value>
  </data>
  <data name="SortOrder" xml:space="preserve">
    <value>Razeni</value>
  </data>
  <data name="MoreActions" xml:space="preserve">
    <value>Dalsi akce</value>
  </data>
  <data name="Edit" xml:space="preserve">
    <value>Upravit</value>
  </data>
  <data name="CopyRoute" xml:space="preserve">
    <value>Kopirovat cestu</value>
  </data>
  <data name="DeletePost" xml:space="preserve">
    <value>Smazat prispevek</value>
  </data>
  <data name="ConfirmDeletePost" xml:space="preserve">
    <value>"{0}" bude trvale smazan. Tuto akci nelze vratit zpet.</value>
  </data>
  <data name="DeletePostFailed" xml:space="preserve">
    <value>Prispevek se nepodarilo smazat</value>
  </data>
  <data name="PostsCountFormat" xml:space="preserve">
    <value>Zobrazeno {0} z {1} prispevku</value>
  </data>
```
Then restore the Czech diacritics in the values just written (`příspěvky`, `Všechny stavy`, `Publikováno`, `Naplánováno`, `Nepublikováno`, `Nejnovější první`, `Nejstarší první`, `Název`, `Řazení`, `Další akce`, `Kopírovat cestu`, `Smazat příspěvek`, `bude trvale smazán. Tuto akci nelze vrátit zpět.`, `Příspěvek se nepodařilo smazat`, `Zobrazeno {0} z {1} příspěvků`) — the file is UTF-8 with BOM, so the accented characters round-trip.

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Theme/Icons.xaml src/app/MZikmund.App/Strings/en/Resources.resw src/app/MZikmund.App/Strings/cs/Resources.resw
git commit -m "feat: Add the add glyph and the Posts manager strings"
```

---

### Task 71: Add the shared dense-row style library

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/ListRows.xaml`
- Modify: `src/app/MZikmund.App/App.xaml` (merged dictionary list)

**Interfaces:**
- Consumes: `MzShimmerBrush`, `MzAccentSubtleBrush`, `MzStrokeFocusBrush`, `MzStrokeDividerBrush`, `MzTextPrimaryBrush`, `MzTextSecondaryBrush`, `MzControlCornerRadius`, `MzCaptionTextStyle`, `MzMetaTextStyle`, `MzMonoFontFamily`.
- Produces: `MzListRowButtonStyle`, `MzListRowIconButtonStyle`, `MzListRowDividerStyle`, `MzListRowTitleTextStyle`, `MzListRowMetaTextStyle`.

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/Styles/ListRows.xaml`**

Full templates rather than `BasedOn` a stock key: the row must stretch its content and carry its own hover/press/focus visuals, and no state changes a non-GPU property, so no `EnableDependentAnimation` is involved.
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<!--  Full-width dense list row. Shared by the Posts, Categories and Tags managers.  -->
	<Style x:Key="MzListRowButtonStyle" TargetType="Button">
		<Setter Property="Background" Value="Transparent" />
		<Setter Property="BorderThickness" Value="0" />
		<Setter Property="Padding" Value="12,10,12,10" />
		<Setter Property="MinHeight" Value="48" />
		<Setter Property="HorizontalAlignment" Value="Stretch" />
		<Setter Property="HorizontalContentAlignment" Value="Stretch" />
		<Setter Property="VerticalContentAlignment" Value="Center" />
		<Setter Property="CornerRadius" Value="{StaticResource MzControlCornerRadius}" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="UseSystemFocusVisuals" Value="False" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid>
						<Border
							x:Name="RowFill"
							Background="{TemplateBinding Background}"
							CornerRadius="{TemplateBinding CornerRadius}" />
						<Border
							x:Name="RowFocus"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="{TemplateBinding CornerRadius}"
							Opacity="0" />
						<ContentPresenter
							x:Name="RowContent"
							Padding="{TemplateBinding Padding}"
							HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
							VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"
							Content="{TemplateBinding Content}"
							ContentTemplate="{TemplateBinding ContentTemplate}" />
						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<VisualState.Setters>
										<Setter Target="RowFill.Background" Value="{ThemeResource MzShimmerBrush}" />
									</VisualState.Setters>
								</VisualState>
								<VisualState x:Name="Pressed">
									<VisualState.Setters>
										<Setter Target="RowFill.Background" Value="{ThemeResource MzAccentSubtleBrush}" />
										<Setter Target="RowContent.Opacity" Value="0.7" />
									</VisualState.Setters>
								</VisualState>
								<VisualState x:Name="Disabled">
									<VisualState.Setters>
										<Setter Target="RowContent.Opacity" Value="0.4" />
									</VisualState.Setters>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Unfocused" />
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Focused">
									<VisualState.Setters>
										<Setter Target="RowFocus.Opacity" Value="1" />
									</VisualState.Setters>
								</VisualState>
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<!--  32px square affordance that lives inside a row (overflow, inline actions).  -->
	<Style x:Key="MzListRowIconButtonStyle" TargetType="Button">
		<Setter Property="Background" Value="Transparent" />
		<Setter Property="BorderThickness" Value="0" />
		<Setter Property="Padding" Value="0" />
		<Setter Property="Width" Value="32" />
		<Setter Property="Height" Value="32" />
		<Setter Property="MinWidth" Value="32" />
		<Setter Property="CornerRadius" Value="{StaticResource MzControlCornerRadius}" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
		<Setter Property="HorizontalContentAlignment" Value="Center" />
		<Setter Property="VerticalContentAlignment" Value="Center" />
		<Setter Property="UseSystemFocusVisuals" Value="False" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid>
						<Border
							x:Name="IconFill"
							Background="{TemplateBinding Background}"
							CornerRadius="{TemplateBinding CornerRadius}" />
						<Border
							x:Name="IconFocus"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							BorderThickness="2"
							CornerRadius="{TemplateBinding CornerRadius}"
							Opacity="0" />
						<ContentPresenter
							x:Name="IconContent"
							HorizontalContentAlignment="Center"
							VerticalContentAlignment="Center"
							Content="{TemplateBinding Content}" />
						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<VisualState.Setters>
										<Setter Target="IconFill.Background" Value="{ThemeResource MzShimmerBrush}" />
									</VisualState.Setters>
								</VisualState>
								<VisualState x:Name="Pressed">
									<VisualState.Setters>
										<Setter Target="IconFill.Background" Value="{ThemeResource MzAccentSubtleBrush}" />
										<Setter Target="IconContent.Opacity" Value="0.7" />
									</VisualState.Setters>
								</VisualState>
								<VisualState x:Name="Disabled">
									<VisualState.Setters>
										<Setter Target="IconContent.Opacity" Value="0.4" />
									</VisualState.Setters>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Unfocused" />
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Focused">
									<VisualState.Setters>
										<Setter Target="IconFocus.Opacity" Value="1" />
									</VisualState.Setters>
								</VisualState>
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<Style x:Key="MzListRowDividerStyle" TargetType="Border">
		<Setter Property="Height" Value="1" />
		<Setter Property="Margin" Value="12,0,12,0" />
		<Setter Property="VerticalAlignment" Value="Bottom" />
		<Setter Property="Background" Value="{ThemeResource MzStrokeDividerBrush}" />
		<Setter Property="IsHitTestVisible" Value="False" />
	</Style>

	<Style x:Key="MzListRowTitleTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzCaptionTextStyle}">
		<Setter Property="FontSize" Value="15" />
		<Setter Property="LineHeight" Value="20" />
		<Setter Property="TextWrapping" Value="NoWrap" />
		<Setter Property="TextTrimming" Value="CharacterEllipsis" />
	</Style>

	<!--  Routes and other machine values read as data, not prose.  -->
	<Style x:Key="MzListRowMetaTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzMetaTextStyle}">
		<Setter Property="FontFamily" Value="{StaticResource MzMonoFontFamily}" />
		<Setter Property="FontWeight" Value="Normal" />
		<Setter Property="FontSize" Value="12" />
		<Setter Property="LineHeight" Value="16" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 2: Merge it in `src/app/MZikmund.App/App.xaml`**

Replace the line:
```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Badges.xaml" />
```
with:
```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Badges.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Styles/ListRows.xaml" />
```

- [ ] **Step 3: Build the desktop head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Theme/Styles/ListRows.xaml src/app/MZikmund.App/App.xaml
git commit -m "feat: Add the shared dense list-row style library"
```

---

### Task 72: Introduce `PostRowViewModel`

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/PostRowViewModel.cs`

**Interfaces:**
- Consumes: `MZikmund.DataContracts.Blog.PostListItem`, `Localizer.Instance`.
- Produces: `MZikmund.ViewModels.Items.PostRowStatus`, `MZikmund.ViewModels.Items.PostRowViewModel`.

- [ ] **Step 1: Create the file**

`PostListItem` carries no status field, so status is derived from `PublishedDate`. Formatting happens once, at construction, so the row template binds plain strings and the list stays cheap to realize.
```csharp
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels.Items;

public enum PostRowStatus
{
	Draft,
	Scheduled,
	Published,
}

/// <summary>
/// One row of the posts manager: derived status, preformatted meta and the category chip labels.
/// </summary>
public sealed class PostRowViewModel
{
	public PostRowViewModel(PostListItem post)
	{
		Post = post ?? throw new ArgumentNullException(nameof(post));

		var published = post.PublishedDate;
		Status = published is null
			? PostRowStatus.Draft
			: published.Value > DateTimeOffset.Now ? PostRowStatus.Scheduled : PostRowStatus.Published;

		StatusText = Localizer.Instance.GetString(Status switch
		{
			PostRowStatus.Draft => "PostStatusDraft",
			PostRowStatus.Scheduled => "PostStatusScheduled",
			_ => "PostStatusPublished",
		});

		DateText = published is null
			? Localizer.Instance.GetString("NotPublished")
			: published.Value.ToLocalTime().ToString("d MMM yyyy");

		SortDate = published ?? post.LastModifiedDate ?? DateTimeOffset.MinValue;
		RouteText = "/" + post.RouteName;
		Categories = post.Categories.Select(category => category.DisplayName).ToArray();
	}

	public PostListItem Post { get; }

	public Guid Id => Post.Id;

	public string Title => Post.Title;

	public string RouteText { get; }

	public string DateText { get; }

	public DateTimeOffset SortDate { get; }

	public PostRowStatus Status { get; }

	public string StatusText { get; }

	public bool IsPublished => Status == PostRowStatus.Published;

	public bool IsScheduled => Status == PostRowStatus.Scheduled;

	public bool IsDraft => Status == PostRowStatus.Draft;

	public IReadOnlyList<string> Categories { get; }

	public bool HasCategories => Categories.Count > 0;
}
```

- [ ] **Step 2: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Items/PostRowViewModel.cs
git commit -m "feat: Add PostRowViewModel for the posts manager rows"
```

---

### Task 73: Rebuild `PostsManagerViewModel` around search, status filter, sort and delete

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/PostsManagerViewModel.cs:1-155` (full replacement)

**Interfaces:**
- Consumes: `PostRowViewModel`, `IMZikmundApi.GetAllPostsAsync`, `IMZikmundApi.DeletePostAsync`, `IDialogService.ShowConfirmationDialogAsync`, `ObservableCollectionExtensions.AddRange`, strings from Task 1 and Phase 1's `Refresh`.
- Produces: `Posts`, `StatusFilters`, `SortOptions`, `SearchText`, `SelectedStatusFilterIndex`, `SelectedSortIndex`, `CountSummary`, `IsEmpty`, `RefreshCommand`, `DeletePostCommand`.

- [ ] **Step 1: Replace the file in full**

The paged API is the only source, so search/filter/sort run over everything loaded so far; `_allPosts` keeps the unfiltered truth. Both index setters reject negative values — a `ComboBox` pushes `-1` the moment its `ItemsSource` is still empty, and without the guard that would wipe the filter on load.
```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;

namespace MZikmund.ViewModels.Admin;

public partial class PostsManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly INavigationService _navigationService;
	private readonly IMZikmundApi _api;
	private readonly List<PostRowViewModel> _allPosts = new();
	private string _searchText = "";
	private int _selectedStatusFilterIndex;
	private int _selectedSortIndex;
	private int _currentPage = 1;
	private int _totalPages = 1;
	private int _totalCount;
	private bool _isLoadingMore;

	public PostsManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator,
		INavigationService navigationService)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
		_api = api ?? throw new ArgumentNullException(nameof(api));
		LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => CanLoadMore);

		StatusFilters = new[]
		{
			Localizer.Instance.GetString("PostStatusAll"),
			Localizer.Instance.GetString("PostStatusPublished"),
			Localizer.Instance.GetString("PostStatusScheduled"),
			Localizer.Instance.GetString("PostStatusDraft"),
		};

		SortOptions = new[]
		{
			Localizer.Instance.GetString("SortNewestFirst"),
			Localizer.Instance.GetString("SortOldestFirst"),
			Localizer.Instance.GetString("SortTitleAscending"),
			Localizer.Instance.GetString("SortTitleDescending"),
		};
	}

	public override string Title => Localizer.Instance.GetString("Posts");

	public ObservableCollection<PostRowViewModel> Posts { get; } = new();

	public IReadOnlyList<string> StatusFilters { get; }

	public IReadOnlyList<string> SortOptions { get; }

	public string SearchText
	{
		get => _searchText;
		set
		{
			if (SetProperty(ref _searchText, value ?? ""))
			{
				ApplyFilter();
			}
		}
	}

	public int SelectedStatusFilterIndex
	{
		get => _selectedStatusFilterIndex;
		set
		{
			if (value < 0)
			{
				return;
			}

			if (SetProperty(ref _selectedStatusFilterIndex, value))
			{
				ApplyFilter();
			}
		}
	}

	public int SelectedSortIndex
	{
		get => _selectedSortIndex;
		set
		{
			if (value < 0)
			{
				return;
			}

			if (SetProperty(ref _selectedSortIndex, value))
			{
				ApplyFilter();
			}
		}
	}

	public int CurrentPage
	{
		get => _currentPage;
		private set => SetProperty(ref _currentPage, value);
	}

	public int TotalPages
	{
		get => _totalPages;
		private set => SetProperty(ref _totalPages, value);
	}

	public int TotalCount
	{
		get => _totalCount;
		private set
		{
			if (SetProperty(ref _totalCount, value))
			{
				OnPropertyChanged(nameof(CountSummary));
			}
		}
	}

	public bool IsLoadingMore
	{
		get => _isLoadingMore;
		private set
		{
			SetProperty(ref _isLoadingMore, value);
			(LoadMoreCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
			OnPropertyChanged(nameof(CanLoadMore));
		}
	}

	public bool CanLoadMore => CurrentPage < TotalPages && !IsLoadingMore;

	public bool IsEmpty => Posts.Count == 0;

	public string CountSummary =>
		string.Format(Localizer.Instance.GetString("PostsCountFormat"), Posts.Count, TotalCount);

	public ICommand LoadMoreCommand { get; }

	public override async void ViewLoaded()
	{
		await RefreshListAsync();
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		await RefreshListAsync();
	}

	private async Task RefreshListAsync()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			_allPosts.Clear();
			Posts.Clear();
			CurrentPage = 1;
			await LoadPostsAsync(1);
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				$"{Localizer.Instance.GetString("ErrorLoadingData")} {ex.Message}");
		}
	}

	private async Task LoadPostsAsync(int pageNumber)
	{
		var posts = await _api.GetAllPostsAsync(pageNumber);
		await posts.EnsureSuccessfulAsync();

		if (posts.Content != null)
		{
			foreach (var post in posts.Content.Data)
			{
				_allPosts.Add(new PostRowViewModel(post));
			}

			TotalCount = posts.Content.TotalCount;
			TotalPages = (int)Math.Ceiling((double)posts.Content.TotalCount / posts.Content.PageSize);
			CurrentPage = posts.Content.PageNumber;
			ApplyFilter();
		}
	}

	private async Task LoadMoreAsync()
	{
		if (!CanLoadMore)
		{
			return;
		}

		IsLoadingMore = true;
		try
		{
			await LoadPostsAsync(CurrentPage + 1);
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				$"{Localizer.Instance.GetString("ErrorLoadingData")} {ex.Message}");
		}
		finally
		{
			IsLoadingMore = false;
		}
	}

	private void ApplyFilter()
	{
		IEnumerable<PostRowViewModel> filtered = _allPosts;

		var search = SearchText.Trim();
		if (search.Length > 0)
		{
			filtered = filtered.Where(post =>
				post.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
				post.RouteText.Contains(search, StringComparison.OrdinalIgnoreCase));
		}

		filtered = SelectedStatusFilterIndex switch
		{
			1 => filtered.Where(post => post.Status == PostRowStatus.Published),
			2 => filtered.Where(post => post.Status == PostRowStatus.Scheduled),
			3 => filtered.Where(post => post.Status == PostRowStatus.Draft),
			_ => filtered,
		};

		filtered = SelectedSortIndex switch
		{
			1 => filtered.OrderBy(post => post.SortDate),
			2 => filtered.OrderBy(post => post.Title, StringComparer.CurrentCultureIgnoreCase),
			3 => filtered.OrderByDescending(post => post.Title, StringComparer.CurrentCultureIgnoreCase),
			_ => filtered.OrderByDescending(post => post.SortDate),
		};

		Posts.Clear();
		Posts.AddRange(filtered);
		OnPropertyChanged(nameof(IsEmpty));
		OnPropertyChanged(nameof(CountSummary));
	}

	[RelayCommand]
	private void AddPost()
	{
		_navigationService.Navigate<PostEditorViewModel>(Guid.Empty);
	}

	[RelayCommand]
	private void UpdatePost(PostListItem? post)
	{
		if (post is null)
		{
			return;
		}

		_navigationService.Navigate<PostEditorViewModel>(post.Id);
	}

	[RelayCommand]
	private async Task DeletePostAsync(PostRowViewModel? row)
	{
		if (row is null)
		{
			return;
		}

		var confirmation = await _dialogService.ShowConfirmationDialogAsync(
			Localizer.Instance.GetString("DeletePost"),
			string.Format(Localizer.Instance.GetString("ConfirmDeletePost"), row.Title));

		if (confirmation != ContentDialogResult.Primary)
		{
			return;
		}

		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			var response = await _api.DeletePostAsync(row.Id);
			await response.EnsureSuccessfulAsync();

			_allPosts.Remove(row);
			TotalCount = Math.Max(0, TotalCount - 1);
			ApplyFilter();
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("DeletePostFailed"),
				ex.Message);
		}
	}
}
```

- [ ] **Step 2: Build the desktop head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```
The build breaks at `PostsManagerView.xaml`'s `{x:Bind ViewModel.Posts}` only if the next task has not run yet — that is expected; run the next task and rebuild there.

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Admin/PostsManagerViewModel.cs
git commit -m "feat: Add search, status filter, sort and delete to the posts manager"
```

---

### Task 74: Rebuild `PostsManagerView` as dense rows with a CommandBar

**Files:**
- Modify: `src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml:1-79` (full replacement)
- Modify: `src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml.cs:1-19` (full replacement)

**Interfaces:**
- Consumes: `MzListRowButtonStyle`, `MzListRowIconButtonStyle`, `MzListRowDividerStyle`, `MzListRowTitleTextStyle`, `MzListRowMetaTextStyle`, `MzBadgeAccentStyle`, `MzBadgeSubtleStyle`, `MzBadgeNeutralStyle`, `MzLayerSurfaceStyle`, `MzEmptyStatePanelStyle`, `MzEmptyStateIconStyle`, `MzEmptyStateTitleTextStyle`, `MzEmptyStateBodyTextStyle`, `MzIconFontFamily`, `MzGlyphAdd`, `MzGlyphArrowClockwise`, `MzGlyphSearch`, `MzGlyphMore`, `MzPagePaddingCompact`, `MzTextSecondaryBrush`, `BoolToVisibilityConverter`, `MZikmund.Controls.MzPageHeader`, `PostRowViewModel`.
- Produces: nothing consumed elsewhere.

- [ ] **Step 1: Replace `src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml` in full**

`SystemColorHighlightBrush` is gone: it is the high-contrast *selection* fill, and painting every row with it produced saturated blue blocks with untouched foreground. Rows now sit on one `MzLayerSurfaceStyle` surface, hover comes from the row style, and the only colour is the status badge. `ItemsRepeater` + `StackLayout` because `GridView`/`ItemsControl` do not virtualize on Skia. The overflow button is nested inside the row button — `ButtonBase` captures the pointer, so the row's `Click` never fires behind it.
```xml
<local:PostsManagerViewBase
    x:Class="MZikmund.Views.Admin.PostsManagerView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:MZikmund.Views.Admin"
    xmlns:controls="using:MZikmund.Controls"
    xmlns:items="using:MZikmund.ViewModels.Items"
    xmlns:xaml="using:MZikmund.Extensions.Xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

	<local:PostsManagerViewBase.Resources>
		<DataTemplate x:Key="MzPostRowTemplate" x:DataType="items:PostRowViewModel">
			<Grid>
				<Button
					Click="OnRowClick"
					Style="{StaticResource MzListRowButtonStyle}"
					Tag="{x:Bind}"
					ToolTipService.ToolTip="{x:Bind Title}">
					<Grid ColumnSpacing="12">
						<Grid.ColumnDefinitions>
							<ColumnDefinition Width="Auto" MinWidth="96" />
							<ColumnDefinition Width="*" />
							<ColumnDefinition Width="Auto" />
							<ColumnDefinition Width="Auto" />
							<ColumnDefinition Width="Auto" />
						</Grid.ColumnDefinitions>

						<Grid VerticalAlignment="Center">
							<ContentControl
								Content="{x:Bind StatusText}"
								Style="{StaticResource MzBadgeSubtleStyle}"
								Visibility="{x:Bind IsPublished, Converter={StaticResource BoolToVisibilityConverter}}" />
							<ContentControl
								Content="{x:Bind StatusText}"
								Style="{StaticResource MzBadgeAccentStyle}"
								Visibility="{x:Bind IsScheduled, Converter={StaticResource BoolToVisibilityConverter}}" />
							<ContentControl
								Content="{x:Bind StatusText}"
								Style="{StaticResource MzBadgeNeutralStyle}"
								Visibility="{x:Bind IsDraft, Converter={StaticResource BoolToVisibilityConverter}}" />
						</Grid>

						<StackPanel Grid.Column="1" VerticalAlignment="Center">
							<TextBlock Style="{StaticResource MzListRowTitleTextStyle}" Text="{x:Bind Title}" />
							<TextBlock Style="{StaticResource MzListRowMetaTextStyle}" Text="{x:Bind RouteText}" />
						</StackPanel>

						<ItemsControl
							x:Name="CategoriesPanel"
							Grid.Column="2"
							MaxWidth="260"
							VerticalAlignment="Center"
							IsTabStop="False"
							ItemsSource="{x:Bind Categories}"
							Visibility="Collapsed">
							<ItemsControl.ItemsPanel>
								<ItemsPanelTemplate>
									<StackPanel Orientation="Horizontal" Spacing="4" />
								</ItemsPanelTemplate>
							</ItemsControl.ItemsPanel>
							<ItemsControl.ItemTemplate>
								<DataTemplate x:DataType="x:String">
									<ContentControl Content="{x:Bind}" Style="{StaticResource MzBadgeNeutralStyle}" />
								</DataTemplate>
							</ItemsControl.ItemTemplate>
						</ItemsControl>

						<TextBlock
							x:Name="RowDate"
							Grid.Column="3"
							MinWidth="96"
							VerticalAlignment="Center"
							Style="{StaticResource MzListRowMetaTextStyle}"
							Text="{x:Bind DateText}"
							TextAlignment="Right"
							Visibility="Collapsed" />

						<Button
							x:Name="RowOverflow"
							Grid.Column="4"
							VerticalAlignment="Center"
							AutomationProperties.Name="{xaml:Localize Key=MoreActions}"
							Click="OnRowOverflowClick"
							Style="{StaticResource MzListRowIconButtonStyle}"
							Tag="{x:Bind}"
							ToolTipService.ToolTip="{xaml:Localize Key=MoreActions}">
							<FontIcon
								FontFamily="{StaticResource MzIconFontFamily}"
								FontSize="16"
								Foreground="{ThemeResource MzTextSecondaryBrush}"
								Glyph="{StaticResource MzGlyphMore}" />
						</Button>

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="MzPostRowStates">
								<VisualState x:Name="RowNarrow" />
								<VisualState x:Name="RowWide">
									<VisualState.StateTriggers>
										<AdaptiveTrigger MinWindowWidth="768" />
									</VisualState.StateTriggers>
									<VisualState.Setters>
										<Setter Target="CategoriesPanel.Visibility" Value="Visible" />
										<Setter Target="RowDate.Visibility" Value="Visible" />
									</VisualState.Setters>
								</VisualState>
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</Button>
				<Border Style="{StaticResource MzListRowDividerStyle}" />
			</Grid>
		</DataTemplate>
	</local:PostsManagerViewBase.Resources>

	<Grid Padding="{StaticResource MzPagePaddingCompact}" RowSpacing="16">
		<Grid.RowDefinitions>
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
			<RowDefinition Height="*" />
			<RowDefinition Height="Auto" />
		</Grid.RowDefinitions>

		<controls:MzPageHeader Title="{xaml:Localize Key=Posts}" Lead="{x:Bind ViewModel.CountSummary, Mode=OneWay}" />

		<CommandBar
			Grid.Row="1"
			HorizontalAlignment="Left"
			Background="Transparent"
			BorderThickness="0"
			DefaultLabelPosition="Right"
			IsOpen="False"
			OverflowButtonVisibility="Collapsed">
			<AppBarButton Command="{x:Bind ViewModel.AddPostCommand}" Label="{xaml:Localize Key=AddPost}">
				<AppBarButton.Icon>
					<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphAdd}" />
				</AppBarButton.Icon>
			</AppBarButton>
			<AppBarButton Command="{x:Bind ViewModel.RefreshCommand}" Label="{xaml:Localize Key=Refresh}">
				<AppBarButton.Icon>
					<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphArrowClockwise}" />
				</AppBarButton.Icon>
			</AppBarButton>
		</CommandBar>

		<Grid Grid.Row="2" ColumnSpacing="12">
			<Grid.ColumnDefinitions>
				<ColumnDefinition Width="*" MaxWidth="360" />
				<ColumnDefinition Width="Auto" />
				<ColumnDefinition Width="Auto" />
			</Grid.ColumnDefinitions>
			<AutoSuggestBox
				x:Name="SearchBox"
				AutomationProperties.Name="{xaml:Localize Key=SearchPosts}"
				PlaceholderText="{xaml:Localize Key=SearchPosts}"
				TextChanged="OnSearchTextChanged">
				<AutoSuggestBox.QueryIcon>
					<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphSearch}" />
				</AutoSuggestBox.QueryIcon>
			</AutoSuggestBox>
			<ComboBox
				Grid.Column="1"
				MinWidth="160"
				AutomationProperties.Name="{xaml:Localize Key=FilterByStatus}"
				ItemsSource="{x:Bind ViewModel.StatusFilters}"
				SelectedIndex="{x:Bind ViewModel.SelectedStatusFilterIndex, Mode=TwoWay}" />
			<ComboBox
				Grid.Column="2"
				MinWidth="180"
				AutomationProperties.Name="{xaml:Localize Key=SortOrder}"
				ItemsSource="{x:Bind ViewModel.SortOptions}"
				SelectedIndex="{x:Bind ViewModel.SelectedSortIndex, Mode=TwoWay}" />
		</Grid>

		<Border Grid.Row="3" Style="{StaticResource MzLayerSurfaceStyle}">
			<Grid>
				<ScrollViewer Padding="0,4,0,4" VerticalScrollBarVisibility="Auto">
					<ItemsRepeater ItemTemplate="{StaticResource MzPostRowTemplate}" ItemsSource="{x:Bind ViewModel.Posts}">
						<ItemsRepeater.Layout>
							<StackLayout />
						</ItemsRepeater.Layout>
					</ItemsRepeater>
				</ScrollViewer>
				<StackPanel
					Style="{StaticResource MzEmptyStatePanelStyle}"
					Visibility="{x:Bind ViewModel.IsEmpty, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}">
					<FontIcon Style="{StaticResource MzEmptyStateIconStyle}" Glyph="{StaticResource MzGlyphSearch}" />
					<TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{xaml:Localize Key=NoPostsFound}" />
					<TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{xaml:Localize Key=NoPostsFoundDescription}" />
				</StackPanel>
			</Grid>
		</Border>

		<StackPanel Grid.Row="4" Spacing="8">
			<Button
				HorizontalAlignment="Center"
				Command="{x:Bind ViewModel.LoadMoreCommand, Mode=OneWay}"
				Content="{xaml:Localize Key=LoadMore}"
				Style="{StaticResource MzStandardButtonStyle}"
				Visibility="{x:Bind ViewModel.CanLoadMore, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}" />
			<ProgressRing
				HorizontalAlignment="Center"
				IsActive="{x:Bind ViewModel.IsLoadingMore, Mode=OneWay}"
				Visibility="{x:Bind ViewModel.IsLoadingMore, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}" />
		</StackPanel>
	</Grid>
</local:PostsManagerViewBase>
```

- [ ] **Step 2: Replace `src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml.cs` in full**

The overflow menu is built in code so its labels come from `Localizer` and no `MenuFlyout` has to inherit a `DataContext` through a flyout boundary; the row identity travels on `Tag`, which `x:Bind` fills from the item itself.
```csharp
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Admin;
using MZikmund.ViewModels.Items;
using Windows.ApplicationModel.DataTransfer;

namespace MZikmund.Views.Admin;

public sealed partial class PostsManagerView : PostsManagerViewBase
{
	public PostsManagerView() => InitializeComponent();

	private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		if (ViewModel is not null)
		{
			ViewModel.SearchText = sender.Text;
		}
	}

	private void OnRowClick(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: PostRowViewModel row })
		{
			ViewModel?.UpdatePostCommand.Execute(row.Post);
		}
	}

	private void OnRowOverflowClick(object sender, RoutedEventArgs e)
	{
		if (sender is not FrameworkElement { Tag: PostRowViewModel row } button)
		{
			return;
		}

		var menu = new MenuFlyout();

		var edit = new MenuFlyoutItem { Text = Localizer.Instance.GetString("Edit") };
		edit.Click += (_, _) => ViewModel?.UpdatePostCommand.Execute(row.Post);
		menu.Items.Add(edit);

		var copyRoute = new MenuFlyoutItem { Text = Localizer.Instance.GetString("CopyRoute") };
		copyRoute.Click += (_, _) =>
		{
			var package = new DataPackage();
			package.SetText(row.RouteText);
			Clipboard.SetContent(package);
		};
		menu.Items.Add(copyRoute);

		menu.Items.Add(new MenuFlyoutSeparator());

		var delete = new MenuFlyoutItem { Text = Localizer.Instance.GetString("Delete") };
		delete.Click += (_, _) => ViewModel?.DeletePostCommand.Execute(row);
		menu.Items.Add(delete);

		menu.ShowAt(button);
	}
}

public partial class PostsManagerViewBase : PageBase<PostsManagerViewModel>
{
}
```

- [ ] **Step 3: Build the desktop head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 4: Build the Windows head / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

- [ ] **Step 5: Run and verify with the Uno App MCP**

Launch the desktop head, navigate to Admin → Posts and confirm: no blue block rows; each row shows badge, title, mono route, date and chips at ≥768 wide; typing in the search box filters live; both combo boxes re-filter and re-sort; the overflow menu opens with Edit / Copy route / Delete and Delete asks for confirmation; the empty state appears when a filter matches nothing. Repeat in dark theme.

- [ ] **Step 6: Format**
```bash
dotnet format MZikmund.slnx
```

- [ ] **Step 7: Commit**
```bash
git add src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml.cs
git commit -m "feat: Rebuild the posts manager as dense rows with a CommandBar"
```## CategoriesManagerView and TagsManagerView (Phase 4)

### Interfaces

PRODUCED (later work may consume):
- `MZikmund.Controls.MzWrapLayout : VirtualizingLayout` — `double HorizontalSpacing` / `HorizontalSpacingProperty`, `double VerticalSpacing` / `VerticalSpacingProperty`. XAML: `xmlns:controls="using:MZikmund.Controls"`.
- `src/app/MZikmund.App/Theme/Styles/Taxonomy.xaml` resource keys: `MzTaxonomyRowStyle` (Button), `MzTaxonomyIconButtonStyle` (Button), `MzTaxonomyRowNameTextStyle` (TextBlock), `MzTaxonomyCountTextStyle` (TextBlock), `MzTagPillStyle` (Button), `MzTagCountChipStyle` (Border), `MzTagCountTextStyle` (TextBlock), `MzSearchFieldStyle` (TextBox).
- Added to `Theme/Icons.xaml` (new keys only, never redefining Phase 1's): `MzGlyphAdd` (E00D), `MzGlyphEdit` (E837), `MzGlyphDelete` (E66D), `MzGlyphDismiss` (E6D3).
- `MZikmund.ViewModels.Items.CategoryListItemViewModel` — `Guid Id`, `string DisplayName`, `string RouteName`, `int PostCount`, `string PostCountText`, `ICommand EditCommand`, `ICommand DeleteCommand`.
- `MZikmund.ViewModels.Items.TagListItemViewModel` — same members plus `double DimOpacity` (0.55 when `PostCount == 0`).
- `CategoriesManagerViewModel`: `ObservableCollection<CategoryListItemViewModel> Categories`, `string Lead`, `string SearchText` (observable), `bool IsEmpty`, `bool HasError`, `AddCategoryCommand`, `EditCategoryCommand`, `DeleteCategoryCommand`, `RefreshCommand`. `UpdateCategoryCommand` is REMOVED.
- `TagsManagerViewModel`: `ObservableCollection<TagListItemViewModel> Tags`, `string Lead`, `string SearchText`, `bool IsEmpty`, `bool HasError`, `AddTagCommand`, `EditTagCommand`, `DeleteTagCommand`, `RefreshCommand`. `UpdateTagCommand` is REMOVED.
- `IMZikmundApi.GetCategoriesWithPostCountAsync() : Task<ApiResponse<CategoryWithPostCount[]>>` (`GET /v1/categories/with-post-count`), `IMZikmundApi.GetTagsWithPostCountAsync() : Task<ApiResponse<TagWithPostCount[]>>` (`GET /v1/tags/with-post-count`).
- Named XAML elements: `CategoriesRepeater`, `CategorySearchBox`, `TagsRepeater`, `TagsScroll`, `TagSearchBox`.
- New resw keys (en + cs): `CategoriesLead`, `TagsLead`, `SearchCategories`, `SearchTags`, `NoCategoriesFound`, `NoCategoriesFoundDescription`, `NoTagsFound`, `NoTagsFoundDescription`, `DeleteCategory`, `ConfirmDeleteCategory`, `DeleteTag`, `ConfirmDeleteTag`. cs-only additions: `AddCategory`, `AddTag`, `EditCategory`, `EditTag`.

CONSUMED from Phase 1: `MzBgBaseBrush` `MzShimmerBrush` `MzAccentFillBrush` `MzAccentTextBrush` `MzAccentSubtleBrush` `MzAccentSubtleStrongBrush` `MzCardSecondaryBrush` `MzTextPrimaryBrush` `MzTextSecondaryBrush` `MzTextTertiaryBrush` `MzStrokeControlBrush` `MzStrokeControlStrongBrush` `MzStrokeDividerBrush` `MzStrokeFocusBrush` `MzStrokeFocusInnerBrush`; `MzPagePaddingCompact` `MzSpace2` `MzSpace4`; `MzControlCornerRadius` `MzPillCornerRadius`; `MzEaseStandard` `MzDurationFast` `MzDurationInstant`; `MzFontFamilyMedium` `MzMonoFontFamily` `MzCaptionTextStyle` `MzBodyTextStyle`; `MzMetaTextStyle`; `MzLayerSurfaceStyle`; `MzAccentButtonStyle` `MzSubtleButtonStyle`; `MzEmptyStatePanelStyle` `MzEmptyStateIconStyle` `MzEmptyStateTitleTextStyle` `MzEmptyStateBodyTextStyle`; `MzIconFontFamily` `MzGlyphFolder` `MzGlyphTag` `MzGlyphSearch`.
CONSUMED from Phase 2: `MZikmund.Controls.MzPageHeader` (`Title`, `Lead`, `Actions`).
CONSUMED from existing app: `BoolToVisibilityConverter` (`Resources/Converters.xaml`), `MZikmund.Extensions.Xaml.Localize`, `IDialogService.ShowConfirmationDialogAsync`, `ILoadingIndicator.BeginLoading`.

### Task 75: Expose post counts on the categories and tags endpoints

`CategoryWithPostCount` / `TagWithPostCount` exist in `MZikmund.DataContracts` and the web already has `GetCategoriesWithPostCountQuery` / `GetTagsWithPostCountQuery`, but **no HTTP route and no Refit method reach them** — the app can only call `GET /v1/categories`, which returns bare `Category[]`. Without this task the count column has nothing to bind to.

**Files:**
- Modify: `src/web/MZikmund.Web/Controllers/CategoriesController.cs:26-33`
- Modify: `src/web/MZikmund.Web/Controllers/TagsController.cs:26-33`
- Modify: `src/shared/MZikmund.Api/IMZikmundApi.Categories.cs:8-9`
- Modify: `src/shared/MZikmund.Api/IMZikmundApi.Tags.cs:8-9`
- Modify: `src/shared/MZikmund.Api/Serialization/MZikmundApiSerializerContext.cs` (the `CategoryWithPostCount types` and `TagWithPostCount types` blocks)

**Interfaces:**
- Consumes: `MZikmund.Web.Core.Features.Categories.GetCategoriesWithPostCountQuery`, `MZikmund.Web.Core.Features.Tags.GetTagsWithPostCountQuery`.
- Produces: `IMZikmundApi.GetCategoriesWithPostCountAsync()`, `IMZikmundApi.GetTagsWithPostCountAsync()`.

- [ ] **Step 1: Add the route to `CategoriesController`**

Insert directly after the existing `GetAll()` method (before the `GetById` block):
```csharp
	/// <summary>
	/// Gets all categories together with the number of published posts in each.
	/// </summary>
	/// <returns>All blog categories with post counts.</returns>
	[HttpGet]
	[Route("with-post-count")]
	public async Task<IActionResult> GetAllWithPostCount() =>
		Ok(await _mediator.Send(new GetCategoriesWithPostCountQuery()));
```
The literal `with-post-count` segment wins over the `{id}` template, so `GetById` is unaffected.

- [ ] **Step 2: Add the route to `TagsController`**

Insert directly after the existing `GetAll()` method:
```csharp
	/// <summary>
	/// Gets all tags together with the number of published posts in each.
	/// </summary>
	/// <returns>All blog tags with post counts.</returns>
	[HttpGet]
	[Route("with-post-count")]
	public async Task<IActionResult> GetAllWithPostCount() =>
		Ok(await _mediator.Send(new GetTagsWithPostCountQuery()));
```

- [ ] **Step 3: Add the Refit methods**

In `src/shared/MZikmund.Api/IMZikmundApi.Categories.cs`, after `GetCategoriesAsync()`:
```csharp
	[Get("/v1/categories/with-post-count")]
	Task<ApiResponse<CategoryWithPostCount[]>> GetCategoriesWithPostCountAsync();
```
In `src/shared/MZikmund.Api/IMZikmundApi.Tags.cs`, after `GetTagsAsync()`:
```csharp
	[Get("/v1/tags/with-post-count")]
	Task<ApiResponse<TagWithPostCount[]>> GetTagsWithPostCountAsync();
```

- [ ] **Step 4: Register the two `ApiResponse<T[]>` shapes in the serializer context**

`MZikmundApiSerializerContext` already lists `CategoryWithPostCount[]` and `TagWithPostCount[]` but not the Refit envelopes. Add to the `// CategoryWithPostCount types` block:
```csharp
[JsonSerializable(typeof(ApiResponse<CategoryWithPostCount[]>))]
```
and to the `// TagWithPostCount types` block:
```csharp
[JsonSerializable(typeof(ApiResponse<TagWithPostCount[]>))]
```

- [ ] **Step 5: Build the API and the web app / Expected: Build succeeded**
```bash
dotnet build src/shared/MZikmund.Api/MZikmund.Api.csproj
dotnet build src/web/MZikmund.Web/MZikmund.Web.csproj
```

- [ ] **Step 6: Commit**
```bash
git add src/web/MZikmund.Web/Controllers/CategoriesController.cs src/web/MZikmund.Web/Controllers/TagsController.cs src/shared/MZikmund.Api
git commit -m "feat: Expose category and tag post counts over the API"
```

---

### Task 76: Localization keys for the taxonomy managers

**Files:**
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw`
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw`

**Interfaces:**
- Produces: the 12 new keys plus 4 Czech backfills listed in Interfaces.

- [ ] **Step 1: Append to `en/Resources.resw`, immediately before the closing `</root>`**
```xml
  <data name="CategoriesLead" xml:space="preserve"><value>Group posts into a handful of broad topics.</value></data>
  <data name="TagsLead" xml:space="preserve"><value>Fine-grained labels. Tags with no posts are dimmed but still editable.</value></data>
  <data name="SearchCategories" xml:space="preserve"><value>Search categories</value></data>
  <data name="SearchTags" xml:space="preserve"><value>Search tags</value></data>
  <data name="NoCategoriesFound" xml:space="preserve"><value>No categories found</value></data>
  <data name="NoCategoriesFoundDescription" xml:space="preserve"><value>Nothing matches your search. Clear it, or add a new category.</value></data>
  <data name="NoTagsFound" xml:space="preserve"><value>No tags found</value></data>
  <data name="NoTagsFoundDescription" xml:space="preserve"><value>Nothing matches your search. Clear it, or add a new tag.</value></data>
  <data name="DeleteCategory" xml:space="preserve"><value>Delete category</value></data>
  <data name="ConfirmDeleteCategory" xml:space="preserve"><value>Delete the category "{0}"? Posts keep their other categories.</value></data>
  <data name="DeleteTag" xml:space="preserve"><value>Delete tag</value></data>
  <data name="ConfirmDeleteTag" xml:space="preserve"><value>Delete the tag "{0}"? It will be removed from every post that uses it.</value></data>
```

- [ ] **Step 2: Append to `cs/Resources.resw`, immediately before the closing `</root>`**
```xml
  <data name="AddCategory" xml:space="preserve"><value>Přidat kategorii</value></data>
  <data name="AddTag" xml:space="preserve"><value>Přidat štítek</value></data>
  <data name="EditCategory" xml:space="preserve"><value>Upravit kategorii</value></data>
  <data name="EditTag" xml:space="preserve"><value>Upravit štítek</value></data>
  <data name="CategoriesLead" xml:space="preserve"><value>Seskupte příspěvky do několika širokých témat.</value></data>
  <data name="TagsLead" xml:space="preserve"><value>Podrobné štítky. Štítky bez příspěvků jsou ztlumené, ale stále upravitelné.</value></data>
  <data name="SearchCategories" xml:space="preserve"><value>Hledat kategorie</value></data>
  <data name="SearchTags" xml:space="preserve"><value>Hledat štítky</value></data>
  <data name="NoCategoriesFound" xml:space="preserve"><value>Žádné kategorie nenalezeny</value></data>
  <data name="NoCategoriesFoundDescription" xml:space="preserve"><value>Hledání nic nenašlo. Zrušte jej, nebo přidejte novou kategorii.</value></data>
  <data name="NoTagsFound" xml:space="preserve"><value>Žádné štítky nenalezeny</value></data>
  <data name="NoTagsFoundDescription" xml:space="preserve"><value>Hledání nic nenašlo. Zrušte jej, nebo přidejte nový štítek.</value></data>
  <data name="DeleteCategory" xml:space="preserve"><value>Smazat kategorii</value></data>
  <data name="ConfirmDeleteCategory" xml:space="preserve"><value>Smazat kategorii „{0}“? Příspěvkům zůstanou ostatní kategorie.</value></data>
  <data name="DeleteTag" xml:space="preserve"><value>Smazat štítek</value></data>
  <data name="ConfirmDeleteTag" xml:space="preserve"><value>Smazat štítek „{0}“? Bude odebrán ze všech příspěvků, které jej používají.</value></data>
```

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Strings
git commit -m "feat: Localize the categories and tags manager strings"
```

---

### Task 77: `MzWrapLayout` — a virtualizing wrap layout for the tag cloud

**No built-in layout virtualizes a wrap of variable-width items.** The complete set of options was checked:

| Candidate | Wraps? | Virtualizes? |
|---|---|---|
| `StackLayout` | no | yes |
| `UniformGridLayout` | yes | yes — but forces one uniform cell size, so every pill would be as wide as the widest tag |
| `ItemsWrapGrid` | yes | only inside `ListViewBase`, and `GridView` does not virtualize on Skia (uno#21964) |
| `VariableSizedWrapGrid` | yes | never, on any platform |
| `WrapPanel` in an `ItemsControl` | yes | never |
| `CommunityToolkit.WinUI.Controls.WrapLayout` | yes | yes — but its `net9.0`/`android35.0`/`ios18.0` assets are compiled against **Uno.WinUI 5.5.87 (native renderer)** while this app ships the Skia Uno.UI. The spec already flags that combination as a runtime hazard, and a `VirtualizingLayout` is the deepest possible renderer integration. Rejected. |

The concrete alternative is a `VirtualizingLayout` in the app assembly, compiled against the exact Uno.WinUI the app ships. Pills have variable width but uniform height, which makes a correct implementation short.

**Files:**
- Create: `src/app/MZikmund.App/Controls/MzWrapLayout.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MZikmund.Controls.MzWrapLayout` with `HorizontalSpacing` / `VerticalSpacing`.

- [ ] **Step 1: Create `src/app/MZikmund.App/Controls/MzWrapLayout.cs`**
```csharp
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace MZikmund.Controls;

/// <summary>
/// Per-context cache of item sizes and the bounds derived from them.
/// </summary>
internal sealed class MzWrapLayoutState
{
	public List<Size?> Sizes { get; } = new();

	public List<Rect> Bounds { get; } = new();

	public void EnsureCapacity(int itemCount)
	{
		while (Sizes.Count > itemCount)
		{
			Sizes.RemoveAt(Sizes.Count - 1);
			Bounds.RemoveAt(Bounds.Count - 1);
		}

		while (Sizes.Count < itemCount)
		{
			Sizes.Add(null);
			Bounds.Add(default);
		}
	}

	public void ClearSizes()
	{
		for (var i = 0; i < Sizes.Count; i++)
		{
			Sizes[i] = null;
		}
	}
}

/// <summary>
/// Virtualizing wrap layout for <see cref="ItemsRepeater"/>. Item widths are cached, so a resize
/// only reflows; it does not re-realize. Item sizes do not depend on the available width because
/// pills never wrap internally.
/// </summary>
public sealed class MzWrapLayout : VirtualizingLayout
{
	public static readonly DependencyProperty HorizontalSpacingProperty =
		DependencyProperty.Register(
			nameof(HorizontalSpacing),
			typeof(double),
			typeof(MzWrapLayout),
			new PropertyMetadata(0d, OnSpacingChanged));

	public static readonly DependencyProperty VerticalSpacingProperty =
		DependencyProperty.Register(
			nameof(VerticalSpacing),
			typeof(double),
			typeof(MzWrapLayout),
			new PropertyMetadata(0d, OnSpacingChanged));

	public double HorizontalSpacing
	{
		get => (double)GetValue(HorizontalSpacingProperty);
		set => SetValue(HorizontalSpacingProperty, value);
	}

	public double VerticalSpacing
	{
		get => (double)GetValue(VerticalSpacingProperty);
		set => SetValue(VerticalSpacingProperty, value);
	}

	protected override void InitializeForContextCore(VirtualizingLayoutContext context)
	{
		context.LayoutState = new MzWrapLayoutState();
		base.InitializeForContextCore(context);
	}

	protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
	{
		context.LayoutState = null;
		base.UninitializeForContextCore(context);
	}

	protected override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		// Indices shifted, so every cached measurement now belongs to the wrong item.
		GetState(context).ClearSizes();
		InvalidateMeasure();
	}

	protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
	{
		var state = GetState(context);
		var itemCount = context.ItemCount;
		state.EnsureCapacity(itemCount);

		var lineLimit = availableSize.Width;
		if (double.IsNaN(lineLimit) || double.IsInfinity(lineLimit) || lineLimit <= 0)
		{
			lineLimit = double.PositiveInfinity;
		}

		var constraint = new Size(lineLimit, double.PositiveInfinity);
		var realization = context.RealizationRect;

		double x = 0, y = 0, lineHeight = 0, widest = 0;

		for (var index = 0; index < itemCount; index++)
		{
			var size = state.Sizes[index];
			if (size is null)
			{
				// One-off probe. ItemsRepeater recycles everything ArrangeOverride does not
				// keep, so this never leaves hundreds of live containers behind.
				var probe = context.GetOrCreateElementAt(index);
				probe.Measure(constraint);
				size = probe.DesiredSize;
				state.Sizes[index] = size;
			}

			var desired = size.Value;
			if (x > 0 && x + desired.Width > lineLimit)
			{
				x = 0;
				y += lineHeight + VerticalSpacing;
				lineHeight = 0;
			}

			state.Bounds[index] = new Rect(x, y, desired.Width, desired.Height);

			if (y < realization.Bottom && y + desired.Height > realization.Top)
			{
				context.GetOrCreateElementAt(index).Measure(constraint);
			}

			x += desired.Width + HorizontalSpacing;
			lineHeight = Math.Max(lineHeight, desired.Height);
			widest = Math.Max(widest, x);
		}

		var width = double.IsInfinity(lineLimit)
			? Math.Max(0, widest - HorizontalSpacing)
			: lineLimit;

		return new Size(width, itemCount == 0 ? 0 : y + lineHeight);
	}

	protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
	{
		var state = GetState(context);
		var realization = context.RealizationRect;
		var count = Math.Min(context.ItemCount, state.Bounds.Count);

		for (var index = 0; index < count; index++)
		{
			var bounds = state.Bounds[index];
			if (bounds.Y < realization.Bottom && bounds.Y + bounds.Height > realization.Top)
			{
				context.GetOrCreateElementAt(index).Arrange(bounds);
			}
		}

		return finalSize;
	}

	private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((MzWrapLayout)d).InvalidateMeasure();

	private static MzWrapLayoutState GetState(VirtualizingLayoutContext context)
	{
		if (context.LayoutState is not MzWrapLayoutState state)
		{
			state = new MzWrapLayoutState();
			context.LayoutState = state;
		}

		return state;
	}
}
```

- [ ] **Step 2: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Controls/MzWrapLayout.cs
git commit -m "feat: Add MzWrapLayout, a virtualizing wrap layout for variable-width items"
```

---

### Task 78: `Theme/Styles/Taxonomy.xaml` — the taxonomy row and tag pill vocabulary

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/Taxonomy.xaml`
- Modify: `src/app/MZikmund.App/Theme/Icons.xaml` (append four glyph keys before `</ResourceDictionary>`)
- Modify: `src/app/MZikmund.App/App.xaml` (merged dictionaries list)

**Interfaces:**
- Consumes: Phase 1 brushes, radii, motion, type styles, `MzSubtleButtonStyle`, `MzIconFontFamily`.
- Produces: `MzTaxonomyRowStyle`, `MzTaxonomyIconButtonStyle`, `MzTaxonomyRowNameTextStyle`, `MzTaxonomyCountTextStyle`, `MzTagPillStyle`, `MzTagCountChipStyle`, `MzTagCountTextStyle`, `MzSearchFieldStyle`, `MzGlyphAdd`, `MzGlyphEdit`, `MzGlyphDelete`, `MzGlyphDismiss`.

- [ ] **Step 1: Append the four glyphs to `Theme/Icons.xaml`**

All four codepoints are already in the subsetted font (`Assets/Fonts/icons.txt`). Insert before the closing `</ResourceDictionary>`:
```xml
	<x:String x:Key="MzGlyphAdd">&#xE00D;</x:String>
	<x:String x:Key="MzGlyphEdit">&#xE837;</x:String>
	<x:String x:Key="MzGlyphDelete">&#xE66D;</x:String>
	<x:String x:Key="MzGlyphDismiss">&#xE6D3;</x:String>
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Theme/Styles/Taxonomy.xaml`**

The indicator is the *vertical* 3px leading-edge bar (the list-shaped idiom), grown from the centre by animating `ScaleTransform.ScaleY` — `UIElement.Scale` is not implemented in Uno. Opacity and `RenderTransform` are GPU properties, so no `EnableDependentAnimation` is needed; the count column uses JetBrains Mono, which is genuinely tabular.
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<Style x:Key="MzTaxonomyRowNameTextStyle" TargetType="TextBlock" BasedOn="{StaticResource MzBodyTextStyle}">
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilyMedium}" />
		<Setter Property="FontSize" Value="15" />
		<Setter Property="FontWeight" Value="Medium" />
		<Setter Property="LineHeight" Value="20" />
		<Setter Property="TextWrapping" Value="NoWrap" />
		<Setter Property="TextTrimming" Value="CharacterEllipsis" />
	</Style>

	<!--  Monospace gives the count column real tabular figures on every head.  -->
	<Style x:Key="MzTaxonomyCountTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzMonoFontFamily}" />
		<Setter Property="FontSize" Value="13" />
		<Setter Property="TextAlignment" Value="Right" />
		<Setter Property="MinWidth" Value="32" />
		<Setter Property="VerticalAlignment" Value="Center" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextTertiaryBrush}" />
	</Style>

	<Style x:Key="MzTaxonomyRowStyle" TargetType="Button">
		<Setter Property="Background" Value="Transparent" />
		<Setter Property="BorderThickness" Value="0" />
		<Setter Property="Padding" Value="0" />
		<Setter Property="MinHeight" Value="44" />
		<Setter Property="HorizontalAlignment" Value="Stretch" />
		<Setter Property="HorizontalContentAlignment" Value="Stretch" />
		<Setter Property="VerticalContentAlignment" Value="Center" />
		<Setter Property="UseSystemFocusVisuals" Value="False" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid x:Name="RowRoot" MinHeight="44" Background="Transparent">
						<Border x:Name="HoverFill" Opacity="0" Background="{ThemeResource MzShimmerBrush}" />
						<Border
							x:Name="Indicator"
							Width="3"
							Margin="0,7"
							HorizontalAlignment="Left"
							VerticalAlignment="Stretch"
							Opacity="0"
							Background="{ThemeResource MzAccentFillBrush}"
							CornerRadius="{StaticResource MzPillCornerRadius}"
							RenderTransformOrigin="0.5,0.5">
							<Border.RenderTransform>
								<ScaleTransform x:Name="IndicatorScale" ScaleY="0" />
							</Border.RenderTransform>
						</Border>
						<Border
							x:Name="FocusOuter"
							Opacity="0"
							BorderThickness="2"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							CornerRadius="{StaticResource MzControlCornerRadius}" />
						<Border
							x:Name="FocusInner"
							Margin="2"
							Opacity="0"
							BorderThickness="1"
							BorderBrush="{ThemeResource MzStrokeFocusInnerBrush}"
							CornerRadius="{StaticResource MzControlCornerRadius}" />
						<ContentPresenter
							x:Name="Presenter"
							Padding="{TemplateBinding Padding}"
							HorizontalContentAlignment="Stretch"
							VerticalContentAlignment="Center"
							Content="{TemplateBinding Content}"
							ContentTemplate="{TemplateBinding ContentTemplate}" />

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="HoverFill" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame KeyTime="{StaticResource MzDurationFast}" KeySpline="{StaticResource MzEaseStandard}" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="Indicator" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame KeyTime="{StaticResource MzDurationFast}" KeySpline="{StaticResource MzEaseStandard}" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="IndicatorScale" Storyboard.TargetProperty="ScaleY">
											<SplineDoubleKeyFrame KeyTime="{StaticResource MzDurationFast}" KeySpline="{StaticResource MzEaseStandard}" Value="1" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<DoubleAnimation Storyboard.TargetName="HoverFill" Storyboard.TargetProperty="Opacity" Duration="{StaticResource MzDurationInstant}" To="1" />
										<DoubleAnimation Storyboard.TargetName="Indicator" Storyboard.TargetProperty="Opacity" Duration="{StaticResource MzDurationInstant}" To="1" />
										<DoubleAnimation Storyboard.TargetName="IndicatorScale" Storyboard.TargetProperty="ScaleY" Duration="{StaticResource MzDurationInstant}" To="1" />
										<DoubleAnimation Storyboard.TargetName="Presenter" Storyboard.TargetProperty="Opacity" Duration="{StaticResource MzDurationInstant}" To="0.7" />
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<DoubleAnimation Storyboard.TargetName="Presenter" Storyboard.TargetProperty="Opacity" Duration="{StaticResource MzDurationInstant}" To="0.4" />
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Unfocused" />
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Focused">
									<Storyboard>
										<DoubleAnimation Storyboard.TargetName="FocusOuter" Storyboard.TargetProperty="Opacity" Duration="{StaticResource MzDurationInstant}" To="1" />
										<DoubleAnimation Storyboard.TargetName="FocusInner" Storyboard.TargetProperty="Opacity" Duration="{StaticResource MzDurationInstant}" To="1" />
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<Style x:Key="MzTaxonomyIconButtonStyle" TargetType="Button" BasedOn="{StaticResource MzSubtleButtonStyle}">
		<Setter Property="Width" Value="40" />
		<Setter Property="Height" Value="40" />
		<Setter Property="Padding" Value="0" />
		<Setter Property="VerticalAlignment" Value="Center" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
	</Style>

	<Style x:Key="MzTagCountTextStyle" TargetType="TextBlock">
		<Setter Property="FontFamily" Value="{StaticResource MzMonoFontFamily}" />
		<Setter Property="FontSize" Value="11" />
		<Setter Property="VerticalAlignment" Value="Center" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextTertiaryBrush}" />
	</Style>

	<Style x:Key="MzTagCountChipStyle" TargetType="Border">
		<Setter Property="Padding" Value="6,1,6,1" />
		<Setter Property="VerticalAlignment" Value="Center" />
		<Setter Property="Background" Value="{ThemeResource MzShimmerBrush}" />
		<Setter Property="CornerRadius" Value="{StaticResource MzPillCornerRadius}" />
	</Style>

	<!--  Fluent presses down by 1px and never scales; Translation is not implemented in Uno.  -->
	<Style x:Key="MzTagPillStyle" TargetType="Button">
		<Setter Property="MinHeight" Value="32" />
		<Setter Property="MaxWidth" Value="320" />
		<Setter Property="Padding" Value="12,5,12,5" />
		<Setter Property="BorderThickness" Value="1" />
		<Setter Property="UseSystemFocusVisuals" Value="False" />
		<Setter Property="HorizontalContentAlignment" Value="Center" />
		<Setter Property="VerticalContentAlignment" Value="Center" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamilyMedium}" />
		<Setter Property="FontSize" Value="13" />
		<Setter Property="FontWeight" Value="Medium" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<Grid x:Name="PillRoot" RenderTransformOrigin="0.5,0.5">
						<Grid.RenderTransform>
							<CompositeTransform x:Name="PillTransform" />
						</Grid.RenderTransform>
						<Border
							x:Name="PillFill"
							Background="{ThemeResource MzCardSecondaryBrush}"
							BorderThickness="{TemplateBinding BorderThickness}"
							BorderBrush="{ThemeResource MzStrokeControlBrush}"
							CornerRadius="{StaticResource MzPillCornerRadius}" />
						<Border
							x:Name="PillFocus"
							Opacity="0"
							BorderThickness="2"
							BorderBrush="{ThemeResource MzStrokeFocusBrush}"
							CornerRadius="{StaticResource MzPillCornerRadius}" />
						<ContentPresenter
							x:Name="PillContent"
							Padding="{TemplateBinding Padding}"
							HorizontalContentAlignment="Center"
							VerticalContentAlignment="Center"
							Content="{TemplateBinding Content}"
							ContentTemplate="{TemplateBinding ContentTemplate}"
							Foreground="{TemplateBinding Foreground}" />

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />
								<VisualState x:Name="PointerOver">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentSubtleBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="BorderBrush">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzStrokeControlStrongBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Pressed">
									<Storyboard>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillFill" Storyboard.TargetProperty="Background">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentSubtleStrongBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<ObjectAnimationUsingKeyFrames Storyboard.TargetName="PillContent" Storyboard.TargetProperty="Foreground">
											<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzAccentTextBrush}" />
										</ObjectAnimationUsingKeyFrames>
										<DoubleAnimation Storyboard.TargetName="PillTransform" Storyboard.TargetProperty="TranslateY" Duration="{StaticResource MzDurationInstant}" To="1" />
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Disabled">
									<Storyboard>
										<DoubleAnimation Storyboard.TargetName="PillContent" Storyboard.TargetProperty="Opacity" Duration="{StaticResource MzDurationInstant}" To="0.4" />
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
							<VisualStateGroup x:Name="FocusStates">
								<VisualState x:Name="Unfocused" />
								<VisualState x:Name="PointerFocused" />
								<VisualState x:Name="Focused">
									<Storyboard>
										<DoubleAnimation Storyboard.TargetName="PillFocus" Storyboard.TargetProperty="Opacity" Duration="{StaticResource MzDurationInstant}" To="1" />
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<Style x:Key="MzSearchFieldStyle" TargetType="TextBox">
		<Setter Property="MaxWidth" Value="420" />
		<Setter Property="HorizontalAlignment" Value="Left" />
		<Setter Property="Padding" Value="36,7,12,7" />
		<Setter Property="FontFamily" Value="{StaticResource MzFontFamily}" />
		<Setter Property="FontSize" Value="14" />
		<Setter Property="CornerRadius" Value="{StaticResource MzControlCornerRadius}" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 3: Merge the dictionary in `App.xaml`**

Add immediately after the `Theme/Styles/EmptyState.xaml` entry inside `<ResourceDictionary.MergedDictionaries>`:
```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Taxonomy.xaml" />
```

- [ ] **Step 4: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App/Theme/Styles/Taxonomy.xaml src/app/MZikmund.App/Theme/Icons.xaml src/app/MZikmund.App/App.xaml
git commit -m "feat: Add the taxonomy row and tag pill styles"
```

---

### Task 79: Rebuild `CategoriesManagerViewModel` on post counts, search and per-row commands

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/CategoryListItemViewModel.cs`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/CategoriesManagerViewModel.cs:1-108` (full replacement)

**Interfaces:**
- Consumes: `IMZikmundApi.GetCategoriesWithPostCountAsync()`, `IDialogService.ShowConfirmationDialogAsync`, the new resw keys.
- Produces: `CategoryListItemViewModel`; `Categories`, `Lead`, `SearchText`, `IsEmpty`, `HasError`, `AddCategoryCommand`, `EditCategoryCommand`, `DeleteCategoryCommand`, `RefreshCommand`.

- [ ] **Step 1: Create `src/app/MZikmund.App.Core/ViewModels/Items/CategoryListItemViewModel.cs`**

The row commands live on the item so the `DataTemplate` can `x:Bind` them without reaching back up to the page.
```csharp
using System.Globalization;
using System.Windows.Input;
using MZikmund.DataContracts.Blog;

namespace MZikmund.ViewModels.Items;

public sealed class CategoryListItemViewModel
{
	public CategoryListItemViewModel(CategoryWithPostCount category, ICommand editCommand, ICommand deleteCommand)
	{
		Id = category.Id;
		DisplayName = category.DisplayName;
		RouteName = category.RouteName;
		PostCount = category.PostCount;
		EditCommand = editCommand;
		DeleteCommand = deleteCommand;
	}

	public Guid Id { get; }

	public string DisplayName { get; }

	public string RouteName { get; }

	public int PostCount { get; }

	public string PostCountText => PostCount.ToString(CultureInfo.CurrentCulture);

	public ICommand EditCommand { get; }

	public ICommand DeleteCommand { get; }
}
```

- [ ] **Step 2: Replace `src/app/MZikmund.App.Core/ViewModels/Admin/CategoriesManagerViewModel.cs` in full**
```csharp
using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Items;

namespace MZikmund.ViewModels.Admin;

public partial class CategoriesManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;
	private readonly List<CategoryListItemViewModel> _allCategories = new();

	[ObservableProperty]
	private string _searchText = "";

	[ObservableProperty]
	private bool _isEmpty;

	[ObservableProperty]
	private bool _hasError;

	public CategoriesManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
	}

	public override string Title => Localizer.Instance.GetString("Categories");

	public string Lead => Localizer.Instance.GetString("CategoriesLead");

	public ObservableCollection<CategoryListItemViewModel> Categories { get; } = new();

	public override async void ViewLoaded() => await RefreshListAsync();

	partial void OnSearchTextChanged(string value) => ApplyFilter();

	[RelayCommand]
	private Task RefreshAsync() => RefreshListAsync();

	private async Task RefreshListAsync()
	{
		using (_loadingIndicator.BeginLoading())
		{
			try
			{
				var response = await _api.GetCategoriesWithPostCountAsync();
				var categories = response.Content ?? Array.Empty<CategoryWithPostCount>();

				_allCategories.Clear();
				_allCategories.AddRange(categories
					.OrderBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase)
					.Select(c => new CategoryListItemViewModel(c, EditCategoryCommand, DeleteCategoryCommand)));

				HasError = false;
			}
			catch (Exception)
			{
				_allCategories.Clear();
				HasError = true;

				await _dialogService.ShowStatusMessageAsync(
					StatusMessageDialogType.Error,
					Localizer.Instance.GetString("CouldNotLoadData"),
					Localizer.Instance.GetString("ErrorLoadingData"));
			}
		}

		ApplyFilter();
	}

	private void ApplyFilter()
	{
		var query = SearchText?.Trim() ?? "";

		Categories.Clear();
		foreach (var item in _allCategories)
		{
			if (query.Length == 0
				|| item.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
				|| item.RouteName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
			{
				Categories.Add(item);
			}
		}

		IsEmpty = Categories.Count == 0;
	}

	[RelayCommand]
	private async Task AddCategoryAsync()
	{
		var dialogViewModel = new AddOrUpdateCategoryDialogViewModel();
		if (await _dialogService.ShowAsync(dialogViewModel) != ContentDialogResult.Primary)
		{
			return;
		}

		using (_loadingIndicator.BeginLoading())
		{
			await _api.AddCategoryAsync(new Category
			{
				DisplayName = dialogViewModel.Category.DisplayName,
				RouteName = dialogViewModel.Category.RouteName,
			});
		}

		await RefreshListAsync();
	}

	[RelayCommand]
	private async Task EditCategoryAsync(CategoryListItemViewModel? item)
	{
		if (item is null)
		{
			return;
		}

		var dialogViewModel = new AddOrUpdateCategoryDialogViewModel(new CategoryViewModel
		{
			Id = item.Id,
			DisplayName = item.DisplayName,
			RouteName = item.RouteName,
		});

		if (await _dialogService.ShowAsync(dialogViewModel) != ContentDialogResult.Primary)
		{
			return;
		}

		using (_loadingIndicator.BeginLoading())
		{
			await _api.UpdateCategoryAsync(item.Id, new Category
			{
				DisplayName = dialogViewModel.Category.DisplayName,
				RouteName = dialogViewModel.Category.RouteName,
			});
		}

		await RefreshListAsync();
	}

	[RelayCommand]
	private async Task DeleteCategoryAsync(CategoryListItemViewModel? item)
	{
		if (item is null)
		{
			return;
		}

		var confirmation = await _dialogService.ShowConfirmationDialogAsync(
			Localizer.Instance.GetString("DeleteCategory"),
			string.Format(
				System.Globalization.CultureInfo.CurrentCulture,
				Localizer.Instance.GetString("ConfirmDeleteCategory"),
				item.DisplayName));

		if (confirmation != ContentDialogResult.Primary)
		{
			return;
		}

		using (_loadingIndicator.BeginLoading())
		{
			await _api.DeleteCategoryAsync(item.Id);
		}

		await RefreshListAsync();
	}
}
```

- [ ] **Step 3: Run `dotnet build src/app/MZikmund.App.Core/MZikmund.App.Core.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Items/CategoryListItemViewModel.cs src/app/MZikmund.App.Core/ViewModels/Admin/CategoriesManagerViewModel.cs
git commit -m "feat: Rebuild the categories manager view model with counts, search and row commands"
```

---

### Task 80: Rebuild `CategoriesManagerView` as a taxonomy list

Fixes the two live defects: `d:Background` at line 10 becomes a real `Background`, and the high-contrast `SystemColorHighlightBrush` card background at line 50 disappears with the `GridView`.

**Files:**
- Modify: `src/app/MZikmund.App/Views/Admin/CategoriesManagerView.xaml:1-65` (full replacement)
- Modify: `src/app/MZikmund.App/Views/Admin/CategoriesManagerView.xaml.cs:1-19` (full replacement)

**Interfaces:**
- Consumes: `MzPageHeader`, `MzTaxonomyRowStyle`, `MzTaxonomyIconButtonStyle`, `MzTaxonomyRowNameTextStyle`, `MzTaxonomyCountTextStyle`, `MzSearchFieldStyle`, `MzLayerSurfaceStyle`, `MzEmptyState*`, `CategoryListItemViewModel`.
- Produces: named elements `CategoriesRepeater`, `CategorySearchBox`.

- [ ] **Step 1: Replace `CategoriesManagerView.xaml` in full**
```xml
<local:CategoriesManagerViewBase
	x:Class="MZikmund.Views.Admin.CategoriesManagerView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:controls="using:MZikmund.Controls"
	xmlns:items="using:MZikmund.ViewModels.Items"
	xmlns:local="using:MZikmund.Views.Admin"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Background="{ThemeResource MzBgBaseBrush}">

	<Grid Padding="{StaticResource MzPagePaddingCompact}" RowSpacing="16">
		<Grid.RowDefinitions>
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
			<RowDefinition Height="*" />
		</Grid.RowDefinitions>

		<controls:MzPageHeader Title="{xaml:Localize Key=Categories}" Lead="{xaml:Localize Key=CategoriesLead}">
			<controls:MzPageHeader.Actions>
				<Button
					AutomationProperties.AutomationId="AddCategoryButton"
					Command="{x:Bind ViewModel.AddCategoryCommand}"
					Style="{StaticResource MzAccentButtonStyle}">
					<StackPanel Orientation="Horizontal" Spacing="8">
						<FontIcon
							FontFamily="{StaticResource MzIconFontFamily}"
							FontSize="16"
							Glyph="{StaticResource MzGlyphAdd}" />
						<TextBlock Text="{xaml:Localize Key=AddCategory}" />
					</StackPanel>
				</Button>
			</controls:MzPageHeader.Actions>
		</controls:MzPageHeader>

		<Grid Grid.Row="1" HorizontalAlignment="Left">
			<TextBox
				x:Name="CategorySearchBox"
				AutomationProperties.AutomationId="CategorySearchBox"
				AutomationProperties.Name="{xaml:Localize Key=SearchCategories}"
				PlaceholderText="{xaml:Localize Key=SearchCategories}"
				Style="{StaticResource MzSearchFieldStyle}"
				Text="{x:Bind ViewModel.SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
			<FontIcon
				Margin="12,0,0,0"
				HorizontalAlignment="Left"
				VerticalAlignment="Center"
				FontFamily="{StaticResource MzIconFontFamily}"
				FontSize="16"
				Foreground="{ThemeResource MzTextTertiaryBrush}"
				Glyph="{StaticResource MzGlyphSearch}"
				IsHitTestVisible="False" />
		</Grid>

		<!--  One r8 Layer surface holding every row, per the web's .taxonomy-list.  -->
		<Border Grid.Row="2" Style="{StaticResource MzLayerSurfaceStyle}">
			<Grid>
				<ScrollViewer HorizontalScrollMode="Disabled" VerticalScrollBarVisibility="Auto">
					<ItemsRepeater x:Name="CategoriesRepeater" ItemsSource="{x:Bind ViewModel.Categories, Mode=OneWay}">
						<ItemsRepeater.Layout>
							<StackLayout />
						</ItemsRepeater.Layout>
						<ItemsRepeater.ItemTemplate>
							<DataTemplate x:DataType="items:CategoryListItemViewModel">
								<Grid>
									<Grid.ColumnDefinitions>
										<ColumnDefinition Width="*" />
										<ColumnDefinition Width="Auto" />
										<ColumnDefinition Width="Auto" />
									</Grid.ColumnDefinitions>

									<Button
										AutomationProperties.Name="{x:Bind DisplayName}"
										Command="{x:Bind EditCommand}"
										CommandParameter="{x:Bind}"
										Style="{StaticResource MzTaxonomyRowStyle}"
										ToolTipService.ToolTip="{x:Bind RouteName}">
										<Grid Padding="12,0,8,0" ColumnSpacing="12">
											<Grid.ColumnDefinitions>
												<ColumnDefinition Width="Auto" />
												<ColumnDefinition Width="*" />
												<ColumnDefinition Width="Auto" />
											</Grid.ColumnDefinitions>
											<FontIcon
												VerticalAlignment="Center"
												FontFamily="{StaticResource MzIconFontFamily}"
												FontSize="16"
												Foreground="{ThemeResource MzTextTertiaryBrush}"
												Glyph="{StaticResource MzGlyphFolder}" />
											<TextBlock
												Grid.Column="1"
												VerticalAlignment="Center"
												Style="{StaticResource MzTaxonomyRowNameTextStyle}"
												Text="{x:Bind DisplayName}" />
											<TextBlock
												Grid.Column="2"
												Style="{StaticResource MzTaxonomyCountTextStyle}"
												Text="{x:Bind PostCountText}" />
										</Grid>
									</Button>

									<Button
										Grid.Column="1"
										AutomationProperties.Name="{xaml:Localize Key=EditCategory}"
										Command="{x:Bind EditCommand}"
										CommandParameter="{x:Bind}"
										Style="{StaticResource MzTaxonomyIconButtonStyle}"
										ToolTipService.ToolTip="{xaml:Localize Key=EditCategory}">
										<FontIcon
											FontFamily="{StaticResource MzIconFontFamily}"
											FontSize="16"
											Glyph="{StaticResource MzGlyphEdit}" />
									</Button>
									<Button
										Grid.Column="2"
										AutomationProperties.Name="{xaml:Localize Key=DeleteCategory}"
										Command="{x:Bind DeleteCommand}"
										CommandParameter="{x:Bind}"
										Style="{StaticResource MzTaxonomyIconButtonStyle}"
										ToolTipService.ToolTip="{xaml:Localize Key=DeleteCategory}">
										<FontIcon
											FontFamily="{StaticResource MzIconFontFamily}"
											FontSize="16"
											Glyph="{StaticResource MzGlyphDelete}" />
									</Button>

									<Border
										Grid.ColumnSpan="3"
										Height="1"
										VerticalAlignment="Bottom"
										Background="{ThemeResource MzStrokeDividerBrush}" />
								</Grid>
							</DataTemplate>
						</ItemsRepeater.ItemTemplate>
					</ItemsRepeater>
				</ScrollViewer>

				<StackPanel
					Style="{StaticResource MzEmptyStatePanelStyle}"
					Visibility="{x:Bind ViewModel.IsEmpty, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
					<FontIcon Style="{StaticResource MzEmptyStateIconStyle}" Glyph="{StaticResource MzGlyphFolder}" />
					<TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{xaml:Localize Key=NoCategoriesFound}" />
					<TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{xaml:Localize Key=NoCategoriesFoundDescription}" />
					<Button
						Command="{x:Bind ViewModel.AddCategoryCommand}"
						Content="{xaml:Localize Key=AddCategory}"
						Style="{StaticResource MzStandardButtonStyle}" />
				</StackPanel>
			</Grid>
		</Border>
	</Grid>
</local:CategoriesManagerViewBase>
```

- [ ] **Step 2: Replace `CategoriesManagerView.xaml.cs` in full**

The `GridView_ItemClick` handler goes away — activation is a real `Button` `Command`, so keyboard and focus work.
```csharp
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class CategoriesManagerView : CategoriesManagerViewBase
{
	public CategoriesManagerView() => InitializeComponent();
}

public partial class CategoriesManagerViewBase : PageBase<CategoriesManagerViewModel>
{
}
```

- [ ] **Step 3: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Views/Admin/CategoriesManagerView.xaml src/app/MZikmund.App/Views/Admin/CategoriesManagerView.xaml.cs
git commit -m "feat: Rebuild the categories manager as a dense taxonomy list"
```

---

### Task 81: Rebuild `TagsManagerViewModel` on post counts, search and per-row commands

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/TagListItemViewModel.cs`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/TagsManagerViewModel.cs:1-110` (full replacement)

**Interfaces:**
- Consumes: `IMZikmundApi.GetTagsWithPostCountAsync()`.
- Produces: `TagListItemViewModel` (incl. `DimOpacity`); `Tags`, `Lead`, `SearchText`, `IsEmpty`, `HasError`, `AddTagCommand`, `EditTagCommand`, `DeleteTagCommand`, `RefreshCommand`.

- [ ] **Step 1: Create `src/app/MZikmund.App.Core/ViewModels/Items/TagListItemViewModel.cs`**
```csharp
using System.Globalization;
using System.Windows.Input;
using MZikmund.DataContracts.Blog;

namespace MZikmund.ViewModels.Items;

public sealed class TagListItemViewModel
{
	public TagListItemViewModel(TagWithPostCount tag, ICommand editCommand, ICommand deleteCommand)
	{
		Id = tag.Id;
		DisplayName = tag.DisplayName;
		RouteName = tag.RouteName;
		PostCount = tag.PostCount;
		EditCommand = editCommand;
		DeleteCommand = deleteCommand;
	}

	public Guid Id { get; }

	public string DisplayName { get; }

	public string RouteName { get; }

	public int PostCount { get; }

	public string PostCountText => PostCount.ToString(CultureInfo.CurrentCulture);

	/// <summary>
	/// Tags with no posts stay reachable but stop competing with tags that go somewhere.
	/// </summary>
	public double DimOpacity => PostCount == 0 ? 0.55 : 1.0;

	public ICommand EditCommand { get; }

	public ICommand DeleteCommand { get; }
}
```

- [ ] **Step 2: Replace `src/app/MZikmund.App.Core/ViewModels/Admin/TagsManagerViewModel.cs` in full**

The unused `Newtonsoft.Json` and `Windows.Storage.Pickers` usings go with it.
```csharp
using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Items;

namespace MZikmund.ViewModels.Admin;

public partial class TagsManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;
	private readonly List<TagListItemViewModel> _allTags = new();

	[ObservableProperty]
	private string _searchText = "";

	[ObservableProperty]
	private bool _isEmpty;

	[ObservableProperty]
	private bool _hasError;

	public TagsManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
	}

	public override string Title => Localizer.Instance.GetString("Tags");

	public string Lead => Localizer.Instance.GetString("TagsLead");

	public ObservableCollection<TagListItemViewModel> Tags { get; } = new();

	public override async void ViewLoaded() => await RefreshListAsync();

	partial void OnSearchTextChanged(string value) => ApplyFilter();

	[RelayCommand]
	private Task RefreshAsync() => RefreshListAsync();

	private async Task RefreshListAsync()
	{
		using (_loadingIndicator.BeginLoading())
		{
			try
			{
				var response = await _api.GetTagsWithPostCountAsync();
				var tags = response.Content ?? Array.Empty<TagWithPostCount>();

				_allTags.Clear();
				_allTags.AddRange(tags
					.OrderBy(t => t.DisplayName, StringComparer.CurrentCultureIgnoreCase)
					.Select(t => new TagListItemViewModel(t, EditTagCommand, DeleteTagCommand)));

				HasError = false;
			}
			catch (Exception)
			{
				_allTags.Clear();
				HasError = true;

				await _dialogService.ShowStatusMessageAsync(
					StatusMessageDialogType.Error,
					Localizer.Instance.GetString("CouldNotLoadData"),
					Localizer.Instance.GetString("ErrorLoadingData"));
			}
		}

		ApplyFilter();
	}

	private void ApplyFilter()
	{
		var query = SearchText?.Trim() ?? "";

		Tags.Clear();
		foreach (var item in _allTags)
		{
			if (query.Length == 0
				|| item.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
				|| item.RouteName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
			{
				Tags.Add(item);
			}
		}

		IsEmpty = Tags.Count == 0;
	}

	[RelayCommand]
	private async Task AddTagAsync()
	{
		var dialogViewModel = new AddOrUpdateTagDialogViewModel();
		if (await _dialogService.ShowAsync(dialogViewModel) != ContentDialogResult.Primary)
		{
			return;
		}

		using (_loadingIndicator.BeginLoading())
		{
			await _api.AddTagAsync(new Tag
			{
				DisplayName = dialogViewModel.Tag.DisplayName,
				RouteName = dialogViewModel.Tag.RouteName,
			});
		}

		await RefreshListAsync();
	}

	[RelayCommand]
	private async Task EditTagAsync(TagListItemViewModel? item)
	{
		if (item is null)
		{
			return;
		}

		var dialogViewModel = new AddOrUpdateTagDialogViewModel(new TagViewModel
		{
			Id = item.Id,
			DisplayName = item.DisplayName,
			RouteName = item.RouteName,
		});

		if (await _dialogService.ShowAsync(dialogViewModel) != ContentDialogResult.Primary)
		{
			return;
		}

		using (_loadingIndicator.BeginLoading())
		{
			await _api.UpdateTagAsync(item.Id, new Tag
			{
				DisplayName = dialogViewModel.Tag.DisplayName,
				RouteName = dialogViewModel.Tag.RouteName,
			});
		}

		await RefreshListAsync();
	}

	[RelayCommand]
	private async Task DeleteTagAsync(TagListItemViewModel? item)
	{
		if (item is null)
		{
			return;
		}

		var confirmation = await _dialogService.ShowConfirmationDialogAsync(
			Localizer.Instance.GetString("DeleteTag"),
			string.Format(
				System.Globalization.CultureInfo.CurrentCulture,
				Localizer.Instance.GetString("ConfirmDeleteTag"),
				item.DisplayName));

		if (confirmation != ContentDialogResult.Primary)
		{
			return;
		}

		using (_loadingIndicator.BeginLoading())
		{
			await _api.DeleteTagAsync(item.Id);
		}

		await RefreshListAsync();
	}
}
```

- [ ] **Step 3: Run `dotnet build src/app/MZikmund.App.Core/MZikmund.App.Core.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Items/TagListItemViewModel.cs src/app/MZikmund.App.Core/ViewModels/Admin/TagsManagerViewModel.cs
git commit -m "feat: Rebuild the tags manager view model with counts, search and row commands"
```

---

### Task 82: Rebuild `TagsManagerView` as a virtualized tag cloud

Fixes the same two defects as the categories view (`d:Background` line 10, `SystemColorHighlightBrush` line 50) and replaces the 120px tile `GridView` — which does not virtualize on Skia at all — with `ItemsRepeater` + `MzWrapLayout`.

**Files:**
- Modify: `src/app/MZikmund.App/Views/Admin/TagsManagerView.xaml:1-65` (full replacement)
- Modify: `src/app/MZikmund.App/Views/Admin/TagsManagerView.xaml.cs:1-19` (full replacement)

**Interfaces:**
- Consumes: `MzWrapLayout`, `MzTagPillStyle`, `MzTagCountChipStyle`, `MzTagCountTextStyle`, `MzPageHeader`, `MzLayerSurfaceStyle`, `MzSearchFieldStyle`, `TagListItemViewModel`.
- Produces: named elements `TagsRepeater`, `TagsScroll`, `TagSearchBox`.

- [ ] **Step 1: Replace `TagsManagerView.xaml` in full**

Edit is the pill's primary action; delete lives on the pill's `ContextFlyout` so a 32px pill stays a pill.
```xml
<local:TagsManagerViewBase
	x:Class="MZikmund.Views.Admin.TagsManagerView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:controls="using:MZikmund.Controls"
	xmlns:items="using:MZikmund.ViewModels.Items"
	xmlns:local="using:MZikmund.Views.Admin"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Background="{ThemeResource MzBgBaseBrush}">

	<Grid Padding="{StaticResource MzPagePaddingCompact}" RowSpacing="16">
		<Grid.RowDefinitions>
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
			<RowDefinition Height="*" />
		</Grid.RowDefinitions>

		<controls:MzPageHeader Title="{xaml:Localize Key=Tags}" Lead="{xaml:Localize Key=TagsLead}">
			<controls:MzPageHeader.Actions>
				<Button
					AutomationProperties.AutomationId="AddTagButton"
					Command="{x:Bind ViewModel.AddTagCommand}"
					Style="{StaticResource MzAccentButtonStyle}">
					<StackPanel Orientation="Horizontal" Spacing="8">
						<FontIcon
							FontFamily="{StaticResource MzIconFontFamily}"
							FontSize="16"
							Glyph="{StaticResource MzGlyphAdd}" />
						<TextBlock Text="{xaml:Localize Key=AddTag}" />
					</StackPanel>
				</Button>
			</controls:MzPageHeader.Actions>
		</controls:MzPageHeader>

		<Grid Grid.Row="1" HorizontalAlignment="Left">
			<TextBox
				x:Name="TagSearchBox"
				AutomationProperties.AutomationId="TagSearchBox"
				AutomationProperties.Name="{xaml:Localize Key=SearchTags}"
				PlaceholderText="{xaml:Localize Key=SearchTags}"
				Style="{StaticResource MzSearchFieldStyle}"
				Text="{x:Bind ViewModel.SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
			<FontIcon
				Margin="12,0,0,0"
				HorizontalAlignment="Left"
				VerticalAlignment="Center"
				FontFamily="{StaticResource MzIconFontFamily}"
				FontSize="16"
				Foreground="{ThemeResource MzTextTertiaryBrush}"
				Glyph="{StaticResource MzGlyphSearch}"
				IsHitTestVisible="False" />
		</Grid>

		<Border Grid.Row="2" Style="{StaticResource MzLayerSurfaceStyle}">
			<Grid>
				<!--  HorizontalScrollMode=Disabled keeps the measure width finite, which MzWrapLayout needs to wrap.  -->
				<ScrollViewer
					x:Name="TagsScroll"
					Padding="16"
					HorizontalScrollMode="Disabled"
					VerticalScrollBarVisibility="Auto">
					<ItemsRepeater x:Name="TagsRepeater" ItemsSource="{x:Bind ViewModel.Tags, Mode=OneWay}">
						<ItemsRepeater.Layout>
							<controls:MzWrapLayout HorizontalSpacing="8" VerticalSpacing="8" />
						</ItemsRepeater.Layout>
						<ItemsRepeater.ItemTemplate>
							<DataTemplate x:DataType="items:TagListItemViewModel">
								<Button
									AutomationProperties.Name="{x:Bind DisplayName}"
									Command="{x:Bind EditCommand}"
									CommandParameter="{x:Bind}"
									Opacity="{x:Bind DimOpacity}"
									Style="{StaticResource MzTagPillStyle}"
									ToolTipService.ToolTip="{x:Bind RouteName}">
									<StackPanel Orientation="Horizontal" Spacing="6">
										<TextBlock
											VerticalAlignment="Center"
											Text="{x:Bind DisplayName}"
											TextTrimming="CharacterEllipsis" />
										<Border Style="{StaticResource MzTagCountChipStyle}">
											<TextBlock Style="{StaticResource MzTagCountTextStyle}" Text="{x:Bind PostCountText}" />
										</Border>
									</StackPanel>
									<Button.ContextFlyout>
										<MenuFlyout>
											<MenuFlyoutItem
												Command="{x:Bind EditCommand}"
												CommandParameter="{x:Bind}"
												Text="{xaml:Localize Key=EditTag}" />
											<MenuFlyoutItem
												Command="{x:Bind DeleteCommand}"
												CommandParameter="{x:Bind}"
												Text="{xaml:Localize Key=DeleteTag}" />
										</MenuFlyout>
									</Button.ContextFlyout>
								</Button>
							</DataTemplate>
						</ItemsRepeater.ItemTemplate>
					</ItemsRepeater>
				</ScrollViewer>

				<StackPanel
					Style="{StaticResource MzEmptyStatePanelStyle}"
					Visibility="{x:Bind ViewModel.IsEmpty, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
					<FontIcon Style="{StaticResource MzEmptyStateIconStyle}" Glyph="{StaticResource MzGlyphTag}" />
					<TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{xaml:Localize Key=NoTagsFound}" />
					<TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{xaml:Localize Key=NoTagsFoundDescription}" />
					<Button
						Command="{x:Bind ViewModel.AddTagCommand}"
						Content="{xaml:Localize Key=AddTag}"
						Style="{StaticResource MzStandardButtonStyle}" />
				</StackPanel>
			</Grid>
		</Border>
	</Grid>
</local:TagsManagerViewBase>
```

- [ ] **Step 2: Replace `TagsManagerView.xaml.cs` in full**

The DEBUG hook is the repeatable virtualization check used by the next task.
```csharp
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class TagsManagerView : TagsManagerViewBase
{
	public TagsManagerView()
	{
		InitializeComponent();

#if DEBUG
		Loaded += (_, _) => LogRealization("loaded");
		TagsScroll.ViewChanged += (_, e) =>
		{
			if (!e.IsIntermediate)
			{
				LogRealization("scrolled");
			}
		};
#endif
	}

#if DEBUG
	private void LogRealization(string reason)
	{
		var realized = TagsRepeater.Children.Count;
		var total = ViewModel?.Tags.Count ?? 0;
		System.Diagnostics.Debug.WriteLine($"[TagsManagerView] {reason}: realized={realized} total={total}");
	}
#endif
}

public partial class TagsManagerViewBase : PageBase<TagsManagerViewModel>
{
}
```

- [ ] **Step 3: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Views/Admin/TagsManagerView.xaml src/app/MZikmund.App/Views/Admin/TagsManagerView.xaml.cs
git commit -m "feat: Rebuild the tags manager as a virtualized tag cloud"
```

---

### Task 83: Verify `MzWrapLayout` really virtualizes at ~500 tags

Virtualization is a requirement, not an aspiration — this task measures it against realistic data instead of assuming it.

**Files:**
- Modify (temporarily, reverted in Step 6): `src/app/MZikmund.App.Core/ViewModels/Admin/TagsManagerViewModel.cs` (`RefreshListAsync`)

**Interfaces:**
- Consumes: the `#if DEBUG` `LogRealization` hook in `TagsManagerView.xaml.cs`.
- Produces: nothing (verification only).

- [ ] **Step 1: Temporarily seed 500 synthetic tags**

Replace the body of `RefreshListAsync` in `TagsManagerViewModel.cs` with this stub. Deliberately varied name lengths, because uniform widths would hide a wrap bug:
```csharp
	private Task RefreshListAsync()
	{
		var random = new Random(1337);
		var words = new[] { "uno", "xaml", "fluent", "skia", "webassembly", "dotnet", "azure", "winui", "accessibility", "virtualization", "cs", "performance-tuning", "ci", "blazor", "maui" };

		_allTags.Clear();
		for (var i = 0; i < 500; i++)
		{
			var name = $"{words[random.Next(words.Length)]}-{i}";
			_allTags.Add(new TagListItemViewModel(
				new TagWithPostCount { Id = Guid.NewGuid(), DisplayName = name, RouteName = name, PostCount = i % 7 },
				EditTagCommand,
				DeleteTagCommand));
		}

		HasError = false;
		ApplyFilter();
		return Task.CompletedTask;
	}
```

- [ ] **Step 2: Run the desktop head and open the Tags page**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```
Use the Uno App MCP to drive the window: navigate to Admin → Tags, then screenshot in both light and dark.

- [ ] **Step 3: Read the realization counts / Expected: `realized` is a small multiple of the visible rows, never 500**

The `[TagsManagerView] loaded: realized=… total=500` line appears in the debug output. **Pass criterion: `realized <= 120`.** A 900px-tall viewport holds roughly 20 rows of ~6 pills, so the healthy number is 100–130 including the recycle buffer; `realized=500` means the layout is not virtualizing and the task fails.

- [ ] **Step 4: Scroll to the bottom and re-read / Expected: `realized` stays in the same band**

Drag the scrollbar to the end. A second `[TagsManagerView] scrolled: realized=… total=500` line must report a comparable number. A monotonically climbing count means elements are never being recycled — also a failure.

- [ ] **Step 5: Resize the window from wide to narrow and back / Expected: pills reflow, `realized` stays bounded, no stall**

`MzWrapLayout` caches item sizes independently of the line limit, so a resize reflows without re-measuring all 500. A visible multi-second freeze on resize means the size cache is being dropped and must be investigated before this task passes.

- [ ] **Step 6: Revert the seeding stub**
```bash
git checkout -- src/app/MZikmund.App.Core/ViewModels/Admin/TagsManagerViewModel.cs
```

- [ ] **Step 7: Screenshot the Categories page in both themes**

Drive Admin → Categories with the Uno App MCP. Confirm: one rounded surface, 44px rows, the folder icon, the accent bar growing from the row's vertical centre on hover, the count column right-aligned and monospaced, and no trace of the old high-contrast blue tiles.

- [ ] **Step 8: Build the remaining heads / Expected: Build succeeded on all four**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
dotnet format MZikmund.slnx
```

- [ ] **Step 9: Commit any formatting fallout**
```bash
git add -A
git commit -m "chore: Format after the taxonomy manager rebuild"
```## MediaBrowserView

### Interfaces

PRODUCED (later work / Phase 5 relies on these):

Type `MZikmund.Controls.MzMediaTile : Control` (file `src/app/MZikmund.App/Controls/MzMediaTile.cs`), XAML namespace `xmlns:controls="using:MZikmund.Controls"`. Dependency properties (each with the matching `*Property` static field): `string FileName`, `string SizeText`, `ImageSource? Preview`, `bool IsImage`, `bool AreActionsEnabled` (default `true`), `bool IsSelected`, `object? Item`, `ICommand? ShowVariantsCommand`, `ICommand? DeleteCommand`, `ICommand? CopyUrlCommand`. Template part names: `TileRoot`, `CardSurface`, `ActionsBar`, `ActionsTransform`, `SelectionRing`. Visual state groups: `MzMediaTileRevealStates` (`ActionsVisible` / `ActionsHidden`), `MzMediaTileSelectionStates` (`Selected` / `Unselected`).

Dictionary `src/app/MZikmund.App/Theme/Styles/MediaTile.xaml` (merged in `App.xaml` after `Theme/Styles/Buttons.xaml`): `x:Double` `MzMediaTileMinWidth`=200 `MzMediaTileMinHeight`=208 `MzMediaTileCompactMinWidth`=148 `MzMediaTileCompactMinHeight`=168 `MzMediaTileGlyphSize`=40; `Style TargetType="controls:MzMediaTile"` **`MzMediaTileStyle`** (full size, actions on) and **`MzMediaTileCompactStyle`** (`BasedOn` the former; smaller min box, `AreActionsEnabled="False"`).
**Phase 5 note — `MediaBrowserDialog` consumes `MzMediaTileCompactStyle`.** The dialog's tile template is deleted there and replaced by a single `<controls:MzMediaTile Style="{StaticResource MzMediaTileCompactStyle}" IsSelected="{x:Bind ...}" .../>`; its currently no-op selection VSM (setters targeting `Border.BorderBrush` on a `Grid`) is replaced by the tile's own `IsSelected` DP. **Do not edit `Dialogs/Admin/MediaBrowserDialog.xaml` in Phase 4.**

Glyph keys appended to `src/app/MZikmund.App/Theme/Icons.xaml`: `MzGlyphDocument` (E783), `MzGlyphDelete` (E66D), `MzGlyphCopy` (E5D7), `MzGlyphUpload` (E1B1). All four codepoints are already inside the Phase 1 font subset.

`MediaBrowserViewModel` additions: `sealed record MediaFilterOption(MediaFilterMode Mode, string DisplayName)` in `MZikmund.ViewModels.Admin`; `IReadOnlyList<MediaFilterOption> FilterOptions`; `MediaFilterOption? SelectedFilterOption` (observable, two-way); `bool IsEmpty` (observable); `CopyUrlCommand` (`IRelayCommand<StorageItemInfoViewModel>`). **Removed:** `MediaBrowserViewModel.FilteredFiles` (the view binds `MediaFiles` directly). `MediaBrowserDialogViewModel.FilteredFiles` is a different class and is untouched.
`StorageItemInfoViewModel.Preview : ImageSource?` (lazy `BitmapImage`, null for non-images).

Localization keys added (en + cs): `NoFilesFoundDescription`, `Filter`.

CONSUMED from Phase 1: brushes `MzBgBaseBrush` `MzCardBrush` `MzCardMediaEmptyBrush` `MzBgElevatedBrush` `MzStrokeCardBrush` `MzStrokeControlBrush` `MzAccentFillBrush` `MzTextTertiaryBrush`; radii `MzCardCornerRadius` `MzCardTopCornerRadius` `MzControlCornerRadius`; spacing `MzPagePaddingCompact` `MzPagePaddingWide` `MzCardPadding` `MzSpace2` `MzSpace4`; motion `MzEaseEntrance` `MzEaseExit` `MzDurationFast`; type `MzBodyTextStyle` `MzMetaTextStyle` `MzFontFamilySemiBold`; buttons `MzAccentButtonStyle` `MzStandardButtonStyle` `MzSubtleButtonSmallStyle`; empty state `MzEmptyStatePanelStyle` `MzEmptyStateIconStyle` `MzEmptyStateTitleTextStyle` `MzEmptyStateBodyTextStyle`; icons `MzIconFontFamily` `MzGlyphImageMultiple`; converters `BoolToVisibilityConverter` `InverseBoolToVisibilityConverter`.
CONSUMED from Phase 2: `MZikmund.Controls.MzPageHeader` (`Title`).

### Task 84: The shared `MzMediaTile` control

**Files:**
- Create: `src/app/MZikmund.App/Controls/MzMediaTile.cs`

**Interfaces:**
- Consumes: nothing (pure control code).
- Produces: `MZikmund.Controls.MzMediaTile` with `FileName`, `SizeText`, `Preview`, `IsImage`, `AreActionsEnabled`, `IsSelected`, `Item`, `ShowVariantsCommand`, `DeleteCommand`, `CopyUrlCommand`; visual state groups `MzMediaTileRevealStates` and `MzMediaTileSelectionStates`.

- [ ] **Step 1: Create the control**

Hover/focus reveal is driven from C# rather than a `CommonStates`/focus VSM, because the thing that receives focus is a *descendant* button, not the tile — there is no reliable focus-within visual state on any Skia head. `LostFocus` is re-checked on the next dispatcher turn because the new focus target is not set yet while the event is raised.

`src/app/MZikmund.App/Controls/MzMediaTile.cs`:
```csharp
using System.Windows.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace MZikmund.Controls;

/// <summary>
/// One media tile, shared by the media browser page and the media picker dialog.
/// The action buttons stay in the tree so they remain tabbable, and are revealed
/// on pointer-over or keyboard focus.
/// </summary>
public sealed partial class MzMediaTile : Control
{
	public static readonly DependencyProperty FileNameProperty =
		DependencyProperty.Register(nameof(FileName), typeof(string), typeof(MzMediaTile), new PropertyMetadata(string.Empty));

	public static readonly DependencyProperty SizeTextProperty =
		DependencyProperty.Register(nameof(SizeText), typeof(string), typeof(MzMediaTile), new PropertyMetadata(string.Empty));

	public static readonly DependencyProperty PreviewProperty =
		DependencyProperty.Register(nameof(Preview), typeof(ImageSource), typeof(MzMediaTile), new PropertyMetadata(null));

	public static readonly DependencyProperty IsImageProperty =
		DependencyProperty.Register(nameof(IsImage), typeof(bool), typeof(MzMediaTile), new PropertyMetadata(false));

	public static readonly DependencyProperty AreActionsEnabledProperty =
		DependencyProperty.Register(nameof(AreActionsEnabled), typeof(bool), typeof(MzMediaTile), new PropertyMetadata(true, OnAreActionsEnabledChanged));

	public static readonly DependencyProperty IsSelectedProperty =
		DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(MzMediaTile), new PropertyMetadata(false, OnIsSelectedChanged));

	public static readonly DependencyProperty ItemProperty =
		DependencyProperty.Register(nameof(Item), typeof(object), typeof(MzMediaTile), new PropertyMetadata(null));

	public static readonly DependencyProperty ShowVariantsCommandProperty =
		DependencyProperty.Register(nameof(ShowVariantsCommand), typeof(ICommand), typeof(MzMediaTile), new PropertyMetadata(null));

	public static readonly DependencyProperty DeleteCommandProperty =
		DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(MzMediaTile), new PropertyMetadata(null));

	public static readonly DependencyProperty CopyUrlCommandProperty =
		DependencyProperty.Register(nameof(CopyUrlCommand), typeof(ICommand), typeof(MzMediaTile), new PropertyMetadata(null));

	private bool _isPointerOver;
	private bool _hasFocusWithin;

	public MzMediaTile()
	{
		PointerEntered += OnTilePointerEntered;
		PointerExited += OnTilePointerExited;
		PointerCanceled += OnTilePointerExited;
		PointerCaptureLost += OnTilePointerExited;
		GotFocus += OnTileGotFocus;
		LostFocus += OnTileLostFocus;
	}

	public string FileName
	{
		get => (string)GetValue(FileNameProperty);
		set => SetValue(FileNameProperty, value);
	}

	public string SizeText
	{
		get => (string)GetValue(SizeTextProperty);
		set => SetValue(SizeTextProperty, value);
	}

	public ImageSource? Preview
	{
		get => (ImageSource?)GetValue(PreviewProperty);
		set => SetValue(PreviewProperty, value);
	}

	public bool IsImage
	{
		get => (bool)GetValue(IsImageProperty);
		set => SetValue(IsImageProperty, value);
	}

	public bool AreActionsEnabled
	{
		get => (bool)GetValue(AreActionsEnabledProperty);
		set => SetValue(AreActionsEnabledProperty, value);
	}

	public bool IsSelected
	{
		get => (bool)GetValue(IsSelectedProperty);
		set => SetValue(IsSelectedProperty, value);
	}

	public object? Item
	{
		get => GetValue(ItemProperty);
		set => SetValue(ItemProperty, value);
	}

	public ICommand? ShowVariantsCommand
	{
		get => (ICommand?)GetValue(ShowVariantsCommandProperty);
		set => SetValue(ShowVariantsCommandProperty, value);
	}

	public ICommand? DeleteCommand
	{
		get => (ICommand?)GetValue(DeleteCommandProperty);
		set => SetValue(DeleteCommandProperty, value);
	}

	public ICommand? CopyUrlCommand
	{
		get => (ICommand?)GetValue(CopyUrlCommandProperty);
		set => SetValue(CopyUrlCommandProperty, value);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		UpdateRevealState(false);
		UpdateSelectionState(false);
	}

	private static void OnAreActionsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((MzMediaTile)d).UpdateRevealState(false);

	private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((MzMediaTile)d).UpdateSelectionState(true);

	private void OnTilePointerEntered(object sender, PointerRoutedEventArgs e)
	{
		_isPointerOver = true;
		UpdateRevealState(true);
	}

	private void OnTilePointerExited(object sender, PointerRoutedEventArgs e)
	{
		_isPointerOver = false;
		UpdateRevealState(true);
	}

	private void OnTileGotFocus(object sender, RoutedEventArgs e)
	{
		_hasFocusWithin = true;
		UpdateRevealState(true);
	}

	private void OnTileLostFocus(object sender, RoutedEventArgs e)
	{
		// The new focus target is not assigned yet while LostFocus is raised.
		DispatcherQueue.TryEnqueue(() =>
		{
			_hasFocusWithin = ContainsFocus();
			UpdateRevealState(true);
		});
	}

	private bool ContainsFocus()
	{
		if (XamlRoot is null)
		{
			return false;
		}

		var node = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
		while (node is not null)
		{
			if (ReferenceEquals(node, this))
			{
				return true;
			}

			node = VisualTreeHelper.GetParent(node);
		}

		return false;
	}

	private void UpdateRevealState(bool useTransitions)
	{
		var visible = AreActionsEnabled && (_isPointerOver || _hasFocusWithin);
		VisualStateManager.GoToState(this, visible ? "ActionsVisible" : "ActionsHidden", useTransitions);
	}

	private void UpdateSelectionState(bool useTransitions) =>
		VisualStateManager.GoToState(this, IsSelected ? "Selected" : "Unselected", useTransitions);
}
```

- [ ] **Step 2: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Controls/MzMediaTile.cs
git commit -m "feat: Add the shared MzMediaTile control with hover and focus reveal"
```

---

### Task 85: The shared tile styles and the media glyphs

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/MediaTile.xaml`
- Modify: `src/app/MZikmund.App/Theme/Icons.xaml` (append four glyphs before `</ResourceDictionary>`)
- Modify: `src/app/MZikmund.App/App.xaml` (one added merge line)

**Interfaces:**
- Consumes: `MzCardBrush` `MzCardMediaEmptyBrush` `MzBgElevatedBrush` `MzStrokeCardBrush` `MzStrokeControlBrush` `MzAccentFillBrush` `MzTextTertiaryBrush`, `MzCardCornerRadius` `MzCardTopCornerRadius` `MzControlCornerRadius`, `MzCardPadding`, `MzEaseEntrance` `MzEaseExit` `MzDurationFast`, `MzBodyTextStyle` `MzMetaTextStyle` `MzFontFamilySemiBold`, `MzSubtleButtonSmallStyle`, `MzIconFontFamily` `MzGlyphImageMultiple`, `BoolToVisibilityConverter` `InverseBoolToVisibilityConverter`, `MZikmund.Controls.MzMediaTile`.
- Produces: `MzMediaTileStyle`, `MzMediaTileCompactStyle`, `MzMediaTileMinWidth`, `MzMediaTileMinHeight`, `MzMediaTileCompactMinWidth`, `MzMediaTileCompactMinHeight`, `MzMediaTileGlyphSize`, `MzGlyphDocument`, `MzGlyphDelete`, `MzGlyphCopy`, `MzGlyphUpload`.

- [ ] **Step 1: Append the media glyphs to `src/app/MZikmund.App/Theme/Icons.xaml`**

Insert immediately after the `MzGlyphMore` line, before the closing `</ResourceDictionary>`:
```xml
	<x:String x:Key="MzGlyphDocument">&#xE783;</x:String>
	<x:String x:Key="MzGlyphDelete">&#xE66D;</x:String>
	<x:String x:Key="MzGlyphCopy">&#xE5D7;</x:String>
	<x:String x:Key="MzGlyphUpload">&#xE1B1;</x:String>
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Theme/Styles/MediaTile.xaml`**

The tile never sets a fixed `Width`/`Height` — `UniformGridLayout` sizes it, and the style contributes only a minimum box. `Opacity` is a GPU property so it animates unaided; the 4px lift rides a named `CompositeTransform` (`Translation`/`ScalarTransition` do not exist on Uno) and carries `EnableDependentAnimation="True"` so no head silently drops it.
```xml
<ResourceDictionary
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:controls="using:MZikmund.Controls"
	xmlns:xaml="using:MZikmund.Extensions.Xaml">

	<x:Double x:Key="MzMediaTileMinWidth">200</x:Double>
	<x:Double x:Key="MzMediaTileMinHeight">208</x:Double>
	<x:Double x:Key="MzMediaTileCompactMinWidth">148</x:Double>
	<x:Double x:Key="MzMediaTileCompactMinHeight">168</x:Double>
	<x:Double x:Key="MzMediaTileGlyphSize">40</x:Double>

	<Style x:Key="MzMediaTileStyle" TargetType="controls:MzMediaTile">
		<Setter Property="MinWidth" Value="{StaticResource MzMediaTileMinWidth}" />
		<Setter Property="MinHeight" Value="{StaticResource MzMediaTileMinHeight}" />
		<Setter Property="HorizontalAlignment" Value="Stretch" />
		<Setter Property="VerticalAlignment" Value="Stretch" />
		<Setter Property="IsTabStop" Value="False" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="controls:MzMediaTile">
					<Grid x:Name="TileRoot">
						<Border
							x:Name="CardSurface"
							Background="{ThemeResource MzCardBrush}"
							BorderBrush="{ThemeResource MzStrokeCardBrush}"
							BorderThickness="1"
							CornerRadius="{StaticResource MzCardCornerRadius}">
							<Grid>
								<Grid.RowDefinitions>
									<RowDefinition Height="*" />
									<RowDefinition Height="Auto" />
								</Grid.RowDefinitions>

								<Border
									Grid.Row="0"
									Background="{ThemeResource MzCardMediaEmptyBrush}"
									CornerRadius="{StaticResource MzCardTopCornerRadius}">
									<Grid>
										<Image
											Margin="{StaticResource MzCardPadding}"
											Source="{Binding Preview, RelativeSource={RelativeSource TemplatedParent}}"
											Stretch="Uniform"
											Visibility="{Binding IsImage, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource BoolToVisibilityConverter}}" />
										<FontIcon
											FontFamily="{StaticResource MzIconFontFamily}"
											FontSize="{StaticResource MzMediaTileGlyphSize}"
											Foreground="{ThemeResource MzTextTertiaryBrush}"
											Glyph="{StaticResource MzGlyphDocument}"
											Visibility="{Binding IsImage, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource InverseBoolToVisibilityConverter}}" />
									</Grid>
								</Border>

								<StackPanel Grid.Row="1" Padding="12,8,12,10">
									<TextBlock
										FontFamily="{StaticResource MzFontFamilySemiBold}"
										FontWeight="SemiBold"
										Style="{StaticResource MzBodyTextStyle}"
										Text="{Binding FileName, RelativeSource={RelativeSource TemplatedParent}}"
										TextTrimming="CharacterEllipsis"
										TextWrapping="NoWrap"
										ToolTipService.ToolTip="{Binding FileName, RelativeSource={RelativeSource TemplatedParent}}" />
									<TextBlock
										Style="{StaticResource MzMetaTextStyle}"
										Text="{Binding SizeText, RelativeSource={RelativeSource TemplatedParent}}"
										TextTrimming="CharacterEllipsis"
										TextWrapping="NoWrap" />
								</StackPanel>
							</Grid>
						</Border>

						<Border
							x:Name="SelectionRing"
							BorderBrush="{ThemeResource MzAccentFillBrush}"
							BorderThickness="2"
							CornerRadius="{StaticResource MzCardCornerRadius}"
							IsHitTestVisible="False"
							Opacity="0" />

						<Border
							x:Name="ActionsBar"
							Margin="8"
							Padding="2"
							HorizontalAlignment="Right"
							VerticalAlignment="Top"
							Background="{ThemeResource MzBgElevatedBrush}"
							BorderBrush="{ThemeResource MzStrokeControlBrush}"
							BorderThickness="1"
							CornerRadius="{StaticResource MzControlCornerRadius}"
							Opacity="0"
							Visibility="{Binding AreActionsEnabled, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource BoolToVisibilityConverter}}">
							<Border.RenderTransform>
								<CompositeTransform x:Name="ActionsTransform" TranslateY="-4" />
							</Border.RenderTransform>
							<StackPanel Orientation="Horizontal" Spacing="2">
								<Button
									AutomationProperties.Name="{xaml:Localize Key=ViewVariants}"
									Command="{TemplateBinding ShowVariantsCommand}"
									CommandParameter="{TemplateBinding Item}"
									Style="{StaticResource MzSubtleButtonSmallStyle}"
									ToolTipService.ToolTip="{xaml:Localize Key=ViewVariants}"
									Visibility="{Binding IsImage, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource BoolToVisibilityConverter}}">
									<FontIcon
										FontFamily="{StaticResource MzIconFontFamily}"
										FontSize="16"
										Glyph="{StaticResource MzGlyphImageMultiple}" />
								</Button>
								<Button
									AutomationProperties.Name="{xaml:Localize Key=CopyUrl}"
									Command="{TemplateBinding CopyUrlCommand}"
									CommandParameter="{TemplateBinding Item}"
									Style="{StaticResource MzSubtleButtonSmallStyle}"
									ToolTipService.ToolTip="{xaml:Localize Key=CopyUrl}">
									<FontIcon
										FontFamily="{StaticResource MzIconFontFamily}"
										FontSize="16"
										Glyph="{StaticResource MzGlyphCopy}" />
								</Button>
								<Button
									AutomationProperties.Name="{xaml:Localize Key=Delete}"
									Command="{TemplateBinding DeleteCommand}"
									CommandParameter="{TemplateBinding Item}"
									Style="{StaticResource MzSubtleButtonSmallStyle}"
									ToolTipService.ToolTip="{xaml:Localize Key=Delete}">
									<FontIcon
										FontFamily="{StaticResource MzIconFontFamily}"
										FontSize="16"
										Glyph="{StaticResource MzGlyphDelete}" />
								</Button>
							</StackPanel>
						</Border>

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="MzMediaTileRevealStates">
								<VisualState x:Name="ActionsHidden">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="ActionsBar" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseExit}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="0" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="ActionsTransform"
											Storyboard.TargetProperty="TranslateY">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseExit}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="-4" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="ActionsVisible">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="ActionsBar" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseEntrance}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames
											EnableDependentAnimation="True"
											Storyboard.TargetName="ActionsTransform"
											Storyboard.TargetProperty="TranslateY">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseEntrance}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="0" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>

							<VisualStateGroup x:Name="MzMediaTileSelectionStates">
								<VisualState x:Name="Unselected">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="SelectionRing" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseExit}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="0" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
								<VisualState x:Name="Selected">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="SelectionRing" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseEntrance}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<!--  Size variant consumed by MediaBrowserDialog: smaller box, no hover actions.  -->
	<Style
		x:Key="MzMediaTileCompactStyle"
		BasedOn="{StaticResource MzMediaTileStyle}"
		TargetType="controls:MzMediaTile">
		<Setter Property="MinWidth" Value="{StaticResource MzMediaTileCompactMinWidth}" />
		<Setter Property="MinHeight" Value="{StaticResource MzMediaTileCompactMinHeight}" />
		<Setter Property="AreActionsEnabled" Value="False" />
	</Style>

</ResourceDictionary>
```

- [ ] **Step 3: Merge the dictionary in `src/app/MZikmund.App/App.xaml`**

Insert one line after the `Theme/Styles/Buttons.xaml` entry (it must come after `Buttons.xaml`, which defines `MzSubtleButtonSmallStyle`, and after `Resources/Converters.xaml`):
```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/MediaTile.xaml" />
```

- [ ] **Step 4: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 5: Commit**
```bash
git add src/app/MZikmund.App/Theme/Styles/MediaTile.xaml src/app/MZikmund.App/Theme/Icons.xaml src/app/MZikmund.App/App.xaml
git commit -m "feat: Add one shared media tile style with a compact size variant"
```

---

### Task 86: Give the media ViewModel a bindable filter, a copy command and an empty flag

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserViewModel.cs:75-89`, `:112-152`, `:154-191`, `:298-303`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Items/StorageItemInfoViewModel.cs:1-40`
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw`, `src/app/MZikmund.App/Strings/cs/Resources.resw`

**Interfaces:**
- Consumes: existing `MediaFilterMode`, `IRelayCommand`.
- Produces: `MediaFilterOption` record, `FilterOptions`, `SelectedFilterOption`, `IsEmpty`, `CopyUrlCommand`, `StorageItemInfoViewModel.Preview`; removes `MediaBrowserViewModel.FilteredFiles`.

- [ ] **Step 1: Add the lazy preview to `StorageItemInfoViewModel.cs`**

Replace lines 1-5 (the usings + namespace) with:
```csharp
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blobs;
using MZikmund.DataContracts.Extensions;

namespace MZikmund.ViewModels.Items;
```

Add the field next to the existing ones (after `private readonly AppConfig _appConfig;`):
```csharp
	private BitmapImage? _preview;
```

Add this property after `IsImage`, before the closing brace of the class:
```csharp
	/// <summary>
	/// Thumbnail source for the media tile. Created lazily on the UI thread during binding;
	/// null for non-image blobs so the tile falls back to its document glyph.
	/// </summary>
	public ImageSource? Preview => IsImage && Url is not null ? _preview ??= new BitmapImage(Url) : null;
```

- [ ] **Step 2: Replace the filter surface in `MediaBrowserViewModel.cs`**

Replace lines 75-89 (from `[ObservableProperty]` on `FilterMode` through the closing brace of `OnFilterModeChanged`) with:
```csharp
	[ObservableProperty]
	public partial MediaFilterMode FilterMode { get; set; } = MediaFilterMode.All;

	[ObservableProperty]
	public partial bool HasMoreItems { get; set; } = true;

	[ObservableProperty]
	public partial bool IsLoadingMore { get; set; }

	[ObservableProperty]
	public partial bool IsEmpty { get; set; }

	public IReadOnlyList<MediaFilterOption> FilterOptions { get; }

	[ObservableProperty]
	public partial MediaFilterOption? SelectedFilterOption { get; set; }

	partial void OnSelectedFilterOptionChanged(MediaFilterOption? value)
	{
		FilterMode = value?.Mode ?? MediaFilterMode.All;
	}

	partial void OnFilterModeChanged(MediaFilterMode value)
	{
		_ = RefreshListAsync();
	}
```

- [ ] **Step 3: Build the options in the constructor**

In `MediaBrowserViewModel.cs`, insert immediately before the closing brace of the constructor (after the `_debounceTimer.Tick += ...` block):
```csharp
		FilterOptions =
		[
			new MediaFilterOption(MediaFilterMode.All, Localizer.Instance.GetString("AllFiles")),
			new MediaFilterOption(MediaFilterMode.Images, Localizer.Instance.GetString("Images")),
			new MediaFilterOption(MediaFilterMode.Files, Localizer.Instance.GetString("Files")),
		];

		SelectedFilterOption = FilterOptions[0];
```
Assigning `SelectedFilterOption` here sets `FilterMode` to the value it already holds, so no refresh is triggered before `ViewLoaded`.

- [ ] **Step 4: Drop `FilteredFiles` and raise `IsEmpty` instead**

In `MediaBrowserViewModel.cs`, delete this line entirely (it forced a full `ItemsRepeater` reset on every page load):
```csharp
	public StorageItemInfoViewModel[] FilteredFiles => MediaFiles.ToArray();
```

In `RefreshListAsync`, replace:
```csharp
			HasMoreItems = _hasMoreItems;
			OnPropertyChanged(nameof(FilteredFiles));
		}
		catch (Exception ex)
```
with:
```csharp
			HasMoreItems = _hasMoreItems;
			IsEmpty = MediaFiles.Count == 0;
		}
		catch (Exception ex)
```
and, in the same method, replace `MediaFiles.Clear();` with:
```csharp
			MediaFiles.Clear();
			IsEmpty = false;
```

In `LoadMoreAsync`, replace:
```csharp
			HasMoreItems = _hasMoreItems;
			OnPropertyChanged(nameof(FilteredFiles));
```
with:
```csharp
			HasMoreItems = _hasMoreItems;
			IsEmpty = MediaFiles.Count == 0;
```

- [ ] **Step 5: Add the copy-URL command and the filter option record**

Add `using Windows.ApplicationModel.DataTransfer;` to the using block at the top of `MediaBrowserViewModel.cs` (alphabetically it goes last, after `using Windows.Storage.Pickers;`).

Add this command after `ShowVariantsAsync`, before `IsImageFile`:
```csharp
	[RelayCommand]
	private void CopyUrl(StorageItemInfoViewModel? file)
	{
		if (file?.Url is null)
		{
			return;
		}

		var dataPackage = new DataPackage();
		dataPackage.SetUri(file.Url);
		Clipboard.SetContent(dataPackage);
	}
```

Replace the trailing enum block with:
```csharp
public enum MediaFilterMode
{
	All,
	Images,
	Files
}

/// <summary>
/// One entry of the media kind filter, so the ComboBox can bind items instead of indices.
/// </summary>
/// <param name="Mode">Filter this option selects.</param>
/// <param name="DisplayName">Localized label.</param>
public sealed record MediaFilterOption(MediaFilterMode Mode, string DisplayName);
```

- [ ] **Step 6: Add the two strings and drop the duplicate `LoadMore`**

In `src/app/MZikmund.App/Strings/en/Resources.resw`, delete the **second** `LoadMore` entry (the one that follows `ErrorDeletingFile`, currently lines 243-245):
```xml
  <data name="LoadMore" xml:space="preserve">
    <value>Load More</value>
  </data>
```
Then insert before `</root>`:
```xml
  <data name="NoFilesFoundDescription" xml:space="preserve">
    <value>Upload an image or a file and it will show up here.</value>
  </data>
  <data name="Filter" xml:space="preserve">
    <value>Filter</value>
  </data>
```

In `src/app/MZikmund.App/Strings/cs/Resources.resw`, insert before `</root>`:
```xml
  <data name="NoFilesFoundDescription" xml:space="preserve">
    <value>Nahrajte obrázek nebo soubor a objeví se zde.</value>
  </data>
  <data name="Filter" xml:space="preserve">
    <value>Filtr</value>
  </data>
```

- [ ] **Step 7: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded** (`MediaBrowserView.xaml` still binds the removed `FilteredFiles`; if the XAML compiler reports it here, continue to the next task — the next task replaces that file. Prefer running this task and the next as one build.)

- [ ] **Step 8: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserViewModel.cs src/app/MZikmund.App.Core/ViewModels/Items/StorageItemInfoViewModel.cs src/app/MZikmund.App/Strings
git commit -m "feat: Bind the media filter properly and add a copy-URL command"
```

---

### Task 87: Rebuild `MediaBrowserView` on `ItemsRepeater`

**Files:**
- Modify (replace in full): `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml:1-191`
- Modify (replace in full): `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml.cs:1-27`

**Interfaces:**
- Consumes: `MzMediaTileStyle`, `MzMediaTileMinWidth`, `MzMediaTileMinHeight`, `MzPageHeader`, `MzBgBaseBrush`, `MzPagePaddingCompact` / `MzPagePaddingWide`, `MzSpace2` / `MzSpace4`, `MzAccentButtonStyle` / `MzStandardButtonStyle`, `MzEmptyState*`, `MzIconFontFamily`, `MzGlyphUpload` / `MzGlyphImageMultiple`, `BoolToVisibilityConverter` / `InverseBoolToVisibilityConverter`, `MediaBrowserViewModel.{MediaFiles,SearchFilter,FilterOptions,SelectedFilterOption,IsEmpty,HasMoreItems,IsLoadingMore,UploadFileCommand,LoadMoreCommand,ShowVariantsCommand,DeleteFileCommand,CopyUrlCommand}`.
- Produces: nothing downstream.

- [ ] **Step 1: Replace `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml` in full**

`d:Background` becomes a real `Background`. `ItemsControl` + `ItemsWrapGrid` (which virtualizes nothing) becomes `ItemsRepeater` + `UniformGridLayout`. "Load More" leaves the scrolled `StackPanel` for its own grid row, so it stays pinned under the grid instead of drifting below the last row. Tile commands are wired in `ElementPrepared` rather than by `ElementName` — `ElementName` reaching out of a repeater item template is not dependable on Uno.
```xml
<local:MediaBrowserViewBase
    x:Class="MZikmund.Views.Admin.MediaBrowserView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:MZikmund.Controls"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:items="using:MZikmund.ViewModels.Items"
    xmlns:local="using:MZikmund.Views.Admin"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:vm="using:MZikmund.ViewModels.Admin"
    xmlns:xaml="using:MZikmund.Extensions.Xaml"
    x:Name="Root"
    Background="{ThemeResource MzBgBaseBrush}"
    mc:Ignorable="d">

    <Grid
        x:Name="PageRoot"
        Padding="{StaticResource MzPagePaddingCompact}"
        RowSpacing="{StaticResource MzSpace4}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <controls:MzPageHeader Grid.Row="0" Title="{x:Bind ViewModel.Title, Mode=OneWay}" />

        <Grid Grid.Row="1" ColumnSpacing="{StaticResource MzSpace2}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>

            <Button Command="{x:Bind ViewModel.UploadFileCommand}" Style="{StaticResource MzAccentButtonStyle}">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <FontIcon
                        FontFamily="{StaticResource MzIconFontFamily}"
                        FontSize="16"
                        Glyph="{StaticResource MzGlyphUpload}" />
                    <TextBlock Text="{xaml:Localize Key=Upload}" />
                </StackPanel>
            </Button>

            <TextBox
                Grid.Column="1"
                PlaceholderText="{xaml:Localize Key=Search}"
                Text="{x:Bind ViewModel.SearchFilter, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

            <ComboBox
                Grid.Column="2"
                MinWidth="160"
                AutomationProperties.Name="{xaml:Localize Key=Filter}"
                ItemsSource="{x:Bind ViewModel.FilterOptions, Mode=OneWay}"
                SelectedItem="{x:Bind ViewModel.SelectedFilterOption, Mode=TwoWay}">
                <ComboBox.ItemTemplate>
                    <DataTemplate x:DataType="vm:MediaFilterOption">
                        <TextBlock Text="{x:Bind DisplayName}" />
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>
        </Grid>

        <ScrollViewer Grid.Row="2" Padding="0,0,0,8">
            <ItemsRepeater x:Name="MediaRepeater" ElementPrepared="MediaRepeater_ElementPrepared" ItemsSource="{x:Bind ViewModel.MediaFiles, Mode=OneWay}">
                <ItemsRepeater.Layout>
                    <UniformGridLayout
                        ItemsStretch="Fill"
                        MinColumnSpacing="{StaticResource MzSpace4}"
                        MinItemHeight="{StaticResource MzMediaTileMinHeight}"
                        MinItemWidth="{StaticResource MzMediaTileMinWidth}"
                        MinRowSpacing="{StaticResource MzSpace4}" />
                </ItemsRepeater.Layout>
                <ItemsRepeater.ItemTemplate>
                    <DataTemplate x:DataType="items:StorageItemInfoViewModel">
                        <controls:MzMediaTile
                            AutomationProperties.Name="{x:Bind FileName}"
                            FileName="{x:Bind FileName}"
                            IsImage="{x:Bind IsImage}"
                            Item="{x:Bind}"
                            Preview="{x:Bind Preview}"
                            SizeText="{x:Bind FormattedSize}"
                            Style="{StaticResource MzMediaTileStyle}" />
                    </DataTemplate>
                </ItemsRepeater.ItemTemplate>
            </ItemsRepeater>
        </ScrollViewer>

        <StackPanel
            Grid.Row="2"
            Style="{StaticResource MzEmptyStatePanelStyle}"
            Visibility="{x:Bind ViewModel.IsEmpty, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
            <FontIcon Glyph="{StaticResource MzGlyphImageMultiple}" Style="{StaticResource MzEmptyStateIconStyle}" />
            <TextBlock Style="{StaticResource MzEmptyStateTitleTextStyle}" Text="{xaml:Localize Key=NoFilesFound}" />
            <TextBlock Style="{StaticResource MzEmptyStateBodyTextStyle}" Text="{xaml:Localize Key=NoFilesFoundDescription}" />
            <Button
                HorizontalAlignment="Center"
                Command="{x:Bind ViewModel.UploadFileCommand}"
                Content="{xaml:Localize Key=Upload}"
                Style="{StaticResource MzStandardButtonStyle}" />
        </StackPanel>

        <!--  Outside the scroller: it stays pinned under the grid instead of drifting below the last row.  -->
        <Button
            Grid.Row="3"
            HorizontalAlignment="Center"
            Command="{x:Bind ViewModel.LoadMoreCommand}"
            IsEnabled="{x:Bind ViewModel.HasMoreItems, Mode=OneWay}"
            Style="{StaticResource MzStandardButtonStyle}"
            Visibility="{x:Bind ViewModel.HasMoreItems, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <ProgressRing
                    Width="16"
                    Height="16"
                    IsActive="{x:Bind ViewModel.IsLoadingMore, Mode=OneWay}"
                    Visibility="{x:Bind ViewModel.IsLoadingMore, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />
                <TextBlock Text="{xaml:Localize Key=LoadMore}" />
            </StackPanel>
        </Button>

        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="MzTypeScale">
                <VisualState x:Name="MzTypeScaleWide">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="768" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="PageRoot.Padding" Value="{StaticResource MzPagePaddingWide}" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="MzTypeScaleCompact" />
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    </Grid>
</local:MediaBrowserViewBase>
```

- [ ] **Step 2: Replace `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml.cs` in full**

The `CopyUrl_Click` handler is gone — copying is `MediaBrowserViewModel.CopyUrlCommand` now, which the dialog can reuse.
```csharp
using Microsoft.UI.Xaml.Controls;
using MZikmund.Controls;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class MediaBrowserView : MediaBrowserViewBase
{
	public MediaBrowserView()
	{
		InitializeComponent();
	}

	private void MediaRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
	{
		if (args.Element is MzMediaTile tile && ViewModel is { } viewModel)
		{
			tile.ShowVariantsCommand = viewModel.ShowVariantsCommand;
			tile.DeleteCommand = viewModel.DeleteFileCommand;
			tile.CopyUrlCommand = viewModel.CopyUrlCommand;
		}
	}
}

public class MediaBrowserViewBase : PageBase<MediaBrowserViewModel>
{
}
```

- [ ] **Step 3: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop` / Expected: Build succeeded**

- [ ] **Step 4: Run `dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100` / Expected: Build succeeded**

- [ ] **Step 5: Run the desktop head and verify the page** — `dotnet run --project src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop`, sign in, open Admin → Media. Check: tiles fill the width and reflow on resize; hovering a tile fades its action bar in from 4px above; tabbing into a tile reveals the same bar and the bar stays revealed while focus is inside it; the filter ComboBox shows the three localized labels and reloads on change; with an empty result the 40px tertiary icon, heading, paragraph and Upload button are centred; "Load More" stays pinned below the grid while scrolling.

- [ ] **Step 6: Run `dotnet format MZikmund.slnx`**

- [ ] **Step 7: Commit**
```bash
git add src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml.cs
git commit -m "refactor: Rebuild MediaBrowserView on a virtualized repeater of shared tiles"
```
## PostEditorView

### Interfaces

CONSUMES (Phase 1): brushes `MzBgBaseBrush` `MzCardBrush` `MzLayerBrush` `MzTextPrimaryBrush` `MzTextSecondaryBrush` `MzTextTertiaryBrush` `MzStrokeCardBrush` `MzStrokeDividerBrush` `MzAccentSubtleBrush` `MzAccentTextBrush` `MzCardMediaEmptyBrush`; styles `MzCardSurfaceStyle` `MzLayerSurfaceStyle` `MzCaptionTextStyle` `MzBodyTextStyle` `MzSubtitleTextStyle` `MzStandardButtonStyle` `MzStandardButtonSmallStyle` `MzSubtleButtonSmallStyle` `MzAccentButtonStyle`; tokens `MzSpace1..6` `MzPagePaddingCompact` `MzCardPadding` `MzCardCornerRadius` `MzControlCornerRadius` `MzPillCornerRadius`; fonts `MzMonoFontFamily` `MzFontFamilyMedium`; icons `MzIconFontFamily` + glyphs `MzGlyphFolder` `MzGlyphImageMultiple` `MzGlyphImageOff` `MzGlyphTag`; `theme:MzElevation.Level` (`xmlns:theme="using:MZikmund.Theme"`).
CONSUMES (Phase 2): `MZikmund.Controls.MzPageHeader` (`Title`), `MZikmund.App.Core.Infrastructure.WindowShellLocator.FindForXamlRoot(XamlRoot?)`.
PRODUCES (new resource keys in `Theme/Icons.xaml`): `MzGlyphSave` `MzGlyphAdd` `MzGlyphDismiss` `MzGlyphLink` `MzGlyphOpen`.
PRODUCES (types): `MZikmund.ViewModels.Items.MzChipViewModel` (`string Text`, `object? Payload`, `ICommand RemoveCommand`); `MZikmund.Controls.MzWrapPanel : Panel` (`double HorizontalSpacing`, `double VerticalSpacing`).
PRODUCES (PostEditorViewModel members): `string HeaderTitle`, `string PostAbstract`, `string HeroImageAlt`, `bool HasHeroImage`, `ImageSource? HeroImagePreview`, `ObservableCollection<MzChipViewModel> CategoryChips`, `ObservableCollection<MzChipViewModel> TagChips`, `string NewTagText`, `ICommand AddTagCommand`, `bool IsDirty`, `bool IsSaving`, `bool SaveSucceeded`, `bool SaveFailed`, `string SaveErrorMessage`, `bool HasTitleError`, `bool HasRouteError`, `bool CanSave`, `bool HasCategories`, `bool HasTags`. REMOVED: `string Tags`, `string CategoriesText`.
PRODUCES (resw keys, en+cs): `NewPost` `EditPost` `PostProperties` `UnsavedChanges` `Saving` `PostSaved` `SaveFailed` `TitleRequired` `RouteNameRequired` `AddTag` `RemoveTag` `RemoveCategory` `NoTagsSelected` `PickCategories` `HeroImage` `Editor`.
PRODUCES (template): `Templates/PostPreviewTemplate.html` keeps the `{POSTCONTENT}` and `{ACTUALTHEME}` placeholders; `{ACTUALTHEME}` now feeds `data-theme` (was `data-bs-theme`) and all CSS is inline — zero network requests.

### Task 88: Editor icon glyphs and localization keys

**Files:**
- Modify: `src/app/MZikmund.App/Theme/Icons.xaml` (append before `</ResourceDictionary>`)
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw` (append before `</root>`)
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw` (append before `</root>`)

**Interfaces:**
- Consumes: `MzIconFontFamily` (Phase 1 Task 1).
- Produces: `MzGlyphSave` `MzGlyphAdd` `MzGlyphDismiss` `MzGlyphLink` `MzGlyphOpen`; the 16 resw keys listed below.

- [ ] **Step 1: Add the five glyph keys**

Codepoints are already in the subset (`icons.txt`: `save EF99`, `add E00D`, `dismiss E6D3`, `link EB68`, `open ECF4`), so no font regeneration is needed. Insert immediately before the closing `</ResourceDictionary>` of `src/app/MZikmund.App/Theme/Icons.xaml`:
```xml
	<x:String x:Key="MzGlyphSave">&#xEF99;</x:String>
	<x:String x:Key="MzGlyphAdd">&#xE00D;</x:String>
	<x:String x:Key="MzGlyphDismiss">&#xE6D3;</x:String>
	<x:String x:Key="MzGlyphLink">&#xEB68;</x:String>
	<x:String x:Key="MzGlyphOpen">&#xECF4;</x:String>
```

- [ ] **Step 2: Add the English strings**

Insert before `</root>` in `src/app/MZikmund.App/Strings/en/Resources.resw`:
```xml
  <data name="NewPost" xml:space="preserve">
    <value>New post</value>
  </data>
  <data name="EditPost" xml:space="preserve">
    <value>Edit post</value>
  </data>
  <data name="PostProperties" xml:space="preserve">
    <value>Post properties</value>
  </data>
  <data name="UnsavedChanges" xml:space="preserve">
    <value>Unsaved changes</value>
  </data>
  <data name="Saving" xml:space="preserve">
    <value>Saving…</value>
  </data>
  <data name="PostSaved" xml:space="preserve">
    <value>Post saved.</value>
  </data>
  <data name="SaveFailed" xml:space="preserve">
    <value>Could not save the post</value>
  </data>
  <data name="TitleRequired" xml:space="preserve">
    <value>A title is required.</value>
  </data>
  <data name="RouteNameRequired" xml:space="preserve">
    <value>A route name is required.</value>
  </data>
  <data name="AddTag" xml:space="preserve">
    <value>Add tag</value>
  </data>
  <data name="RemoveTag" xml:space="preserve">
    <value>Remove tag</value>
  </data>
  <data name="RemoveCategory" xml:space="preserve">
    <value>Remove category</value>
  </data>
  <data name="NoTagsSelected" xml:space="preserve">
    <value>No tags yet</value>
  </data>
  <data name="PickCategories" xml:space="preserve">
    <value>Pick categories</value>
  </data>
  <data name="HeroImage" xml:space="preserve">
    <value>Hero image</value>
  </data>
  <data name="Editor" xml:space="preserve">
    <value>Editor</value>
  </data>
```

- [ ] **Step 3: Add the Czech strings**

Insert before `</root>` in `src/app/MZikmund.App/Strings/cs/Resources.resw`:
```xml
  <data name="NewPost" xml:space="preserve">
    <value>Nový příspěvek</value>
  </data>
  <data name="EditPost" xml:space="preserve">
    <value>Upravit příspěvek</value>
  </data>
  <data name="PostProperties" xml:space="preserve">
    <value>Vlastnosti příspěvku</value>
  </data>
  <data name="UnsavedChanges" xml:space="preserve">
    <value>Neuložené změny</value>
  </data>
  <data name="Saving" xml:space="preserve">
    <value>Ukládání…</value>
  </data>
  <data name="PostSaved" xml:space="preserve">
    <value>Příspěvek byl uložen.</value>
  </data>
  <data name="SaveFailed" xml:space="preserve">
    <value>Příspěvek se nepodařilo uložit</value>
  </data>
  <data name="TitleRequired" xml:space="preserve">
    <value>Název je povinný.</value>
  </data>
  <data name="RouteNameRequired" xml:space="preserve">
    <value>Cesta je povinná.</value>
  </data>
  <data name="AddTag" xml:space="preserve">
    <value>Přidat štítek</value>
  </data>
  <data name="RemoveTag" xml:space="preserve">
    <value>Odebrat štítek</value>
  </data>
  <data name="RemoveCategory" xml:space="preserve">
    <value>Odebrat kategorii</value>
  </data>
  <data name="NoTagsSelected" xml:space="preserve">
    <value>Zatím žádné štítky</value>
  </data>
  <data name="PickCategories" xml:space="preserve">
    <value>Vybrat kategorie</value>
  </data>
  <data name="HeroImage" xml:space="preserve">
    <value>Hlavní obrázek</value>
  </data>
  <data name="Editor" xml:space="preserve">
    <value>Editor</value>
  </data>
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App/Theme/Icons.xaml src/app/MZikmund.App/Strings/en/Resources.resw src/app/MZikmund.App/Strings/cs/Resources.resw
git commit -m "feat: Add post-editor glyphs and localized strings"
```

---

### Task 89: `MzChipViewModel` and `MzWrapPanel` — the chip primitives

`ItemsRepeater`+`UniformGridLayout` forces one width on every item, which truncates tag chips; WinUI ships no wrapping panel. `MzWrapPanel` is 45 lines and works identically on all five heads. Chip counts here are single-digit, so no virtualization is required (the Tags manager view handles the ~500-item case separately).

**Files:**
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/MzChipViewModel.cs`
- Create: `src/app/MZikmund.App/Controls/MzWrapPanel.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `MZikmund.ViewModels.Items.MzChipViewModel`, `MZikmund.Controls.MzWrapPanel`.

- [ ] **Step 1: Create `src/app/MZikmund.App.Core/ViewModels/Items/MzChipViewModel.cs`**
```csharp
namespace MZikmund.ViewModels.Items;

/// <summary>
/// A removable token. Self-contained so chip templates can use x:Bind with no ElementName reach-out.
/// </summary>
public sealed partial class MzChipViewModel : ObservableObject
{
	private readonly Action<MzChipViewModel> _remove;

	public MzChipViewModel(string text, object? payload, Action<MzChipViewModel> remove)
	{
		Text = text;
		Payload = payload;
		_remove = remove;
	}

	public string Text { get; }

	public object? Payload { get; }

	[RelayCommand]
	private void Remove() => _remove(this);
}
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Controls/MzWrapPanel.cs`**
```csharp
namespace MZikmund.Controls;

/// <summary>
/// Left-to-right wrapping panel. WinUI has none, and UniformGridLayout would force one width on every chip.
/// </summary>
public partial class MzWrapPanel : Panel
{
	public static readonly DependencyProperty HorizontalSpacingProperty =
		DependencyProperty.Register(
			nameof(HorizontalSpacing),
			typeof(double),
			typeof(MzWrapPanel),
			new PropertyMetadata(8d, OnSpacingChanged));

	public static readonly DependencyProperty VerticalSpacingProperty =
		DependencyProperty.Register(
			nameof(VerticalSpacing),
			typeof(double),
			typeof(MzWrapPanel),
			new PropertyMetadata(8d, OnSpacingChanged));

	public double HorizontalSpacing
	{
		get => (double)GetValue(HorizontalSpacingProperty);
		set => SetValue(HorizontalSpacingProperty, value);
	}

	public double VerticalSpacing
	{
		get => (double)GetValue(VerticalSpacingProperty);
		set => SetValue(VerticalSpacingProperty, value);
	}

	private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((MzWrapPanel)d).InvalidateMeasure();

	protected override Size MeasureOverride(Size availableSize)
	{
		var lineWidth = 0d;
		var lineHeight = 0d;
		var totalWidth = 0d;
		var totalHeight = 0d;
		var childConstraint = new Size(availableSize.Width, double.PositiveInfinity);

		foreach (var child in Children)
		{
			child.Measure(childConstraint);
			var desired = child.DesiredSize;
			var advance = lineWidth == 0 ? desired.Width : lineWidth + HorizontalSpacing + desired.Width;

			if (advance > availableSize.Width && lineWidth > 0)
			{
				totalWidth = Math.Max(totalWidth, lineWidth);
				totalHeight += lineHeight + VerticalSpacing;
				lineWidth = desired.Width;
				lineHeight = desired.Height;
			}
			else
			{
				lineWidth = advance;
				lineHeight = Math.Max(lineHeight, desired.Height);
			}
		}

		totalWidth = Math.Max(totalWidth, lineWidth);
		totalHeight += lineHeight;
		return new Size(totalWidth, totalHeight);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var x = 0d;
		var y = 0d;
		var lineHeight = 0d;

		foreach (var child in Children)
		{
			var desired = child.DesiredSize;

			if (x > 0 && x + desired.Width > finalSize.Width)
			{
				x = 0;
				y += lineHeight + VerticalSpacing;
				lineHeight = 0;
			}

			child.Arrange(new Rect(x, y, desired.Width, desired.Height));
			x += desired.Width + HorizontalSpacing;
			lineHeight = Math.Max(lineHeight, desired.Height);
		}

		return finalSize;
	}
}
```

- [ ] **Step 3: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 4: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Items/MzChipViewModel.cs src/app/MZikmund.App/Controls/MzWrapPanel.cs
git commit -m "feat: Add chip view model and wrapping panel primitives"
```

---

### Task 90: `PostEditorViewModel` — chips, dirty state, validation, save feedback

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/PostEditorViewModel.cs:1-45` (usings + ctor)
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/PostEditorViewModel.cs:47-110` (properties)
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/PostEditorViewModel.cs:126-155` (`SaveAsync`)
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/PostEditorViewModel.cs:290-320` (`DraftTimerOnTick`)
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/PostEditorViewModel.cs:370-396` (`PopulateInfo`)

**Interfaces:**
- Consumes: `MzChipViewModel`.
- Produces: the members listed in the view interface block; removes `Tags` and `CategoriesText`.

- [ ] **Step 1: Extend the usings and the constructor**

Replace the `using Newtonsoft.Json;` line with:
```csharp
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.ViewModels.Items;
using Newtonsoft.Json;
```
Add a field beside `private bool _isPreviewDirty = true;`:
```csharp
	private readonly ILogger<PostEditorViewModel> _logger;
	private bool _isPopulating;
```
Change the constructor signature's final parameter list and body — replace
```csharp
	public PostEditorViewModel(IMZikmundApi api, IWindowShellProvider windowShellProvider, IDialogService dialogService, ILoadingIndicator loadingIndicator, IPostContentProcessor postContentProcessor, ITimerFactory timerFactory, IOptions<AppConfig> appConfig, ILogger<MediaBrowserDialogViewModel> mediaBrowserLogger)
	{
```
with
```csharp
	public PostEditorViewModel(IMZikmundApi api, IWindowShellProvider windowShellProvider, IDialogService dialogService, ILoadingIndicator loadingIndicator, IPostContentProcessor postContentProcessor, ITimerFactory timerFactory, IOptions<AppConfig> appConfig, ILogger<MediaBrowserDialogViewModel> mediaBrowserLogger, ILogger<PostEditorViewModel> logger)
	{
		_logger = logger;
```

- [ ] **Step 2: Replace the property block**

Replace everything from `public override string Title => Post?.Title ?? "";` (line 47) down to and including the `CategoriesText` expression body (line 103) with:
```csharp
	public override string Title => Post?.Title ?? "";

	public string HeaderTitle => Post is null || Post.Id == Guid.Empty
		? Localizer.Instance.GetString("NewPost")
		: Localizer.Instance.GetString("EditPost");

	[ObservableProperty]
	public partial string PostContent { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasRouteError))]
	[NotifyPropertyChangedFor(nameof(CanSave))]
	[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
	public partial string PostRouteName { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasTitleError))]
	[NotifyPropertyChangedFor(nameof(CanSave))]
	[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
	public partial string PostTitle { get; set; } = "";

	[ObservableProperty]
	public partial string PostAbstract { get; set; } = "";

	[ObservableProperty]
	public partial string HeroImageAlt { get; set; } = "";

	[ObservableProperty]
	public partial string NewTagText { get; set; } = "";

	[ObservableProperty]
	public partial string HtmlPreview { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasCategories))]
	public partial Category[] Categories { get; set; } = Array.Empty<Category>();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HeaderTitle))]
	[NotifyPropertyChangedFor(nameof(Title))]
	public partial PostAdmin? Post { get; set; } = null;

	[ObservableProperty]
	public partial bool IsPublished { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset PublishDate { get; set; } = DateTimeOffset.Now;

	[ObservableProperty]
	public partial TimeSpan PublishTime { get; set; } = DateTimeOffset.Now.TimeOfDay;

	[ObservableProperty]
	public partial int SelectionStart { get; set; }

	[ObservableProperty]
	public partial int SelectionLength { get; set; }

	[ObservableProperty]
	public partial bool IsDirty { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanSave))]
	[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
	public partial bool IsSaving { get; set; }

	[ObservableProperty]
	public partial bool SaveSucceeded { get; set; }

	[ObservableProperty]
	public partial bool SaveFailed { get; set; }

	[ObservableProperty]
	public partial string SaveErrorMessage { get; set; } = "";

	public event EventHandler? UpdateSelectionRequested;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasHeroImage))]
	[NotifyPropertyChangedFor(nameof(HeroImagePreview))]
	public partial string? HeroImageUrl { get; set; }

	public ObservableCollection<MzChipViewModel> CategoryChips { get; } = new();

	public ObservableCollection<MzChipViewModel> TagChips { get; } = new();

	public bool HasCategories => Categories.Length > 0;

	public bool HasTags => TagChips.Count > 0;

	public bool HasTitleError => string.IsNullOrWhiteSpace(PostTitle);

	public bool HasRouteError => string.IsNullOrWhiteSpace(PostRouteName);

	public bool CanSave => !HasTitleError && !HasRouteError && !IsSaving;

	public bool HasHeroImage => Uri.TryCreate(HeroImageUrl, UriKind.Absolute, out _);

	public ImageSource? HeroImagePreview =>
		Uri.TryCreate(HeroImageUrl, UriKind.Absolute, out var uri) ? new BitmapImage(uri) : null;

	private void MarkDirty()
	{
		if (_isPopulating)
		{
			return;
		}

		IsDirty = true;
		SaveSucceeded = false;
	}

	partial void OnPostTitleChanged(string value)
	{
		PostRouteName = value.GenerateRouteName();
		MarkDirty();
	}

	partial void OnPostRouteNameChanged(string value) => MarkDirty();

	partial void OnPostAbstractChanged(string value) => MarkDirty();

	partial void OnHeroImageUrlChanged(string? value) => MarkDirty();

	partial void OnHeroImageAltChanged(string value) => MarkDirty();

	partial void OnIsPublishedChanged(bool value) => MarkDirty();

	partial void OnPublishDateChanged(DateTimeOffset value) => MarkDirty();

	partial void OnPublishTimeChanged(TimeSpan value) => MarkDirty();

	partial void OnPostContentChanged(string value)
	{
		_isPreviewDirty = true;
		MarkDirty();
	}

	partial void OnCategoriesChanged(Category[] value)
	{
		CategoryChips.Clear();
		foreach (var category in value)
		{
			CategoryChips.Add(new MzChipViewModel(category.DisplayName, category, RemoveCategoryChip));
		}

		MarkDirty();
	}

	private void RemoveCategoryChip(MzChipViewModel chip)
	{
		if (chip.Payload is Category category)
		{
			Categories = Categories.Where(c => c.Id != category.Id).ToArray();
		}
	}

	private void RemoveTagChip(MzChipViewModel chip)
	{
		TagChips.Remove(chip);
		OnPropertyChanged(nameof(HasTags));
		MarkDirty();
	}

	[RelayCommand]
	private void AddTag()
	{
		var parts = NewTagText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (var part in parts)
		{
			if (!Tag.IsValid(part) || TagChips.Any(c => string.Equals(c.Text, part, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}

			TagChips.Add(new MzChipViewModel(part, null, RemoveTagChip));
		}

		NewTagText = "";
		OnPropertyChanged(nameof(HasTags));
		MarkDirty();
	}

	private Tag[] BuildTags() => TagChips.Select(c => new Tag { DisplayName = c.Text }).ToArray();
```
The old `OnPostContentChanged` (lines 96-99) and `CategoriesText` are now gone; delete the leftover copies.

- [ ] **Step 3: Replace `SaveAsync`**

Replace the whole `[RelayCommand] private async Task SaveAsync()` method with:
```csharp
	[RelayCommand(CanExecute = nameof(CanSave))]
	private async Task SaveAsync()
	{
		if (Post is null)
		{
			return;
		}

		IsSaving = true;
		SaveSucceeded = false;
		SaveFailed = false;

		try
		{
			Post.Title = PostTitle;
			Post.RouteName = PostRouteName;
			Post.Abstract = PostAbstract;
			Post.Tags = BuildTags();
			Post.Categories = Categories;
			Post.IsPublished = IsPublished;
			Post.PublishedDate = GetCombinedPublishDateTime();
			Post.Content = PostContent;
			Post.HeroImageUrl = HeroImageUrl;
			Post.HeroImageAlt = HeroImageAlt;

			if (Post.Id == Guid.Empty)
			{
				await _api.AddPostAsync(Post);
			}
			else
			{
				await _api.UpdatePostAsync(Post.Id, Post);
			}

			IsDirty = false;
			SaveSucceeded = true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save post {PostId}", Post.Id);
			SaveErrorMessage = ex.Message;
			SaveFailed = true;
		}
		finally
		{
			IsSaving = false;
		}
	}
```

- [ ] **Step 4: Repoint the draft snapshot at the chips**

In `DraftTimerOnTick`, replace the `Tags = ...` and hero lines of the `new Post { … }` initializer:
```csharp
				Tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(t => new Tag { DisplayName = t.Trim() })
					.ToArray(),
				Categories = Categories,
				IsPublished = IsPublished,
				PublishedDate = GetCombinedPublishDateTime(),
				HeroImageUrl = Post.HeroImageUrl,
				HeroImageAlt = Post.HeroImageAlt
```
with:
```csharp
				Abstract = PostAbstract,
				Tags = BuildTags(),
				Categories = Categories,
				IsPublished = IsPublished,
				PublishedDate = GetCombinedPublishDateTime(),
				HeroImageUrl = HeroImageUrl,
				HeroImageAlt = HeroImageAlt
```

- [ ] **Step 5: Replace `PopulateInfo`**
```csharp
	private void PopulateInfo(Post post)
	{
		if (post is null)
		{
			throw new ArgumentNullException(nameof(post));
		}

		_isPopulating = true;
		try
		{
			TagChips.Clear();
			foreach (var tag in post.Tags)
			{
				TagChips.Add(new MzChipViewModel(tag.DisplayName, null, RemoveTagChip));
			}

			Categories = post.Categories.ToArray();
			PostTitle = post.Title;
			PostRouteName = post.RouteName;
			PostContent = post.Content;
			PostAbstract = post.Abstract;
			HeroImageUrl = post.HeroImageUrl;
			HeroImageAlt = post.HeroImageAlt ?? "";
			IsPublished = post.IsPublished;

			if (post.PublishedDate.HasValue)
			{
				PublishDate = post.PublishedDate.Value;
				PublishTime = post.PublishedDate.Value.TimeOfDay;
			}
			else
			{
				PublishDate = DateTimeOffset.Now;
				PublishTime = DateTimeOffset.Now.TimeOfDay;
			}
		}
		finally
		{
			_isPopulating = false;
		}

		OnPropertyChanged(nameof(HasTags));
		IsDirty = false;
		SaveSucceeded = false;
		SaveFailed = false;
	}
```
`PostTitle` is assigned before `PostRouteName`, and `OnPostTitleChanged` regenerates the route — so the order above matters: the explicit `PostRouteName` assignment must come after it, which it does.

- [ ] **Step 6: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 7: Commit**
```bash
git add src/app/MZikmund.App.Core/ViewModels/Admin/PostEditorViewModel.cs
git commit -m "feat: Give the post editor chips, dirty tracking, validation and save feedback"
```

---

### Task 91: `PostEditorView.xaml` — CommandBar, properties pane and adaptive states

Three states in one `VisualStateGroup`, all driven by `Grid.Row` / `Grid.Column` / `Grid.ColumnSpan` setters plus row-height and column-width setters. Every animated property here is a state *setter*, not a `Storyboard`, so the `EnableDependentAnimation` rule does not apply. `Wide` is declared before `Mid`: with two `AdaptiveTrigger`s active, the first declared wins.

**Files:**
- Modify: `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml:1-137` (full rewrite)

**Interfaces:**
- Consumes: all Phase 1 keys plus `controls:MzPageHeader`, `MzChipViewModel`, `MzWrapPanel`.
- Produces: named elements `PageRoot`, `EditorLayout`, `PropertiesPane`, `EditorPane`, `PreviewPane`, `ContentTextBox`, `PreviewWebViewContainer`; visual states `Wide` / `Mid` / `Narrow` in group `MzEditorLayoutStates`.

- [ ] **Step 1: Replace the file in full**
```xml
<local:PostEditorViewBase
	x:Class="MZikmund.Views.Admin.PostEditorView"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:controls="using:MZikmund.Controls"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:items="using:MZikmund.ViewModels.Items"
	xmlns:local="using:MZikmund.Views.Admin"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	xmlns:theme="using:MZikmund.Theme"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Background="{ThemeResource MzBgBaseBrush}"
	mc:Ignorable="d">

	<local:PostEditorViewBase.Resources>
		<DataTemplate x:Key="MzChipTemplate" x:DataType="items:MzChipViewModel">
			<Border
				Padding="12,4,4,4"
				Background="{ThemeResource MzAccentSubtleBrush}"
				BorderBrush="{ThemeResource MzStrokeControlBrush}"
				BorderThickness="1"
				CornerRadius="{StaticResource MzPillCornerRadius}">
				<StackPanel Orientation="Horizontal" Spacing="4">
					<TextBlock
						VerticalAlignment="Center"
						Foreground="{ThemeResource MzAccentTextBrush}"
						Style="{StaticResource MzCaptionTextStyle}"
						Text="{x:Bind Text}"
						TextWrapping="NoWrap" />
					<Button
						Width="24"
						Height="24"
						MinWidth="24"
						MinHeight="24"
						Padding="0"
						AutomationProperties.Name="{xaml:Localize Key=RemoveTag}"
						Command="{x:Bind RemoveCommand}"
						CornerRadius="{StaticResource MzPillCornerRadius}"
						Style="{StaticResource MzSubtleButtonSmallStyle}">
						<FontIcon
							FontFamily="{StaticResource MzIconFontFamily}"
							FontSize="12"
							Glyph="{StaticResource MzGlyphDismiss}" />
					</Button>
				</StackPanel>
			</Border>
		</DataTemplate>

		<ItemsPanelTemplate x:Key="MzChipPanelTemplate">
			<controls:MzWrapPanel HorizontalSpacing="8" VerticalSpacing="8" />
		</ItemsPanelTemplate>
	</local:PostEditorViewBase.Resources>

	<Grid
		x:Name="PageRoot"
		Padding="{StaticResource MzPagePaddingCompact}"
		RowSpacing="{StaticResource MzSpace4}">
		<Grid.RowDefinitions>
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
			<RowDefinition Height="Auto" />
			<RowDefinition Height="*" />
		</Grid.RowDefinitions>

		<controls:MzPageHeader Title="{x:Bind ViewModel.HeaderTitle, Mode=OneWay}" />

		<Grid Grid.Row="1" ColumnDefinitions="*, Auto">
			<CommandBar
				HorizontalAlignment="Left"
				Background="Transparent"
				DefaultLabelPosition="Right"
				IsOpen="False">
				<AppBarButton
					AutomationProperties.Name="{xaml:Localize Key=Save}"
					Command="{x:Bind ViewModel.SaveCommand, Mode=OneWay}"
					Label="{xaml:Localize Key=Save}">
					<AppBarButton.Icon>
						<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphSave}" />
					</AppBarButton.Icon>
				</AppBarButton>
				<AppBarSeparator />
				<AppBarButton
					AutomationProperties.Name="{xaml:Localize Key=InsertImage}"
					Command="{x:Bind ViewModel.InsertImageCommand, Mode=OneWay}"
					Label="{xaml:Localize Key=InsertImage}">
					<AppBarButton.Icon>
						<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphImageMultiple}" />
					</AppBarButton.Icon>
				</AppBarButton>
				<AppBarButton
					AutomationProperties.Name="{xaml:Localize Key=InsertFile}"
					Command="{x:Bind ViewModel.InsertFileCommand, Mode=OneWay}"
					Label="{xaml:Localize Key=InsertFile}">
					<AppBarButton.Icon>
						<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphLink}" />
					</AppBarButton.Icon>
				</AppBarButton>
				<CommandBar.SecondaryCommands>
					<AppBarButton
						AutomationProperties.Name="{xaml:Localize Key=OpenPreview}"
						Command="{x:Bind ViewModel.OpenPreviewCommand, Mode=OneWay}"
						Label="{xaml:Localize Key=OpenPreview}">
						<AppBarButton.Icon>
							<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphOpen}" />
						</AppBarButton.Icon>
					</AppBarButton>
				</CommandBar.SecondaryCommands>
			</CommandBar>

			<StackPanel
				Grid.Column="1"
				VerticalAlignment="Center"
				Orientation="Horizontal"
				Spacing="{StaticResource MzSpace2}">
				<ProgressRing
					Width="16"
					Height="16"
					IsActive="{x:Bind ViewModel.IsSaving, Mode=OneWay}"
					Visibility="{x:Bind ViewModel.IsSaving, Mode=OneWay}" />
				<TextBlock
					VerticalAlignment="Center"
					Foreground="{ThemeResource MzTextTertiaryBrush}"
					Style="{StaticResource MzCaptionTextStyle}"
					Text="{xaml:Localize Key=UnsavedChanges}"
					Visibility="{x:Bind ViewModel.IsDirty, Mode=OneWay}" />
			</StackPanel>
		</Grid>

		<StackPanel Grid.Row="2" Spacing="{StaticResource MzSpace2}">
			<InfoBar
				IsOpen="{x:Bind ViewModel.HasTitleError, Mode=OneWay}"
				IsClosable="False"
				Message="{xaml:Localize Key=TitleRequired}"
				Severity="Warning" />
			<InfoBar
				IsOpen="{x:Bind ViewModel.HasRouteError, Mode=OneWay}"
				IsClosable="False"
				Message="{xaml:Localize Key=RouteNameRequired}"
				Severity="Warning" />
			<InfoBar
				IsOpen="{x:Bind ViewModel.SaveSucceeded, Mode=TwoWay}"
				Message="{xaml:Localize Key=PostSaved}"
				Severity="Success" />
			<InfoBar
				IsOpen="{x:Bind ViewModel.SaveFailed, Mode=TwoWay}"
				Message="{x:Bind ViewModel.SaveErrorMessage, Mode=OneWay}"
				Severity="Error"
				Title="{xaml:Localize Key=SaveFailed}" />
		</StackPanel>

		<Grid
			x:Name="EditorLayout"
			Grid.Row="3"
			ColumnSpacing="{StaticResource MzSpace4}"
			RowSpacing="{StaticResource MzSpace4}">
			<Grid.ColumnDefinitions>
				<ColumnDefinition x:Name="EditorColumn" Width="*" />
				<ColumnDefinition x:Name="PreviewColumn" Width="*" />
				<ColumnDefinition x:Name="PropertiesColumn" Width="320" />
			</Grid.ColumnDefinitions>
			<Grid.RowDefinitions>
				<RowDefinition x:Name="StackedPropertiesRow" Height="0" />
				<RowDefinition x:Name="MainRow" Height="*" />
				<RowDefinition x:Name="StackedPreviewRow" Height="0" />
			</Grid.RowDefinitions>

			<Grid
				x:Name="EditorPane"
				Grid.Row="1"
				Grid.Column="0"
				RowSpacing="{StaticResource MzSpace3}">
				<Grid.RowDefinitions>
					<RowDefinition Height="Auto" />
					<RowDefinition Height="Auto" />
					<RowDefinition Height="*" />
				</Grid.RowDefinitions>

				<TextBox Header="{xaml:Localize Key=Title}" Text="{x:Bind ViewModel.PostTitle, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

				<TextBox
					Grid.Row="1"
					Height="76"
					AcceptsReturn="True"
					Header="{xaml:Localize Key=Abstract}"
					Text="{x:Bind ViewModel.PostAbstract, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
					TextWrapping="Wrap" />

				<TextBox
					x:Name="ContentTextBox"
					Grid.Row="2"
					VerticalAlignment="Stretch"
					VerticalContentAlignment="Top"
					AcceptsReturn="True"
					FontFamily="{StaticResource MzMonoFontFamily}"
					FontSize="14"
					Header="{xaml:Localize Key=Content}"
					IsSpellCheckEnabled="False"
					ScrollViewer.VerticalScrollBarVisibility="Auto"
					SelectionChanged="ContentTextBox_SelectionChanged"
					Text="{x:Bind ViewModel.PostContent, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
					TextWrapping="Wrap" />
			</Grid>

			<Grid
				x:Name="PreviewPane"
				Grid.Row="1"
				Grid.Column="1"
				RowSpacing="{StaticResource MzSpace2}">
				<Grid.RowDefinitions>
					<RowDefinition Height="Auto" />
					<RowDefinition Height="*" />
				</Grid.RowDefinitions>
				<TextBlock
					Foreground="{ThemeResource MzTextSecondaryBrush}"
					Style="{StaticResource MzCaptionTextStyle}"
					Text="{xaml:Localize Key=Preview}" />
				<Border
					Grid.Row="1"
					Padding="0"
					theme:MzElevation.Level="Card"
					Style="{StaticResource MzCardSurfaceStyle}">
					<ContentControl
						x:Name="PreviewWebViewContainer"
						HorizontalAlignment="Stretch"
						VerticalAlignment="Stretch"
						HorizontalContentAlignment="Stretch"
						VerticalContentAlignment="Stretch" />
				</Border>
			</Grid>

			<Expander
				x:Name="PropertiesPane"
				Grid.Row="1"
				Grid.Column="2"
				HorizontalAlignment="Stretch"
				HorizontalContentAlignment="Stretch"
				VerticalAlignment="Top"
				Header="{xaml:Localize Key=PostProperties}"
				IsExpanded="True">
				<ScrollViewer MaxHeight="640" VerticalScrollBarVisibility="Auto">
					<StackPanel Spacing="{StaticResource MzSpace3}">
						<TextBox Header="{xaml:Localize Key=Route}" Text="{x:Bind ViewModel.PostRouteName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

						<TextBlock
							Foreground="{ThemeResource MzTextSecondaryBrush}"
							Style="{StaticResource MzCaptionTextStyle}"
							Text="{xaml:Localize Key=HeroImage}" />
						<Grid
							Height="90"
							Background="{ThemeResource MzCardMediaEmptyBrush}"
							CornerRadius="{StaticResource MzCardCornerRadius}">
							<FontIcon
								FontFamily="{StaticResource MzIconFontFamily}"
								FontSize="24"
								Foreground="{ThemeResource MzTextTertiaryBrush}"
								Glyph="{StaticResource MzGlyphImageOff}"
								Visibility="{x:Bind ViewModel.HasHeroImage, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}" />
							<Image
								Source="{x:Bind ViewModel.HeroImagePreview, Mode=OneWay}"
								Stretch="UniformToFill"
								Visibility="{x:Bind ViewModel.HasHeroImage, Mode=OneWay}" />
						</Grid>
						<Grid ColumnDefinitions="*, Auto" ColumnSpacing="{StaticResource MzSpace1}">
							<TextBox Header="{xaml:Localize Key=HeroImageUrl}" Text="{x:Bind ViewModel.HeroImageUrl, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
							<Button
								Grid.Column="1"
								VerticalAlignment="Bottom"
								AutomationProperties.Name="{xaml:Localize Key=Browse}"
								Command="{x:Bind ViewModel.BrowseImagesCommand, Mode=OneWay}"
								Style="{StaticResource MzStandardButtonStyle}">
								<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphFolder}" />
							</Button>
						</Grid>
						<TextBox Header="{xaml:Localize Key=HeroImageAlt}" Text="{x:Bind ViewModel.HeroImageAlt, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

						<TextBlock
							Foreground="{ThemeResource MzTextSecondaryBrush}"
							Style="{StaticResource MzCaptionTextStyle}"
							Text="{xaml:Localize Key=Categories}" />
						<ItemsControl
							ItemTemplate="{StaticResource MzChipTemplate}"
							ItemsPanel="{StaticResource MzChipPanelTemplate}"
							ItemsSource="{x:Bind ViewModel.CategoryChips, Mode=OneWay}"
							Visibility="{x:Bind ViewModel.HasCategories, Mode=OneWay}" />
						<TextBlock
							Foreground="{ThemeResource MzTextTertiaryBrush}"
							Style="{StaticResource MzCaptionTextStyle}"
							Text="{xaml:Localize Key=NoCategoriesSelected}"
							Visibility="{x:Bind ViewModel.HasCategories, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}" />
						<Button
							HorizontalAlignment="Stretch"
							Command="{x:Bind ViewModel.PickCategoriesCommand, Mode=OneWay}"
							Content="{xaml:Localize Key=PickCategories}"
							Style="{StaticResource MzStandardButtonSmallStyle}" />

						<TextBlock
							Foreground="{ThemeResource MzTextSecondaryBrush}"
							Style="{StaticResource MzCaptionTextStyle}"
							Text="{xaml:Localize Key=Tags}" />
						<ItemsControl
							ItemTemplate="{StaticResource MzChipTemplate}"
							ItemsPanel="{StaticResource MzChipPanelTemplate}"
							ItemsSource="{x:Bind ViewModel.TagChips, Mode=OneWay}"
							Visibility="{x:Bind ViewModel.HasTags, Mode=OneWay}" />
						<TextBlock
							Foreground="{ThemeResource MzTextTertiaryBrush}"
							Style="{StaticResource MzCaptionTextStyle}"
							Text="{xaml:Localize Key=NoTagsSelected}"
							Visibility="{x:Bind ViewModel.HasTags, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}" />
						<Grid ColumnDefinitions="*, Auto" ColumnSpacing="{StaticResource MzSpace1}">
							<TextBox
								Header="{xaml:Localize Key=AddTag}"
								Text="{x:Bind ViewModel.NewTagText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}">
								<TextBox.KeyboardAccelerators>
									<KeyboardAccelerator Key="Enter" Invoked="OnAddTagAccelerator" />
								</TextBox.KeyboardAccelerators>
							</TextBox>
							<Button
								Grid.Column="1"
								VerticalAlignment="Bottom"
								AutomationProperties.Name="{xaml:Localize Key=AddTag}"
								Command="{x:Bind ViewModel.AddTagCommand, Mode=OneWay}"
								Style="{StaticResource MzStandardButtonStyle}">
								<FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphAdd}" />
							</Button>
						</Grid>

						<ToggleSwitch Header="{xaml:Localize Key=Published}" IsOn="{x:Bind ViewModel.IsPublished, Mode=TwoWay}" />
						<DatePicker Date="{x:Bind ViewModel.PublishDate, Mode=TwoWay}" Header="{xaml:Localize Key=PublishDate}" />
						<TimePicker Header="{xaml:Localize Key=PublishTime}" Time="{x:Bind ViewModel.PublishTime, Mode=TwoWay}" />
					</StackPanel>
				</ScrollViewer>
			</Expander>

			<VisualStateManager.VisualStateGroups>
				<VisualStateGroup x:Name="MzEditorLayoutStates">
					<VisualState x:Name="Wide">
						<VisualState.StateTriggers>
							<AdaptiveTrigger MinWindowWidth="1280" />
						</VisualState.StateTriggers>
						<VisualState.Setters>
							<Setter Target="PropertiesColumn.Width" Value="320" />
							<Setter Target="PreviewColumn.Width" Value="*" />
							<Setter Target="StackedPropertiesRow.Height" Value="0" />
							<Setter Target="StackedPreviewRow.Height" Value="0" />
							<Setter Target="PropertiesPane.(Grid.Row)" Value="1" />
							<Setter Target="PropertiesPane.(Grid.Column)" Value="2" />
							<Setter Target="PropertiesPane.(Grid.ColumnSpan)" Value="1" />
							<Setter Target="PropertiesPane.IsExpanded" Value="True" />
							<Setter Target="EditorPane.(Grid.Row)" Value="1" />
							<Setter Target="EditorPane.(Grid.Column)" Value="0" />
							<Setter Target="EditorPane.(Grid.ColumnSpan)" Value="1" />
							<Setter Target="PreviewPane.(Grid.Row)" Value="1" />
							<Setter Target="PreviewPane.(Grid.Column)" Value="1" />
							<Setter Target="PreviewPane.(Grid.ColumnSpan)" Value="1" />
						</VisualState.Setters>
					</VisualState>
					<VisualState x:Name="Mid">
						<VisualState.StateTriggers>
							<AdaptiveTrigger MinWindowWidth="768" />
						</VisualState.StateTriggers>
						<VisualState.Setters>
							<Setter Target="PropertiesColumn.Width" Value="0" />
							<Setter Target="PreviewColumn.Width" Value="*" />
							<Setter Target="StackedPropertiesRow.Height" Value="Auto" />
							<Setter Target="StackedPreviewRow.Height" Value="0" />
							<Setter Target="PropertiesPane.(Grid.Row)" Value="0" />
							<Setter Target="PropertiesPane.(Grid.Column)" Value="0" />
							<Setter Target="PropertiesPane.(Grid.ColumnSpan)" Value="3" />
							<Setter Target="PropertiesPane.IsExpanded" Value="False" />
							<Setter Target="EditorPane.(Grid.Row)" Value="1" />
							<Setter Target="EditorPane.(Grid.Column)" Value="0" />
							<Setter Target="EditorPane.(Grid.ColumnSpan)" Value="1" />
							<Setter Target="PreviewPane.(Grid.Row)" Value="1" />
							<Setter Target="PreviewPane.(Grid.Column)" Value="1" />
							<Setter Target="PreviewPane.(Grid.ColumnSpan)" Value="1" />
						</VisualState.Setters>
					</VisualState>
					<VisualState x:Name="Narrow">
						<VisualState.StateTriggers>
							<AdaptiveTrigger MinWindowWidth="0" />
						</VisualState.StateTriggers>
						<VisualState.Setters>
							<Setter Target="PropertiesColumn.Width" Value="0" />
							<Setter Target="PreviewColumn.Width" Value="0" />
							<Setter Target="StackedPropertiesRow.Height" Value="Auto" />
							<Setter Target="StackedPreviewRow.Height" Value="*" />
							<Setter Target="PropertiesPane.(Grid.Row)" Value="0" />
							<Setter Target="PropertiesPane.(Grid.Column)" Value="0" />
							<Setter Target="PropertiesPane.(Grid.ColumnSpan)" Value="3" />
							<Setter Target="PropertiesPane.IsExpanded" Value="False" />
							<Setter Target="EditorPane.(Grid.Row)" Value="1" />
							<Setter Target="EditorPane.(Grid.Column)" Value="0" />
							<Setter Target="EditorPane.(Grid.ColumnSpan)" Value="3" />
							<Setter Target="PreviewPane.(Grid.Row)" Value="2" />
							<Setter Target="PreviewPane.(Grid.Column)" Value="0" />
							<Setter Target="PreviewPane.(Grid.ColumnSpan)" Value="3" />
						</VisualState.Setters>
					</VisualState>
				</VisualStateGroup>
			</VisualStateManager.VisualStateGroups>
		</Grid>
	</Grid>
</local:PostEditorViewBase>
```

- [ ] **Step 2: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Views/Admin/PostEditorView.xaml
git commit -m "feat: Rebuild the post editor with a CommandBar, properties pane and adaptive states"
```

---

### Task 92: `PostEditorView.xaml.cs` — theme-aware preview and the tag accelerator

The preview template bakes the theme into its markup, so the WebView must be re-rendered when the app theme flips; today it never is. The `WindowShell` cast is already gone (Phase 2 Task 1) — this rewrite keeps the locator.

**Files:**
- Modify: `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs:1-87` (full rewrite)

**Interfaces:**
- Consumes: `WindowShellLocator.FindForXamlRoot`, `PostEditorViewModel.AddTagCommand`.
- Produces: `OnAddTagAccelerator` handler referenced by the XAML.

- [ ] **Step 1: Replace the file in full**
```csharp
using Microsoft.UI.Xaml.Input;
using MZikmund.App.Core.Infrastructure;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class PostEditorView : PostEditorViewBase
{
	// Fully qualified: Uno 6.8 added a root-level WebView2 namespace that shadows the simple name.
	private readonly Microsoft.UI.Xaml.Controls.WebView2 _previewWebView;

	private readonly string _postPreviewTemplate;

	private bool _isWebViewReady;

	public PostEditorView()
	{
		InitializeComponent();
		_postPreviewTemplate = typeof(PostEditorView).Assembly.GetManifestResourceStream("MZikmund.App.Templates.PostPreviewTemplate.html")!.ReadToEnd()!;
		PreviewWebViewContainer.Content = _previewWebView = new Microsoft.UI.Xaml.Controls.WebView2();
		Loaded += PostEditorView_Loaded;
		Unloaded += PostEditorView_Unloaded;
	}

	private async void PostEditorView_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			_previewWebView.CoreWebView2Initialized += OnWebViewInitialized;
			await _previewWebView.EnsureCoreWebView2Async();

			if (ViewModel is not null)
			{
				ViewModel.PropertyChanged += ViewModel_PropertyChanged;
				ViewModel.UpdateSelectionRequested += OnUpdateSelectionRequested;
			}

			ActualThemeChanged += OnActualThemeChanged;
		}
		catch (Exception ex)
		{
			var shell = WindowShellLocator.FindForXamlRoot(XamlRoot);
			shell?.ServiceProvider
				.GetRequiredService<ILogger<PostEditorView>>()
				.LogError(ex, "Failed to initialize WebView2 in PostEditorView");
		}
	}

	private void PostEditorView_Unloaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel is not null)
		{
			ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
			ViewModel.UpdateSelectionRequested -= OnUpdateSelectionRequested;
		}

		ActualThemeChanged -= OnActualThemeChanged;
		_previewWebView.CoreWebView2Initialized -= OnWebViewInitialized;
	}

	private void OnActualThemeChanged(FrameworkElement sender, object args) => UpdatePreview();

	private void OnUpdateSelectionRequested(object? sender, EventArgs e) =>
		ContentTextBox.Select(ViewModel!.SelectionStart, ViewModel!.SelectionLength);

	private void OnWebViewInitialized(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2InitializedEventArgs args)
	{
		_isWebViewReady = true;
		UpdatePreview();
	}

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewModel.HtmlPreview))
		{
			UpdatePreview();
		}
	}

	private void ContentTextBox_SelectionChanged(object sender, RoutedEventArgs e) =>
		ViewModel?.SetSelection(ContentTextBox.SelectionStart, ContentTextBox.SelectionLength);

	private void OnAddTagAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		args.Handled = true;
		if (ViewModel?.AddTagCommand.CanExecute(null) == true)
		{
			ViewModel.AddTagCommand.Execute(null);
		}
	}

	private void UpdatePreview()
	{
		if (!_isWebViewReady || ViewModel is null)
		{
			return;
		}

		var preview = _postPreviewTemplate
			.Replace("{POSTCONTENT}", ViewModel.HtmlPreview)
			.Replace("{ACTUALTHEME}", ActualTheme == ElementTheme.Light ? "light" : "dark");
		_previewWebView.NavigateToString(preview);
	}
}

public partial class PostEditorViewBase : PageBase<PostEditorViewModel>
{
}
```

- [ ] **Step 2: Build / Expected: Build succeeded**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Views/Admin/PostEditorView.xaml.cs
git commit -m "refactor: Make the post preview theme-aware and add the tag accelerator"
```

---

### Task 93: Retarget `PostPreviewTemplate.html` to offline redesign tokens

Today the template pulls Bootstrap 5.3.1 and `bootstrap.bundle.min.js` from jsDelivr plus the **live** `https://mzikmund.dev/css/site.min.css` and `site.js` — so the preview is blank offline and shows whatever is deployed to production rather than the redesign. Every rule below is inlined; the file makes zero network requests.

**Files:**
- Modify: `src/app/MZikmund.App/Templates/PostPreviewTemplate.html:1-25` (full rewrite)

**Interfaces:**
- Consumes: `{POSTCONTENT}` and `{ACTUALTHEME}` placeholders written by `PostEditorView.UpdatePreview()`.
- Produces: `data-theme="light|dark"` on `<html>` (was `data-bs-theme`).

- [ ] **Step 1: Replace the file in full**

Colour values are the same tokens as `Theme/Colors.xaml`; the type ramp is the same 13/17/19/20/24/30 steps; the code palette is the web's `_code.scss` pair. Font stacks are system-only — a `WebView2` cannot resolve `ms-appx:///Assets/Fonts/…`, so it falls back to the nearest native Fluent face.
```html
<!DOCTYPE html>
<html lang="en" data-theme="{ACTUALTHEME}">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<style>
:root {
	--mz-bg: #F3F3F3;
	--mz-card: #FFFFFF;
	--mz-text: rgba(0,0,0,.894);
	--mz-text-secondary: rgba(0,0,0,.607);
	--mz-text-tertiary: rgba(0,0,0,.447);
	--mz-stroke: rgba(0,0,0,.078);
	--mz-divider: rgba(0,0,0,.078);
	--mz-accent-text: #005A9E;
	--mz-accent-subtle: rgba(0,120,212,.08);
	--mz-code-bg: #F9F9F9;
	--mz-code-comment: #6a737d;
	--mz-code-keyword: #a626a4;
	--mz-code-string: #50a14f;
	--mz-code-number: #b76b01;
	--mz-code-function: #4078f2;
}
[data-theme="dark"] {
	--mz-bg: #202020;
	--mz-card: #2B2B2B;
	--mz-text: #FFFFFF;
	--mz-text-secondary: rgba(255,255,255,.784);
	--mz-text-tertiary: rgba(255,255,255,.545);
	--mz-stroke: rgba(255,255,255,.071);
	--mz-divider: rgba(255,255,255,.082);
	--mz-accent-text: #76B9ED;
	--mz-accent-subtle: rgba(118,185,237,.102);
	--mz-code-bg: #282828;
	--mz-code-comment: #7f848e;
	--mz-code-keyword: #c578dd;
	--mz-code-string: #97c279;
	--mz-code-number: #d19965;
	--mz-code-function: #61afef;
}
* { box-sizing: border-box; }
html, body { margin: 0; padding: 0; }
body {
	background: var(--mz-bg);
	color: var(--mz-text);
	font-family: "Segoe UI Variable Text", "Segoe UI", system-ui, -apple-system, Roboto, sans-serif;
	font-size: 17px;
	line-height: 1.5;
	-webkit-font-smoothing: antialiased;
}
.post-content {
	max-width: 680px;
	margin: 0 auto;
	padding: 24px 16px 64px;
	font-family: Georgia, Cambria, "Times New Roman", serif;
	font-size: 19px;
	line-height: 1.7;
}
.post-content > *:first-child { margin-top: 0; }
.post-content h1, .post-content h2, .post-content h3,
.post-content h4, .post-content h5, .post-content h6 {
	font-family: "Segoe UI Variable Display", "Segoe UI", system-ui, sans-serif;
	font-weight: 600;
	line-height: 1.15;
	margin: 2rem 0 .75rem;
}
.post-content h1 { font-size: 30px; letter-spacing: -0.022em; }
.post-content h2 { font-size: 24px; letter-spacing: -0.022em; }
.post-content h3 { font-size: 20px; letter-spacing: normal; }
.post-content h4, .post-content h5, .post-content h6 { font-size: 17px; letter-spacing: normal; }
.post-content p { margin: 0 0 1.25rem; }
.post-content a { color: var(--mz-accent-text); text-decoration-thickness: 1px; text-underline-offset: 2px; }
.post-content strong { font-weight: 600; }
.post-content ul, .post-content ol { margin: 0 0 1.25rem; padding-left: 1.5rem; }
.post-content li { margin-bottom: .375rem; }
.post-content hr { border: 0; border-top: 1px solid var(--mz-divider); margin: 2rem 0; }
.post-content blockquote {
	margin: 1.5rem 0;
	padding: .5rem 0 .5rem 1rem;
	border-left: 3px solid var(--mz-accent-text);
	background: var(--mz-accent-subtle);
	color: var(--mz-text-secondary);
}
.post-content blockquote > *:last-child { margin-bottom: 0; }
.post-content img { max-width: 100%; height: auto; border-radius: 8px; display: block; }
.post-content figure { margin: 1.5rem 0; }
.post-content figcaption {
	margin-top: .5rem;
	font-family: "Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif;
	font-size: 13px;
	color: var(--mz-text-tertiary);
	text-align: center;
}
.post-content table {
	width: 100%;
	border-collapse: collapse;
	margin: 1.5rem 0;
	font-family: "Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif;
	font-size: 15px;
}
.post-content th, .post-content td {
	border-bottom: 1px solid var(--mz-divider);
	padding: .5rem .75rem;
	text-align: left;
}
.post-content th { font-weight: 600; }
.post-content code {
	font-family: "Cascadia Mono", Consolas, ui-monospace, monospace;
	font-size: .875em;
	background: var(--mz-accent-subtle);
	border-radius: 4px;
	padding: .125em .375em;
}
.post-content pre {
	background: var(--mz-code-bg);
	border: 1px solid var(--mz-stroke);
	border-radius: 8px;
	padding: 16px;
	overflow-x: auto;
	margin: 1.5rem 0;
	line-height: 1.5;
}
.post-content pre code { background: none; padding: 0; font-size: 14px; }
.post-content .hljs-comment { color: var(--mz-code-comment); font-style: italic; }
.post-content .hljs-keyword { color: var(--mz-code-keyword); }
.post-content .hljs-string { color: var(--mz-code-string); }
.post-content .hljs-number { color: var(--mz-code-number); }
.post-content .hljs-title, .post-content .hljs-function { color: var(--mz-code-function); }
</style>
</head>
<body>
	<article class="post-content">
		{POSTCONTENT}
	</article>
</body>
</html>
```

- [ ] **Step 2: Confirm the template ships no remote reference / Expected: no output**
```bash
grep -n "jsdelivr\|mzikmund.dev\|http://\|https://" src/app/MZikmund.App/Templates/PostPreviewTemplate.html
```

- [ ] **Step 3: Commit**
```bash
git add src/app/MZikmund.App/Templates/PostPreviewTemplate.html
git commit -m "refactor: Inline the post preview stylesheet so the editor preview works offline"
```

---

### Task 94: Verify the editor across breakpoints and themes

**Files:** none created or modified beyond formatting.

**Interfaces:**
- Consumes: everything above.
- Produces: nothing.

- [ ] **Step 1: Format**
```bash
dotnet format MZikmund.slnx
```

- [ ] **Step 2: Build the desktop and Windows heads / Expected: Build succeeded, 0 errors**
```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

- [ ] **Step 3: Confirm no dependent-property Storyboard slipped in / Expected: no output**

Every adaptive change in this view is a `VisualState.Setter`, never a `Storyboard`, so nothing needs `EnableDependentAnimation`. This asserts that:
```bash
grep -n "DoubleAnimation\|ColorAnimation" src/app/MZikmund.App/Views/Admin/PostEditorView.xaml
```

- [ ] **Step 4: Drive the desktop head with the Uno App MCP and check each state**

Run `net10.0-desktop`, navigate Admin → Posts → open a post, and screenshot at three widths in **both** light and dark:
  - **1400 px** — `Wide`: editor | preview | 320px properties column, properties expanded.
  - **1000 px** — `Mid`: properties collapsed to a full-width expander above a side-by-side editor and preview.
  - **600 px** — `Narrow`: properties expander, then editor, then preview, all full width; the preview column collapses to zero.
Confirm in each: the content box renders in JetBrains Mono (glyph widths are uniform), the CommandBar icons are not tofu, the category and tag chips wrap rather than clip, the hero thumbnail shows the image (or the `MzGlyphImageOff` placeholder when the URL is empty), typing a title clears the `TitleRequired` InfoBar and enables Save, and the preview WebView repaints when the app theme is toggled.

- [ ] **Step 5: Commit**
```bash
git add -A
git commit -m "style: Format after the post editor rebuild"
```

---

## Phase 5 — Dialogs, motion, accessibility, localization

# Phase 5 — dialogs, motion pass, accessibility pass, localization sweep

## File structure

**Created**

- `src/app/MZikmund.App/Theme/Status.xaml` — severity brushes (`MzStatusInfo/Warning/Error` + subtle variants) in Light/Default/HighContrast theme dictionaries. Replaces `Colors.DeepSkyBlue`/`DarkOrange`/`DarkRed` picked in the ViewModel.
- `src/app/MZikmund.App/Theme/Accessibility.xaml` — Fluent two-tone focus ring brushes, the dark accent-button foreground flip (spec 10.1), the WinUI key overrides that make both apply to stock controls, and `MzMinTouchTarget` (44).
- `src/app/MZikmund.App/Theme/Styles/Dialogs.xaml` — `MzContentDialogStyle` plus the shared dialog list/combo container styles, the single collapsed one-`TextBlock` item template, and the field label style.
- `src/app/MZikmund.App/Theme/Styles/MediaTiles.xaml` — the de-cloned media tile: one `DataTemplate` body plus one `ToggleButton` style with a size variant, carrying the corrected selection visual state.
- `src/app/MZikmund.App.Core/ViewModels/Items/MediaSelectionTracker.cs` — enforces single selection across tiles whose `IsSelected` is two-way bound to a `ToggleButton`; shared by the dialog and the manager view.

**Modified**

- `src/app/MZikmund.App/App.xaml` — merges the four new dictionaries, in order, after `Theme/Colors.xaml`.
- `src/app/MZikmund.App/Dialogs/DialogBase.cs` — applies `MzElevationLevel.Dialog` to the `BackgroundElement` template part on open, for all six dialogs at once.
- `src/app/MZikmund.App/Dialogs/StatusMessageDialog.xaml` — severity icon and colour driven by `StateTrigger` + theme brushes; adds an opt-in Details expander.
- `src/app/MZikmund.App/Dialogs/Admin/AddOrUpdateCategoryDialog.xaml`, `AddOrUpdateTagDialog.xaml`, `CategoryPickerDialog.xaml`, `ImageVariantsDialog.xaml`, `MediaBrowserDialog.xaml` — shared style, localized `Header="Id"`, automation ids, fixed sizes and dead columns removed.
- `src/app/MZikmund.App/Dialogs/Admin/MediaBrowserDialog.xaml.cs` — the `FileCard_Tapped` handler is deleted.
- `src/app/MZikmund.App.Core/ViewModels/StatusMessageDialogViewModel.cs` — no `Brush`, no glyph; exposes severity as booleans and an optional `Details`.
- `src/app/MZikmund.App.Core/Services/Dialogs/IDialogService.cs`, `DialogService.cs` — `details` parameter added; the hand-built confirmation dialog picks up `MzContentDialogStyle`.
- `src/app/MZikmund.App.Core/ViewModels/Admin/CategoriesManagerViewModel.cs`, `TagsManagerViewModel.cs`, `PostsManagerViewModel.cs`, `MediaBrowserViewModel.cs` — `ILogger<T>` injected; exceptions logged, localized text shown, `ex.Message` only behind the Details expander.
- `src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserDialogViewModel.cs` — owns a `MediaSelectionTracker` that drives `SelectedFile`.
- `src/app/MZikmund.App.Core/ViewModels/Items/StorageItemInfoViewModel.cs` — becomes an `ObservableObject` with `IsSelected`.
- `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml` — consumes the shared tile; actions revealed on selection.
- `src/app/MZikmund.App/Views/BlogView.xaml`, `VideosView.xaml`, `WindowShell.xaml` — `Read more`, `Load More`, `Refresh`, `Watch on YouTube`, `Videos` localized; decorative logo marked `Raw`.
- `src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml`, `CategoriesManagerView.xaml`, `TagsManagerView.xaml`, `PostEditorView.xaml` — automation ids and names.
- `src/app/MZikmund.App/Strings/en/Resources.resw`, `Strings/cs/Resources.resw` — duplicates removed, eight new keys, nineteen Czech keys backfilled to parity.

## Interfaces

**Resource keys produced (XAML)**

- `Theme/Status.xaml`: `MzStatusInfoBrush`, `MzStatusWarningBrush`, `MzStatusErrorBrush`, `MzStatusInfoSubtleBrush`, `MzStatusWarningSubtleBrush`, `MzStatusErrorSubtleBrush` — all three theme dictionaries (`Light`, `Default`, `HighContrast`).
- `Theme/Accessibility.xaml`: `MzFocusStrokeOuterBrush`, `MzFocusStrokeInnerBrush`, `MzTextOnAccentHoverBrush`, `MzTextOnAccentPressedBrush`; `x:Double` `MzMinTouchTarget` = 44, `MzFocusVisualPrimaryThickness` = 2, `MzFocusVisualSecondaryThickness` = 1. Overrides the WinUI keys `FocusStrokeColorOuterBrush`, `FocusStrokeColorInnerBrush`, `AccentButtonForeground`, `AccentButtonForegroundPointerOver`, `AccentButtonForegroundPressed`.
- `Theme/Styles/Dialogs.xaml`: `MzContentDialogStyle` (`TargetType="ContentDialog"`), `MzDialogListItemContainerStyle` (`ListViewItem`), `MzDialogComboBoxItemContainerStyle` (`ComboBoxItem`), `MzDialogTextItemTemplate` (`DataTemplate`, binds `DisplayText`), `MzDialogFieldLabelStyle` (`TextBlock`).
- `Theme/Styles/MediaTiles.xaml`: `MzMediaTileBodyTemplate` (`DataTemplate`, classic `Binding` on `IsImage`/`Url`/`FileName`/`FormattedSize`), `MzMediaTileStyle` (`TargetType="ToggleButton"`, 200x200), `MzMediaTileCompactStyle` (BasedOn, 160x180). Named template parts: `TileRoot`, `TileTransform` (`CompositeTransform`), `TileSurface`, `TileContent`, `HoverOverlay`, `SelectionRing`. Visual states, one `CommonStates` group: `Normal`, `PointerOver`, `Pressed`, `Disabled`, `Checked`, `CheckedPointerOver`, `CheckedPressed`, `CheckedDisabled`, `Indeterminate`.

**C# signatures produced**

- `StatusMessageDialogViewModel(StatusMessageDialogType dialogType, string title, string text, string? details = null)`; properties `StatusMessageDialogType DialogType`, `bool IsInformation`, `bool IsWarning`, `bool IsError`, `string Text`, `string? Details`, `bool HasDetails`.
- `IDialogService.ShowStatusMessageAsync(StatusMessageDialogType type, string title, string text, string? details = null)`.
- `StorageItemInfoViewModel : ObservableObject` with `[ObservableProperty] public partial bool IsSelected { get; set; }`.
- `MediaSelectionTracker(Action<StorageItemInfoViewModel?>? selectionChanged = null)` with `void Track(IEnumerable<StorageItemInfoViewModel>)`, `void Reset()`, `void Dispose()`.

**Consumed from earlier phases (hard contract)**

- Phase 1 `Theme/Colors.xaml` brushes: `MzCardBrush`, `MzBgTertiaryBrush`, `MzTextPrimaryBrush`, `MzTextSecondaryBrush`, `MzTextTertiaryBrush`, `MzTextOnAccentBrush`, `MzAccentTextBrush`, `MzAccentFillBrush`, `MzShimmerBrush`, `MzStrokeCardBrush`, `MzStrokeControlStrongBrush`.
- Phase 1 `Theme/Radii.xaml`: `MzCardCornerRadius` (`CornerRadius`, 8).
- Phase 1 `Theme/Motion.xaml` as `x:String`, WinUI-style: `MzDurationFast` `00:00:00.167`, `MzDurationNormal` `00:00:00.250`, `MzDurationSlow` `00:00:00.333`, `MzEaseEntrance` `0,0,0,1`, `MzEaseExit` `1,0,1,1`, `MzEaseStandard` `0.55,0.55,0,1`.
- Phase 1 `Theme/Typography.xaml`: `MzCaptionTextStyle`, `MzBodyTextStyle`.
- Phase 1 `Theme/Styles/Buttons.xaml`: `MzAccentButtonStyle`, `MzStandardButtonStyle`, `MzSubtleButtonStyle`. `MzAccentButtonStyle` **must** resolve its per-state foreground through the WinUI keys `AccentButtonForeground` / `AccentButtonForegroundPointerOver` / `AccentButtonForegroundPressed`, which Task 1 re-points; otherwise the dark hover/pressed contrast fix does not reach it.
- Phase 1 `Theme/Icons.xaml`: `MzIconFontFamily`, and glyph strings `MzGlyphInfo`, `MzGlyphWarning`, `MzGlyphError`, `MzGlyphDocument`, `MzGlyphImage`, `MzGlyphDelete`, `MzGlyphCopy`.
- Phase 1 `Theme/MzElevation.cs`: `MZikmund.Theme.MzElevation.SetLevel(DependencyObject, MzElevationLevel)` and `MZikmund.Theme.MzElevationLevel.Dialog`.
- Existing `Resources/Converters.xaml`: `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter`, `NullToVisibilityConverter`, `EmptyCollectionToVisibilityConverter`, `InverseEmptyCollectionToVisibilityConverter`, `EnumToIntConverter`.
- WinUI/Uno base styles: `DefaultContentDialogStyle`, `DefaultListViewItemStyle`, `DefaultComboBoxItemStyle`.

**Localization keys produced (en + cs)**

`Videos`, `ReadMore`, `Refresh`, `WatchOnYouTube`, `Id`, `Details`, `PickCategories`, `MediaFilter`; plus Czech parity for `AddCategory`, `AddPost`, `AddTag`, `Admin`, `Blog`, `Cancel`, `Contact`, `DisplayName`, `EditCategory`, `EditTag`, `Home`, `Localizations`, `Login`, `Logout`, `NoCategoriesSelected`, `Ok`, `Posts`, `RouteName`, `Settings`.

## Tasks

### Task 95: Status and accessibility theme dictionaries

**Files:**
- Create: `src/app/MZikmund.App/Theme/Status.xaml`
- Create: `src/app/MZikmund.App/Theme/Accessibility.xaml`
- Modify: `src/app/MZikmund.App/App.xaml:6-17`

**Interfaces:**
- Consumes: Phase 1 `Theme/Colors.xaml` keys `MzCardBrush`, `MzBgTertiaryBrush`, `MzTextPrimaryBrush`, `MzTextSecondaryBrush`, `MzTextTertiaryBrush`, `MzAccentFillBrush`, `MzShimmerBrush`, `MzStrokeCardBrush`, `MzTextOnAccentBrush`
- Produces: `MzStatusInfoBrush`, `MzStatusWarningBrush`, `MzStatusErrorBrush`, `MzStatusInfoSubtleBrush`, `MzStatusWarningSubtleBrush`, `MzStatusErrorSubtleBrush`, `MzFocusStrokeOuterBrush`, `MzFocusStrokeInnerBrush`, `MzTextOnAccentHoverBrush`, `MzTextOnAccentPressedBrush`, `MzMinTouchTarget`, `MzFocusVisualPrimaryThickness`, `MzFocusVisualSecondaryThickness`; overrides of `FocusStrokeColorOuterBrush`, `FocusStrokeColorInnerBrush`, `AccentButtonForeground`, `AccentButtonForegroundPointerOver`, `AccentButtonForegroundPressed`

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/Status.xaml`**

```xml
<ResourceDictionary
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<ResourceDictionary.ThemeDictionaries>

		<!--  Severity colours live here, never in a ViewModel.  -->
		<ResourceDictionary x:Key="Light">
			<SolidColorBrush x:Key="MzStatusInfoBrush" Color="#FF005A9E" />
			<SolidColorBrush x:Key="MzStatusWarningBrush" Color="#FF9D5D00" />
			<SolidColorBrush x:Key="MzStatusErrorBrush" Color="#FFC42B1C" />
			<SolidColorBrush x:Key="MzStatusInfoSubtleBrush" Color="#14005A9E" />
			<SolidColorBrush x:Key="MzStatusWarningSubtleBrush" Color="#149D5D00" />
			<SolidColorBrush x:Key="MzStatusErrorSubtleBrush" Color="#14C42B1C" />
		</ResourceDictionary>

		<!--
			Dark. The old ViewModel picked Colors.DarkRed (#8B0000), which sits near
			1.5:1 on the dark card ground. These are the Fluent SystemFillColor* dark values.
		-->
		<ResourceDictionary x:Key="Default">
			<SolidColorBrush x:Key="MzStatusInfoBrush" Color="#FF76B9ED" />
			<SolidColorBrush x:Key="MzStatusWarningBrush" Color="#FFFCE100" />
			<SolidColorBrush x:Key="MzStatusErrorBrush" Color="#FFFF99A4" />
			<SolidColorBrush x:Key="MzStatusInfoSubtleBrush" Color="#1A76B9ED" />
			<SolidColorBrush x:Key="MzStatusWarningSubtleBrush" Color="#1AFCE100" />
			<SolidColorBrush x:Key="MzStatusErrorSubtleBrush" Color="#1AFF99A4" />
		</ResourceDictionary>

		<ResourceDictionary x:Key="HighContrast">
			<StaticResource x:Key="MzStatusInfoBrush" ResourceKey="SystemColorWindowTextColorBrush" />
			<StaticResource x:Key="MzStatusWarningBrush" ResourceKey="SystemColorWindowTextColorBrush" />
			<StaticResource x:Key="MzStatusErrorBrush" ResourceKey="SystemColorWindowTextColorBrush" />
			<StaticResource x:Key="MzStatusInfoSubtleBrush" ResourceKey="SystemColorWindowColorBrush" />
			<StaticResource x:Key="MzStatusWarningSubtleBrush" ResourceKey="SystemColorWindowColorBrush" />
			<StaticResource x:Key="MzStatusErrorSubtleBrush" ResourceKey="SystemColorWindowColorBrush" />
		</ResourceDictionary>

	</ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

- [ ] **Step 2: Create `src/app/MZikmund.App/Theme/Accessibility.xaml`**

```xml
<ResourceDictionary
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<ResourceDictionary.ThemeDictionaries>

		<ResourceDictionary x:Key="Light">
			<!--  Fluent two-tone focus ring: dark outer stroke, light inner halo.  -->
			<SolidColorBrush x:Key="MzFocusStrokeOuterBrush" Color="#E4000000" />
			<SolidColorBrush x:Key="MzFocusStrokeInnerBrush" Color="#B3FFFFFF" />
			<StaticResource x:Key="FocusStrokeColorOuterBrush" ResourceKey="MzFocusStrokeOuterBrush" />
			<StaticResource x:Key="FocusStrokeColorInnerBrush" ResourceKey="MzFocusStrokeInnerBrush" />

			<!--  Light accent fills are dark enough that white holds through every state.  -->
			<SolidColorBrush x:Key="MzTextOnAccentHoverBrush" Color="#FFFFFFFF" />
			<SolidColorBrush x:Key="MzTextOnAccentPressedBrush" Color="#FFFFFFFF" />
			<StaticResource x:Key="AccentButtonForeground" ResourceKey="MzTextOnAccentBrush" />
			<StaticResource x:Key="AccentButtonForegroundPointerOver" ResourceKey="MzTextOnAccentHoverBrush" />
			<StaticResource x:Key="AccentButtonForegroundPressed" ResourceKey="MzTextOnAccentPressedBrush" />
		</ResourceDictionary>

		<ResourceDictionary x:Key="Default">
			<SolidColorBrush x:Key="MzFocusStrokeOuterBrush" Color="#FFFFFFFF" />
			<SolidColorBrush x:Key="MzFocusStrokeInnerBrush" Color="#B3000000" />
			<StaticResource x:Key="FocusStrokeColorOuterBrush" ResourceKey="MzFocusStrokeOuterBrush" />
			<StaticResource x:Key="FocusStrokeColorInnerBrush" ResourceKey="MzFocusStrokeInnerBrush" />

			<!--
				Spec 10.1. The website keeps #FFFFFF over the dark hover fill #429CE3 (2.96:1)
				and the dark active fill #76B9ED (2.11:1). The XAML port flips to black,
				matching WinUI's dark TextOnAccentFillColorPrimary.
			-->
			<SolidColorBrush x:Key="MzTextOnAccentHoverBrush" Color="#FF000000" />
			<SolidColorBrush x:Key="MzTextOnAccentPressedBrush" Color="#FF000000" />
			<StaticResource x:Key="AccentButtonForeground" ResourceKey="MzTextOnAccentBrush" />
			<StaticResource x:Key="AccentButtonForegroundPointerOver" ResourceKey="MzTextOnAccentHoverBrush" />
			<StaticResource x:Key="AccentButtonForegroundPressed" ResourceKey="MzTextOnAccentPressedBrush" />
		</ResourceDictionary>

		<ResourceDictionary x:Key="HighContrast">
			<!--  Only the eight permitted SystemColor* brushes. WinUI's own HighContrast focus values stand.  -->
			<StaticResource x:Key="MzFocusStrokeOuterBrush" ResourceKey="SystemColorWindowTextColorBrush" />
			<StaticResource x:Key="MzFocusStrokeInnerBrush" ResourceKey="SystemColorWindowColorBrush" />
			<StaticResource x:Key="MzTextOnAccentHoverBrush" ResourceKey="SystemColorHighlightTextColorBrush" />
			<StaticResource x:Key="MzTextOnAccentPressedBrush" ResourceKey="SystemColorHighlightTextColorBrush" />
		</ResourceDictionary>

	</ResourceDictionary.ThemeDictionaries>

	<!--  Theme-invariant. 44 is the minimum touch target from spec 10.2.  -->
	<x:Double x:Key="MzMinTouchTarget">44</x:Double>
	<x:Double x:Key="MzFocusVisualPrimaryThickness">2</x:Double>
	<x:Double x:Key="MzFocusVisualSecondaryThickness">1</x:Double>
</ResourceDictionary>
```

- [ ] **Step 3: Merge both dictionaries in `src/app/MZikmund.App/App.xaml`**

Inside `<ResourceDictionary.MergedDictionaries>`, immediately after the line that merges `ms-appx:///Theme/Colors.xaml`, insert these two lines. They must come after `Colors.xaml` so the `AccentButtonForeground*` and `FocusStrokeColor*` overrides win.

```xml
                <ResourceDictionary Source="ms-appx:///Theme/Status.xaml" />
                <ResourceDictionary Source="ms-appx:///Theme/Accessibility.xaml" />
```

- [ ] **Step 4: Verify Phase 1 exposes the keys these dictionaries alias**

```powershell
Select-String -Path "src/app/MZikmund.App/Theme/Colors.xaml" -Pattern 'MzTextOnAccentBrush|MzCardBrush|MzStrokeCardBrush|MzBgTertiaryBrush|MzShimmerBrush|MzAccentFillBrush|MzTextTertiaryBrush|MzTextSecondaryBrush|MzTextPrimaryBrush' | Select-Object -ExpandProperty Line -Unique
```

Expected: every one of the nine keys appears at least once. If `MzTextOnAccentBrush` is missing, add `<SolidColorBrush x:Key="MzTextOnAccentBrush" Color="#FFFFFFFF" />` to the `Light` and `Default` theme dictionaries of `Theme/Colors.xaml`, and `<StaticResource x:Key="MzTextOnAccentBrush" ResourceKey="SystemColorHighlightTextColorBrush" />` to its `HighContrast` dictionary.

- [ ] **Step 5: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors. A `XamlCompiler` error naming an unresolved `Mz*` key means Phase 1's `Colors.xaml` is missing that key; fix it as in Step 4.

- [ ] **Step 6: Commit**

```bash
git add src/app/MZikmund.App/Theme/Status.xaml src/app/MZikmund.App/Theme/Accessibility.xaml src/app/MZikmund.App/App.xaml
git commit -m "feat: Add status severity and accessibility theme dictionaries"
```

---

### Task 96: Shared dialog dictionary and Dialog-level elevation

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/Dialogs.xaml`
- Modify: `src/app/MZikmund.App/App.xaml`
- Modify: `src/app/MZikmund.App/Dialogs/DialogBase.cs:1-39`
- Modify: `src/app/MZikmund.App.Core/Services/Dialogs/DialogService.cs:27-36`

**Interfaces:**
- Consumes: `MzCardBrush`, `MzTextPrimaryBrush`, `MzTextSecondaryBrush`, `MzStrokeCardBrush`, `MzCardCornerRadius`, `MzAccentButtonStyle`, `MzStandardButtonStyle`, `MzBodyTextStyle`, `MzCaptionTextStyle` (Phase 1); `MzMinTouchTarget` (Task 1); `MZikmund.Theme.MzElevation.SetLevel(DependencyObject, MzElevationLevel)` and `MZikmund.Theme.MzElevationLevel.Dialog` (Phase 1); WinUI `DefaultContentDialogStyle`, `DefaultListViewItemStyle`, `DefaultComboBoxItemStyle`
- Produces: `MzContentDialogStyle`, `MzDialogListItemContainerStyle`, `MzDialogComboBoxItemContainerStyle`, `MzDialogTextItemTemplate`, `MzDialogFieldLabelStyle`

- [ ] **Step 1: Create `src/app/MZikmund.App/Theme/Styles/Dialogs.xaml`**

If Phase 1 already created a placeholder at this path, replace its whole contents with this. This file is the authoritative dialog dictionary.

```xml
<ResourceDictionary
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<!--
		One style for all seven dialog surfaces: r8, MzCard ground, MzStrokeCard contour.
		ContentDialog's BackgroundElement template part is a {TemplateBinding} on
		Background / BorderBrush / BorderThickness / CornerRadius, so plain setters reach it.
		Dialog-level elevation is applied to that part in DialogBase, because the
		ContentDialog itself is a full-window overlay and must not carry the shadow.
	-->
	<Style
		x:Key="MzContentDialogStyle"
		BasedOn="{StaticResource DefaultContentDialogStyle}"
		TargetType="ContentDialog">
		<Setter Property="Background" Value="{ThemeResource MzCardBrush}" />
		<Setter Property="Foreground" Value="{ThemeResource MzTextPrimaryBrush}" />
		<Setter Property="BorderBrush" Value="{ThemeResource MzStrokeCardBrush}" />
		<Setter Property="BorderThickness" Value="1" />
		<Setter Property="CornerRadius" Value="{StaticResource MzCardCornerRadius}" />
		<Setter Property="PrimaryButtonStyle" Value="{StaticResource MzAccentButtonStyle}" />
		<Setter Property="SecondaryButtonStyle" Value="{StaticResource MzStandardButtonStyle}" />
		<Setter Property="CloseButtonStyle" Value="{StaticResource MzStandardButtonStyle}" />
	</Style>

	<!--  44px minimum touch target on every dialog list row.  -->
	<Style
		x:Key="MzDialogListItemContainerStyle"
		BasedOn="{StaticResource DefaultListViewItemStyle}"
		TargetType="ListViewItem">
		<Setter Property="MinHeight" Value="{StaticResource MzMinTouchTarget}" />
		<Setter Property="Padding" Value="12,0" />
		<Setter Property="HorizontalContentAlignment" Value="Stretch" />
		<Setter Property="VerticalContentAlignment" Value="Center" />
	</Style>

	<Style
		x:Key="MzDialogComboBoxItemContainerStyle"
		BasedOn="{StaticResource DefaultComboBoxItemStyle}"
		TargetType="ComboBoxItem">
		<Setter Property="MinHeight" Value="{StaticResource MzMinTouchTarget}" />
		<Setter Property="VerticalContentAlignment" Value="Center" />
	</Style>

	<!--
		Replaces the two identical one-TextBlock ImageVariant templates
		(ImageVariantsDialog ListView, MediaBrowserDialog ComboBox).
		CategoryPickerDialog's third one is replaced by DisplayMemberPath="DisplayName",
		because Category exposes DisplayName rather than the DisplayText these two bind.
	-->
	<DataTemplate x:Key="MzDialogTextItemTemplate">
		<TextBlock
			VerticalAlignment="Center"
			Style="{StaticResource MzBodyTextStyle}"
			Text="{Binding DisplayText}"
			TextTrimming="CharacterEllipsis" />
	</DataTemplate>

	<Style
		x:Key="MzDialogFieldLabelStyle"
		BasedOn="{StaticResource MzCaptionTextStyle}"
		TargetType="TextBlock">
		<Setter Property="Foreground" Value="{ThemeResource MzTextSecondaryBrush}" />
		<Setter Property="Margin" Value="0,0,0,4" />
	</Style>
</ResourceDictionary>
```

- [ ] **Step 2: Merge the dialog dictionary in `src/app/MZikmund.App/App.xaml`**

Inside `<ResourceDictionary.MergedDictionaries>`, after the `ms-appx:///Theme/Accessibility.xaml` line, ensure this line is present exactly once. Add it if Phase 1 did not already merge the file.

```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/Dialogs.xaml" />
```

- [ ] **Step 3: Apply Dialog elevation to the dialog card in `src/app/MZikmund.App/Dialogs/DialogBase.cs`**

Replace the file's contents with:

```csharp
using MZikmund.Theme;
using MZikmund.ViewModels;

namespace MZikmund.Dialogs;

public abstract partial class DialogBase<TDialogViewModel> : ContentDialog
	where TDialogViewModel : DialogViewModel
{
	protected DialogBase()
	{
		DataContextChanged += OnDataContextChanged;
		PrimaryButtonClick += OnPrimaryButtonClick;
		SecondaryButtonClick += OnSecondaryButtonClick;
		CloseButtonClick += OnCloseButtonClick;
		Opened += OnOpened;
	}

	public TDialogViewModel? ViewModel { get; private set; }

	private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
	{
		// The visible dialog card is the BackgroundElement part, not the full-window root.
		if (GetTemplateChild("BackgroundElement") is FrameworkElement backgroundElement)
		{
			MzElevation.SetLevel(backgroundElement, MzElevationLevel.Dialog);
		}

		ViewModel?.OnOpened(this);
	}

	private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
		ViewModel?.OnPrimaryButtonClick(sender, args);

	private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
		ViewModel?.OnSecondaryButtonClick(sender, args);

	private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
		ViewModel?.OnCloseButtonClick(sender, args);

	private void OnDataContextChanged(Microsoft.UI.Xaml.DependencyObject sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args) =>
		ViewModel = args.NewValue switch
		{
			TDialogViewModel viewModel => viewModel,
			{ } incompatibleViewModel => throw new InvalidOperationException(
				$"View model has incompatible type {incompatibleViewModel.GetType().Name}. Expected {typeof(TDialogViewModel).Name}"),
			_ => null
		};
}
```

- [ ] **Step 4: Style the hand-built confirmation dialog in `src/app/MZikmund.App.Core/Services/Dialogs/DialogService.cs`**

Replace `ShowConfirmationDialogAsync` (lines 27-36) with:

```csharp
	public Task<ContentDialogResult> ShowConfirmationDialogAsync(string title, string text)
	{
		ContentDialog confirmationDialog = new()
		{
			Title = title,
			Content = text,
			PrimaryButtonText = Localizer.Instance.GetString("Ok"),
			CloseButtonText = Localizer.Instance.GetString("Cancel"),
			DefaultButton = ContentDialogButton.Close,
			Style = (Style)Application.Current.Resources["MzContentDialogStyle"],
		};

		Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(confirmationDialog, "ConfirmationDialog");

		return _dialogCoordinator.ShowAsync(confirmationDialog);
	}
```

- [ ] **Step 5: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors. `CS0246: MzElevation` means Phase 1's attached property lives in a different namespace; correct the `using` in `DialogBase.cs` to match `Theme/MzElevation.cs`.

- [ ] **Step 6: Commit**

```bash
git add src/app/MZikmund.App/Theme/Styles/Dialogs.xaml src/app/MZikmund.App/App.xaml src/app/MZikmund.App/Dialogs/DialogBase.cs src/app/MZikmund.App.Core/Services/Dialogs/DialogService.cs
git commit -m "feat: Add shared MzContentDialogStyle and dialog elevation"
```

---

### Task 97: Take severity colours and raw exceptions out of the status ViewModel

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/StatusMessageDialogViewModel.cs:1-41`
- Modify: `src/app/MZikmund.App.Core/Services/Dialogs/IDialogService.cs:8-11`
- Modify: `src/app/MZikmund.App.Core/Services/Dialogs/DialogService.cs:18-25`

**Interfaces:**
- Consumes: `MZikmund.Models.Dialogs.StatusMessageDialogType`
- Produces: `StatusMessageDialogViewModel(StatusMessageDialogType, string title, string text, string? details = null)` with `DialogType`, `IsInformation`, `IsWarning`, `IsError`, `Text`, `Details`, `HasDetails`; `IDialogService.ShowStatusMessageAsync(StatusMessageDialogType, string, string, string? details = null)`

- [ ] **Step 1: Replace `src/app/MZikmund.App.Core/ViewModels/StatusMessageDialogViewModel.cs`**

```csharp
using MZikmund.Models.Dialogs;
using MZikmund.ViewModels;

namespace MZikmund.ViewModels.Dialogs;

public class StatusMessageDialogViewModel : DialogViewModel
{
	private readonly string _title;

	public StatusMessageDialogViewModel(
		StatusMessageDialogType dialogType,
		string title,
		string text,
		string? details = null)
	{
		DialogType = dialogType;
		_title = title ?? throw new ArgumentNullException(nameof(title));
		Text = text ?? throw new ArgumentNullException(nameof(text));
		Details = details;
	}

	public override string Title => _title;

	public StatusMessageDialogType DialogType { get; }

	public bool IsInformation => DialogType == StatusMessageDialogType.Information;

	public bool IsWarning => DialogType == StatusMessageDialogType.Warning;

	public bool IsError => DialogType == StatusMessageDialogType.Error;

	public string Text { get; }

	/// <summary>
	/// Optional technical detail. Never a full stack trace - the exception goes to ILogger.
	/// </summary>
	public string? Details { get; }

	public bool HasDetails => !string.IsNullOrWhiteSpace(Details);
}
```

- [ ] **Step 2: Widen the service contract in `src/app/MZikmund.App.Core/Services/Dialogs/IDialogService.cs`**

Replace lines 8-11 with:

```csharp
	Task<ContentDialogResult> ShowStatusMessageAsync(
		StatusMessageDialogType type,
		string title,
		string text,
		string? details = null);
```

- [ ] **Step 3: Pass the detail through in `src/app/MZikmund.App.Core/Services/Dialogs/DialogService.cs`**

Replace lines 18-25 with:

```csharp
	public async Task<ContentDialogResult> ShowStatusMessageAsync(
		StatusMessageDialogType type,
		string title,
		string text,
		string? details = null)
	{
		StatusMessageDialogViewModel statusMessageViewModel = new(type, title, text, details);
		return await ShowAsync(statusMessageViewModel);
	}
```

- [ ] **Step 4: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: one error, `CS1061 'StatusMessageDialogViewModel' does not contain a definition for 'IconForeground'`, from `StatusMessageDialog.xaml`'s generated code. Task 4 fixes it.

- [ ] **Step 5: Commit**

```bash
git add src/app/MZikmund.App.Core/ViewModels/StatusMessageDialogViewModel.cs src/app/MZikmund.App.Core/Services/Dialogs/IDialogService.cs src/app/MZikmund.App.Core/Services/Dialogs/DialogService.cs
git commit -m "refactor: Move status severity out of StatusMessageDialogViewModel"
```

---

### Task 98: Rebuild StatusMessageDialog on theme brushes

**Files:**
- Modify: `src/app/MZikmund.App/Dialogs/StatusMessageDialog.xaml:1-26`

**Interfaces:**
- Consumes: `MzContentDialogStyle` (Task 2); `MzStatusInfoBrush`, `MzStatusWarningBrush`, `MzStatusErrorBrush` (Task 1); `MzBodyTextStyle`, `MzCaptionTextStyle`, `MzIconFontFamily`, `MzGlyphInfo`, `MzGlyphWarning`, `MzGlyphError` (Phase 1); `BoolToVisibilityConverter`
- Produces: nothing consumed downstream

- [ ] **Step 1: Confirm the three status glyph keys exist**

```powershell
Select-String -Path "src/app/MZikmund.App/Theme/Icons.xaml" -Pattern 'MzGlyphInfo|MzGlyphWarning|MzGlyphError|MzIconFontFamily'
```

Expected: four hits. If a glyph key is missing, add it to `Theme/Icons.xaml` as `<x:String x:Key="MzGlyphInfo">&#xE000;</x:String>` using the codepoint `Assets/Fonts/icons.txt` assigns to `ic_fluent_info_24_filled`, `ic_fluent_warning_24_filled` and `ic_fluent_error_circle_24_filled` respectively.

- [ ] **Step 2: Replace `src/app/MZikmund.App/Dialogs/StatusMessageDialog.xaml`**

```xml
<dialogs:StatusMessageDialogBase
	x:Class="MZikmund.Dialogs.StatusMessageDialog"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:dialogs="using:MZikmund.Dialogs"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Title="{x:Bind ViewModel.Title}"
	AutomationProperties.AutomationId="StatusMessageDialog"
	PrimaryButtonText="{xaml:Localize Key=Ok}"
	Style="{StaticResource MzContentDialogStyle}">

	<Grid MaxWidth="440" ColumnSpacing="16">
		<Grid.ColumnDefinitions>
			<ColumnDefinition Width="Auto" />
			<ColumnDefinition Width="*" />
		</Grid.ColumnDefinitions>

		<!--  Decorative: the severity is already carried by the title and body text.  -->
		<FontIcon
			x:Name="SeverityIcon"
			VerticalAlignment="Top"
			AutomationProperties.AccessibilityView="Raw"
			FontFamily="{StaticResource MzIconFontFamily}"
			FontSize="28"
			Foreground="{ThemeResource MzStatusInfoBrush}"
			Glyph="{StaticResource MzGlyphInfo}" />

		<StackPanel Grid.Column="1" Spacing="12">
			<ScrollViewer MaxHeight="200" Padding="0,0,12,0">
				<TextBlock
					IsTextSelectionEnabled="True"
					Style="{StaticResource MzBodyTextStyle}"
					Text="{x:Bind ViewModel.Text}"
					TextWrapping="Wrap" />
			</ScrollViewer>

			<!--  Technical detail is opt-in and never a stack trace.  -->
			<Expander
				x:Name="DetailsExpander"
				HorizontalAlignment="Stretch"
				HorizontalContentAlignment="Stretch"
				AutomationProperties.AutomationId="StatusMessageDetailsExpander"
				Header="{xaml:Localize Key=Details}"
				Visibility="{x:Bind ViewModel.HasDetails, Converter={StaticResource BoolToVisibilityConverter}}">
				<ScrollViewer MaxHeight="160">
					<TextBlock
						IsTextSelectionEnabled="True"
						Style="{StaticResource MzCaptionTextStyle}"
						Text="{x:Bind ViewModel.Details}"
						TextWrapping="Wrap" />
				</ScrollViewer>
			</Expander>
		</StackPanel>

		<VisualStateManager.VisualStateGroups>
			<VisualStateGroup x:Name="SeverityStates">
				<VisualState x:Name="InformationState">
					<VisualState.StateTriggers>
						<StateTrigger IsActive="{x:Bind ViewModel.IsInformation}" />
					</VisualState.StateTriggers>
				</VisualState>
				<VisualState x:Name="WarningState">
					<VisualState.StateTriggers>
						<StateTrigger IsActive="{x:Bind ViewModel.IsWarning}" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="SeverityIcon.Foreground" Value="{ThemeResource MzStatusWarningBrush}" />
						<Setter Target="SeverityIcon.Glyph" Value="{StaticResource MzGlyphWarning}" />
					</VisualState.Setters>
				</VisualState>
				<VisualState x:Name="ErrorState">
					<VisualState.StateTriggers>
						<StateTrigger IsActive="{x:Bind ViewModel.IsError}" />
					</VisualState.StateTriggers>
					<VisualState.Setters>
						<Setter Target="SeverityIcon.Foreground" Value="{ThemeResource MzStatusErrorBrush}" />
						<Setter Target="SeverityIcon.Glyph" Value="{StaticResource MzGlyphError}" />
					</VisualState.Setters>
				</VisualState>
			</VisualStateGroup>
		</VisualStateManager.VisualStateGroups>
	</Grid>
</dialogs:StatusMessageDialogBase>
```

- [ ] **Step 3: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors. The `IconForeground` / `IconGlyph` error from Task 3 Step 4 is gone.

- [ ] **Step 4: Commit**

```bash
git add src/app/MZikmund.App/Dialogs/StatusMessageDialog.xaml
git commit -m "refactor: Drive status dialog severity from theme brushes"
```

---

### Task 99: Log exceptions instead of showing them

**Files:**
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/CategoriesManagerViewModel.cs:1-54`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/TagsManagerViewModel.cs:1-56`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/PostsManagerViewModel.cs:1-137`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserViewModel.cs:141-147,180-186,224-230,233-239,270-276`

**Interfaces:**
- Consumes: `IDialogService.ShowStatusMessageAsync(..., string? details = null)` (Task 3); resource keys `CouldNotLoadData`, `ErrorLoadingData`, `UploadFailed`, `ErrorUploadingFile`, `ErrorOpeningFilePicker`, `DeleteFailed`, `ErrorDeletingFile`
- Produces: nothing consumed downstream

- [ ] **Step 1: Add a logger to `CategoriesManagerViewModel`**

In `src/app/MZikmund.App.Core/ViewModels/Admin/CategoriesManagerViewModel.cs`, add `using Microsoft.Extensions.Logging;` as the first using, then replace lines 12-26 with:

```csharp
public partial class CategoriesManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;
	private readonly ILogger<CategoriesManagerViewModel> _logger;

	public CategoriesManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator,
		ILogger<CategoriesManagerViewModel> logger)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
```

- [ ] **Step 2: Replace the `CategoriesManagerViewModel` catch block**

Replace lines 47-53 (the `catch (Exception ex)` in `RefreshListAsync`) with:

```csharp
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load categories");
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				Localizer.Instance.GetString("ErrorLoadingData"),
				ex.Message);
		}
```

- [ ] **Step 3: Apply the same change to `TagsManagerViewModel`**

In `src/app/MZikmund.App.Core/ViewModels/Admin/TagsManagerViewModel.cs`, add `using Microsoft.Extensions.Logging;` as the first using, add the field `private readonly ILogger<TagsManagerViewModel> _logger;` after `_api`, add the constructor parameter `ILogger<TagsManagerViewModel> logger` after `loadingIndicator` with `_logger = logger ?? throw new ArgumentNullException(nameof(logger));` in the body, then replace lines 49-55 with:

```csharp
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load tags");
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				Localizer.Instance.GetString("ErrorLoadingData"),
				ex.Message);
		}
```

- [ ] **Step 4: Apply the same change to `PostsManagerViewModel`**

In `src/app/MZikmund.App.Core/ViewModels/Admin/PostsManagerViewModel.cs`, add `using Microsoft.Extensions.Logging;` as the first using, add the field `private readonly ILogger<PostsManagerViewModel> _logger;` after `_api`, add the constructor parameter `ILogger<PostsManagerViewModel> logger` after `navigationService` with `_logger = logger ?? throw new ArgumentNullException(nameof(logger));` in the body, then replace **both** catch blocks (lines 91-97 in `RefreshListAsync` and lines 126-132 in `LoadMoreAsync`) with:

```csharp
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load posts");
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				Localizer.Instance.GetString("ErrorLoadingData"),
				ex.Message);
		}
```

- [ ] **Step 5: Add a logger field to `MediaBrowserViewModel`**

In `src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserViewModel.cs`, add `private readonly ILogger<MediaBrowserViewModel> _logger;` after line 27, add the constructor parameter `ILogger<MediaBrowserViewModel> logger` after `imageVariantsLogger` (line 41), and `_logger = logger ?? throw new ArgumentNullException(nameof(logger));` after line 48.

- [ ] **Step 6: Replace the five `MediaBrowserViewModel` catch blocks**

Lines 141-147 and 180-186 become:

```csharp
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load media");
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				Localizer.Instance.GetString("ErrorLoadingData"),
				ex.Message);
		}
```

Lines 224-230 become:

```csharp
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to upload {FileName}", file.Name);
					await _dialogService.ShowStatusMessageAsync(
						StatusMessageDialogType.Error,
						Localizer.Instance.GetString("UploadFailed"),
						Localizer.Instance.GetString("ErrorUploadingFile"),
						ex.Message);
				}
```

Lines 233-239 become:

```csharp
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to open the file picker");
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("UploadFailed"),
				Localizer.Instance.GetString("ErrorOpeningFilePicker"),
				ex.Message);
		}
```

Lines 270-276 become:

```csharp
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to delete {BlobPath}", file.BlobPath);
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("DeleteFailed"),
				Localizer.Instance.GetString("ErrorDeletingFile"),
				ex.Message);
		}
```

- [ ] **Step 7: Confirm no raw exception reaches the UI**

```powershell
Get-ChildItem -Recurse -Filter *.cs src/app/MZikmund.App.Core/ViewModels | Select-String -Pattern '\{ex\}'
```

Expected: no output.

- [ ] **Step 8: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors. `ILogger<T>` resolves from the `UseLogging` registration in `App.xaml.cs`, so no DI change is needed.

- [ ] **Step 9: Commit**

```bash
git add src/app/MZikmund.App.Core/ViewModels/Admin
git commit -m "fix: Log admin exceptions instead of rendering them at the user"
```

---

### Task 100: Restyle and localize the three form dialogs

**Files:**
- Modify: `src/app/MZikmund.App/Dialogs/Admin/AddOrUpdateCategoryDialog.xaml:1-25`
- Modify: `src/app/MZikmund.App/Dialogs/Admin/AddOrUpdateTagDialog.xaml:1-24`
- Modify: `src/app/MZikmund.App/Dialogs/Admin/CategoryPickerDialog.xaml:1-23`

**Interfaces:**
- Consumes: `MzContentDialogStyle`, `MzDialogListItemContainerStyle` (Task 2); `MzStrokeCardBrush`, `MzCardCornerRadius` (Phase 1); resource keys `Id`, `PickCategories`, `Categories` (Task 10 adds `Id` and `PickCategories`)
- Produces: `x:Name="CategoriesListView"` is preserved, so `CategoryPickerDialog.xaml.cs` needs no change

- [ ] **Step 1: Replace `src/app/MZikmund.App/Dialogs/Admin/AddOrUpdateCategoryDialog.xaml`**

```xml
<adminDialogs:AddOrUpdateCategoryDialogBase
	x:Class="MZikmund.Dialogs.Admin.AddOrUpdateCategoryDialog"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:adminDialogs="using:MZikmund.Dialogs.Admin"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Title="{x:Bind ViewModel.Title}"
	AutomationProperties.AutomationId="AddOrUpdateCategoryDialog"
	CloseButtonText="{xaml:Localize Key=Cancel}"
	DefaultButton="Primary"
	PrimaryButtonText="{xaml:Localize Key=Ok}"
	SecondaryButtonText="{x:Bind ViewModel.SecondaryButtonText, Mode=OneWay}"
	Style="{StaticResource MzContentDialogStyle}">

	<StackPanel MinWidth="320" Spacing="12">
		<TextBox
			AutomationProperties.AutomationId="CategoryIdTextBox"
			Header="{xaml:Localize Key=Id}"
			IsEnabled="False"
			IsReadOnly="True"
			Text="{x:Bind ViewModel.Category.Id, Mode=OneWay}"
			Visibility="{x:Bind ViewModel.IsEditing}" />
		<TextBox
			AutomationProperties.AutomationId="CategoryDisplayNameTextBox"
			Header="{xaml:Localize Key=DisplayName}"
			Text="{x:Bind ViewModel.Category.DisplayName, Mode=TwoWay}" />
		<TextBox
			AutomationProperties.AutomationId="CategoryRouteNameTextBox"
			Header="{xaml:Localize Key=RouteName}"
			Text="{x:Bind ViewModel.Category.RouteName, Mode=TwoWay}" />
	</StackPanel>
</adminDialogs:AddOrUpdateCategoryDialogBase>
```

- [ ] **Step 2: Replace `src/app/MZikmund.App/Dialogs/Admin/AddOrUpdateTagDialog.xaml`**

The tag dialog's Id box also gains `IsEnabled="False"` so it matches the category dialog. A read-only box that can never be edited is a keyboard stop for no benefit.

```xml
<adminDialogs:AddOrUpdateTagDialogBase
	x:Class="MZikmund.Dialogs.Admin.AddOrUpdateTagDialog"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:adminDialogs="using:MZikmund.Dialogs.Admin"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Title="{x:Bind ViewModel.Title}"
	AutomationProperties.AutomationId="AddOrUpdateTagDialog"
	CloseButtonText="{xaml:Localize Key=Cancel}"
	DefaultButton="Primary"
	PrimaryButtonText="{xaml:Localize Key=Ok}"
	SecondaryButtonText="{x:Bind ViewModel.SecondaryButtonText, Mode=OneWay}"
	Style="{StaticResource MzContentDialogStyle}">

	<StackPanel MinWidth="320" Spacing="12">
		<TextBox
			AutomationProperties.AutomationId="TagIdTextBox"
			Header="{xaml:Localize Key=Id}"
			IsEnabled="False"
			IsReadOnly="True"
			Text="{x:Bind ViewModel.Tag.Id, Mode=OneWay}"
			Visibility="{x:Bind ViewModel.IsEditing}" />
		<TextBox
			AutomationProperties.AutomationId="TagDisplayNameTextBox"
			Header="{xaml:Localize Key=DisplayName}"
			Text="{x:Bind ViewModel.Tag.DisplayName, Mode=TwoWay}" />
		<TextBox
			AutomationProperties.AutomationId="TagRouteNameTextBox"
			Header="{xaml:Localize Key=RouteName}"
			Text="{x:Bind ViewModel.Tag.RouteName, Mode=TwoWay}" />
	</StackPanel>
</adminDialogs:AddOrUpdateTagDialogBase>
```

- [ ] **Step 3: Replace `src/app/MZikmund.App/Dialogs/Admin/CategoryPickerDialog.xaml`**

The one-`TextBlock` `DataTemplate` goes away entirely. `Category` exposes `DisplayName` rather than the `DisplayText` the other two lists bind, so `DisplayMemberPath` is the honest collapse here, leaving exactly one shared template in `Dialogs.xaml`.

```xml
<local:CategoryPickerDialogBase
	x:Class="MZikmund.Dialogs.Admin.CategoryPickerDialog"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:local="using:MZikmund.Dialogs.Admin"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Title="{xaml:Localize Key=PickCategories}"
	AutomationProperties.AutomationId="CategoryPickerDialog"
	CloseButtonText="{xaml:Localize Key=Cancel}"
	DefaultButton="Primary"
	PrimaryButtonText="{xaml:Localize Key=Save}"
	Style="{StaticResource MzContentDialogStyle}">

	<ListView
		x:Name="CategoriesListView"
		MinWidth="320"
		MaxHeight="320"
		AutomationProperties.AutomationId="CategoryPickerList"
		AutomationProperties.Name="{xaml:Localize Key=Categories}"
		BorderBrush="{ThemeResource MzStrokeCardBrush}"
		BorderThickness="1"
		CornerRadius="{StaticResource MzCardCornerRadius}"
		DisplayMemberPath="DisplayName"
		ItemContainerStyle="{StaticResource MzDialogListItemContainerStyle}"
		ItemsSource="{x:Bind ViewModel.AllCategories, Mode=OneWay}"
		SelectionChanged="ListView_SelectionChanged"
		SelectionMode="Multiple" />
</local:CategoryPickerDialogBase>
```

- [ ] **Step 4: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors. `Id` and `PickCategories` are not in the `.resw` yet, so at runtime they render `???Id???` until Task 10 - which is exactly what `Localizer.GetString` is designed to surface.

- [ ] **Step 5: Commit**

```bash
git add src/app/MZikmund.App/Dialogs/Admin/AddOrUpdateCategoryDialog.xaml src/app/MZikmund.App/Dialogs/Admin/AddOrUpdateTagDialog.xaml src/app/MZikmund.App/Dialogs/Admin/CategoryPickerDialog.xaml
git commit -m "refactor: Restyle and localize the three form dialogs"
```

---

### Task 101: Fix ImageVariantsDialog sizing and the leftover column

**Files:**
- Modify: `src/app/MZikmund.App/Dialogs/Admin/ImageVariantsDialog.xaml:1-78`

**Interfaces:**
- Consumes: `MzContentDialogStyle`, `MzDialogTextItemTemplate`, `MzDialogListItemContainerStyle`, `MzDialogFieldLabelStyle` (Task 2); `MzBgTertiaryBrush`, `MzStrokeCardBrush`, `MzCardCornerRadius`, `MzStandardButtonStyle` (Phase 1)
- Produces: nothing consumed downstream

- [ ] **Step 1: Replace `src/app/MZikmund.App/Dialogs/Admin/ImageVariantsDialog.xaml`**

Three defects go at once: the hard `Width="400"`, the hard `Height="200"` / `Height="150"`, and the item template's `ColumnDefinitions="*, Auto"` whose `Auto` column never held anything.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<local:ImageVariantsDialogBase
	x:Class="MZikmund.App.Dialogs.Admin.ImageVariantsDialog"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:local="using:MZikmund.App.Dialogs.Admin"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Title="{x:Bind ViewModel.FileName, Mode=OneWay}"
	AutomationProperties.AutomationId="ImageVariantsDialog"
	CloseButtonText="{xaml:Localize Key=Close}"
	Style="{StaticResource MzContentDialogStyle}">

	<!--
		No fixed Width or Height. ContentDialog already constrains itself to
		ContentDialogMinWidth 320 / ContentDialogMaxWidth 548, and the preview is
		bounded by MinHeight/MaxHeight so a tall PNG cannot blow the dialog open.
	-->
	<Grid
		MinWidth="360"
		RowDefinitions="Auto,Auto,Auto,Auto"
		RowSpacing="16">

		<Border
			Grid.Row="0"
			MinHeight="160"
			MaxHeight="240"
			Background="{ThemeResource MzBgTertiaryBrush}"
			BorderBrush="{ThemeResource MzStrokeCardBrush}"
			BorderThickness="1"
			CornerRadius="{StaticResource MzCardCornerRadius}">
			<Image
				Margin="8"
				AutomationProperties.Name="{x:Bind ViewModel.FileName, Mode=OneWay}"
				Stretch="Uniform">
				<Image.Source>
					<BitmapImage UriSource="{x:Bind ViewModel.SelectedVariantUrl, Mode=OneWay}" />
				</Image.Source>
			</Image>
		</Border>

		<ProgressRing
			Grid.Row="1"
			HorizontalAlignment="Center"
			AutomationProperties.Name="{xaml:Localize Key=AvailableVariants}"
			IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
			Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />

		<StackPanel
			Grid.Row="2"
			Spacing="8"
			Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}">
			<TextBlock Style="{StaticResource MzDialogFieldLabelStyle}" Text="{xaml:Localize Key=AvailableVariants}" />
			<ListView
				MaxHeight="200"
				AutomationProperties.AutomationId="ImageVariantsList"
				AutomationProperties.Name="{xaml:Localize Key=AvailableVariants}"
				BorderBrush="{ThemeResource MzStrokeCardBrush}"
				BorderThickness="1"
				CornerRadius="{StaticResource MzCardCornerRadius}"
				ItemContainerStyle="{StaticResource MzDialogListItemContainerStyle}"
				ItemTemplate="{StaticResource MzDialogTextItemTemplate}"
				ItemsSource="{x:Bind ViewModel.AvailableVariants, Mode=OneWay}"
				SelectedItem="{x:Bind ViewModel.SelectedVariant, Mode=TwoWay}" />
		</StackPanel>

		<Button
			Grid.Row="3"
			HorizontalAlignment="Stretch"
			AutomationProperties.AutomationId="ImageVariantsCopyUrlButton"
			AutomationProperties.Name="{xaml:Localize Key=CopyUrl}"
			Command="{x:Bind ViewModel.CopyUrlCommand}"
			Content="{xaml:Localize Key=CopyUrl}"
			Style="{StaticResource MzStandardButtonStyle}"
			Visibility="{x:Bind ViewModel.SelectedVariant, Mode=OneWay, Converter={StaticResource NullToVisibilityConverter}}" />
	</Grid>
</local:ImageVariantsDialogBase>
```

- [ ] **Step 2: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/app/MZikmund.App/Dialogs/Admin/ImageVariantsDialog.xaml
git commit -m "fix: Drop fixed sizes and the dead column from ImageVariantsDialog"
```

---

### Task 102: One media tile style with a size variant, and a selection state that works

**Files:**
- Create: `src/app/MZikmund.App/Theme/Styles/MediaTiles.xaml`
- Create: `src/app/MZikmund.App.Core/ViewModels/Items/MediaSelectionTracker.cs`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Items/StorageItemInfoViewModel.cs:1-40`
- Modify: `src/app/MZikmund.App/App.xaml`

**Interfaces:**
- Consumes: `MzCardBrush`, `MzStrokeCardBrush`, `MzBgTertiaryBrush`, `MzShimmerBrush`, `MzAccentFillBrush`, `MzTextSecondaryBrush`, `MzTextTertiaryBrush`, `MzCardCornerRadius`, `MzCaptionTextStyle`, `MzIconFontFamily`, `MzGlyphDocument`, `MzDurationFast`, `MzEaseStandard`, `MzEaseEntrance` (Phase 1); `BoolToVisibilityConverter`, `InverseBoolToVisibilityConverter`
- Produces: `MzMediaTileBodyTemplate`, `MzMediaTileStyle` (ToggleButton, 200x200), `MzMediaTileCompactStyle` (160x180); `StorageItemInfoViewModel.IsSelected`; `MediaSelectionTracker(Action<StorageItemInfoViewModel?>? selectionChanged = null)` with `Track`, `Reset`, `Dispose`

- [ ] **Step 1: Make the item ViewModel observable - replace `src/app/MZikmund.App.Core/ViewModels/Items/StorageItemInfoViewModel.cs`**

```csharp
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blobs;
using MZikmund.DataContracts.Extensions;

namespace MZikmund.ViewModels.Items;

public partial class StorageItemInfoViewModel : ObservableObject
{
	private readonly StorageItemInfo _info;
	private readonly AppConfig _appConfig;

	public StorageItemInfoViewModel(StorageItemInfo info, AppConfig appConfig)
	{
		_info = info;
		_appConfig = appConfig;
	}

	public string FileName => _info.FileName;

	public string Extension => _info.Extension;

	public DateTimeOffset? LastModified => _info.LastModified;

	public string BlobPath => _info.BlobPath;

	public Uri? Url => _info.Url;

	public long Size => _info.Size;

	public string FormattedSize => Size.ToFileSizeString();

	/// <summary>
	/// Two-way bound to the tile's ToggleButton. Single selection is enforced by
	/// <see cref="MediaSelectionTracker"/>, not by the tile.
	/// </summary>
	[ObservableProperty]
	public partial bool IsSelected { get; set; }

	public bool IsImage
	{
		get
		{
			var extension = Extension.ToLowerInvariant();
			return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".svg";
		}
	}
}
```

- [ ] **Step 2: Create `src/app/MZikmund.App.Core/ViewModels/Items/MediaSelectionTracker.cs`**

```csharp
using System.ComponentModel;

namespace MZikmund.ViewModels.Items;

/// <summary>
/// Enforces single selection across a set of media tiles whose IsSelected is driven
/// directly by a two-way bound ToggleButton, and reports the winner to the owning ViewModel.
/// </summary>
public sealed class MediaSelectionTracker : IDisposable
{
	private readonly List<StorageItemInfoViewModel> _tracked = new();
	private readonly Action<StorageItemInfoViewModel?>? _selectionChanged;
	private bool _isUpdating;

	public MediaSelectionTracker(Action<StorageItemInfoViewModel?>? selectionChanged = null)
	{
		_selectionChanged = selectionChanged;
	}

	public void Track(IEnumerable<StorageItemInfoViewModel> items)
	{
		foreach (var item in items)
		{
			item.PropertyChanged += OnItemPropertyChanged;
			_tracked.Add(item);
		}
	}

	public void Reset()
	{
		foreach (var item in _tracked)
		{
			item.PropertyChanged -= OnItemPropertyChanged;
		}

		_tracked.Clear();
	}

	public void Dispose() => Reset();

	private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (_isUpdating ||
			e.PropertyName != nameof(StorageItemInfoViewModel.IsSelected) ||
			sender is not StorageItemInfoViewModel changed)
		{
			return;
		}

		_isUpdating = true;
		try
		{
			if (!changed.IsSelected)
			{
				_selectionChanged?.Invoke(null);
				return;
			}

			foreach (var other in _tracked)
			{
				if (!ReferenceEquals(other, changed))
				{
					other.IsSelected = false;
				}
			}

			_selectionChanged?.Invoke(changed);
		}
		finally
		{
			_isUpdating = false;
		}
	}
}
```

- [ ] **Step 3: Create `src/app/MZikmund.App/Theme/Styles/MediaTiles.xaml`**

```xml
<ResourceDictionary
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<!--
		The one tile body. Previously cloned between MediaBrowserView (200x200)
		and MediaBrowserDialog (160x180). Classic Bindings, not x:Bind: this template
		lives in a loose ResourceDictionary with no x:Class.
	-->
	<DataTemplate x:Key="MzMediaTileBodyTemplate">
		<Grid>
			<Grid.RowDefinitions>
				<RowDefinition Height="*" />
				<RowDefinition Height="Auto" />
			</Grid.RowDefinitions>

			<Border
				Grid.Row="0"
				Background="{ThemeResource MzBgTertiaryBrush}"
				CornerRadius="8,8,0,0">
				<Grid>
					<Image
						Margin="8"
						Stretch="Uniform"
						Visibility="{Binding IsImage, Converter={StaticResource BoolToVisibilityConverter}}">
						<Image.Source>
							<BitmapImage UriSource="{Binding Url}" />
						</Image.Source>
					</Image>

					<FontIcon
						AutomationProperties.AccessibilityView="Raw"
						FontFamily="{StaticResource MzIconFontFamily}"
						FontSize="40"
						Foreground="{ThemeResource MzTextTertiaryBrush}"
						Glyph="{StaticResource MzGlyphDocument}"
						Visibility="{Binding IsImage, Converter={StaticResource InverseBoolToVisibilityConverter}}" />
				</Grid>
			</Border>

			<StackPanel Grid.Row="1" Padding="12,8,12,12">
				<TextBlock
					Style="{StaticResource MzCaptionTextStyle}"
					Text="{Binding FileName}"
					TextTrimming="CharacterEllipsis"
					TextWrapping="NoWrap"
					ToolTipService.ToolTip="{Binding FileName}" />
				<TextBlock
					Foreground="{ThemeResource MzTextSecondaryBrush}"
					Style="{StaticResource MzCaptionTextStyle}"
					Text="{Binding FormattedSize}"
					TextTrimming="CharacterEllipsis" />
			</StackPanel>
		</Grid>
	</DataTemplate>

	<!--
		The corrected selection visual. The old MediaBrowserDialog markup set
		Target="(Border.BorderBrush)" and Target="(Border.BorderThickness)" on a Grid -
		an attached-property target that resolves to nothing - and nothing ever called
		GoToState, so the group was dead twice over. Here the target is a named Border
		inside a real ControlTemplate and ToggleButton itself drives CommonStates.
		Every animated property is Opacity or CompositeTransform.TranslateY, both
		independent, so no EnableDependentAnimation is required.
	-->
	<Style x:Key="MzMediaTileStyle" TargetType="ToggleButton">
		<Setter Property="Width" Value="200" />
		<Setter Property="Height" Value="200" />
		<Setter Property="Padding" Value="0" />
		<Setter Property="Margin" Value="0" />
		<Setter Property="Background" Value="{ThemeResource MzCardBrush}" />
		<Setter Property="BorderBrush" Value="{ThemeResource MzStrokeCardBrush}" />
		<Setter Property="BorderThickness" Value="1" />
		<Setter Property="CornerRadius" Value="{StaticResource MzCardCornerRadius}" />
		<Setter Property="HorizontalContentAlignment" Value="Stretch" />
		<Setter Property="VerticalContentAlignment" Value="Stretch" />
		<Setter Property="UseSystemFocusVisuals" Value="True" />
		<Setter Property="FocusVisualMargin" Value="-2" />
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="ToggleButton">
					<Grid x:Name="TileRoot">
						<Grid.RenderTransform>
							<CompositeTransform x:Name="TileTransform" />
						</Grid.RenderTransform>

						<Border
							x:Name="TileSurface"
							Background="{TemplateBinding Background}"
							BorderBrush="{TemplateBinding BorderBrush}"
							BorderThickness="{TemplateBinding BorderThickness}"
							CornerRadius="{TemplateBinding CornerRadius}" />

						<ContentPresenter
							x:Name="TileContent"
							Padding="{TemplateBinding Padding}"
							HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
							VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"
							AutomationProperties.AccessibilityView="Raw"
							Content="{TemplateBinding Content}"
							ContentTemplate="{TemplateBinding ContentTemplate}" />

						<Border
							x:Name="HoverOverlay"
							Background="{ThemeResource MzShimmerBrush}"
							CornerRadius="{TemplateBinding CornerRadius}"
							IsHitTestVisible="False"
							Opacity="0" />

						<Border
							x:Name="SelectionRing"
							BorderBrush="{ThemeResource MzAccentFillBrush}"
							BorderThickness="2"
							CornerRadius="{TemplateBinding CornerRadius}"
							IsHitTestVisible="False"
							Opacity="0" />

						<VisualStateManager.VisualStateGroups>
							<VisualStateGroup x:Name="CommonStates">
								<VisualState x:Name="Normal" />

								<VisualState x:Name="PointerOver">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="HoverOverlay" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>

								<VisualState x:Name="Pressed">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="HoverOverlay" Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<!--  Fluent presses down 1px at 0ms. Never scales.  -->
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="TileTransform" Storyboard.TargetProperty="TranslateY">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>

								<VisualState x:Name="Disabled">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="TileRoot" Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="0.4" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>

								<VisualState x:Name="Checked">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="SelectionRing" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseEntrance}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>

								<VisualState x:Name="CheckedPointerOver">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="SelectionRing" Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="HoverOverlay" Storyboard.TargetProperty="Opacity">
											<SplineDoubleKeyFrame
												KeySpline="{StaticResource MzEaseStandard}"
												KeyTime="{StaticResource MzDurationFast}"
												Value="1" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>

								<VisualState x:Name="CheckedPressed">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="SelectionRing" Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="HoverOverlay" Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="TileTransform" Storyboard.TargetProperty="TranslateY">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>

								<VisualState x:Name="CheckedDisabled">
									<Storyboard>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="SelectionRing" Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="1" />
										</DoubleAnimationUsingKeyFrames>
										<DoubleAnimationUsingKeyFrames Storyboard.TargetName="TileRoot" Storyboard.TargetProperty="Opacity">
											<DiscreteDoubleKeyFrame KeyTime="0" Value="0.4" />
										</DoubleAnimationUsingKeyFrames>
									</Storyboard>
								</VisualState>

								<VisualState x:Name="Indeterminate" />
							</VisualStateGroup>
						</VisualStateManager.VisualStateGroups>
					</Grid>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>

	<!--  The size variant. The dialog grid is denser than the manager view's.  -->
	<Style
		x:Key="MzMediaTileCompactStyle"
		BasedOn="{StaticResource MzMediaTileStyle}"
		TargetType="ToggleButton">
		<Setter Property="Width" Value="160" />
		<Setter Property="Height" Value="180" />
	</Style>
</ResourceDictionary>
```

- [ ] **Step 4: Merge the tile dictionary in `src/app/MZikmund.App/App.xaml`**

Inside `<ResourceDictionary.MergedDictionaries>`, after the `ms-appx:///Theme/Styles/Dialogs.xaml` line, add:

```xml
                <ResourceDictionary Source="ms-appx:///Theme/Styles/MediaTiles.xaml" />
```

- [ ] **Step 5: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/app/MZikmund.App/Theme/Styles/MediaTiles.xaml src/app/MZikmund.App.Core/ViewModels/Items/MediaSelectionTracker.cs src/app/MZikmund.App.Core/ViewModels/Items/StorageItemInfoViewModel.cs src/app/MZikmund.App/App.xaml
git commit -m "feat: Add one media tile style with a working selection state"
```

---

### Task 103: Wire both media browsers onto the shared tile

**Files:**
- Modify: `src/app/MZikmund.App/Dialogs/Admin/MediaBrowserDialog.xaml:1-202`
- Modify: `src/app/MZikmund.App/Dialogs/Admin/MediaBrowserDialog.xaml.cs:1-29`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserDialogViewModel.cs:20,45,157,197`
- Modify: `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml:62-154`
- Modify: `src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserViewModel.cs:28,134,173`

**Interfaces:**
- Consumes: `MzMediaTileStyle`, `MzMediaTileCompactStyle`, `MzMediaTileBodyTemplate`, `MediaSelectionTracker`, `StorageItemInfoViewModel.IsSelected` (Task 8); `MzContentDialogStyle`, `MzDialogTextItemTemplate`, `MzDialogComboBoxItemContainerStyle` (Task 2); `MzStandardButtonStyle`, `MzSubtleButtonStyle`, `MzBodyTextStyle`, `MzCaptionTextStyle`, `MzTextSecondaryBrush`, `MzTextTertiaryBrush`, `MzIconFontFamily`, `MzGlyphDocument`, `MzGlyphImage`, `MzGlyphDelete`, `MzGlyphCopy` (Phase 1)
- Produces: `MediaBrowserDialog` with no code-behind `Tapped` handler

- [ ] **Step 1: Track selection in `src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserDialogViewModel.cs`**

Add the field after line 20:

```csharp
	private readonly MediaSelectionTracker _selectionTracker;
```

and initialise it at the end of the constructor body, after line 45:

```csharp
		_selectionTracker = new MediaSelectionTracker(selected => SelectedFile = selected);
```

- [ ] **Step 2: Feed the tracker in `MediaBrowserDialogViewModel.LoadFilesAsync` and `LoadMoreAsync`**

In `LoadFilesAsync`, replace line 157 with:

```csharp
					_selectionTracker.Reset();
					MediaFiles = response.Content.Data.Select(i => new StorageItemInfoViewModel(i, _appConfig.Value)).ToList();
					_selectionTracker.Track(MediaFiles);
```

In `LoadMoreAsync`, replace line 197 with:

```csharp
					var newItems = response.Content.Data.Select(i => new StorageItemInfoViewModel(i, _appConfig.Value)).ToList();
					MediaFiles.AddRange(newItems);
					_selectionTracker.Track(newItems);
```

- [ ] **Step 3: Replace `src/app/MZikmund.App/Dialogs/Admin/MediaBrowserDialog.xaml`**

The in-content `TitleTextBlockStyle` heading moves to the dialog's own `Title`, the fixed `Width="400"` goes, `ItemsControl` + `ItemsWrapGrid` becomes `ItemsRepeater` + `UniformGridLayout` (spec 4.1: neither virtualizes on Skia), Load More leaves the scrolled `StackPanel`, and the tile is the shared `ToggleButton`.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<local:MediaBrowserDialogBase
	x:Class="MZikmund.App.Dialogs.Admin.MediaBrowserDialog"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:items="using:MZikmund.ViewModels.Items"
	xmlns:local="using:MZikmund.App.Dialogs.Admin"
	xmlns:xaml="using:MZikmund.Extensions.Xaml"
	Title="{xaml:Localize Key=MediaBrowser}"
	AutomationProperties.AutomationId="MediaBrowserDialog"
	CloseButtonText="{xaml:Localize Key=Cancel}"
	DefaultButton="Primary"
	IsPrimaryButtonEnabled="{x:Bind ViewModel.CanSelect, Mode=OneWay}"
	PrimaryButtonText="{xaml:Localize Key=Select}"
	Style="{StaticResource MzContentDialogStyle}">

	<Grid
		MinWidth="480"
		RowDefinitions="Auto,*,Auto"
		RowSpacing="16">

		<Grid
			Grid.Row="0"
			ColumnDefinitions="Auto,*"
			ColumnSpacing="8">
			<Button
				Grid.Column="0"
				AutomationProperties.AutomationId="MediaBrowserUploadButton"
				AutomationProperties.Name="{xaml:Localize Key=Upload}"
				Command="{x:Bind ViewModel.UploadFileCommand}"
				Content="{xaml:Localize Key=Upload}"
				Style="{StaticResource MzStandardButtonStyle}" />
			<TextBox
				Grid.Column="1"
				AutomationProperties.AutomationId="MediaBrowserSearchBox"
				AutomationProperties.Name="{xaml:Localize Key=Search}"
				PlaceholderText="{xaml:Localize Key=Search}"
				Text="{x:Bind ViewModel.SearchFilter, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
		</Grid>

		<Grid Grid.Row="1" Height="320">
			<ProgressRing
				x:Name="LoadingRing"
				HorizontalAlignment="Center"
				VerticalAlignment="Center"
				AutomationProperties.Name="{xaml:Localize Key=Media}"
				IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />

			<ScrollViewer Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}">
				<ItemsRepeater ItemsSource="{x:Bind ViewModel.FilteredFiles, Mode=OneWay}">
					<ItemsRepeater.Layout>
						<UniformGridLayout
							ItemsStretch="None"
							MinColumnSpacing="12"
							MinItemHeight="180"
							MinItemWidth="160"
							MinRowSpacing="12" />
					</ItemsRepeater.Layout>
					<ItemsRepeater.ItemTemplate>
						<DataTemplate x:DataType="items:StorageItemInfoViewModel">
							<ToggleButton
								AutomationProperties.Name="{x:Bind FileName}"
								Content="{x:Bind}"
								ContentTemplate="{StaticResource MzMediaTileBodyTemplate}"
								IsChecked="{x:Bind IsSelected, Mode=TwoWay}"
								Style="{StaticResource MzMediaTileCompactStyle}" />
						</DataTemplate>
					</ItemsRepeater.ItemTemplate>
				</ItemsRepeater>
			</ScrollViewer>

			<StackPanel
				HorizontalAlignment="Center"
				VerticalAlignment="Center"
				Spacing="12"
				Visibility="{x:Bind ViewModel.FilteredFiles, Mode=OneWay, Converter={StaticResource EmptyCollectionToVisibilityConverter}}">
				<FontIcon
					AutomationProperties.AccessibilityView="Raw"
					FontFamily="{StaticResource MzIconFontFamily}"
					FontSize="40"
					Foreground="{ThemeResource MzTextTertiaryBrush}"
					Glyph="{StaticResource MzGlyphDocument}" />
				<TextBlock
					Foreground="{ThemeResource MzTextSecondaryBrush}"
					Style="{StaticResource MzBodyTextStyle}"
					Text="{xaml:Localize Key=NoFilesFound}"
					TextAlignment="Center" />
			</StackPanel>
		</Grid>

		<Grid
			Grid.Row="2"
			ColumnDefinitions="*,Auto"
			ColumnSpacing="8">
			<StackPanel
				Grid.Column="0"
				Orientation="Horizontal"
				Spacing="8"
				Visibility="{x:Bind ViewModel.SelectedFile, Mode=OneWay, Converter={StaticResource NullToVisibilityConverter}}">
				<TextBlock
					VerticalAlignment="Center"
					Style="{StaticResource MzCaptionTextStyle}"
					Text="{xaml:Localize Key=Size}" />
				<ComboBox
					MinWidth="200"
					AutomationProperties.AutomationId="MediaBrowserVariantComboBox"
					AutomationProperties.Name="{xaml:Localize Key=Size}"
					ItemContainerStyle="{StaticResource MzDialogComboBoxItemContainerStyle}"
					ItemTemplate="{StaticResource MzDialogTextItemTemplate}"
					ItemsSource="{x:Bind ViewModel.AvailableVariants, Mode=OneWay}"
					SelectedItem="{x:Bind ViewModel.SelectedVariant, Mode=TwoWay}"
					Visibility="{x:Bind ViewModel.AvailableVariants, Mode=OneWay, Converter={StaticResource InverseEmptyCollectionToVisibilityConverter}}" />
			</StackPanel>

			<Button
				Grid.Column="1"
				AutomationProperties.AutomationId="MediaBrowserLoadMoreButton"
				Command="{x:Bind ViewModel.LoadMoreCommand}"
				Content="{xaml:Localize Key=LoadMore}"
				IsEnabled="{x:Bind ViewModel.HasMoreItems, Mode=OneWay}"
				Style="{StaticResource MzSubtleButtonStyle}"
				Visibility="{x:Bind ViewModel.HasMoreItems, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />
		</Grid>
	</Grid>
</local:MediaBrowserDialogBase>
```

- [ ] **Step 4: Delete the code-behind tap handler - replace `src/app/MZikmund.App/Dialogs/Admin/MediaBrowserDialog.xaml.cs`**

```csharp
using MZikmund.Dialogs;
using MZikmund.ViewModels.Admin;

namespace MZikmund.App.Dialogs.Admin;

public sealed partial class MediaBrowserDialog : MediaBrowserDialogBase
{
	public MediaBrowserDialog()
	{
		InitializeComponent();
	}
}

public abstract partial class MediaBrowserDialogBase : DialogBase<MediaBrowserDialogViewModel>
{
}
```

- [ ] **Step 5: Track selection in `src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserViewModel.cs`**

Add the field after line 28:

```csharp
	private readonly MediaSelectionTracker _selectionTracker = new();
```

In `RefreshListAsync`, replace line 134 with:

```csharp
					_selectionTracker.Reset();
					MediaFiles.AddRange(response.Content.Data.Select(i => new StorageItemInfoViewModel(i, _appConfig.Value)));
					_selectionTracker.Track(MediaFiles);
```

In `LoadMoreAsync`, replace line 173 with:

```csharp
				var newItems = response.Content.Data.Select(i => new StorageItemInfoViewModel(i, _appConfig.Value)).ToList();
				MediaFiles.AddRange(newItems);
				_selectionTracker.Track(newItems);
```

- [ ] **Step 6: Put the manager view on the same tile - replace `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml` lines 62-154 (the whole `<ItemsControl.ItemTemplate>` element)**

This also delivers the spec's "actions revealed rather than always-on": they appear when the tile is selected, which works on touch as well as hover.

```xml
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="items:StorageItemInfoViewModel">
                            <Grid Margin="8">
                                <ToggleButton
                                    AutomationProperties.Name="{x:Bind FileName}"
                                    Content="{x:Bind}"
                                    ContentTemplate="{StaticResource MzMediaTileBodyTemplate}"
                                    IsChecked="{x:Bind IsSelected, Mode=TwoWay}"
                                    Style="{StaticResource MzMediaTileStyle}" />

                                <!--  Actions ride above the tile so their clicks never reach it.  -->
                                <StackPanel
                                    Margin="8"
                                    HorizontalAlignment="Right"
                                    VerticalAlignment="Top"
                                    Orientation="Horizontal"
                                    Spacing="4"
                                    Visibility="{x:Bind IsSelected, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
                                    <Button
                                        AutomationProperties.AutomationId="MediaViewVariantsButton"
                                        AutomationProperties.Name="{xaml:Localize Key=ViewVariants}"
                                        Command="{Binding ElementName=Root, Path=ViewModel.ShowVariantsCommand}"
                                        CommandParameter="{x:Bind}"
                                        Style="{StaticResource MzSubtleButtonStyle}"
                                        ToolTipService.ToolTip="{xaml:Localize Key=ViewVariants}"
                                        Visibility="{x:Bind IsImage, Converter={StaticResource BoolToVisibilityConverter}}">
                                        <FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphImage}" />
                                    </Button>
                                    <Button
                                        AutomationProperties.AutomationId="MediaDeleteButton"
                                        AutomationProperties.Name="{xaml:Localize Key=Delete}"
                                        Command="{Binding ElementName=Root, Path=ViewModel.DeleteFileCommand}"
                                        CommandParameter="{x:Bind}"
                                        Style="{StaticResource MzSubtleButtonStyle}"
                                        ToolTipService.ToolTip="{xaml:Localize Key=Delete}">
                                        <FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphDelete}" />
                                    </Button>
                                    <Button
                                        AutomationProperties.AutomationId="MediaCopyUrlButton"
                                        AutomationProperties.Name="{xaml:Localize Key=CopyUrl}"
                                        Click="CopyUrl_Click"
                                        Style="{StaticResource MzSubtleButtonStyle}"
                                        Tag="{x:Bind Url}"
                                        ToolTipService.ToolTip="{xaml:Localize Key=CopyUrl}">
                                        <FontIcon FontFamily="{StaticResource MzIconFontFamily}" Glyph="{StaticResource MzGlyphCopy}" />
                                    </Button>
                                </StackPanel>
                            </Grid>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
```

- [ ] **Step 7: Confirm the three action glyph keys exist**

```powershell
Select-String -Path "src/app/MZikmund.App/Theme/Icons.xaml" -Pattern 'MzGlyphImage|MzGlyphDelete|MzGlyphCopy'
```

Expected: three hits. If any is missing, add it to `Theme/Icons.xaml` using the codepoint `Assets/Fonts/icons.txt` assigns to `ic_fluent_image_24_regular`, `ic_fluent_delete_24_regular` and `ic_fluent_copy_24_regular`.

- [ ] **Step 8: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors. `XamlCompiler` complaining that `items` is an undeclared prefix in `MediaBrowserView.xaml` means the `xmlns:items="using:MZikmund.ViewModels.Items"` declaration (line 7) was dropped; restore it.

- [ ] **Step 9: Visually verify selection in the dialog**

Launch the desktop head through the Uno App MCP: call `uno_app_select_solution` with `D:/Personal/mzikmund-worktrees/dev-mazi-appredesign/MZikmund.slnx`, then `uno_discover_tools`, then `uno_execute_tool` to run `MZikmund.App` on `net10.0-desktop`. Log in, open Admin > Posts > edit a post > Browse, and screenshot in **light** and then **dark**.

Expected: clicking a tile draws a 2px accent ring that fades in over ~167ms; clicking a second tile clears the first; Select enables only once a tile is chosen; Tab reaches the tiles and Space toggles them; the two-tone focus ring is visible on both grounds. Before this task the ring never appeared at all.

- [ ] **Step 10: Commit**

```bash
git add src/app/MZikmund.App/Dialogs/Admin/MediaBrowserDialog.xaml src/app/MZikmund.App/Dialogs/Admin/MediaBrowserDialog.xaml.cs src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserDialogViewModel.cs src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml src/app/MZikmund.App.Core/ViewModels/Admin/MediaBrowserViewModel.cs
git commit -m "fix: Make the media browser selection state actually render"
```

---

### Task 104: Localization sweep

**Files:**
- Modify: `src/app/MZikmund.App/Strings/en/Resources.resw:120-315`
- Modify: `src/app/MZikmund.App/Strings/cs/Resources.resw:120-249`
- Modify: `src/app/MZikmund.App/WindowShell.xaml:62`
- Modify: `src/app/MZikmund.App/Views/BlogView.xaml:88-110`
- Modify: `src/app/MZikmund.App/Views/VideosView.xaml:19-24,88-92`

**Interfaces:**
- Consumes: the `{xaml:Localize Key=...}` markup extension (`MZikmund.Extensions.Xaml.Localize`)
- Produces: resource keys `Videos`, `ReadMore`, `Refresh`, `WatchOnYouTube`, `Id`, `Details`, `PickCategories`, `MediaFilter` in **both** `en` and `cs`, plus full `cs` parity with `en`

- [ ] **Step 1: Remove the three duplicate `<data>` entries from `src/app/MZikmund.App/Strings/en/Resources.resw`**

Delete the second `LoadMore` block (lines 243-245), the second `Categories` block (lines 270-272) and the second `Tags` block (lines 273-275). The first occurrence of each stays.

- [ ] **Step 2: Add the eight new keys to `src/app/MZikmund.App/Strings/en/Resources.resw`**

`PickCategories` is already referenced by `CategoryPickerDialog` and has never existed; it renders `???PickCategories???` today. Insert immediately before the closing `</root>`:

```xml
  <data name="Videos" xml:space="preserve">
    <value>Videos</value>
  </data>
  <data name="ReadMore" xml:space="preserve">
    <value>Read more</value>
  </data>
  <data name="Refresh" xml:space="preserve">
    <value>Refresh</value>
  </data>
  <data name="WatchOnYouTube" xml:space="preserve">
    <value>Watch on YouTube</value>
  </data>
  <data name="Id" xml:space="preserve">
    <value>Id</value>
  </data>
  <data name="Details" xml:space="preserve">
    <value>Details</value>
  </data>
  <data name="PickCategories" xml:space="preserve">
    <value>Pick categories</value>
  </data>
  <data name="MediaFilter" xml:space="preserve">
    <value>Media filter</value>
  </data>
```

- [ ] **Step 3: Backfill the nineteen missing keys in `src/app/MZikmund.App/Strings/cs/Resources.resw`**

Insert immediately before the closing `</root>`:

```xml
  <data name="AddCategory" xml:space="preserve">
    <value>Přidat kategorii</value>
  </data>
  <data name="AddPost" xml:space="preserve">
    <value>Přidat příspěvek</value>
  </data>
  <data name="AddTag" xml:space="preserve">
    <value>Přidat štítek</value>
  </data>
  <data name="Admin" xml:space="preserve">
    <value>Správa</value>
  </data>
  <data name="Blog" xml:space="preserve">
    <value>Blog</value>
  </data>
  <data name="Cancel" xml:space="preserve">
    <value>Zrušit</value>
  </data>
  <data name="Contact" xml:space="preserve">
    <value>Kontakt</value>
  </data>
  <data name="DisplayName" xml:space="preserve">
    <value>Zobrazovaný název</value>
  </data>
  <data name="EditCategory" xml:space="preserve">
    <value>Upravit kategorii</value>
  </data>
  <data name="EditTag" xml:space="preserve">
    <value>Upravit štítek</value>
  </data>
  <data name="Home" xml:space="preserve">
    <value>Domů</value>
  </data>
  <data name="Localizations" xml:space="preserve">
    <value>Lokalizace</value>
  </data>
  <data name="Login" xml:space="preserve">
    <value>Přihlásit se</value>
  </data>
  <data name="Logout" xml:space="preserve">
    <value>Odhlásit se</value>
  </data>
  <data name="NoCategoriesSelected" xml:space="preserve">
    <value>Nejsou vybrány žádné kategorie</value>
  </data>
  <data name="Ok" xml:space="preserve">
    <value>OK</value>
  </data>
  <data name="Posts" xml:space="preserve">
    <value>Příspěvky</value>
  </data>
  <data name="RouteName" xml:space="preserve">
    <value>Název cesty</value>
  </data>
  <data name="Settings" xml:space="preserve">
    <value>Nastavení</value>
  </data>
```

- [ ] **Step 4: Add the eight new keys to `src/app/MZikmund.App/Strings/cs/Resources.resw`**

Insert immediately before the closing `</root>`:

```xml
  <data name="Videos" xml:space="preserve">
    <value>Videa</value>
  </data>
  <data name="ReadMore" xml:space="preserve">
    <value>Číst dál</value>
  </data>
  <data name="Refresh" xml:space="preserve">
    <value>Obnovit</value>
  </data>
  <data name="WatchOnYouTube" xml:space="preserve">
    <value>Přehrát na YouTube</value>
  </data>
  <data name="Id" xml:space="preserve">
    <value>Id</value>
  </data>
  <data name="Details" xml:space="preserve">
    <value>Podrobnosti</value>
  </data>
  <data name="PickCategories" xml:space="preserve">
    <value>Vybrat kategorie</value>
  </data>
  <data name="MediaFilter" xml:space="preserve">
    <value>Filtr médií</value>
  </data>
```

- [ ] **Step 5: Localize the Videos nav item in `src/app/MZikmund.App/WindowShell.xaml`**

Replace line 62 with:

```xml
                <mux:NavigationViewItem x:Name="VideosNavigationViewItem" Content="{extensions:Localize Key=Videos}">
```

- [ ] **Step 6: Localize BlogView's two literals**

In `src/app/MZikmund.App/Views/BlogView.xaml`, replace lines 88-94 (the Read More `Border`) with:

```xml
                            <!--  Read More  -->
                            <Border Grid.Row="2" Padding="16,0,16,16">
                                <TextBlock
                                    Foreground="{ThemeResource MzAccentTextBrush}"
                                    Style="{StaticResource MzCaptionTextStyle}"
                                    Text="{xaml:Localize Key=ReadMore}" />
                            </Border>
```

and replace line 109 with:

```xml
                Content="{xaml:Localize Key=LoadMore}"
```

Add `xmlns:xaml="using:MZikmund.Extensions.Xaml"` to the root element if Phase 1 has not already added it.

- [ ] **Step 7: Localize VideosView's two literals**

In `src/app/MZikmund.App/Views/VideosView.xaml`, replace line 24 with:

```xml
			Content="{xaml:Localize Key=Refresh}" />
```

and replace line 91 with:

```xml
									Content="{xaml:Localize Key=WatchOnYouTube}"
```

Add `xmlns:xaml="using:MZikmund.Extensions.Xaml"` to the root element if Phase 3 has not already added it.

- [ ] **Step 8: Audit for any remaining hardcoded literal**

```powershell
Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App |
	Select-String -Pattern '(Content|Text|Header|PlaceholderText|PaneTitle|ToolTipService\.ToolTip)="[A-Z][^"{]*"'
```

Expected: only `PaneTitle="Martin Zikmund"` in `WindowShell.xaml`, which is the brand name and stays. Any other hit is a missed string.

- [ ] **Step 9: Confirm the two resource files carry the same key set**

```powershell
$en = ([xml](Get-Content src/app/MZikmund.App/Strings/en/Resources.resw)).root.data.name
$cs = ([xml](Get-Content src/app/MZikmund.App/Strings/cs/Resources.resw)).root.data.name
"en only: " + (($en | Where-Object { $cs -notcontains $_ }) -join ', ')
"cs only: " + (($cs | Where-Object { $en -notcontains $_ }) -join ', ')
"duplicate en: " + ((($en | Group-Object | Where-Object Count -gt 1).Name) -join ', ')
"duplicate cs: " + ((($cs | Group-Object | Where-Object Count -gt 1).Name) -join ', ')
```

Expected: all four lines print with nothing after the colon.

- [ ] **Step 10: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 11: Commit**

```bash
git add src/app/MZikmund.App/Strings src/app/MZikmund.App/WindowShell.xaml src/app/MZikmund.App/Views/BlogView.xaml src/app/MZikmund.App/Views/VideosView.xaml
git commit -m "feat: Complete en and cs localization coverage"
```

---

### Task 105: Accessibility pass across every view

**Files:**
- Modify: `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml:32-51`
- Modify: `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml:44,70,79,89,122`
- Modify: `src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml:25,68`
- Modify: `src/app/MZikmund.App/Views/Admin/CategoriesManagerView.xaml:24`
- Modify: `src/app/MZikmund.App/Views/Admin/TagsManagerView.xaml:24`
- Modify: `src/app/MZikmund.App/Views/VideosView.xaml:19`
- Modify: `src/app/MZikmund.App/WindowShell.xaml:30-33`

**Interfaces:**
- Consumes: `MzMinTouchTarget`, `MzFocusStrokeOuterBrush`, `MzFocusStrokeInnerBrush` and the `FocusStrokeColorOuterBrush` / `FocusStrokeColorInnerBrush` overrides (Task 1); resource keys `MediaFilter`, `Search`, `Browse`, `InsertImage`, `InsertFile`, `Save`, `Content`, `AddPost`, `AddCategory`, `AddTag`, `Refresh`
- Produces: nothing consumed downstream

- [ ] **Step 1: Name the media toolbar controls in `src/app/MZikmund.App/Views/Admin/MediaBrowserView.xaml`**

Replace lines 44-47 (the `ComboBox` opening tag and its first attributes) with:

```xml
            <ComboBox
                Grid.Column="2"
                MinWidth="150"
                AutomationProperties.AutomationId="MediaFilterComboBox"
                AutomationProperties.Name="{xaml:Localize Key=MediaFilter}"
                SelectedIndex="{x:Bind ViewModel.FilterMode, Mode=TwoWay, Converter={StaticResource EnumToIntConverter}}">
```

Add `AutomationProperties.AutomationId="MediaUploadButton"` to the upload `Button` (line 32), and `AutomationProperties.AutomationId="MediaSearchBox"` plus `AutomationProperties.Name="{xaml:Localize Key=Search}"` to the search `TextBox` (line 39).

- [ ] **Step 2: Name the icon-only controls in `src/app/MZikmund.App/Views/Admin/PostEditorView.xaml`**

Add `AutomationProperties.AutomationId="PostEditorBrowseHeroButton"` and `AutomationProperties.Name="{xaml:Localize Key=Browse}"` to the Browse `Button` (line 44).
Add `AutomationProperties.AutomationId="PostEditorInsertImageButton"` and `AutomationProperties.Name="{xaml:Localize Key=InsertImage}"` to the Insert Image `Button` (line 70).
Add `AutomationProperties.AutomationId="PostEditorInsertFileButton"` and `AutomationProperties.Name="{xaml:Localize Key=InsertFile}"` to the Insert File `Button` (line 79).
Add `AutomationProperties.AutomationId="PostEditorContentTextBox"` and `AutomationProperties.Name="{xaml:Localize Key=Content}"` to the content `TextBox` (line 89).
Add `AutomationProperties.AutomationId="PostEditorSaveButton"` and `AutomationProperties.Name="{xaml:Localize Key=Save}"` to the Save `Button` (line 122).

- [ ] **Step 3: Add automation ids to the three manager views' primary buttons**

In `src/app/MZikmund.App/Views/Admin/PostsManagerView.xaml` add `AutomationProperties.AutomationId="AddPostButton"` and `AutomationProperties.Name="{xaml:Localize Key=AddPost}"` to the `Button` on line 25, and `AutomationProperties.AutomationId="PostsLoadMoreButton"` to the Load More `Button` on line 68.

In `src/app/MZikmund.App/Views/Admin/CategoriesManagerView.xaml` add `AutomationProperties.AutomationId="AddCategoryButton"` and `AutomationProperties.Name="{xaml:Localize Key=AddCategory}"` to the `Button` on line 24.

In `src/app/MZikmund.App/Views/Admin/TagsManagerView.xaml` add `AutomationProperties.AutomationId="AddTagButton"` and `AutomationProperties.Name="{xaml:Localize Key=AddTag}"` to the `Button` on line 24.

- [ ] **Step 4: Add automation ids to VideosView and mark the title-bar logo decorative**

In `src/app/MZikmund.App/Views/VideosView.xaml` add `AutomationProperties.AutomationId="VideosRefreshButton"` and `AutomationProperties.Name="{xaml:Localize Key=Refresh}"` to the refresh `Button` (line 19).

In `src/app/MZikmund.App/WindowShell.xaml` replace lines 30-33 with:

```xml
            <BitmapIcon
                Width="16"
                AutomationProperties.AccessibilityView="Raw"
                ShowAsMonochrome="False"
                UriSource="ms-appx:///Assets/NavigationViewLogo.png" />
```

The logo is decorative next to the title text; marking it `Raw` stops the app name being announced twice.

- [ ] **Step 5: Audit for icon-only controls with no accessible name**

```powershell
Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App |
	Select-String -Pattern '<(Button|HyperlinkButton|ToggleButton|AppBarButton)\b' -Context 0,12 |
	Where-Object { $_.Context.PostContext -match '<(FontIcon|SymbolIcon|BitmapIcon|PathIcon)\b' -and $_.Context.PostContext -notmatch 'AutomationProperties\.Name' }
```

Expected: no output. Every hit is an icon-only control that would be announced as "button" with no label; give it `AutomationProperties.Name="{xaml:Localize Key=...}"` using the key already used for its tooltip.

- [ ] **Step 6: Audit that every `Mz*` brush used in a view has a HighContrast definition**

```powershell
$used = Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App |
	Select-String -Pattern 'ThemeResource (Mz[A-Za-z0-9]+)' -AllMatches |
	ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique

$colors = (Get-Content -Raw src/app/MZikmund.App/Theme/Colors.xaml)
$hcBlock = [regex]::Match($colors, '(?s)x:Key="HighContrast".*?</ResourceDictionary>').Value
$status = (Get-Content -Raw src/app/MZikmund.App/Theme/Status.xaml)
$a11y = (Get-Content -Raw src/app/MZikmund.App/Theme/Accessibility.xaml)

$used | Where-Object { -not ($hcBlock -match [regex]::Escape($_) -or $status -match [regex]::Escape($_) -or $a11y -match [regex]::Escape($_)) }
```

Expected: no output. Any key printed has no HighContrast value and will keep its Light or Dark colour under forced colours. Fix it by adding a `<StaticResource x:Key="…" ResourceKey="…" />` alias to the `HighContrast` dictionary of `Theme/Colors.xaml`, mapping to one of the eight permitted brushes: `SystemColorWindowTextColorBrush`, `SystemColorWindowColorBrush`, `SystemColorButtonFaceColorBrush`, `SystemColorButtonTextColorBrush`, `SystemColorHighlightColorBrush`, `SystemColorHighlightTextColorBrush`, `SystemColorHotlightColorBrush`, `SystemColorGrayTextColorBrush`.

- [ ] **Step 7: Confirm no legacy or misused SystemColor brush survives outside the theme dictionaries**

```powershell
Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App |
	Select-String -Pattern 'SystemColor|SystemControl' |
	Where-Object { $_.Path -notmatch 'Theme\\(Colors|Status|Accessibility)\.xaml' }
```

Expected: no output. `SystemColorHighlightBrush` used as a card background in the three manager views (the defect in spec section 2) and the five `SystemControlForegroundBaseMediumBrush` usages must all be gone by now; anything still printed is a leftover from Phases 1-4.

- [ ] **Step 8: Verify the two-tone focus ring and 44px targets on the desktop head**

Launch through the Uno App MCP (`uno_app_select_solution` with `D:/Personal/mzikmund-worktrees/dev-mazi-appredesign/MZikmund.slnx`, then `uno_discover_tools`, then `uno_execute_tool` to run `MZikmund.App` on `net10.0-desktop`). Tab through the shell, Blog, Videos, Settings and the admin pages in **light** and then **dark**, screenshotting each.

Expected: every focused control shows a 2px outer stroke with a 1px inner halo of the opposite value, legible on the page ground, on cards and on accent fills; no focused control shows a single-tone ring that vanishes against its surface. Dialog list rows and taxonomy rows measure at least 44px tall.

- [ ] **Step 9: Verify the dark accent-button foreground flip**

With the desktop head still running in **dark**, hover and then press the primary button of any dialog (for example Media Browser's Select) and screenshot each state.

Expected: rest is white on `#0078D4`; hover is **black** on `#429CE3`; pressed is **black** on `#76B9ED`. White on those two fills would be 2.96:1 and 2.11:1 and is the website bug spec 10.1 forbids porting.

- [ ] **Step 10: Verify HighContrast on the Windows head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

Run the produced app, press Left Alt + Left Shift + PrtScn to toggle Windows high contrast, and walk the shell, Blog, Videos, Settings, the four admin pages and all six dialogs.

Expected: every surface picks up the system colours, all text stays legible, no elevation shadow is drawn, and focus rings remain visible. Any surface still painting an `Mz*` colour indicates a key missing from the HighContrast dictionary; fix as in Step 6.

- [ ] **Step 11: Commit**

```bash
git add src/app/MZikmund.App/Views src/app/MZikmund.App/WindowShell.xaml src/app/MZikmund.App/Theme
git commit -m "feat: Accessibility pass - automation names, focus ring, touch targets"
```

---

### Task 106: Motion audit

**Files:**
- Modify: any `.xaml` under `src/app/MZikmund.App/` that Step 3 reports

**Interfaces:**
- Consumes: `MzDurationFast`, `MzDurationNormal`, `MzDurationSlow`, `MzEaseEntrance`, `MzEaseExit`, `MzEaseStandard`, `MzStrokeControlStrongBrush` (Phase 1)
- Produces: nothing consumed downstream

- [ ] **Step 1: List every animation in the app**

```powershell
Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App |
	Select-String -Pattern '<(Double|Color|Point|Object)Animation'
```

Record the file and line of every hit. This is the audit surface: Uno silently drops any of these that animates a non-GPU property without `EnableDependentAnimation="True"`.

- [ ] **Step 2: Isolate the dependent ones**

```powershell
Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App |
	Select-String -Pattern '<ColorAnimation|Storyboard\.TargetProperty="[^"]*(Width|Height|Margin|Padding|FontSize|LineHeight|CornerRadius|Thickness|Spacing|Color)[^"]*"'
```

Every hit is a dependent animation. Independent, and therefore never needing the flag: `Opacity`, and any `CompositeTransform` / `TranslateTransform` / `ScaleTransform` / `RotateTransform` sub-property reached through `RenderTransform`.

- [ ] **Step 3: Confirm each dependent animation carries the flag**

```powershell
Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App |
	Select-String -Pattern '<ColorAnimation|Storyboard\.TargetProperty="[^"]*(Width|Height|Margin|Padding|FontSize|LineHeight|CornerRadius|Thickness|Spacing|Color)[^"]*"' -Context 2,2 |
	Where-Object { ($_.Context.PreContext -join ' ') + $_.Line + ($_.Context.PostContext -join ' ') -notmatch 'EnableDependentAnimation="True"' }
```

Expected: no output.

- [ ] **Step 4: Fix any hit from Step 3**

Add `EnableDependentAnimation="True"` to the `Timeline` element itself, not to the `Storyboard`. A size tween becomes:

```xml
<DoubleAnimationUsingKeyFrames
	EnableDependentAnimation="True"
	Storyboard.TargetName="CardSurface"
	Storyboard.TargetProperty="Width">
	<SplineDoubleKeyFrame
		KeySpline="{StaticResource MzEaseStandard}"
		KeyTime="{StaticResource MzDurationNormal}"
		Value="320" />
</DoubleAnimationUsingKeyFrames>
```

and a brush colour tween becomes:

```xml
<ColorAnimation
	EnableDependentAnimation="True"
	Storyboard.TargetName="CardSurface"
	Storyboard.TargetProperty="(Border.BorderBrush).(SolidColorBrush.Color)"
	To="#FF005A9E"
	Duration="{StaticResource MzDurationFast}" />
```

- [ ] **Step 5: Prefer the discrete idiom where a brush is merely swapped**

```powershell
Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App | Select-String -Pattern '<ColorAnimation'
```

For each hit, if the animation only needs to reach the target colour rather than travel to it, replace it with the WinUI idiom, which is never a dependent animation and never silently dropped:

```xml
<ObjectAnimationUsingKeyFrames Storyboard.TargetName="CardSurface" Storyboard.TargetProperty="BorderBrush">
	<DiscreteObjectKeyFrame KeyTime="0" Value="{ThemeResource MzStrokeControlStrongBrush}" />
</ObjectAnimationUsingKeyFrames>
```

Expected after this step: `<ColorAnimation` appears only where a genuine colour tween is intended, and every one of those carries `EnableDependentAnimation="True"`.

- [ ] **Step 6: Confirm no forbidden Uno API crept into a Storyboard**

```powershell
Get-ChildItem -Recurse -Filter *.xaml src/app/MZikmund.App |
	Select-String -Pattern 'ScalarTransition|Vector3Transition|UIElement\.Translation|"Translation"|Storyboard\.TargetProperty="Scale"'
```

Expected: no output. These are `[NotImplemented]` on every Uno target; travel and press must go through `RenderTransform` + `CompositeTransform`.

- [ ] **Step 7: Verify the motion budget on the desktop head**

Run the desktop head through the Uno App MCP and hover a blog card, a media tile and an accent button in **light** and **dark**.

Expected: fill and border state changes land in ~167ms, the card lift and elevation change in ~250ms, image scale and page transitions in ~333ms. Nothing snaps instantly where an animation was authored - a snap is the signature of a dropped dependent animation.

- [ ] **Step 8: Commit**

```bash
git add src/app/MZikmund.App
git commit -m "fix: Mark every dependent VSM animation so Uno does not drop it"
```

---

### Task 107: Full multi-head build, format and close-out

**Files:**
- Modify: any file `dotnet format` rewrites

**Interfaces:**
- Consumes: everything produced by Tasks 1-12
- Produces: nothing

- [ ] **Step 1: Build the Windows head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-windows10.0.26100
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 2: Build the desktop head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-desktop
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 3: Build the Android head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-android
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Build the iOS head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-ios
```

Expected: `Build succeeded`, 0 errors.

- [ ] **Step 5: Build the WASM head**

```bash
dotnet build src/app/MZikmund.App/MZikmund.App.csproj -f net10.0-browserwasm
```

Expected: `Build succeeded`. If it fails inside Uno's native-asset step with a missing Emscripten toolchain, that is the known local gap from spec section 13 and WASM is CI-verified; record the failure and continue. Do not change project files to work around it.

- [ ] **Step 6: Build the whole solution**

```bash
dotnet build MZikmund.slnx
```

Expected: `Build succeeded`, 0 errors across the web, shared, tools and app projects.

- [ ] **Step 7: Screenshot every dialog in both themes**

Run the desktop head through the Uno App MCP (`uno_app_select_solution` with `D:/Personal/mzikmund-worktrees/dev-mazi-appredesign/MZikmund.slnx`, then `uno_discover_tools`, then `uno_execute_tool`). Open, in **light** and then **dark**: a status message dialog of each severity (trigger one by taking the API offline and refreshing Posts), the confirmation dialog (delete a media file, then cancel), Add category, Add tag, Pick categories, Image variants and Media browser.

Expected in both themes: every dialog card is r8 on `MzCard` with a 1px `MzStrokeCard` contour, and the Windows head additionally shows the Dialog-level shadow. The information icon is blue, the warning amber or yellow, the error red or salmon - and the dark error icon is legible, unlike the old `Colors.DarkRed`. The Details expander appears only when a detail was supplied and never contains a stack trace. No dialog is a fixed 400px wide.

- [ ] **Step 8: Format**

```bash
dotnet format MZikmund.slnx
```

Expected: completes with no error. Review `git diff` and keep the changes.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "chore: Format after the Phase 5 dialogs, motion and accessibility pass"
```