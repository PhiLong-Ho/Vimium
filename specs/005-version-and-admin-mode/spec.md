# Feature Specification: Version Display & Administrator Mode Toggle

**Feature Branch**: `005-version-and-admin-mode`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description: "1st feature is User should be able to see software version. User should be able to turn off run in administrator mode (for example enterprise env), but this is the default running mode."

## Clarifications

### Session 2026-07-10

- Q: (carried from combined spec 003) What is the default activation hotkey for mouse control mode? → A: Not applicable — this spec is version & admin mode only. Mouse control is in `specs/003-mouse-control-mode/`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Software Version (Priority: P1)

A user wants to know which version of Vimium they are running, to verify they have the latest update or to report a bug with the correct version number.

**Why this priority**: Version visibility is essential for troubleshooting, verifying updates, and reporting issues. It is the simplest feature in v1.4 — read-only display with no runtime overhead — making it the highest-ROI item for all users.

**Independent Test**: Open the Vimium settings window and verify the version number is visible without any additional clicks.

**Acceptance Scenarios**:

1. **Given** Vimium is installed, **When** a user opens the settings/options window, **Then** the current software version is clearly displayed (e.g., "v1.4.0").
2. **Given** Vimium is upgraded to a new version, **When** the user opens settings after upgrade, **Then** the displayed version matches the new installed version without any manual update.
3. **Given** Vimium is freshly installed with no prior configuration, **When** the user opens settings, **Then** the version is displayed correctly (read from application metadata, not config).

---

### User Story 2 - Toggle Administrator Mode (Priority: P1)

An enterprise user needs to disable administrator elevation to comply with their organization's security policies, which prohibit user-installed applications from running with elevated privileges.

**Why this priority**: Administrator mode control is critical for enterprise deployment. Without it, organizations that block elevated applications cannot deploy Vimium at all. This is a gating requirement for enterprise adoption.

**Independent Test**: Open settings, uncheck "Run as Administrator", restart Vimium, and confirm it launches without UAC prompt. Re-enable, restart, and confirm UAC prompt returns.

**Acceptance Scenarios**:

1. **Given** Vimium is running with default settings, **When** a user checks the administrator mode setting, **Then** it is enabled by default.
2. **Given** an enterprise user opens settings, **When** they disable "Run as Administrator" and restart Vimium, **Then** the application launches without requesting elevation and runs in the user's current privilege context.
3. **Given** a user has disabled administrator mode, **When** they re-enable it and restart, **Then** Vimium requests elevation on next launch as before.
4. **Given** the administrator mode setting is changed, **When** the user closes and reopens settings, **Then** their preference is persisted correctly.
5. **Given** the user changes the administrator mode setting, **When** the change is made, **Then** a clear message informs the user that a restart is required for the change to take effect.

---

### Edge Cases

- **Administrator mode toggle mid-session**: Changing the setting takes effect on the next application restart. The current session continues with its existing privilege level.
- **Version display on fresh install**: For a first-time install with no prior configuration, the version must still display correctly (read from assembly metadata, not from config).
- **Config migration**: The `runAsAdministrator` key does not exist in config files from versions prior to v1.4. On first launch, the default value (`true`) must be used with no error.
- **UAC disabled system-wide**: On systems where UAC is disabled entirely, the administrator mode toggle has no visible effect (the application always runs elevated). The setting should still be toggleable and persist correctly.
- **Non-admin user**: If the current Windows user does not have administrator credentials, enabling "Run as Administrator" will cause the UAC prompt to fail. The application should handle this gracefully (fall back to non-elevated mode) rather than crashing.

## Requirements *(mandatory)*

### Functional Requirements

**Version Display**

- **FR-001**: The application MUST display its current version number in the settings/options window, visible without requiring any action beyond opening settings.
- **FR-002**: The version number MUST be derived from the application's build metadata (assembly version), not from a manually maintained config value.
- **FR-003**: The version display MUST update automatically when the application is upgraded to a new version.

**Administrator Mode Toggle**

- **FR-004**: The application MUST provide a setting to enable or disable running with administrator privileges.
- **FR-005**: The administrator mode setting MUST default to enabled (elevated) for new installations.
- **FR-006**: Changing the administrator mode setting MUST persist across application restarts.
- **FR-007**: When administrator mode is disabled, the application MUST launch without triggering a User Account Control (UAC) elevation prompt.
- **FR-008**: The administrator mode change MUST take effect on the next application launch, with clear messaging to the user that a restart is required.

### Key Entities *(include if feature involves data)*

- **ApplicationSettings**: Extended to include the `RunAsAdministrator` preference (boolean, default `true`) alongside the existing config fields. The application version string is derived from assembly metadata at runtime and exposed for display — it is NOT a persisted config field.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view the application version in under 3 seconds from the system tray (right-click tray icon → open settings → version visible).
- **SC-002**: Users can toggle administrator mode and restart the application in under 30 seconds.
- **SC-003**: Enterprise users who previously could not install Vimium due to mandatory elevation can now run the application with administrator mode disabled.
- **SC-004**: 100% of existing users upgrading from a previous version retain their existing settings (the new `RunAsAdministrator` default of `true` matches the previous always-elevated behavior).

## Assumptions

- **Version source**: The application version is embedded in the assembly metadata (e.g., `AssemblyInfo.cs` or project file version property) and can be read at runtime without additional build steps.
- **Admin mode implementation**: Disabling administrator mode is achieved by changing the application manifest to `asInvoker` and conditionally relaunching with elevation (`runas` verb) when the setting is enabled. This avoids maintaining two separate executables.
- **Restart requirement**: Users understand that administrator mode changes require an application restart, which is standard for privilege-level changes on Windows.
- **Settings persistence**: The `RunAsAdministrator` setting is stored in the existing `%APPDATA%\Vimium\config.json` file using the existing `System.Text.Json` configuration infrastructure, alongside all other settings.
- **Scope boundary**: This feature covers version display and administrator mode toggle only. Mouse control mode is specified separately in `specs/003-mouse-control-mode/`.
