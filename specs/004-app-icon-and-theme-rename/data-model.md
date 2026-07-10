# Data Model: App Icon Theming & Theme Rename

**Feature**: 004-app-icon-and-theme-rename
**Date**: 2026-07-10

## Entity: ThemeConfiguration

The `Theme` field in `VimiumConfig` is the only data entity affected by this feature.

### Schema (VimiumConfig.Theme)

| Property | Type | Default | Valid Values | Notes |
|----------|------|---------|-------------|-------|
| `Theme` | `string` | `"Light"` | `"Light"`, `"Dark"`, `"Arknights"` | Any other value (including legacy `"Skadi"`) is reset to `"Light"` on load — **only** the `Theme` field is reset |

### State Transitions

```
                    ┌──────────────────────────┐
                    │                          │
    ┌───────┐  select "Light"   ┌──────────┐   │
    │ Light │◄─────────────────│  Dark    │   │
    └───┬───│                  └────┬─────┘   │
        │    ──────────────────►    │          │
        │      select "Dark"        │          │
        │                           │          │
        │  select "Arknights"       │          │
        └──────────────────────┐    │          │
                               │    │          │
                               ▼    ▼          │
                           ┌──────────────┐    │
                           │  Arknights   │────┘
                           └──────────────┘
                             select "Light" or "Dark"
```

- **Light → Arknights**: Icons switch from keyboard to Arknights; theme dictionary swaps to ArknightsTheme.xaml
- **Dark → Arknights**: Icons switch from keyboard to Arknights; theme dictionary swaps to ArknightsTheme.xaml
- **Arknights → Light/Dark**: Icons revert to keyboard default; theme dictionary swaps accordingly

### Validation Rules

1. **FR-008**: On load, an unrecognized `Theme` value (including the legacy `"Skadi"`) MUST be reset to the default `"Light"`. **Only** the `Theme` field is reset — no other field is altered, and no migration to `"Arknights"` occurs.
2. **FR-007**: UI dropdown MUST display `"Arknights"`, never `"Skadi"`.
3. **FR-009**: When the user selects the Arknights theme, `"Arknights"` is written to config.

### Reset / Validation Logic

```csharp
// In VimiumConfig.FromJson(), after deserialization — resets ONLY the Theme field:
if (config.Theme is not ("Light" or "Dark" or "Arknights"))
    config.Theme = "Light";   // legacy "Skadi" and any unknown value fall here

// In ConfigService.Theme setter — no alias; the dropdown only offers valid values:
set {
    if (!SetProperty(_current.Theme, value, v => _current.Theme = v)) return;
    ApplyThemeHintDefaults(value);   // "Arknights" case replaces the old "Skadi" case
    OnPropertyChanged(nameof(IsDirty));
}
```

> Note: this uses a field-level reset, **not** `ConfigService.ResetToDefaults()` (which would wipe the entire config). All non-theme settings are preserved.

## Entity: IconResources

Icon resources are file-system assets, not persisted data. The mapping between theme and icon is defined in code.

### Icon File Manifest

| File | Theme(s) | Description | Sizes |
|------|----------|-------------|-------|
| `Resources/keyboard.ico` | Light, Dark (default) | Keyboard icon — default app identity | 16, 32, 48, 256 |
| `Resources/skadi.ico` | Arknights | Arknights-themed icon (existing icon, repurposed) | 16, 32, 48, 256 |

### Icon Resolution Logic

```
if (theme == "Arknights")
    icon = "Resources/skadi.ico"       // Arknights-themed
else
    icon = "Resources/keyboard.ico"    // Default keyboard
```

### Fallback Behavior (Edge Case: Missing Icon)

If the Arknights icon file is missing/corrupted at runtime:
1. The `BitmapImage` constructor will throw — catch and fall back to `keyboard.ico`
2. Log a warning via `LogService`
3. The app continues with the default keyboard icon (no crash, no blank icon)

### Theme File Rename

| Old Path | New Path | Notes |
|----------|----------|-------|
| `Themes/SkadiTheme.xaml` | `Themes/ArknightsTheme.xaml` | Visual content unchanged; only filename and internal references updated |

The `App.xaml.cs` `ApplyTheme()` method maps `"Arknights"` → `"Themes/ArknightsTheme.xaml"`. There is no backward-compat for the old filename — the reset-to-default validation at the `Theme` string level ensures the code path never requests `"Skadi"`.
