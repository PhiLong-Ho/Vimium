# UI Contract: Version Display & Admin Toggle

**Feature**: 005-version-and-admin-mode
**Scope**: Settings window (`OptionsView.xaml`)

## Version Display

### Location
Settings window sidebar/footer area — outside the page-specific content area, so the version is always visible regardless of which settings page is selected.

### Visual Specification

```text
┌──────────────────────────────────────────────────────┐
│  [Icon]  Vimium                        ─│□│✕│       │
│───────────┬──────────────────────────────────────────│
│           │                                          │
│  General  │  [Page content...]                       │
│  Overlay  │                                          │
│  Keyboard │                                          │
│           │                                          │
│           │                                          │
├───────────┴──────────────────────────────────────────│
│  v1.4.0      [Reset to defaults]  [Close]            │
└──────────────────────────────────────────────────────┘
```

### Binding Contract

| Element | Property | Binding Source | Binding Path |
|---------|----------|---------------|-------------|
| `TextBlock` (version) | `Text` | `OptionsViewModel` | `AppVersion` |
| Format | `"v{version}"` | ViewModel | String formatting |

### Behavior

- **On open**: Version is immediately visible (read from const, no loading delay)
- **On theme change**: Version label color adapts to current theme via `{DynamicResource}`
- **On upgrade**: Version string updates automatically (embedded at compile time)
- **Keyboard**: Not a focusable element — informational only

## Administrator Mode Toggle

### Location
General settings page, as a new card section below the existing "Appearance" card.

### Visual Specification

```text
┌──────────────────────────────────────────────────────┐
│  General                                             │
│                                                      │
│  ┌─ Font Size ────────────────────────────────────┐  │
│  │ ...                                            │  │
│  └────────────────────────────────────────────────┘  │
│                                                      │
│  ┌─ Appearance ───────────────────────────────────┐  │
│  │ ...                                            │  │
│  └────────────────────────────────────────────────┘  │
│                                                      │
│  ┌─ Administrator Mode ───────────────────────────┐  │
│  │                                                │  │
│  │  ☑ Run as Administrator                       │  │
│  │     Vimium will launch with elevated           │  │
│  │     privileges. Requires a UAC prompt.         │  │
│  │                                                │  │
│  │  ⚠ A restart is required for this change      │  │
│  │    to take effect.                             │  │
│  └────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

### Binding Contract

| Element | Property | Binding Source | Binding Path |
|---------|----------|---------------|-------------|
| `CheckBox` | `Content` | Static | `"Run as Administrator"` |
| `CheckBox` | `IsChecked` | `GeneralSettingsViewModel` | `RunAsAdministrator` |
| `TextBlock` (description) | `Text` | Static | Help text about elevator privileges |
| `TextBlock` (restart msg) | `Visibility` | `GeneralSettingsViewModel` | `ShowRestartMessage` (via `BooleanToVisibilityConverter`) |
| `TextBlock` (restart msg) | `Text` | Static | `"A restart is required for this change to take effect."` |

### Behavior

- **Default state**: Checkbox unchecked (`false`, non-elevated). No restart message visible.
- **On toggle (either direction)**: Setting saved immediately (auto-save via `ConfigService`). Restart message appears.
- **On toggle back to the open-time value**: Restart message hidden again (config matches the value when the page opened).
- **On settings close + reopen**: Restart message absent (config matches saved state). Current session privilege level unchanged.
- **On change during same session without restart**: Checkbox reflects config value. The actual privilege level of the running process does NOT change until restart.
- **Keyboard**: `Alt+R` access key on the checkbox label. `Tab` to navigate to it. `Space` to toggle.

### Theme Colors

| Element | Light Theme | Dark Theme | Skadi Theme |
|---------|-------------|------------|-------------|
| CheckBox text | `TextPrimaryBrush` (`#1A1A1A`) | `TextPrimaryBrush` (`#F0F0F0`) | `TextPrimaryBrush` (`#E8F4FF`) |
| Description text | `TextSecondaryBrush` | `TextSecondaryBrush` | `TextSecondaryBrush` |
| Restart message | `WarningBrush` (amber) | `WarningBrush` (amber) | `WarningBrush` (amber) |
| Card background | `CardBackgroundBrush` | `CardBackgroundBrush` | `CardBackgroundBrush` |

All colors use `{DynamicResource}` — no hardcoded values.
