# Configuration Contract: RunAsAdministrator Field

**Feature**: 005-version-and-admin-mode
**Scope**: `%APPDATA%\Vimium\config.json`

## RunAsAdministrator Field

The `runAsAdministrator` field controls whether Vimium launches with administrator privileges. When `true`, the application self-elevates on startup via the Windows `runas` verb. When `false`, the application runs in the user's current privilege context.

### JSON Schema

```json
{
  "runAsAdministrator": {
    "type": "boolean",
    "default": true,
    "description": "Whether to request administrator elevation on startup. Defaults to true (existing behavior)."
  }
}
```

### Accepted Values

| Value | Behavior |
|-------|----------|
| `true` | App self-elevates via `runas` verb on startup. UAC prompt appears (unless UAC is disabled system-wide). |
| `false` | App runs as `asInvoker` — no UAC prompt, no elevation. |

### Backward Compatibility

- **Missing key**: Defaults to `true` (preserves the existing always-elevated behavior for upgrading users)
- **Corrupt value** (non-boolean): `System.Text.Json` throws → `VimiumConfig.FromJson` catches and returns `VimiumConfig.Default` (which has `RunAsAdministrator = true`)
- **Explicit `false`**: Preserved across save/load round-trips

### Serialization Rules

- **Property name in JSON**: `runAsAdministrator` (camelCase, consistent with existing naming policy)
- **Write behavior**: `DefaultIgnoreCondition = WhenWritingDefault` — when `true` (default), the key is omitted from JSON output. When `false`, the key is explicitly written.
- **Rationale**: A config file from a user who never changed the default will not contain the key. A user who explicitly disabled admin mode will see `"runAsAdministrator": false` in their config.

### Migration Path

```
Previous version config:  { "fontSize": "14", "theme": "Light" }
                              ↓ (VimiumConfig.FromJson — missing key → CLR default true)
In memory:                { RunAsAdministrator = true, ... }
                              ↓ (ConfigService — default not written due to WhenWritingDefault)
Config on disk:           { "fontSize": "14", "theme": "Light" }
                              (unchanged — no key added)

After user disables:      { "fontSize": "14", "theme": "Light" }
                              ↓ (user toggles off → ConfigService.Save)
Config on disk:           { "fontSize": "14", "theme": "Light", "runAsAdministrator": false }
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
