# Options Window Modernization

**Status:** In Progress — Post-implementation fixes  
**Date:** 2026-07-03  
**Branch:** master

## Overview

Redesign the Vimium options window with a modern visual style, full keyboard navigability, theme support (Light / Dark / Skadi), and a JSON-backed settings system. The initial release focuses on core settings: font size, overlay appearance, and customizable keyboard shortcuts.

## Decisions from Review

| Question | Decision |
|----------|----------|
| Accent color | Respect `SystemColors` for accent; Vimium branding via theme selection only |
| Dark mode | **Implement now** — theme system covers Light, Dark, and Skadi |
| Settings storage | **JSON config file** — replace `Settings.settings` / `Settings.Designer.cs` |
| Interactions tab | **Deferred** — focus on General, Overlay, and Keyboard (with shortcut binding) |
| Project rename | **Rename `HuntAndPeck` → `Vimium`** — namespaces, folders, project files, solution, docs |

## Requirements

### 1. Keyboard Navigation

| # | Requirement | Details |
|---|-------------|---------|
| 1.1 | **Tab-key navigation** | All interactive controls reachable via `Tab` / `Shift+Tab` in logical order |
| 1.2 | **Sidebar navigation** | `↑`/`↓` arrow keys navigate between settings pages in the sidebar |
| 1.3 | **Default button** | `Enter` activates the default action (Save) from any child control |
| 1.4 | **Escape to close** | `Escape` closes the window (with unsaved-changes prompt) |
| 1.5 | **Access keys** | Mnemonic keys (`Alt+G` for General, `Alt+S` for Save, etc.) on all labeled controls |
| 1.6 | **Focus indicators** | Visible focus rectangles on all controls — never hidden |

### 2. Font Size & Localization

| # | Requirement | Details |
|---|-------------|---------|
| 2.1 | **Font size** | Slider (8–24pt) with live preview text; persisted to JSON config |
| 2.2 | **Localization-ready** | All UI strings sourced from `Resources.resx`; zero hardcoded strings in XAML/C# |
| 2.3 | **Language selector** | `ComboBox` for UI language — visible but disabled/hidden until translations exist |
| 2.4 | **Culture-aware defaults** | CJK locales default to larger minimum font size (10pt vs 8pt) |
| 2.5 | **Hint background color** | User-configurable active/inactive hint background colors via hex input + preset swatches |

### 3. Modernized Layout

| # | Requirement | Details |
|---|-------------|---------|
| 3.1 | **Sidebar navigation** | Left sidebar with icon + label; active page highlighted with accent bar |
| 3.2 | **Settings header** | App icon + "Vimium Settings" title at top of content area |
| 3.3 | **Card-based sections** | Each settings group rendered as a rounded card with section title |
| 3.4 | **Consistent spacing** | 12px window padding, 16px between cards, 8px between label/control |
| 3.5 | **Window chrome** | Title bar with icon + title + close; `WindowStartupLocation="CenterOwner"` |
| 3.6 | **Footer bar** | Save / Cancel / Reset-to-defaults, right-aligned |
| 3.7 | **Responsive sizing** | 520×420 default; min-size enforced; content scrolls if overflow |
| 3.8 | **No new dependencies** | Pure WPF — no third-party UI libraries |
| 3.9 | **Color picker UX** | Per color: hex text input (`#RRGGBB`) with validation + live color preview swatch + 12 preset color swatches in a row for quick selection |

### 4. Theme System

| # | Requirement | Details |
|---|-------------|---------|
| 4.1 | **Light theme** | Clean light palette (default) |
| 4.2 | **Dark theme** | Dark palette for low-light environments |
| 4.3 | **Skadi theme** | Arknights Skadi-inspired palette (deep blues, cyan accents, dark background) |
| 4.4 | **System accent respect** | Accent color (focus rings, active indicators) reads from `SystemColors.HighlightBrush` |
| 4.5 | **Theme persistence** | Selected theme stored in JSON config; applied on app startup |
| 4.6 | **Resource dictionary switching** | Themes implemented as WPF `ResourceDictionary` swap at runtime — no restart needed |

### 5. JSON Configuration

| # | Requirement | Details |
|---|-------------|---------|
| 5.1 | **JSON config file** | Store all settings in `%APPDATA%\Vimium\config.json` |
| 5.2 | **Typed model** | `VimiumConfig` class with properties for all settings; serialized via `System.Text.Json` |
| 5.3 | **Migration** | On first run after upgrade, read old `Settings.settings` FontSize and migrate to JSON |
| 5.4 | **Defaults** | Shipped defaults defined in code (not in the JSON file); missing keys get defaults |
| 5.5 | **Save semantics** | Save only on explicit user action (click Save); detect unsaved changes for Cancel/close |

### 6. Immediate Apply (Live Settings)

| # | Requirement | Details |
|---|-------------|---------|
| 6.1 | **Auto-save** | Every property change in ConfigService persists to JSON immediately — no explicit Save needed |
| 6.2 | **Live overlay colors** | Overlay hint colors read from ConfigService on every activation and update in real-time via `INotifyPropertyChanged` |
| 6.3 | **Live font size** | Hint font size reflects the current config value each time the overlay is created |
| 6.4 | **No Save button** | Footer has only Reset and Close; all changes auto-save immediately via ConfigService |

### 7. Project Rename: HuntAndPeck → Vimium

| # | Requirement | Details |
|---|-------------|---------|
| 6.1 | **Folder rename** | `src/HuntAndPeck/` → `src/Vimium/`, `src/HuntAndPeck.Tests/` → `src/Vimium.Tests/` |
| 6.2 | **Project files** | `HuntAndPeck.csproj` → `Vimium.csproj`, `HuntAndPeck.Tests.csproj` → `Vimium.Tests.csproj` |
| 6.3 | **Solution** | `HuntAndPeck.sln` → `Vimium.sln`, update internal project references |
| 6.4 | **Namespace** | All `namespace HuntAndPeck.*` → `namespace Vimium.*` in ~50 C# files |
| 6.5 | **XAML** | All `x:Class="HuntAndPeck.*"` → `x:Class="Vimium.*"` in 5 XAML files |
| 6.6 | **Assembly name** | Already `Vimium` in csproj — no change needed |
| 6.7 | **Docs & scripts** | Update `README.md`, `docs/`, `build.cake`, `register-startup-task.ps1` |
| 6.8 | **NativeMethods** | Update `NativeMethods.csproj` reference from `..\HuntAndPeck` to `..\Vimium` |
| 6.9 | **InternalsVisibleTo** | Update `[InternalsVisibleTo("HuntAndPeck.Tests")]` → `Vimium.Tests` |

## Design

### Window Layout

```
┌──────────────────────────────────────────────────┐
│  🏠  Vimium Settings                     ✕  │  ← title bar
├──────────┬───────────────────────────────────────┤
│          │                                       │
│  General │  ┌─ Font Size ────────────────────┐  │
│  (active)│  │  [████████░░░░░]  14pt          │  │
│          │  │  Preview:  The quick brown fox… │  │
│          │  └────────────────────────────────┘  │
│  Overlay │                                       │
│          │  ┌─ Appearance ────────────────────┐  │
│ Keyboard │  │  Theme:  [Light ▾]              │  │
│          │  │  Language:  [English ▾] (soon)  │  │
│          │  └────────────────────────────────┘  │
│          │                                       │
│          │  ┌─ Hint Colors ───────────────────┐  │
│          │  │  Active bg:  [#FFFF00] [■■]     │  │
│          │  │  ■■ ■■ ■■ ■■ ■■ ■■             │  │
│          │  │  ■■ ■■ ■■ ■■ ■■ ■■  (presets)  │  │
│          │  │  Inactive:   [#FFFFE0] [■■]     │  │
│          │  │  Text color: [#000000] [■■]     │  │
│          │  └────────────────────────────────┘  │
│          │                                       │
├──────────┴───────────────────────────────────────┤
│                       [Reset to defaults]  [Close]  │  ← footer
└──────────────────────────────────────────────────┘
```

### Settings Pages

| Page | Icon | Content (this feature) | Future |
|------|------|------------------------|--------|
| **General** | `⚙` | Font size slider + live preview, theme selector, language selector (placeholder) | Startup behavior, update checks |
| **Overlay** | `🖥️` | **Active hint background color** (hex input + 12 preset swatches), inactive hint background color, hint text color, hint font family, animation toggle | Opacity, positioning, per-app rules |
| **Keyboard** | `⌨` | Modifier key(s) for overlay activation, **shortcut list with key-binding UI** (add/edit/remove) | Action binding per element type |

### Color Palette by Theme

#### Light Theme

| Role | Color | Usage |
|------|-------|-------|
| Background | `#F0F0F0` | Window / page background |
| Card background | `#FFFFFF` | Section cards |
| Accent | `SystemColors` | Active nav bar, focus rings, primary button |
| Text primary | `#1A1A1A` | Labels, section titles |
| Text secondary | `#666666` | Descriptions, hints |
| Border | `#E0E0E0` | Card borders, separators |
| Footer | `#FAFAFA` | Footer bar background |

#### Dark Theme

| Role | Color | Usage |
|------|-------|-------|
| Background | `#1E1E1E` | Window / page background |
| Card background | `#2D2D2D` | Section cards |
| Accent | `SystemColors` | Active nav bar, focus rings, primary button |
| Text primary | `#E8E8E8` | Labels, section titles |
| Text secondary | `#999999` | Descriptions, hints |
| Border | `#3E3E3E` | Card borders, separators |
| Footer | `#252525` | Footer bar background |

#### Skadi Theme (Arknights)

| Role | Color | Usage |
|------|-------|-------|
| Background | `#0D1B2A` | Deep navy background |
| Card background | `#1B2838` | Slightly lighter navy cards |
| Accent | `#4FC3F7` | Cyan accent (override SystemColors) |
| Text primary | `#E0F0FF` | Light blue-white text |
| Text secondary | `#7B9DBF` | Muted blue-grey hints |
| Border | `#2A3F55` | Navy border |
| Footer | `#111D2B` | Darker navy footer |
| Highlight | `#FF6B6B` | Subtle red highlight (Skadi's eye-color nod) — used sparingly |

## Implementation Plan

### Phase 0 — Project Rename (prerequisite)

1. **Rename folders** — `src/HuntAndPeck/` → `src/Vimium/`, `src/HuntAndPeck.Tests/` → `src/Vimium.Tests/`
2. **Rename project files** — `HuntAndPeck.csproj` → `Vimium.csproj`, `HuntAndPeck.Tests.csproj` → `Vimium.Tests.csproj`
3. **Rename solution** — `HuntAndPeck.sln` → `Vimium.sln`, update internal paths
4. **Update namespaces** — `HuntAndPeck` → `Vimium` in all `.cs` files
5. **Update XAML** — `x:Class="HuntAndPeck.*"` → `x:Class="Vimium.*"`
6. **Update build/docs** — `build.cake`, `README.md`, `register-startup-task.ps1`, `docs/`
7. **Build & verify** — clean + rebuild both projects, run tests

### Phase 1 — JSON Config + Foundation

8. **Create `VimiumConfig.cs`** — settings model with properties for all current + planned settings; JSON serialization with `System.Text.Json`
9. **Create `ConfigService.cs`** — load/save/migrate from old `Settings.settings`; defaults factory; change detection
10. **Create `Themes/` folder** with three `ResourceDictionary` files: `LightTheme.xaml`, `DarkTheme.xaml`, `SkadiTheme.xaml`
11. **Create `Styles.xaml`** — shared control styles (Button, ComboBox, Slider, TextBlock, Card) that reference theme resources

### Phase 2 — UI Rewrite

12. **Rewrite `OptionsView.xaml`** — sidebar + content layout with the card-based design
13. **Refactor `OptionsViewModel.cs`** — modular sub-viewmodels (GeneralVM, OverlayVM with color properties, KeyboardVM), `ICommand` actions (Save, Cancel, Reset)
14. **Wire up `Resources.resx`** — extract all UI strings; add English strings
15. **Update `App.xaml`** — merge theme dictionaries, set startup theme from config

### Phase 3 — Keyboard Navigation

16. **Add key bindings** — `KeyBinding` for Escape/Enter, `KeyDown` handler on sidebar for arrow-key nav
17. **Add access keys** — `_` mnemonics on all labels and buttons
18. **Focus visuals** — ensure `FocusVisualStyle` is visible and themed

### Phase 4 — Polish

19. **Unsaved changes detection** — `IsDirty` tracking in ViewModel; confirmation dialog on close
20. **Reset-to-defaults** — restore all config properties to shipped defaults
21. **Live font preview** — `TextBlock` bound to same value as slider, updates in real time
22. **Shortcut binding UI** — add/edit/remove shortcut entries in Keyboard page

## Files Changed

| File | Change |
|------|--------|
| `src/Vimium/` (was `HuntAndPeck/`) | **Renamed** — all files within |
| `src/Vimium.Tests/` (was `HuntAndPeck.Tests/`) | **Renamed** — all files within |
| `src/Vimium.sln` (was `HuntAndPeck.sln`) | **Renamed** — internal paths updated |
| `src/Vimium/Vimium.csproj` | **Renamed** + add JSON + theme items |
| `src/Vimium/Models/VimiumConfig.cs` | **New** — typed config model |
| `src/Vimium/Services/ConfigService.cs` | **New** — load/save/migrate logic |
| `src/Vimium/Views/OptionsView.xaml` | Full rewrite |
| `src/Vimium/Views/OptionsView.xaml.cs` | Add keyboard + focus handling |
| `src/Vimium/ViewModels/OptionsViewModel.cs` | Refactor — sub-VMs, commands |
| `src/Vimium/ViewModels/GeneralSettingsViewModel.cs` | **New** |
| `src/Vimium/ViewModels/OverlaySettingsViewModel.cs` | **New** |
| `src/Vimium/ViewModels/KeyboardSettingsViewModel.cs` | **New** |
| `src/Vimium/Views/Styles.xaml` | **New** — shared control styles |
| `src/Vimium/Themes/LightTheme.xaml` | **New** |
| `src/Vimium/Themes/DarkTheme.xaml` | **New** |
| `src/Vimium/Themes/SkadiTheme.xaml` | **New** |
| `src/Vimium/App.xaml` | Merge theme dictionaries |
| `src/Vimium/Properties/Resources.resx` | Add string resources |
| `src/Vimium/Properties/Settings.settings` | **Read-only** after migration (keep for one-time FontSize import) |
| `src/build.cake` | Update paths |
| `README.md` | Update name references |
| `docs/feature/options-window-modernization.md` | This file — keep in sync |
| `register-startup-task.ps1` | Update path references |
| `src/Vimium/ViewModels/OverlayViewModel.cs` | Add dynamic color properties, ConfigService subscription |
| `src/Vimium/Views/OverlayView.xaml` | Replace hardcoded Yellow/LightYellow with ConfigService bindings |
| `.claude/skills/spec-driven-dev/skill.md` | **New** — this development workflow skill |

---

## Post-Implementation Findings

### ✅ Done (all phases committed)

- Phase 0–4 complete: rename, JSON config, themes, styles, modern layout, keyboard nav, access keys
- Auto-save on every property change (ConfigService.SetProperty)
- OverlayViewModel exposes `HintActiveBrush`/`HintInactiveBrush`/`HintTextBrush` from ConfigService
- OverlayView.xaml HintStyle binds to dynamic brushes via RelativeSource

### 🔧 In Progress

- §6 Immediate Apply: code written, needs build verification (Vimium.exe lock preventing rebuild)
- §2.2 Localization: Resources.resx still empty — strings hardcoded in XAML
- §2.3 Language selector: visible but disabled (no translations exist)
- §4.3 Skadi theme: dictionary exists but theme-switching at runtime not yet wired to App.xaml.cs
- §4.6 Theme switching: option to change theme in options window not yet reflected in app (requires App.xaml.cs dictionary swap logic)

### ❌ Deferred

- §2.3 Language selector functional (needs actual translation files)
- Keyboard tab → full shortcut binding UI (placeholder text in place)
- Interactions tab entirely
- Dark mode → Options window itself doesn't switch themes live yet
