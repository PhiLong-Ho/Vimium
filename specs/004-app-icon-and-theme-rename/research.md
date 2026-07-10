# Research: App Icon Theming & Theme Rename

**Feature**: 004-app-icon-and-theme-rename
**Date**: 2026-07-10

## Research Task 1: Dynamic WPF Application Icon Switching

### Decision
Use WPF `ResourceDictionary` manipulation to swap `BitmapImage` resources at runtime. The existing `App.xaml` defines `<BitmapImage x:Key="AppIcon" UriSource="...">` — modify `ApplyTheme()` in `App.xaml.cs` to update this resource's `UriSource` based on the active theme.

### Rationale
- The `App.xaml` already defines a global `BitmapImage` resource with key `"AppIcon"` — all views reference it statically
- Changing the `UriSource` at runtime automatically propagates to all bound elements (no explicit refresh needed since the `BitmapImage` is replaced, not mutated)
- The system tray `TaskbarIcon` in `ShellView.xaml` uses a static `IconSource="/Resources/skadi.ico"` — this will be changed to bind to the dynamic icon resource
- The options window sidebar icon in `OptionsView.xaml` uses a static `Source="/Resources/skadi.ico"` — this will also bind dynamically

### Alternatives Considered
1. **Code-behind icon assignment**: Directly setting `TaskbarIcon.IconSource` from `App.xaml.cs` — rejected because it breaks MVVM and requires the App to reach into ShellView's visual tree
2. **WPF Pack URI switching at runtime**: Creating new `BitmapImage` instances on each theme change — works but more verbose; the ResourceDictionary approach is cleaner and reuses existing infrastructure
3. **Two separate icon files bound via DataTrigger**: Using XAML `DataTrigger`s to switch icon based on theme — rejected because WPF `DataTrigger` on `IconSource` requires binding to a property that notifies, and the ResourceDictionary swap is already the established pattern in this codebase

---

## Research Task 2: Windows .ico File Creation with Multiple Sizes

### Decision
Create `.ico` files with embedded 16×16, 32×32, 48×48, and 256×256 sizes. The keyboard icon will be a new asset (`keyboard.ico`), and the existing `skadi.ico` will serve as the Arknights theme icon.

### Rationale
- Windows requires `.ico` format for application icons; embedding multiple sizes ensures crisp rendering at all display scales
- The sizes (16, 32, 48, 256) cover: system tray (16), taskbar small (32), window title bar (16/32), Alt+Tab (48), and high-DPI/large icon views (256)
- The existing `skadi.ico` already has these embedded sizes (verified by file inspection — it's used as the current app icon and renders correctly)
- For the keyboard icon: a simple keyboard glyph on a neutral background, designed to be visually distinct at all sizes

### Alternatives Considered
1. **Single 256px .ico with Windows auto-scaling**: Rejected — Windows downscaling at 16×16 often produces blurry results; embedded sizes ensure pixel-perfect rendering
2. **PNG-based icons**: Rejected — Windows does not support `.png` as application icon format; `.ico` is required
3. **Reuse existing `skadi.ico` as keyboard default**: Rejected — the existing icon is the Skadi/Arknights character, not a keyboard

### Tooling
- Can use online `.ico` converters (e.g., `icoconverter.com`, `convertio.co`) or tools like ImageMagick/GIMP to create the multi-resolution `.ico` from a high-resolution source PNG
- Recommend creating the keyboard icon as a 256×256 PNG first, then converting to `.ico` with all sub-sizes

---

## Research Task 3: Legacy / Unrecognized Theme Value Handling (Reset to Default)

### Decision
Do **not** migrate the legacy `"Skadi"` value. Instead, validate the `Theme` field on load and reset any unrecognized value (including the legacy `"Skadi"`) to the default `"Light"`, touching **only** the `Theme` field:

1. **Load-time validation**: In `VimiumConfig.FromJson()`, after deserialization, check `Theme` against the allowed set `{ "Light", "Dark", "Arknights" }`. If it is not one of these, set `Theme = "Light"`. All other fields are left exactly as deserialized.
2. **No setter alias**: The `ConfigService.Theme` setter no longer treats `"Skadi"` specially — the dropdown only offers `"Light"`, `"Dark"`, `"Arknights"`, so the setter never receives `"Skadi"` from the UI.

### Rationale
- Per the 2026-07-10 clarification, there is no requirement to preserve the old theme selection — a one-time reset of the theme to the default is acceptable for a cosmetic rename, and it keeps the implementation trivial (a single validation branch).
- Resetting **only** the `Theme` field (rather than calling `ConfigService.ResetToDefaults()`, which wipes the whole config) preserves every other user setting — keybindings, font size, hint colors, etc. This satisfies FR-008 ("MUST NOT reset or alter any other setting").
- `VimiumConfig.FromJson()` is the single deserialization boundary (also used by `Clone()`), so the validation runs on every load path exactly once.
- Because the app auto-saves on the next config change, the reset value is persisted naturally the first time the user touches any setting; until then the in-memory value is the default, which is the observable behavior the spec requires.

### Alternatives Considered
1. **Silent alias / two-layer migration ("Skadi" → "Arknights")**: The original approach — rejected by the clarification. The user explicitly does not want the value migrated.
2. **Full `ResetToDefaults()` on unrecognized theme**: Rejected — it would wipe unrelated user settings, contradicting FR-008 and SC-003 (only the `Theme` field may be reset).
3. **Reset in `ConfigService.Load()` instead of `FromJson()`**: Rejected — `FromJson()` is the canonical boundary reused by `Clone()`; validating there guarantees consistency across all load/clone paths with no duplication.

---

## Research Task 4: Theme Resource File Rename

### Decision
Rename `Themes/SkadiTheme.xaml` → `Themes/ArknightsTheme.xaml`. Update the `ApplyTheme()` switch in `App.xaml.cs` to use `"Arknights"` → `"Themes/ArknightsTheme.xaml"`. The old file is removed (the reset-to-default behavior handles legacy values at the config level, not the file level, so no code path ever requests `"Skadi"`). Also rename the `SkadiLoadingIcon` resource key inside the file to `ArknightsLoadingIcon` and update its reference in `OverlayView.xaml.cs`.

### Rationale
- The theme `.xaml` filenames match the theme names: `LightTheme.xaml`, `DarkTheme.xaml`, `SkadiTheme.xaml`. Renaming to `ArknightsTheme.xaml` maintains consistency.
- No code references the theme file by a variable path — only the `ApplyTheme()` switch statement maps theme name → file path. This single switch statement is the only code that needs updating.
- The visual content of the file remains unchanged (per spec assumption: "The visual appearance of the Arknights theme is identical to the former Skadi theme").

### Alternatives Considered
1. **Keep file as `SkadiTheme.xaml` and alias in code**: Rejected — leaves a "Skadi" reference in the codebase which violates FR-010 ("All internal code references to 'Skadi' MUST be updated")
2. **Create symlink/copy**: Rejected as unnecessary complexity; a simple file rename is clean and sufficient

---

## Research Task 5: System Tray Icon Refresh on Theme Change

### Decision
The `TaskbarIcon` in `ShellView.xaml` uses a static `IconSource` attribute. Change it to bind to a dynamic property. Since `ConfigService` already implements `INotifyPropertyChanged` and fires property changes for `Theme`, add an `AppIconPath` property (or expose the `BitmapImage` directly) that the XAML can bind to.

### Rationale
- The existing `ConfigService.PropertyChanged` event is already listened to by `App.xaml.cs` → `ApplyTheme()` is called on theme change
- `ApplyTheme()` can update a global WPF resource, and XAML bindings using `{DynamicResource}` or `{Binding Source={x:Static ...}}` will pick up the change
- The Hardcodet `TaskbarIcon.IconSource` is a dependency property — it supports binding
- No polling or manual refresh needed: WPF's resource/property change propagation handles it automatically

### Alternatives Considered
1. **Set `IconSource` from code-behind**: Rejected — requires ShellView code-behind to subscribe to config changes, breaking MVVM
2. **Recreate the TaskbarIcon on theme change**: Rejected — flickers in the system tray and is unnecessarily disruptive
3. **Use `{DynamicResource AppIcon}` in XAML**: Partially rejected — `TaskbarIcon.IconSource` expects an `ImageSource`, and `DynamicResource` may not resolve correctly in the TaskbarIcon's resource scope. Binding to a property on ConfigService is more reliable.
