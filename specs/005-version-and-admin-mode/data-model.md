# Data Model: Version Display & Administrator Mode Toggle

**Feature**: 005-version-and-admin-mode
**Date**: 2026-07-10

## Entity Overview

This feature extends one existing entity and introduces one read-only view-model entity. No new persisted entities are created.

## VimiumConfig (Modified)

**Source**: `src/Vimium/Models/VimiumConfig.cs`
**Persistence**: `%APPDATA%\Vimium\config.json` via `System.Text.Json`
**Serialization**: `PropertyNamingPolicy = CamelCase`, `WriteIndented = true`

### New Field

| Field | Type | Default | Required | Description |
|-------|------|---------|----------|-------------|
| `RunAsAdministrator` | `bool` | `true` | No (CLR default) | Whether the application should launch with administrator privileges. When `true`, the app self-elevates on startup via `runas` verb. When `false`, the app runs in the user's current privilege context. |

### JSON Serialization

```json
{
  "runAsAdministrator": true
}
```

- Property name in JSON: `runAsAdministrator` (camelCase per existing naming policy)
- Missing key on deserialization → CLR default `true` (preserves backward compatibility)
- `DefaultIgnoreCondition = WhenWritingDefault`: If set to `false`, the key will be written. If `true`, the key is omitted from JSON (matching existing pattern for other boolean fields like `hintAnimationEnabled` which also defaults to `true` and is omitted when `true`).

### Validation Rules

| Rule | Enforcement |
|------|-------------|
| Must be `true` or `false` | Compile-time (C# `bool`) |
| Default value is `true` | Constructor initialization |

### State Transitions

```
┌───────┐  user toggles off   ┌───────┐
│ true  │ ──────────────────→ │ false │
│(elevated)│                    │(non-elev)│
└───────┘  ←────────────────── └───────┘
            user toggles on
```

- Transition is immediate in config (auto-save on change)
- Transition takes effect on next application restart
- Current session continues with its existing privilege level

## AppVersion (Read-Only ViewModel Entity)

**Source**: `src/SolutionInfo.cs` → `AssemblyVersionInformation.Version`
**Exposed via**: `GeneralSettingsViewModel.AppVersion` (or `OptionsViewModel`)

### Field

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| `AppVersion` | `string` | `AssemblyVersionInformation.Version` (compile-time const) | The application version in `M.m.p.r` format (e.g., `"1.4.0.0"`). Read from assembly metadata at compile time — no runtime I/O. |

### Lifecycle

```
Build → Const embedded in assembly → ViewModel reads const → XAML binding displays
```

- No persistence (not stored in config)
- No user modification (never changes at runtime)
- Updates only when a new build is deployed

## GeneralSettingsViewModel (Modified)

**Source**: `src/Vimium/ViewModels/GeneralSettingsViewModel.cs`

### New Properties

| Property | Type | RO/RW | Bound To | Description |
|----------|------|-------|----------|-------------|
| `RunAsAdministrator` | `bool` | RW | `CheckBox.IsChecked` | Delegates to `ConfigService.RunAsAdministrator` |
| `ShowRestartMessage` | `bool` | RO | `TextBlock.Visibility` | `true` when the UI value differs from the saved config (i.e., user toggled but hasn't restarted). Resets to `false` on settings window reopen after restart. |
| `AppVersion` | `string` | RO | `TextBlock.Text` | Returns `AssemblyVersionInformation.Version` |

### Implementation Notes

```csharp
public bool RunAsAdministrator
{
    get => _config.RunAsAdministrator;
    set
    {
        _config.RunAsAdministrator = value;
        NotifyOfPropertyChange();
        NotifyOfPropertyChange(nameof(ShowRestartMessage));
    }
}

// Restart message is always shown after toggle (simplest correct behavior —
// until restart, the setting hasn't taken effect). On reopen after restart,
// the config matches the actual privilege level, so the message is absent.
public bool ShowRestartMessage => true; // shown when admin toggle is on the page

public string AppVersion => AssemblyVersionInformation.Version;
```

> **Design note on `ShowRestartMessage`**: The simplest approach (shown above) always displays the message when the General page is visible. If we want the message hidden before the user interacts, we can track the initial value at construction time and compare:

```csharp
private readonly bool _initialRunAsAdmin;

public GeneralSettingsViewModel()
{
    _initialRunAsAdmin = _config.RunAsAdministrator;
}

public bool ShowRestartMessage => _config.RunAsAdministrator != _initialRunAsAdmin;
```

The second approach is preferred — it avoids showing the message unnecessarily before the user makes a change.

## ConfigService (Modified)

**Source**: `src/Vimium/Services/ConfigService.cs`

### New Convenience Property

```csharp
public bool RunAsAdministrator
{
    get => _current.RunAsAdministrator;
    set
    {
        if (SetProperty(_current.RunAsAdministrator, value,
            v => _current.RunAsAdministrator = v))
            OnPropertyChanged(nameof(IsDirty));
    }
}
```

- Follows the existing pattern of all other convenience properties on `ConfigService`
- Auto-saves on change (via `SetProperty` → `SaveInternal`)
- Raises `PropertyChanged` so bound ViewModels can react

## Relationships

```
┌──────────────────────┐
│  VimiumConfig         │
│  + RunAsAdministrator │◄──── ConfigService (singleton)
└──────────────────────┘       ▲
                               │ delegates
┌──────────────────────┐       │
│ GeneralSettingsVM     │──────┘
│  + RunAsAdministrator │
│  + ShowRestartMessage │
│  + AppVersion         │
└──────────────────────┘
        │
        │ XAML {Binding}
        ▼
┌──────────────────────┐
│  OptionsView.xaml     │
│  CheckBox + TextBlock │
└──────────────────────┘
```

## Test Coverage

| Test Entity | Test Scenarios |
|-------------|---------------|
| `VimiumConfig` | Round-trip serialization with `RunAsAdministrator`; deserialization from JSON missing the key → defaults to `true`; deserialization from JSON with explicit `false` → preserved |
| `ConfigService` | `RunAsAdministrator` get/set; `PropertyChanged` raised on change; `IsDirty` updated; `Save()` persists the value; `Cancel()` reverts |
| `GeneralSettingsViewModel` | `RunAsAdministrator` binding forwards to config; `ShowRestartMessage` transitions; `AppVersion` matches assembly const |
