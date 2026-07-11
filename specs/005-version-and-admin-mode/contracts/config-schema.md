# Configuration Contract: RunAsAdministrator Field

**Feature**: 005-version-and-admin-mode
**Scope**: `%APPDATA%\Vimium\config.json`

## RunAsAdministrator Field

The `runAsAdministrator` field controls whether Vimium launches with administrator privileges. When `true`, the application self-elevates on startup via the Windows `runas` verb. When `false` (the default), the application runs in the user's current privilege context — no elevation, no UAC prompt.

### JSON Schema

```json
{
  "runAsAdministrator": {
    "type": "boolean",
    "default": false,
    "description": "Whether to request administrator elevation on startup. Defaults to false (non-elevated) so managed/enterprise environments run without a UAC prompt. Elevation is opt-in."
  }
}
```

### Accepted Values

| Value | Behavior |
|-------|----------|
| `false` (default) | App runs as `asInvoker` — no UAC prompt, no elevation. |
| `true` | App self-elevates via `runas` verb on startup. UAC prompt appears (unless UAC is disabled system-wide). |

### Backward Compatibility

- **Missing key**: Defaults to `false` (non-elevated). Configs that predate the key — and all fresh installs — run without elevation.
- **Corrupt value** (non-boolean): `System.Text.Json` throws → `VimiumConfig.FromJson` catches and returns `VimiumConfig.Default` (which has `RunAsAdministrator = false`)
- **Explicit value**: Both `true` and `false` are preserved across save/load round-trips.

### Serialization Rules

- **Property name in JSON**: `runAsAdministrator` (camelCase, consistent with existing naming policy)
- **Write behavior**: `[JsonIgnore(Condition = Never)]` forces the key to be written on **every** save regardless of value. The non-elevated default is therefore always visible in `config.json`, and a chosen `true` round-trips. (Without the override, the class-wide `WhenWritingDefault` policy would omit the CLR-default `false`.)
- **Rationale**: Every config file carries an explicit `runAsAdministrator` value, so the setting is discoverable and a user's choice is never silently dropped.

### Migration Path

```
Previous version config:  { "fontSize": "14", "theme": "Light" }
                              ↓ (VimiumConfig.FromJson — missing key → default false)
In memory:                { RunAsAdministrator = false, ... }
                              ↓ (ConfigService.Save — key always written via Never)
Config on disk:           { "fontSize": "14", "theme": "Light", "runAsAdministrator": false }

User opts into elevation: toggle on → ConfigService.Save
Config on disk:           { ..., "runAsAdministrator": true }
```

### Example Config (with explicit false)

```json
{
  "theme": "Light",
  "fontSize": "14",
  "language": "en",
  "hintFontFamily": "",
  "hintActiveBackground": "#FFFFFF",
  "hintInactiveBackground": "#F0F0F0",
  "hintTextColor": "#1A1A1A",
  "hintAnimationEnabled": true,
  "overlayModifier": "Ctrl+;",
  "taskbarModifier": "Ctrl+'",
  "lineNavigationModifier": "Ctrl+.",
  "copyModifier": "Ctrl",
  "benchmarkLogEnabled": true,
  "runAsAdministrator": false
}
```
