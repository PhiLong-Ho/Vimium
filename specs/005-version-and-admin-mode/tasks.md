# Tasks: Version Display & Administrator Mode Toggle

**Input**: Design documents from `/specs/005-version-and-admin-mode/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included per constitution Principle III (≥80% coverage on non-view, non-interop code).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Path Conventions

WPF desktop app — single project structure:

```text
src/Vimium/            # Main WPF application
src/Vimium.Tests/      # xUnit test project
src/NativeMethods/     # Win32 interop (unchanged)
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Version bump and manifest preparation — necessary before any feature work

- [X] T001 Bump assembly version from 1.3.0.0 to 1.4.0.0 in `src/SolutionInfo.cs`
- [X] T002 [P] Bump ApplicationVersion in `src/Vimium/Vimium.csproj` to match 1.4.0.0
- [X] T003 Change app.manifest `requestedExecutionLevel` from `requireAdministrator` to `asInvoker` in `src/Vimium/app.manifest`

**Checkpoint**: Version bumped, manifest ready for runtime elevation. Build should still succeed: `dotnet build src\Vimium.sln`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Config model and service changes that BOTH user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Add `RunAsAdministrator` property (bool, default `true`) to `VimiumConfig` in `src/Vimium/Models/VimiumConfig.cs`
- [X] T005 [P] Add `RunAsAdministrator` convenience property to `ConfigService` following existing pattern (get/set delegates to `_current`, auto-save via `SetProperty`) in `src/Vimium/Services/ConfigService.cs`
- [X] T006 [P] Add unit tests for `VimiumConfig` serialization: round-trip with `RunAsAdministrator`, missing-key defaults to `true`, explicit `false` preserved in `src/Vimium.Tests/Models/VimiumConfigTests.cs`
- [X] T007 [P] Add unit tests for `ConfigService.RunAsAdministrator`: get/set, `PropertyChanged` raised, `IsDirty` updated in `src/Vimium.Tests/Services/ConfigServiceTests.cs`

**Checkpoint**: Foundation ready — config model extended, service property wired, tests passing. User story implementation can begin.

---

## Phase 3: User Story 1 - View Software Version (Priority: P1) 🎯 MVP

**Goal**: Display the current software version in the settings window, visible without any action beyond opening settings.

**Independent Test**: Open settings → version number (e.g., `v1.4.0`) is visible in the sidebar/footer area. Verify it matches `SolutionInfo.cs` const.

### Tests for User Story 1

- [X] T008 [P] [US1] Add unit test for `OptionsViewModel.AppVersion` matches `AssemblyVersionInformation.Version` in `src/Vimium.Tests/ViewModels/OptionsViewModelTests.cs`

### Implementation for User Story 1

- [X] T009 [US1] Add `AppVersion` property (string, read-only) to `OptionsViewModel` reading from `AssemblyVersionInformation.Version` in `src/Vimium/ViewModels/OptionsViewModel.cs`
- [X] T010 [US1] Add version `TextBlock` to the settings window footer area (alongside Reset/Close buttons) in `src/Vimium/Views/OptionsView.xaml`
- [X] T011 [US1] Verify version label adapts to theme via `{DynamicResource TextSecondaryBrush}` for consistent appearance across Light/Dark/Skadi themes

**Checkpoint**: Open settings → version visible. Match `SolutionInfo.cs`. Independent of admin toggle.

---

## Phase 4: User Story 2 - Toggle Administrator Mode (Priority: P1)

**Goal**: Enterprise users can disable administrator elevation. A checkbox in General settings persists the preference; restart required message appears on change.

**Independent Test**: Open settings → General → uncheck "Run as Administrator" → restart message appears. Close + reopen → setting persists. Restart app → no UAC prompt. Re-enable → UAC returns.

### Tests for User Story 2

- [X] T012 [P] [US2] Add unit test for `GeneralSettingsViewModel.RunAsAdministrator` binding (forwards to `ConfigService.RunAsAdministrator`) in `src/Vimium.Tests/ViewModels/GeneralSettingsViewModelTests.cs`
- [X] T013 [P] [US2] Add unit test for `GeneralSettingsViewModel.ShowRestartMessage` transitions (hidden at init, visible after toggle) in `src/Vimium.Tests/ViewModels/GeneralSettingsViewModelTests.cs`

### Implementation for User Story 2

- [X] T014 [US2] Add `RunAsAdministrator` property (delegates to `_config.RunAsAdministrator`) and `ShowRestartMessage` property (compares current vs initial) to `GeneralSettingsViewModel` in `src/Vimium/ViewModels/GeneralSettingsViewModel.cs`
- [X] T015 [US2] Add "Administrator Mode" card section to General page DataTemplate in `src/Vimium/Views/OptionsView.xaml`:
  - `CheckBox` bound to `RunAsAdministrator` with label "Run as Administrator"
  - Description `TextBlock`: "Vimium will launch with elevated privileges. Requires a UAC prompt."
  - Restart warning `TextBlock`: "A restart is required for this change to take effect." bound to `ShowRestartMessage` via `BooleanToVisibilityConverter`
- [X] T016 [US2] Implement elevation check and relaunch logic in `src/Vimium/App.xaml.cs`:
  - Add `IsUserAdmin()` helper using `WindowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator)`
  - In `OnStartup`, after `SingleLaunchMutex` check, before normal startup: if `ConfigService.RunAsAdministrator` is `true` AND `!IsUserAdmin()`, relaunch with `Process.Start(useShellExecute: true, Verb: "runas")` and `Current.Shutdown()`
  - If admin mode is `false` OR already elevated, proceed with normal startup

**Checkpoint**: Admin toggle functional. Disable → no UAC on restart. Re-enable → UAC returns. Setting persists.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, edge case handling, and documentation

- [X] T017 Run full test suite: `dotnet test src\Vimium.sln` — all tests must pass
- [X] T018 [P] Verify all config migration scenarios from `quickstart.md` (missing key, existing config, explicit false)
- [X] T019 [P] Verify theme consistency across Light, Dark, and Skadi themes for new UI elements (version label, admin card)
- [X] T020 [P] Verify keyboard accessibility: Tab order includes admin checkbox, Alt+R access key on label, version label not in tab order (informational only)
- [X] T021 Update `CHANGELOG.md` with v1.4 entries under `Added` (version display, admin mode toggle) referencing `specs/005-version-and-admin-mode/`
- [X] T022 [P] Run quickstart validation checklist from `specs/005-version-and-admin-mode/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) for test infrastructure; US1 is independent of US2
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) for `VimiumConfig.RunAsAdministrator` + `ConfigService`; needs `App.xaml.cs` changes (different file from US1)
- **Polish (Phase 5)**: Depends on both user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on US2. Only needs `SolutionInfo.cs` version (Phase 1) + test project (exists).
- **User Story 2 (P1)**: No dependency on US1. Needs `VimiumConfig.RunAsAdministrator` (Phase 2) + `ConfigService` (Phase 2).

US1 and US2 touch different areas of `OptionsView.xaml`:
- US1: Footer area (alongside Reset/Close buttons)
- US2: General page DataTemplate (new card section)

These are in different XAML regions — path conflicts are minimal. Coordinate if merging.

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- ViewModel properties before XAML bindings
- Config/service changes (Phase 2) before ViewModel integration (Phase 3/4)
- Core implementation before polish

### Parallel Opportunities

```
Phase 1:  T001 ─────────→ T003
              └─ T002 ─┘  (parallel)

Phase 2:  T004 ─────────→ (none — all others depend on T004)
              ├─ T005 ─┘  (parallel with T004, but T005 reads VimiumConfig)
              ├─ T006 ─┘  (parallel tests)
              └─ T007 ─┘  (parallel tests)
          Note: T005-T007 all depend on T004 being complete

Phase 3:  T008 ─→ T009 ─→ T010 ─→ T011
          (test first, then ViewModel, then XAML, then verify)

Phase 4:  T012 ─→ T014 ─→ T015
          T013 ─┘        └─→ T016
          (tests first in parallel, then ViewModel, then XAML + App.xaml.cs)

Phase 5:  T017 ─→ T018, T019, T020, T021, T022 all parallel after T017 passes
```

---

## Parallel Example: User Story 2

```bash
# Step 1: Write tests in parallel (both fail before implementation):
Task: "Add unit test for GeneralSettingsViewModel.RunAsAdministrator in src/Vimium.Tests/ViewModels/GeneralSettingsViewModelTests.cs"
Task: "Add unit test for GeneralSettingsViewModel.ShowRestartMessage in src/Vimium.Tests/ViewModels/GeneralSettingsViewModelTests.cs"

# Step 2: Implement ViewModel property:
Task: "Add RunAsAdministrator + ShowRestartMessage to GeneralSettingsViewModel in src/Vimium/ViewModels/GeneralSettingsViewModel.cs"

# Step 3: XAML + App.xaml.cs can proceed in parallel (different files):
Task: "Add Administrator Mode card to OptionsView.xaml"
Task: "Implement elevation check and relaunch logic in App.xaml.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T007)
3. Complete Phase 3: User Story 1 (T008-T011)
4. **STOP and VALIDATE**: Open settings, verify version visible
5. Deploy/demo if ready

**Estimated effort**: ~4 tasks for US1, ~30 min implementation time.

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Version visible (MVP!)
3. Add User Story 2 → Test independently → Admin toggle functional
4. Polish → Full validation, changelog, keyboard accessibility

### Full Implementation Order

```
T001 ─→ T002 ─→ T003 ─→ T004 ─→ T005 ─→ T006
                                    └─→ T007
                                          │
                    ┌─────────────────────┘
                    ▼
              T008 ─→ T009 ─→ T010 ─→ T011  (US1: Version)
                    │
                    ├─→ T012 ─→ T014 ─→ T015 ─→ T016  (US2: Admin)
                    └─→ T013 ─┘
                                          │
                                          ▼
                                    T017 ─→ T018..T022  (Polish)
```

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Both user stories are P1 — if sequential, do US1 first (simpler, no restart dependency, higher confidence)
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
