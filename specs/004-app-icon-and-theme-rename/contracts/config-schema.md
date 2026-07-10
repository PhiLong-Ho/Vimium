# Configuration Contract: Theme Field

**Feature**: 004-app-icon-and-theme-rename
**Scope**: `%APPDATA%\Vimium\config.json`

## Theme Field

The `theme` field controls the active application theme and determines which icon set is displayed.

### JSON Schema

```json
{
  "theme": {
    "type": "string",
    "default": "Light",
    "enum": ["Light", "Dark", "Arknights"],
    "description": "Active theme. Any value outside the enum — including the legacy 'Skadi' — is reset to 'Light' on read. Only the theme field is reset; it is never migrated to 'Arknights'."
  }
}
```

### Accepted Values

| Value | Canonical | Behavior |
|-------|-----------|----------|
| `"Light"` | Yes | Light color scheme + keyboard icon |
| `"Dark"` | Yes | Dark color scheme + keyboard icon |
| `"Arknights"` | Yes | Arknights color scheme + Arknights-themed icon |
| `"Skadi"` | No (legacy) | Reset to `"Light"` on read (default). Not migrated to `"Arknights"`. Only the `theme` field is reset. |
| _any other value_ | No | Reset to `"Light"` on read (default). |

### Reset Behavior (No Migration)

- **Read**: an out-of-enum value (legacy `"Skadi"` or anything unknown) → reset to `"Light"`; all other config fields are preserved untouched.
- **Write**: whatever theme the user selects is written verbatim (`"Light"`, `"Dark"`, or `"Arknights"`).
- **Scope**: only the `theme` field is reset — this is a field-level reset, not a full-config reset.
- **Case sensitivity**: exact match required (`"skadi"` ≠ `"Skadi"`; both reset to default).

### Reset Path

```
Old config:  { "theme": "Skadi", "fontSize": "18" }
                ↓ (VimiumConfig.FromJson validates Theme, resets only that field)
In memory:   { "theme": "Light",  "fontSize": "18" }   ← other settings preserved
                ↓ (user later selects a theme → ConfigService auto-saves)
New config:  { "theme": "<selected>", "fontSize": "18" }
```

### Example Config

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
  "taskbarModifier": "Ctrl'\''",
  "lineNavigationModifier": "Ctrl+.",
  "copyModifier": "Ctrl",
  "benchmarkLogEnabled": true
}
```
