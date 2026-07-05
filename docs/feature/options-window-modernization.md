# Options Window Modernization

**Status:** Complete
**Date:** 2026-07-05
**Branch:** master
**Release:** [v1.3.0](https://github.com/PhiLong-Ho/Vimium/releases/tag/v1.3.0)

## Overview

Redesign the Vimium options window with a modern visual style, full keyboard navigability, theme support (Light / Dark / Skadi), and a JSON-backed settings system with live-apply semantics.

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

| # | Requirement | Details | Status |
|---|-------------|---------|--------|
| 1.1 | **Tab-key navigation** | All interactive controls reachable via `Tab` / `Shift+Tab` in logical order | ✅ |
| 1.2 | **Sidebar navigation** | `↑`/`↓` arrow keys navigate between settings pages in the sidebar | ✅ |
| 1.3 | **Default button** | `Enter` activates the default action (Close) from any child control | ✅ |
| 1.4 | **Escape to close** | `Escape` closes the window | ✅ |
| 1.5 | **Access keys** | Mnemonic keys (`Alt+R` for Reset, `Alt+C` for Close) on all labeled controls | ✅ |
| 1.6 | **Focus indicators** | Visible focus rectangles on all controls — never hidden | ✅ |

### 2. Font Size & Localization

| # | Requirement | Details | Status |
|---|-------------|---------|--------|
| 2.1 | **Font size** | ComboBox (8–24pt) with live preview text; persisted to JSON config; auto-save | ✅ |
| 2.2 | **Localization-ready** | All UI strings sourced from `Resources.resx`; zero hardcoded strings in XAML/C# | ❌ |
| 2.3 | **Language selector** | `ComboBox` for UI language — visible but disabled until translations exist | ✅ |
| 2.4 | **Culture-aware defaults** | CJK locales default to larger minimum font size (10pt vs 8pt) | ❌ |
| 2.5 | **Hint background color** | User-configurable active/inactive hint background colors via hex input + 12 preset swatches | ✅ |

### 3. Modernized Layout

| # | Requirement | Details | Status |
|---|-------------|---------|--------|
| 3.1 | **Sidebar navigation** | Left sidebar with icon + label; active page highlighted with accent bar | ✅ |
| 3.2 | **Settings header** | App icon + "Vimium" title at top of sidebar area | ✅ |
| 3.3 | **Card-based sections** | Each settings group rendered as a rounded card with section title | ✅ |
| 3.4 | **Consistent spacing** | 12px window padding, 16px between cards, 8px between label/control | ✅ |
| 3.5 | **Window chrome** | Title bar with icon + title + close; `WindowStartupLocation="CenterOwner"` | ✅ |
| 3.6 | **Footer bar** | Reset-to-defaults + Close buttons, right-aligned | ✅ |
| 3.7 | **Responsive sizing** | 800×600 default; 600×500 min-size; content scrolls if overflow | ✅ |
| 3.8 | **No new dependencies** | Pure WPF — no third-party UI libraries | ✅ |
| 3.9 | **Color picker UX** | Per color: hex text input (`#RRGGBB`) + live color preview swatch + 12 preset color swatches | ✅ |

### 4. Theme System

| # | Requirement | Details | Status |
|---|-------------|---------|--------|
| 4.1 | **Light theme** | Clean light palette (default) | ✅ |
| 4.2 | **Dark theme** | Dark palette for low-light environments | ✅ |
| 4.3 | **Skadi theme** | Arknights Skadi-inspired palette (deep blues, cyan accents) | ✅ |
| 4.4 | **System accent respect** | Accent color reads from `SystemColors` (Light only); Dark uses `#4CA1FF`, Skadi uses `#4FC3F7` | ✅ |
| 4.5 | **Theme persistence** | Selected theme stored in JSON config; applied on app startup | ✅ |
| 4.6 | **Resource dictionary switching** | WPF `ResourceDictionary` swap at runtime via `ApplyTheme()` — no restart needed | ✅ |
| 4.7 | **System color overrides** | Dark/Skadi themes override `SystemColors.WindowBrushKey`, `ControlBrushKey`, etc. so all controls (dropdowns, popups) are themed | ✅ |
| 4.8 | **Implicit control styles** | TextBlock, Label, TextBox, CheckBox, ComboBox, ComboBoxItem, Button, ContextMenu, MenuItem, Separator, ListBoxItem all have themed implicit styles | ✅ |
| 4.9 | **Skadi loading icon** | Small Skadi icon shown next to "Generating hints…" text — only in Skadi theme | ✅ |
| 4.10 | **Overlay theme colors** | Overlay loading/match-string text uses theme brushes instead of hardcoded `White` | ✅ |

### 5. JSON Configuration

| # | Requirement | Details | Status |
|---|-------------|---------|--------|
| 5.1 | **JSON config file** | Store all settings in `%APPDATA%\Vimium\config.json` | ✅ |
| 5.2 | **Typed model** | `VimiumConfig` class with properties for all settings; serialized via `System.Text.Json` | ✅ |
| 5.3 | **Migration** | On first run after upgrade, read old `Settings.settings` FontSize and migrate to JSON | ✅ |
| 5.4 | **Defaults** | Shipped defaults defined in code; missing keys get defaults | ✅ |
| 5.5 | **Auto-save** | Every property change in ConfigService persists to JSON immediately — no explicit Save needed | ✅ |

### 6. Immediate Apply (Live Settings)

| # | Requirement | Details | Status |
|---|-------------|---------|--------|
| 6.1 | **Auto-save on change** | ConfigService.SetProperty calls SaveInternal on every setter | ✅ |
| 6.2 | **Live overlay colors** | OverlayViewModel subscribes to ConfigService changes and exposes `HintActiveBrush`/`HintInactiveBrush`/`HintTextBrush` | ✅ |
| 6.3 | **Live overlay foreground** | OverlayView.xaml HintStyle binds to OverlayViewModel brushes via `RelativeSource AncestorType` | ✅ |
| 6.4 | **No Save button** | Footer has only Reset and Close; all changes auto-save immediately | ✅ |
| 6.5 | **Sub-ViewModel relay** | General/Overlay/Keyboard sub-VMs subscribe to ConfigService.PropertyChanged and relay to their own NotifyOfPropertyChange for live UI updates | ✅ |
| 6.6 | **Live hotkey re-registration** | ShellViewModel subscribes to ConfigService changes and re-registers hotkeys on the fly | ✅ |

### 7. Hotkey Configuration

| # | Requirement | Details | Status |
|---|-------------|---------|--------|
| 7.1 | **Config-backed hotkeys** | `OverlayModifier` and `TaskbarModifier` read from JSON config via `ConfigService` | ✅ |
| 7.2 | **String parsing** | `HotKey.Parse("Ctrl+;")` parses modifier + key into `KeyModifier` and `Keys` enums | ✅ |
| 7.3 | **Supported modifiers** | Ctrl, Alt, Shift, Win (comma-separated combinations) | ✅ |
| 7.4 | **Supported keys** | Letters (A-Z), digits (0-9), and common symbols (; , . / \ [ ] - = ` etc.) | ✅ |
| 7.5 | **Format hint** | Keyboard tab shows hint text explaining the shortcut format | ✅ |
| 7.6 | **Live re-registration** | Changing shortcut in options takes effect immediately via ConfigService subscription | ✅ |

### 8. Project Rename: HuntAndPeck → Vimium

| # | Requirement | Details | Status |
|---|-------------|---------|--------|
| 8.1 | **Folder rename** | `src/HuntAndPeck/` → `src/Vimium/`, `src/HuntAndPeck.Tests/` → `src/Vimium.Tests/` | ✅ |
| 8.2 | **Project files** | `HuntAndPeck.csproj` → `Vimium.csproj`, `HuntAndPeck.Tests.csproj` → `Vimium.Tests.csproj` | ✅ |
| 8.3 | **Solution** | `HuntAndPeck.sln` → `Vimium.sln`, update internal project references | ✅ |
| 8.4 | **Namespace** | All `namespace HuntAndPeck.*` → `namespace Vimium.*` in ~50 C# files | ✅ |
| 8.5 | **XAML** | All `x:Class="HuntAndPeck.*"` → `x:Class="Vimium.*"` in 5 XAML files | ✅ |
| 8.6 | **Assembly name** | Already `Vimium` in csproj — no change needed | ✅ |
| 8.7 | **Docs & scripts** | Update `README.md`, `docs/`, `build.cake`, `register-startup-task.ps1` | ✅ |
| 8.8 | **NativeMethods** | Update `NativeMethods.csproj` reference from `..\HuntAndPeck` to `..\Vimium` | ✅ |
| 8.9 | **InternalsVisibleTo** | Update `[InternalsVisibleTo("HuntAndPeck.Tests")]` → `Vimium.Tests` | ✅ |

## Design

### Window Layout (actual)

```
┌──────────────────────────────────────────────────┐
│  🏠  Vimium Settings                     ✕  │  ← title bar
├──────────┬───────────────────────────────────────┤
│          │                                       │
│  General │  ┌─ Font Size ────────────────────┐  │
│  (active)│  │  [14 ▾]  Preview: The quick…   │  │
│          │  └────────────────────────────────┘  │
│  Overlay │                                       │
│          │  ┌─ Appearance ────────────────────┐  │
│ Keyboard │  │  Theme:  [Light ▾]              │  │
│          │  │  Language: [English ▾] (soon)   │  │
│          │  └────────────────────────────────┘  │
│          │                                       │
│          │  ┌─ Hint Colors ───────────────────┐  │
│          │  │  Active: [#FFFF00] [■]          │  │
│          │  │  Inactive: [#FFFFE0] [■]        │  │
│          │  │  Text: [#000000] [■]            │  │
│          │  │  ■■ ■■ ■■ ■■ ■■ ■■ (presets)   │  │
│          │  └────────────────────────────────┘  │
│          │                                       │
│          │  ┌─ Activation Shortcuts ──────────┐  │
│          │  │  Format: Ctrl+; — modifiers:    │  │
│          │  │  Ctrl, Alt, Shift, Win          │  │
│          │  │  Overlay: [Ctrl+;]              │  │
│          │  │  Taskbar: [Ctrl+']              │  │
│          │  └────────────────────────────────┘  │
├──────────┴───────────────────────────────────────┤
│                       [Reset to defaults] [Close]│  ← footer
└──────────────────────────────────────────────────┘
```

### Color Palette (actual implemented values)

#### Light Theme

| Role | Color | Usage |
|------|-------|-------|
| Background | `#F0F0F0` | Window / page background |
| Card background | `#FFFFFF` | Section cards |
| Text primary | `#1A1A1A` | Labels, section titles |
| Text secondary | `#666666` | Descriptions, hints |
| Border | `#E0E0E0` | Card borders, separators |
| Input background | `#FFFFFF` | ComboBox, TextBox |
| Accent | `SystemColors` | Active nav bar, focus rings, primary button |
| Footer | `#FAFAFA` | Footer bar background |
| Sidebar | `#E6E6E6` | Sidebar background |
| Sidebar active | `#DADADA` | Selected sidebar item |

#### Dark Theme

| Role | Color | Usage |
|------|-------|-------|
| Background | `#1A1A1A` | Window / page background |
| Card background | `#2A2A2A` | Section cards |
| Text primary | `#F0F0F0` | Labels, section titles |
| Text secondary | `#A0A0A0` | Descriptions, hints |
| Border | `#3A3A3A` | Card borders, separators |
| Input background | `#1E1E1E` | ComboBox, TextBox (high contrast vs text) |
| Accent | `#4CA1FF` | Active nav bar, focus rings, primary button |
| Footer | `#202020` | Footer bar background |
| Sidebar | `#202020` | Sidebar background |
| Sidebar active | `#383838` | Selected sidebar item |

#### Skadi Theme (Arknights)

| Role | Color | Usage |
|------|-------|-------|
| Background | `#0D1B2A` | Deep navy background |
| Card background | `#152535` | Navy cards |
| Text primary | `#E8F4FF` | Light blue-white text |
| Text secondary | `#90B8D8` | Muted blue-grey hints |
| Border | `#253A50` | Navy border |
| Input background | `#0F2030` | ComboBox, TextBox (near-black) |
| Accent | `#4FC3F7` | Cyan accent |
| Footer | `#0F1D2D` | Darker navy footer |
| Sidebar | `#0E1C2C` | Sidebar background |
| Sidebar active | `#1C3045` | Selected sidebar item |

### Settings Pages

| Page | Icon | Content |
|------|------|---------|
| **General** | `⚙` | Font size ComboBox + live preview, theme selector, language selector (disabled) |
| **Overlay** | `🖥` | Hint active/inactive/text hex color inputs + preset swatches, animation toggle |
| **Keyboard** | `⌨` | Shortcut format hint, overlay modifier input, taskbar modifier input |

## Files Changed (actual)

| File | Change |
|------|--------|
| `src/Vimium/` (was `HuntAndPeck/`) | **Renamed** — all files within |
| `src/Vimium.Tests/` (was `HuntAndPeck.Tests/`) | **Renamed** — all files within |
| `src/Vimium.sln` (was `HuntAndPeck.sln`) | **Renamed** — internal paths updated |
| `src/Vimium/Vimium.csproj` | Renamed + InternalsVisibleTo |
| `src/Vimium.Tests/Vimium.Tests.csproj` | Renamed + ProjectReference |
| `src/Vimium/Models/VimiumConfig.cs` | **New** — typed config model |
| `src/Vimium/Services/ConfigService.cs` | **New** — load/save/migrate, auto-save, INotifyPropertyChanged |
| `src/Vimium/Services/Interfaces/IKeyListenerService.cs` | Add `HotKey.Parse()` |
| `src/Vimium/Views/OptionsView.xaml` | Full rewrite (sidebar + cards + custom ComboBox template) |
| `src/Vimium/Views/OptionsView.xaml.cs` | Keyboard handlers (Escape, arrow nav) |
| `src/Vimium/Views/Styles.xaml` | **New** — shared control styles + custom templates (ComboBox, ContextMenu, MenuItem) |
| `src/Vimium/Views/OverlayView.xaml` | Dynamic color bindings, loading icon |
| `src/Vimium/Views/OverlayView.xaml.cs` | Skadi loading icon logic |
| `src/Vimium/Views/ShellView.xaml` | Explicit ContextMenu styling |
| `src/Vimium/Converters/HexToColorConverter.cs` | **New** — hex string ↔ SolidColorBrush |
| `src/Vimium/ViewModels/OptionsViewModel.cs` | Refactor — sub-VMs, Close/Reset commands |
| `src/Vimium/ViewModels/GeneralSettingsViewModel.cs` | **New** — FontSize, Theme, Language with ConfigService relay |
| `src/Vimium/ViewModels/OverlaySettingsViewModel.cs` | **New** — hint colors, preset swatches with ConfigService relay |
| `src/Vimium/ViewModels/KeyboardSettingsViewModel.cs` | **New** — modifier shortcuts with ConfigService relay |
| `src/Vimium/ViewModels/ShellViewModel.cs` | ApplyHotkeys from config, ConfigService subscription |
| `src/Vimium/ViewModels/OverlayViewModel.cs` | Dynamic color properties, ConfigService subscription |
| `src/Vimium/ViewModels/HintViewModel.cs` | Read FontSize from ConfigService |
| `src/Vimium/Themes/LightTheme.xaml` | **New** — palette + system color overrides + implicit styles |
| `src/Vimium/Themes/DarkTheme.xaml` | **New** — palette + system color overrides + implicit styles |
| `src/Vimium/Themes/SkadiTheme.xaml` | **New** — palette + system color overrides + implicit styles + Skadi icon |
| `src/Vimium/App.xaml` | Merge Styles.xaml + LightTheme.xaml |
| `src/Vimium/App.xaml.cs` | ApplyTheme() method, ConfigService subscription for theme switching |
| `src/SolutionInfo.cs` | AssemblyProduct → Vimium, InternalsVisibleTo → Vimium.Tests |
| `src/NativeMethods/NativeMethods.csproj` | RootNamespace/AssemblyName → Vimium.NativeMethods |
| `src/build.cake` | Paths updated |
| `README.md` | Paths updated |
| `register-startup-task.ps1` | Path updated |
| `.claude/skills/spec-driven-dev/skill.md` | **New** — development workflow skill |
| `.claude/CLAUDE.md` | Add spec-driven-dev workflow section |
| `src/Vimium.Tests/Models/VimiumConfigTest.cs` | **New** — 6 tests |
| `src/Vimium.Tests/Services/HotKeyTest.cs` | **New** — 11 tests |
| `src/Vimium.Tests/Converters/HexToColorConverterTest.cs` | **New** — 8 tests |
| `CHANGELOG.md` | Add v1.3 entry |
| `docs/feature/options-window-modernization.md` | This file |

## Test Coverage

26 tests across 4 test classes (all passing):

| Class | Tests | Coverage |
|-------|-------|----------|
| `VimiumConfigTest` | 6 | Defaults, JSON roundtrip, edge cases |
| `HotKeyTest` | 11 | All modifier/key combos, invalid input |
| `HexToColorConverterTest` | 8 | Valid/invalid hex, null handling, ConvertBack |
| `HintLabelServiceTest` | 1 | Uniqueness of generated hint strings |

Core logic coverage: `HexToColorConverter` 100%, `HotKey` 97.6%, `HintLabelService` 95.4%, `VimiumConfig` 89.2%.

## Deferred (for future releases)

- §2.2 Localization — strings still hardcoded in XAML/C#; Resources.resx is empty
- §2.3 Language selector functional — needs actual translation files
- §2.4 Culture-aware defaults — CJK locale font-size bump
- Keyboard tab — full custom shortcut binding UI (add/edit/remove actions)
- Interactions tab — per-element-type action configuration
- Clean up old `src/HuntAndPeck/` and `src/HuntAndPeck.Tests/` directories from disk (still present due to file locks at rename time)

## Remaining Work

### Deferred
- §2.2 Localization: Resources.resx still empty — strings hardcoded in XAML
- §2.3 Language selector functional (needs actual translation files)
- §2.4 Culture-aware defaults (CJK locales)
- Keyboard tab → full custom shortcut binding UI (add/edit/remove)
- Interactions tab entirely
- Old `src/HuntAndPeck/` and `src/HuntAndPeck.Tests/` directories still on disk (couldn't delete due to file locks)
