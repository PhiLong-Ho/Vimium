---
description: "Task list for App Icon Theming & Theme Rename"
---

# Tasks: App Icon Theming & Theme Rename

**Input**: Design documents from `/specs/004-app-icon-and-theme-rename/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included for the theme validation / reset-to-default logic only — explicitly requested by `quickstart.md` ("Expected test coverage for new logic"). Icon/XAML behavior is validated manually via `quickstart.md` scenarios.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 (maps to spec.md user stories)
- All paths are relative to the repository root `D:\Workspace\Vimium\`

## Path Conventions

- App source: `src/Vimium/`
- Tests: `src/Vimium.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create and register the new default icon asset that all stories depend on.

- [X] T001 [P] Create the default keyboard icon `src/Vimium/Resources/keyboard.ico` with embedded 16×16, 32×32, 48×48, and 256×256 sizes (per research.md Task 2 and SC-005). Use a keyboard glyph on a neutral background; convert from a 256px source PNG.
- [X] T002 Register the new icon and default app icon in `src/Vimium/Vimium.csproj`: add `<Resource Include="Resources\keyboard.ico" />` next to the existing `skadi.ico` resource (line ~51), and change `<ApplicationIcon>Resources/skadi.ico</ApplicationIcon>` (line ~26) to `<ApplicationIcon>Resources/keyboard.ico</ApplicationIcon>`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the single `AppIcon` binding source and wire the views to it. Both US1 and US2 consume this. **No user story icon behavior works until this phase is complete.**

**⚠️ CRITICAL**: T004 and T005 depend on the `AppIcon` property added in T003.

- [X] T003 Add a public `ImageSource AppIcon` property to `src/Vimium/Services/ConfigService.cs` backed by a private icon-resolution helper (stub: return `keyboard.ico` for all themes for now). Raise `OnPropertyChanged(nameof(AppIcon))` inside the existing `Theme` setter (after line ~59) so any theme change notifies the icon binding.
- [X] T004 [P] Bind the system tray icon to the shared source in `src/Vimium/Views/ShellView.xaml`: replace `IconSource="/Resources/skadi.ico"` (line 8) with `IconSource="{Binding AppIcon, Source={x:Static services:ConfigService.Instance}}"` and ensure the `services:` xmlns for `Vimium.Services` is declared.
- [X] T005 [P] Bind the settings sidebar header image (and the settings window `Icon`) to the shared source in `src/Vimium/Views/OptionsView.xaml`: set `Source="{Binding AppIcon, Source={x:Static services:ConfigService.Instance}}"` on the sidebar `Image`, declaring the `services:` xmlns if missing.

**Checkpoint**: The binding path exists end-to-end; a theme change fires `PropertyChanged(AppIcon)`. Icon still resolves to keyboard for every theme (correct for US1, incomplete for US2).

---

## Phase 3: User Story 1 - See Default Keyboard Icon (Priority: P1) 🎯 MVP

**Goal**: The app shows a keyboard icon in the system tray and settings for the default Light and Dark themes, and it does not change when switching between Light and Dark.

**Independent Test**: Launch with default (Light) theme → tray icon is a keyboard; open settings → sidebar/window icon is a keyboard; switch Light↔Dark → icon stays keyboard. (quickstart.md Scenarios 1 & 2.)

### Implementation for User Story 1

- [X] T006 [US1] Implement the keyboard branch of the icon-resolution helper in `src/Vimium/Services/ConfigService.cs`: return `/Resources/keyboard.ico` for any non-Arknights theme (Light, Dark, and default fallthrough). This replaces the T003 stub with the real default-icon logic (FR-001, FR-002, FR-003).
- [X] T007 [P] [US1] Update the global icon resources in `src/Vimium/App.xaml`: change `<BitmapImage x:Key="AppIcon" UriSource="/Resources/skadi.ico" />` to point to `/Resources/keyboard.ico`, and add a constant `<BitmapImage x:Key="FallbackIcon" UriSource="/Resources/keyboard.ico" />` (per icon-resource-contract.md).

**Checkpoint**: User Story 1 fully functional and testable — keyboard icon is the default and persists across Light/Dark.

---

## Phase 4: User Story 2 - Arknights Theme Icons (Priority: P1)

**Goal**: Selecting the Arknights theme switches all app icons to the Arknights-themed icon immediately; switching back to Light/Dark reverts to the keyboard icon; a missing icon file falls back gracefully.

**Independent Test**: Switch to Arknights → tray and sidebar icons become Arknights (within 500ms); switch to Light/Dark → revert to keyboard; delete `skadi.ico` and switch to Arknights → app does not crash, keyboard icon shown, warning logged. (quickstart.md Scenarios 3, 4, 7.)

### Implementation for User Story 2

- [X] T008 [US2] Extend the icon-resolution helper in `src/Vimium/Services/ConfigService.cs` to return `/Resources/skadi.ico` when the (canonical) theme is `"Arknights"`, keeping keyboard for all other themes (FR-004, FR-005, FR-006). Immediate switching is already delivered by the `PropertyChanged(AppIcon)` wiring from T003.
- [X] T009 [US2] Add graceful fallback in the icon-resolution helper in `src/Vimium/Services/ConfigService.cs`: wrap `BitmapImage` construction in try/catch, and on failure return the keyboard icon and log a warning via the existing `LogService` (edge case: missing/corrupt icon; icon-resource-contract.md Fallback Behavior).

**Checkpoint**: User Stories 1 AND 2 both work — default keyboard for Light/Dark, Arknights icon for Arknights, safe fallback.

---

## Phase 5: User Story 3 - Theme Renamed from Skadi to Arknights (Priority: P2)

**Goal**: The theme is displayed and stored as "Arknights"; an existing config containing `"theme": "Skadi"` (or any unrecognized value) has **only its `Theme` field reset to the default `"Light"`** on load — all other settings are preserved and the value is NOT migrated to "Arknights"; no user-facing "Skadi" remains.

**Independent Test**: Seed config with `{"theme":"Skadi","fontSize":"18"}` → app starts in default **Light** theme (keyboard icons), dropdown shows "Light" selected with options Light/Dark/Arknights (no "Skadi"), and Font Size is still 18 (non-theme setting preserved). Selecting "Arknights" then saves `"theme":"Arknights"` while keeping `"fontSize":"18"`. (quickstart.md Scenario 5.)

### Tests for User Story 3 ⚠️

> Write these FIRST and confirm they FAIL before implementing T012–T013.

- [X] T010 [P] [US3] Add reset/validation unit tests in `src/Vimium.Tests/VimiumConfigThemeResetTests.cs`: `FromJson` with `"theme":"Skadi"` yields `Theme == "Light"`; `FromJson` with an unknown value (e.g. `"Neon"`) yields `Theme == "Light"`; `FromJson` with `"Light"`/`"Dark"`/`"Arknights"` leaves `Theme` unchanged (FR-008).
- [X] T011 [P] [US3] Add preservation unit tests in `src/Vimium.Tests/VimiumConfigPreservationTests.cs`: `FromJson` of a legacy config with a non-default non-theme field (e.g. `{"theme":"Skadi","fontSize":"18"}`) resets **only** `Theme` to `"Light"` while `FontSize` remains `"18"` and all other fields keep their deserialized values (FR-008, SC-003).

### Implementation for User Story 3

- [X] T012 [US3] Add post-deserialization theme validation in `VimiumConfig.FromJson()` in `src/Vimium/Models/VimiumConfig.cs`: after deserialization, `if (config.Theme is not ("Light" or "Dark" or "Arknights")) config.Theme = "Light";`. This is a **field-level reset** of only `Theme` (legacy `"Skadi"` and any unknown value fall here) — do NOT migrate to `"Arknights"` and do NOT touch any other field (FR-008, data-model.md Reset / Validation Logic).
- [X] T013 [US3] In `src/Vimium/Services/ConfigService.cs`: rename `case "Skadi":` to `case "Arknights":` in `ApplyThemeHintDefaults()` (line ~74) so hint defaults use the canonical name. Do NOT add any `"Skadi"` alias/normalization to the `Theme` setter — the dropdown only offers valid values (FR-010).
- [X] T014 [P] [US3] Rename the display value in `src/Vimium/ViewModels/GeneralSettingsViewModel.cs` (line 43): change `Themes => new() { "Light", "Dark", "Skadi" }` to `{ "Light", "Dark", "Arknights" }` (FR-007, SC-004).
- [X] T015 [P] [US3] Rename the theme resource file `src/Vimium/Themes/SkadiTheme.xaml` → `src/Vimium/Themes/ArknightsTheme.xaml` (git mv; content unchanged, all required resource keys preserved per icon-resource-contract.md Theme File Contract), and rename the `<BitmapImage x:Key="SkadiLoadingIcon" ...>` key (line ~41) to `x:Key="ArknightsLoadingIcon"` (FR-010).
- [X] T016 [US3] Update the `ApplyTheme()` switch in `src/Vimium/App.xaml.cs` (line ~53): change `"Skadi" => "Themes/SkadiTheme.xaml"` to `"Arknights" => "Themes/ArknightsTheme.xaml"` (FR-010). Depends on T015.

**Checkpoint**: All three user stories independently functional; reset-to-default behavior verified by T010–T011.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Remove residual references, update documentation, and validate end-to-end.

- [X] T017 [P] Update the loading-icon theme check in `src/Vimium/Views/OverlayView.xaml.cs` (line ~35): change `if (ConfigService.Instance.Theme == "Skadi")` to `== "Arknights"` and point the loading icon at the renamed `ArknightsLoadingIcon` resource (or the `skadi.ico` pack URI, which keeps its filename). Then sweep `src/Vimium/` for any remaining `"Skadi"`/`Skadi` identifiers and update to `"Arknights"` — **preserving only** the `skadi.ico` filename (kept for compat per data-model.md). There is NO `"Skadi"` alias to preserve. Depends on T015.
- [X] T018 Run all quickstart.md validation scenarios 1–7 on Windows and confirm SC-001…SC-005 (default keyboard icon, ≤500ms switch, legacy "Skadi" resets only the Theme field while other settings are preserved, no user-facing "Skadi", crisp at 16/32/48/256).
- [X] T019 [P] Update `CHANGELOG.md` with the app-icon theming and Skadi→Arknights rename entry (note: legacy "Skadi" configs reset the theme to default, not migrated).
- [X] T020 [P] Update `README.md` per the constitution "Documentation after feature" gate: document the default keyboard app icon, the Arknights-themed icons, and the "Skadi"→"Arknights" theme rename (including that a legacy "Skadi" config resets the theme to the default on upgrade).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T002 depends on the asset from T001.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks US1 and US2.** T004/T005 depend on T003.
- **User Story 1 (Phase 3)**: Depends on Foundational. T006 extends the ConfigService helper from T003.
- **User Story 2 (Phase 4)**: Depends on Foundational; T008/T009 extend the same helper T006 implements (so US2 builds on US1's resolution method — sequential on `ConfigService.cs`).
- **User Story 3 (Phase 5)**: Independent of US1/US2 (theme rename + reset-to-default). Can run in parallel with US1/US2 work by a second developer, except both touch `ConfigService.cs` (T013 vs T006/T008/T009) — serialize edits to that file.
- **Polish (Phase 6)**: After all desired stories complete.

### `ConfigService.cs` serialization

T003 → T006 → T008 → T009 → T013 all edit `src/Vimium/Services/ConfigService.cs`. They must be applied sequentially (not `[P]` with each other), even though they belong to different phases/stories.

### Within Each User Story

- US3 tests (T010–T011) before US3 implementation (T012–T013).
- Models before services; file rename (T015) before switch update (T016).

### Parallel Opportunities

- **Setup**: T001 can start immediately.
- **Foundational**: T004 and T005 run in parallel after T003 (different XAML files).
- **US1**: T007 (App.xaml) runs in parallel with T006 (ConfigService.cs).
- **US3**: T010, T011 (test files) in parallel; T014 (ViewModel) and T015 (theme file rename) in parallel with the reset/validation edits.
- **Cross-story**: With two developers, US3 (rename/reset) proceeds alongside US1/US2 (icons), coordinating only on `ConfigService.cs`.

---

## Parallel Example: Foundational + US1

```bash
# After T003 completes, wire both views concurrently:
Task: "T004 Bind TaskbarIcon.IconSource to ConfigService.Instance.AppIcon in src/Vimium/Views/ShellView.xaml"
Task: "T005 Bind sidebar Image.Source to ConfigService.Instance.AppIcon in src/Vimium/Views/OptionsView.xaml"

# US1 across different files:
Task: "T006 Keyboard branch of AppIcon resolution in src/Vimium/Services/ConfigService.cs"
Task: "T007 Point App.xaml AppIcon/FallbackIcon resources at keyboard.ico in src/Vimium/App.xaml"
```

## Parallel Example: User Story 3

```bash
# Write reset-to-default tests together (they must fail first):
Task: "T010 Theme reset/validation tests in src/Vimium.Tests/VimiumConfigThemeResetTests.cs"
Task: "T011 Preservation tests in src/Vimium.Tests/VimiumConfigPreservationTests.cs"

# Rename-only edits in parallel (different files):
Task: "T014 Rename Skadi→Arknights in GeneralSettingsViewModel.Themes"
Task: "T015 git mv SkadiTheme.xaml → ArknightsTheme.xaml"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (create + register `keyboard.ico`).
2. Complete Phase 2: Foundational (`AppIcon` property + view bindings).
3. Complete Phase 3: User Story 1 (keyboard as default icon).
4. **STOP and VALIDATE**: quickstart.md Scenarios 1 & 2 — keyboard shows and persists across Light/Dark.
5. Ship as MVP.

### Incremental Delivery

1. Setup + Foundational → binding path ready.
2. US1 → keyboard default (MVP) → validate → ship.
3. US2 → Arknights icon switching + fallback → validate Scenarios 3, 4, 7 → ship.
4. US3 → theme rename + reset-to-default → validate Scenario 5 → ship.
5. Polish → sweep residual references, run full quickstart.

### Parallel Team Strategy

1. Everyone completes Setup + Foundational.
2. Then: Developer A → US1 then US2 (owns `ConfigService.cs` icon logic); Developer B → US3 (rename + reset-to-default + tests), coordinating the single `ConfigService.cs` edit (T013) with Developer A.

---

## Notes

- `skadi.ico` is intentionally kept as the Arknights-themed icon filename (data-model.md) — do NOT rename it.
- There is NO `"Skadi"` alias. A legacy/unrecognized `Theme` value resets **only** the `Theme` field to the default `"Light"` on load (via `VimiumConfig.FromJson`) — it is not migrated to `"Arknights"`, and no other setting is altered.
- `[P]` tasks = different files, no incomplete-task dependency.
- Commit after each task or logical group; verify US3 tests fail before implementing.
