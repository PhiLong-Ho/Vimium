# Feature Specification: App Icon Theming & Theme Rename

**Feature Branch**: `004-app-icon-and-theme-rename`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description: "I want to add this small requirement to the version display. Let the app default icon is a keyboard. Only when I select skadi theme you switch all icon to skadi. Also rename skadi them to arknights"

## Clarifications

### Session 2026-07-10

- Q: When loading a config with a legacy/unrecognized theme value (e.g. `"Skadi"`), what gets reset to default? → A: Reset only the `Theme` field to the default (Light); all other settings are preserved. The legacy `"Skadi"` value is **not** migrated to `"Arknights"`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See Default Keyboard Icon (Priority: P1)

A user running Vimium with the default Light or Dark theme wants to see a keyboard icon representing the application in the system tray and settings, reflecting the keyboard-driven nature of the app.

**Why this priority**: The application icon is the primary visual identity of Vimium. A keyboard icon communicates the app's purpose at a glance and is visible every time the user looks at their system tray. This is the default state for all users on first install.

**Independent Test**: Install Vimium with default settings (Light theme), observe the system tray icon — it should be a keyboard. Switch to Dark theme — the icon should remain a keyboard.

**Acceptance Scenarios**:

1. **Given** Vimium is installed with default settings (Light theme), **When** the user views the system tray, **Then** the Vimium icon is a keyboard icon.
2. **Given** Vimium is using the Light theme, **When** the user opens the settings window, **Then** the window icon is a keyboard icon.
3. **Given** Vimium is using the Dark theme, **When** the user views the system tray, **Then** the icon remains a keyboard icon (default).
4. **Given** the user switches between Light and Dark themes, **When** the theme changes, **Then** the application icon does not change (stays keyboard).

---

### User Story 2 - Arknights Theme Icons (Priority: P1)

A user who selects the Arknights theme (formerly "Skadi") wants all application icons to switch to Arknights-themed icons, providing a cohesive visual experience matching the chosen theme.

**Why this priority**: Theme-specific icons are the defining characteristic that differentiates Arknights from the other themes. Without custom icons, Arknights is just a color scheme; with them, it's a complete visual identity. Users who choose Arknights expect a fully themed experience.

**Independent Test**: Switch theme to Arknights, verify that system tray icon and settings window icon change to Arknights-themed icons. Switch back to Light theme, verify icons return to keyboard default.

**Acceptance Scenarios**:

1. **Given** Vimium is using Light or Dark theme, **When** the user switches to the Arknights theme, **Then** all application icons change to Arknights-themed icons.
2. **Given** Vimium is using the Arknights theme, **When** the user views the system tray, **Then** the icon is an Arknights-themed icon (not the default keyboard).
3. **Given** Vimium is using the Arknights theme, **When** the user switches to Light or Dark theme, **Then** all icons revert to the default keyboard icon.
4. **Given** the Arknights theme is active, **When** the application is restarted, **Then** the Arknights-themed icons remain active (persisted theme selection).

---

### User Story 3 - Theme Renamed from Skadi to Arknights (Priority: P2)

An existing user who previously used the "Skadi" theme finds, after upgrading, that the theme is offered as "Arknights". Because the legacy `"Skadi"` config value is no longer recognized, their `Theme` setting is reset to the default (Light) on first load; they can re-select "Arknights" from the settings dropdown. All other settings are preserved.

**Why this priority**: This is a rename — no new functionality, but it affects existing users who have "Skadi" selected. Rather than migrating the legacy value, the app deliberately resets only the `Theme` field to the default and lets the user re-select "Arknights". This keeps the implementation simple; a one-time theme reset is an acceptable trade-off for a cosmetic rename.

**Independent Test**: On a system with "Skadi" theme selected in config, upgrade Vimium. Open settings — the theme dropdown should show the default (Light) selected, and "Arknights" should be available as an option to choose. All non-theme settings should be unchanged.

**Acceptance Scenarios**:

1. **Given** a user had "Skadi" theme selected before upgrading, **When** they open settings after upgrading, **Then** the theme is reset to the default (Light) and "Arknights" is available in the dropdown to re-select.
2. **Given** a new user opens the theme dropdown in settings, **When** they view available themes, **Then** the options are "Light", "Dark", and "Arknights" (no "Skadi" option).
3. **Given** the config file contains `"theme": "Skadi"` from a previous version, **When** Vimium starts, **Then** only the `Theme` field is reset to the default (Light); all other settings in the config are preserved.
4. **Given** a user selects the Arknights theme after the rename, **When** the config is saved, **Then** the config file stores `"theme": "Arknights"` (the new name).

---

### Edge Cases

- **Legacy / unrecognized theme value**: A config file with `"theme": "Skadi"` (or any value that is not "Light", "Dark", or "Arknights") is reset to the default theme (Light) on load. Only the `Theme` field is reset — all other settings are preserved. The legacy value is not migrated to "Arknights".
- **Theme resource references**: Any internal code or XAML that references "Skadi" by name (e.g., `case "Skadi":`) must be updated to "Arknights" as the canonical name. Legacy `"Skadi"` config values are handled by the reset-to-default behavior above, not by a compatibility alias.
- **Icon file missing**: If the Arknights icon file is missing or corrupted, the application should fall back gracefully to the default keyboard icon rather than crashing or showing a blank icon.
- **Multiple icon sizes**: Windows displays icons at various sizes (16x16, 32x32, 48x48, 256x256). All required sizes must be provided for both the keyboard default and Arknights-themed icons to avoid blurry scaling.
- **Notification area refresh**: When switching themes, the system tray icon update should be immediate and not require hovering or a system tray refresh to take effect.

## Requirements *(mandatory)*

### Functional Requirements

**Default Icon**

- **FR-001**: The application MUST use a keyboard icon as its default application icon for the Light and Dark themes.
- **FR-002**: The keyboard icon MUST be displayed in the system tray at all times when the application is running (unless overridden by a theme-specific icon).
- **FR-003**: The keyboard icon MUST be used for the settings window icon. Any other application windows/dialogs inherit the application default icon (keyboard) via the executable's `ApplicationIcon`, unless a theme-specific icon applies.

**Theme-Specific Icons**

- **FR-004**: When the Arknights theme is selected, ALL application icons (system tray, window icons) MUST switch to Arknights-themed icons.
- **FR-005**: When switching from Arknights back to Light or Dark theme, all icons MUST revert to the default keyboard icon.
- **FR-006**: Icon switching MUST occur immediately upon theme change, without requiring an application restart.

**Theme Rename: Skadi → Arknights**

- **FR-007**: The theme previously named "Skadi" MUST be renamed to "Arknights" in all user-facing UI (settings dropdown, labels).
- **FR-008**: When the config contains an unrecognized theme value (including the legacy `"theme": "Skadi"`), the application MUST reset **only** the `Theme` field to the default (Light) on load. It MUST NOT migrate the legacy value to "Arknights", and MUST NOT reset or alter any other setting.
- **FR-009**: When the user selects the Arknights theme, the application MUST write `"Arknights"` (the new name) to the config.
- **FR-010**: All internal code references to "Skadi" (resource dictionaries, theme switch logic, default color logic, resource keys such as `SkadiLoadingIcon`) MUST be updated to use "Arknights" as the canonical name. **Exception**: the `skadi.ico` asset **filename** is retained as the Arknights-themed icon file for compatibility. Because the filename is not user-facing, this exception does not affect SC-004.

### Key Entities *(include if feature involves data)*

- **ThemeConfiguration**: The existing `Theme` field in `VimiumConfig` (currently accepts "Light", "Dark", "Skadi"). After this feature: accepts "Light", "Dark", "Arknights". Any unrecognized value (including the legacy "Skadi") is reset to the default ("Light") on load — there is no alias or migration.
- **IconResources**: Application icon assets — two sets: (a) default keyboard icon in all required Windows sizes, (b) Arknights-themed icon set in all required sizes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The default keyboard icon is visible in the system tray immediately upon application startup (within the existing 2-second cold-start target).
- **SC-002**: Icon switching between keyboard and Arknights-themed icons occurs within 500ms of theme change — imperceptible to the user.
- **SC-003**: Existing users with a legacy `"theme": "Skadi"` value have **only** their `Theme` field reset to the default (Light) on upgrade; 100% of their other settings are preserved (no full config reset).
- **SC-004**: The word "Skadi" does not appear anywhere in the user-facing UI after the update; only "Arknights" is shown.
- **SC-005**: The application icon renders clearly at all standard Windows icon sizes (16px, 32px, 48px, 256px) without visible blur or pixelation.

## Assumptions

- **Icon asset creation**: The keyboard icon and Arknights-themed icon artwork will be created as part of implementation. This spec does not prescribe the exact visual design, only the functional behavior.
- **Icon format**: Icons will be provided in `.ico` format with embedded sizes (16, 32, 48, 256) as required by Windows. SVG or PNG sources may be used during design but will be converted for deployment.
- **Existing theme infrastructure**: Vimium already has a theme-switching mechanism via `ResourceDictionary` swapping in WPF. Icon switching will leverage this existing infrastructure — changing the active theme triggers an icon update via the same code path that updates colors and styles.
- **No config migration**: The legacy "Skadi" config value is NOT migrated to "Arknights". On load, an unrecognized theme value resets only the `Theme` field to the default (Light); all other settings are preserved, and users can re-select "Arknights" from the dropdown.
- **Skadi theme resource file**: The existing `Themes/SkadiTheme.xaml` file will be renamed to `Themes/ArknightsTheme.xaml` (a hard rename, no alias). The visual appearance of the Arknights theme is identical to the former Skadi theme — only the name changes.
- **Scope boundary**: This feature covers application icons only (system tray, window icons). It does NOT cover hint styling, overlay colors, or any other theme resources — those are already handled by the existing theme system and are unaffected by this change beyond the theme name.
