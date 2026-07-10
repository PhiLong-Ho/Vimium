# Icon Resource Contract

**Feature**: 004-app-icon-and-theme-rename
**Scope**: WPF Application Resources (`App.xaml`)

## Named Resources

The application defines WPF `BitmapImage` resources at the `Application.Resources` level. These resources are available for binding throughout all views.

### Resource: `AppIcon`

The primary application icon. Its `UriSource` is updated at runtime when the theme changes.

```text
Resource Key:  AppIcon
Type:          BitmapImage
Scope:         Application.Resources (global)
Binding:       {DynamicResource AppIcon} or {Binding Source={x:Static ...}}

Theme → Icon mapping:
  Light      → /Resources/keyboard.ico
  Dark       → /Resources/keyboard.ico
  Arknights  → /Resources/skadi.ico
```

### Resource: `FallbackIcon`

A secondary resource always pointing to `keyboard.ico`, used as a fallback if the Arknights icon fails to load.

```text
Resource Key:  FallbackIcon
Type:          BitmapImage
Scope:         Application.Resources (global)
Source:        /Resources/keyboard.ico  (constant)
```

## View Contracts

### ShellView (System Tray)

The `TaskbarIcon` element binds its `IconSource` to the active icon:

```xml
<tb:TaskbarIcon IconSource="{Binding AppIcon, Source={x:Static services:ConfigService.Instance}}"
                ... />
```

**Contract**: The `ConfigService` singleton MUST expose an `AppIcon` property of type `ImageSource` that:
- Returns the keyboard icon when theme is Light or Dark
- Returns the Arknights icon when theme is Arknights
- Fires `PropertyChanged` when theme changes

### OptionsView (Settings Window)

The sidebar header image binds to the active icon:

```xml
<Image Source="{Binding AppIcon, Source={x:Static services:ConfigService.Instance}}"
       Width="20" Height="20" Margin="0,0,8,0" />
```

**Contract**: Same `AppIcon` property as above — both views consume the same binding source.

### Fallback Behavior

If the Arknights icon file (`/Resources/skadi.ico`) is missing:
1. The `AppIcon` property MUST return the keyboard icon (`/Resources/keyboard.ico`) instead
2. A warning MUST be logged
3. The application MUST NOT crash or show a blank/missing icon

## Theme File Contract

The `Themes/ArknightsTheme.xaml` (renamed from `SkadiTheme.xaml`) MUST define the same `ResourceDictionary` keys as `LightTheme.xaml` and `DarkTheme.xaml`. This contract is unchanged from the existing theme system — the rename is a file rename only, with no content changes.

### Required Resource Keys (all themes)

These keys are referenced by views and styles. Every theme `.xaml` must define them:

```text
WindowBackgroundBrush
CardBackgroundBrush
TextPrimaryBrush
TextSecondaryBrush
BorderBrush
AccentBrush
DangerBrush
ScrollBarBrush
CheckBackgroundBrush
CheckForegroundBrush
```
