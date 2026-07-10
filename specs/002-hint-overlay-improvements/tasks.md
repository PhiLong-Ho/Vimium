# Tasks: Hint Overlay Improvements

**Input**: Design documents from `specs/002-hint-overlay-improvements/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — constitution Principle III mandates ≥80% coverage on core logic, and FR-019 explicitly requires unit-testable filtering logic.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/Vimium/` (WPF desktop app)
- **Tests**: `src/Vimium.Tests/`
- **Scripts**: `scripts/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: No new projects needed — this feature refines the existing hint overlay infrastructure within `src/Vimium/`.

*No tasks — all changes are within existing projects.*

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared models and config extensions needed by multiple user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T001 [P] Create `HintAction` enum (Invoke, LeftClick, RightClick, MoveMouse, Hover) and `ActionSlot` model (SlotIndex, Modifier, Action) with JSON serialization attributes in `src/Vimium/Models/HintAction.cs`
- [X] T002 [P] Add `ActionSlots` (ActionSlot[], default: 3 slots with Invoke/LeftClick/Invoke) and `BenchmarkLogEnabled` (bool, default: true) properties to `VimiumConfig` in `src/Vimium/Models/VimiumConfig.cs`

**Checkpoint**: Foundation ready — user story implementation can now begin in parallel.

---

## Phase 3: User Story 1 - Instant Hint Visibility (Priority: P1) 🎯 MVP

**Goal**: Reduce hint enumeration from 1–3s to ≤750ms for 200+ element apps via provider-side pattern-availability condition filtering, tree trimming, and result caching by window handle. Add structured JSON benchmark logging.

**Independent Test**: Activate element mode (`Ctrl+;`) on Chrome with `https://en.wikipedia.org/wiki/Singapore` loaded and maximized at 1920×1080 — all hint labels appear within 750ms on first activation, near-instant on subsequent activations. Check `benchmark.jsonl` has valid entries with `cacheHit: false` for cold starts.

### Models & Interfaces for User Story 1

- [X] T003 [P] [US1] Create `BenchmarkLogEntry` model (Timestamp, WindowTitle, ElementCount, ElapsedMs, CacheHit, FilterMode) with `System.Text.Json` serialization in `src/Vimium/Models/BenchmarkLogEntry.cs`
- [X] T004 [P] [US1] Create `IBenchmarkService` interface (LogSession, InvalidateCache, IsEnabled) in `src/Vimium/Services/Interfaces/IBenchmarkService.cs`
- [X] T005 [US1] Extend `IHintProviderService` interface — add `InvalidateCache()` method and `CancellationToken` parameter to `EnumHintsAsync`; update `UiAutomationHintProviderService.EnumHintsAsync` signature to accept `CancellationToken` in `src/Vimium/Services/Interfaces/IHintProviderService.cs` and `src/Vimium/Services/UiAutomationHintProviderService.cs`
- [X] T006 [P] [US1] Add `CachedHints` (IReadOnlyList<Hint>), `CachedHwnd` (IntPtr), and `CachedFilterMode` (string) fields to `HintSession` in `src/Vimium/Models/HintSession.cs`

### Implementation for User Story 1

- [X] T007 [US1] Implement provider-side pattern-availability condition filtering in `UiAutomationHintProviderService.EnumElements` — construct an OR condition of `IsInvokePatternAvailable`, `IsTogglePatternAvailable`, `IsSelectionItemPatternAvailable`, `IsExpandCollapsePatternAvailable`, `IsValuePatternAvailable`, `IsRangeValuePatternAvailable` and AND it with the existing ControlView + Enabled + OnScreen condition tree before calling `FindAllBuildCache` in `src/Vimium/Services/UiAutomationHintProviderService.cs`
- [X] T008 [US1] Implement tree trimming — add conservative subtree skipping for elements that are definitively non-interactive (not a control element, not enabled, or off-screen) by checking cached properties in the post-`FindAll` loop within `EnumElements` in `src/Vimium/Services/UiAutomationHintProviderService.cs`
- [X] T009 [US1] Implement result caching by foreground window handle — before calling `FindAllBuildCache`, check if `hWnd` matches `_cachedHwnd`; if match, return cached `HintSession` immediately. Clear cache when `InvalidateCache()` is called or when `hWnd` changes in `src/Vimium/Services/UiAutomationHintProviderService.cs`
- [X] T010 [US1] Implement cancellation support — check `CancellationToken` before `FindAllBuildCache` and after the COM call returns; skip post-processing and return null if cancelled in `src/Vimium/Services/UiAutomationHintProviderService.cs`
- [X] T011 [US1] Implement `BenchmarkService` — append one JSON object per line to `%APPDATA%\Vimium\logs\benchmark.jsonl`; create directory and file on first write; implement 10MB rolling policy (evict oldest half of entries); thread-safe file append with lock; catch-all exception handling (logging must never break the feature) in `src/Vimium/Services/BenchmarkService.cs`
- [X] T012 [US1] Integrate benchmark logging — measure enumeration time via `Stopwatch` around `FindAllBuildCache`, construct `BenchmarkLogEntry` with window title (from `GetWindowText`), element count, elapsed ms, cache hit flag, and current filter mode; call `IBenchmarkService.LogSession` after each enumeration in `src/Vimium/Services/UiAutomationHintProviderService.cs`
- [X] T013 [US1] Implement input buffering during hint loading — accumulate keystrokes in a pending buffer while `IsLoading` is true, then apply buffered input to `MatchString` when `PopulateHints` completes (so characters typed during enumeration are not lost) in `src/Vimium/Views/OverlayView.xaml.cs`
- [X] T014 [US1] Update `ShellViewModel._keyListener_OnHotKeyActivated` to pass `CancellationToken` to `EnumHintsAsync`, trigger cancellation on `CloseOverlay` via `CancellationTokenSource`, and wire `BenchmarkService` for cache invalidation in `src/Vimium/ViewModels/ShellViewModel.cs`

### Tests for User Story 1

- [X] T015 [P] [US1] Create unit tests for `UiAutomationHintProviderService` filtering logic — verify condition tree construction includes all six pattern-availability properties, verify tree trimming skips non-control/disabled/offscreen elements, verify caching returns cached result on same hWnd and re-enumerates on different hWnd in `src/Vimium.Tests/Services/UiAutomationHintProviderServiceTest.cs`
- [X] T016 [P] [US1] Create unit tests for `BenchmarkService` — verify JSONL format (valid JSON per line, correct field values), verify rolling behavior (oldest entries evicted when >10MB), verify thread safety (concurrent writes produce valid output), verify no-throw on disk-full or permission-denied in `src/Vimium.Tests/Services/BenchmarkServiceTest.cs`
- [X] T017 [P] [US1] Create `BenchmarkLogEntry` serialization roundtrip tests — verify JSON serialize/deserialize produces identical field values in `src/Vimium.Tests/Models/BenchmarkLogEntryTest.cs`

### Benchmark Script for User Story 1

- [X] T018 [US1] Create PowerShell benchmark log analysis script — parse `benchmark.jsonl`, filter to `cacheHit=false` entries, compute mean/median/P95/min/max for `elapsedMs` in `scripts/parse-benchmark-log.ps1`

**Checkpoint**: At this point, hints appear within 750ms on cold start and near-instant on cache hit. Benchmark log records every session. User Story 1 is independently testable.

---

## Phase 4: User Story 2 - Configurable Hint Actions (Priority: P2)

**Goal**: Replace hardcoded Shift→Click/RightClick behavior with 3 configurable modifier-action slots. Add MoveMouse and Hover actions. Provide key-capture controls in the options window (PowerToys-inspired) instead of manual key-name typing.

**Independent Test**: Open Options → Actions tab, use key-capture to assign Ctrl+Shift to "Move mouse only". Activate hints, hold Ctrl+Shift, type a hint label — cursor moves to element without clicking. Verify default behavior (no modifier → Invoke) is unchanged.

### Implementation for User Story 2

- [X] T019 [US2] Implement multi-slot action resolution in `OverlayViewModel.MatchString` setter — replace hardcoded `GetAsyncKeyState(VK_RSHIFT)`/`GetAsyncKeyState(VK_LSHIFT)` checks with a loop over `ActionSlots[]` from config; resolve first matching slot's modifier combination (check both left and right variants symmetrically) and dispatch the assigned action; preserve reentrancy guard (`_resolving`); actions: Invoke→`hint.Invoke()`, LeftClick→`hint.Click()`, RightClick→`hint.RightClick()`, MoveMouse→`hint.MovePointerToCenter()`, Hover→`hint.MovePointerToCenter()` in `src/Vimium/ViewModels/OverlayViewModel.cs`
- [X] T020 [US2] Create `ActionSettingsViewModel` — expose the three `ActionSlot` entries, provide key-capture state management (Idle/Capturing), expose a `StartCapture(slotIndex)` command that enters capture mode with visual feedback, handle captured modifier string via `PreviewKeyDown`-fed data, validate at least one modifier key is held, and persist changes immediately via `ConfigService` (auto-save) in `src/Vimium/ViewModels/ActionSettingsViewModel.cs`
- [X] T021 [US2] Add Actions page with key-capture controls to Options window — create a new `DataTemplate` for `ActionSettingsViewModel` with three action-slot rows, each containing: a key-capture button (displays captured modifier or "Click to capture"), a `ComboBox` for action type selection (5 options), and a visual indicator showing whether the slot is currently capturing. Wire `PreviewKeyDown` events from the capture button to feed held modifier keys to the ViewModel in `src/Vimium/Views/OptionsView.xaml`
- [X] T022 [US2] Add key-capture event forwarding in code-behind — handle `PreviewKeyDown` on the capture control to detect currently-held modifier keys, build the modifier string (e.g., "Ctrl+Shift"), and pass to `ActionSettingsViewModel`; mark event as handled to prevent bubbling in `src/Vimium/Views/OptionsView.xaml.cs`
- [X] T023 [US2] Register `ActionSettingsViewModel` page in `OptionsViewModel.Pages` collection (with icon and display name "Actions") in `src/Vimium/ViewModels/OptionsViewModel.cs`
- [X] T024 [US2] Pass `ActionSlots` configuration from `ConfigService` to `OverlayViewModel` constructor so the `MatchString` setter can resolve actions — update `ShellViewModel._keyListener_OnHotKeyActivated` and `_keyListener_OnTaskbarHotKeyActivated` to read action slots via `ConfigService.Instance` in `src/Vimium/ViewModels/ShellViewModel.cs`

### Tests for User Story 2

- [X] T025 [P] [US2] Create unit tests for `OverlayViewModel` multi-slot action resolution — verify slot 1 modifier match dispatches correct action, slot 2 modifier match, no-modifier fallback to slot 0, symmetric left/right modifier handling, two-key combo matching (both keys must be held), no match fallback to slot 0 in `src/Vimium.Tests/ViewModels/OverlayViewModelTest.cs`
- [X] T026 [P] [US2] Create unit tests for `ActionSettingsViewModel` key-capture logic — verify capture mode transitions, modifier string construction from held keys, validation (reject empty modifier for alternate slots), and auto-save behavior in `src/Vimium.Tests/ViewModels/ActionSettingsViewModelTest.cs`
- [X] T027 [P] [US2] Create `HintAction` serialization tests — verify all five enum values roundtrip through JSON serialize/deserialize, and `ActionSlot` with modifier+action roundtrips correctly in `src/Vimium.Tests/Models/HintActionTest.cs`

**Checkpoint**: Users can configure all three modifier-action slots via key-capture UI. MoveMouse and Hover actions work. Default behavior preserved.

---

## Phase 5: User Story 3 - Non-Overlapping Hint Labels (Priority: P2)

**Goal**: Eliminate overlapping hint labels on dense UIs (Discord, Slack) using spiral offsetting — each label tries positions in priority order (top-left default → above → below → right → left), stacking vertically if all five positions collide.

**Independent Test**: Activate hints on Discord with 50+ visible message action buttons — no two labels overlap, each label is within 20px of its target element's edge, labels are clearly readable.

### Implementation for User Story 3

- [X] T028 [P] [US3] Create `HintLabelPosition` model (OriginalLeft, OriginalTop, AdjustedLeft, AdjustedTop, Placement enum: Default/Above/Below/Right/Left/Stacked) in `src/Vimium/Models/HintLabelPosition.cs`
- [X] T029 [US3] Create `IOverlapResolver` interface with `ResolveOverlaps(IReadOnlyList<HintLabelPosition> positions, double maxOffset)` method in `src/Vimium/Services/Interfaces/IOverlapResolver.cs`
- [X] T030 [US3] Implement `OverlapResolver` — for each label (ordered by processing priority), test collision against all previously placed labels; try positions in order: default (top-left), above (offset upward by element height), below, right, left; if all five collide, stack vertically below the last conflicting label with 2px gap; update `AdjustedLeft`, `AdjustedTop`, and `Placement` for each position; O(n²) deterministic algorithm in `src/Vimium/Services/OverlapResolver.cs`
- [X] T031 [US3] Integrate overlap resolution into `OverlayViewModel.PopulateHints` — after hint labels are assigned, construct `HintLabelPosition` list from hint bounding rectangles and label dimensions, call `IOverlapResolver.ResolveOverlaps`, and pass adjusted positions to each `HintViewModel` in `src/Vimium/ViewModels/OverlayViewModel.cs`
- [X] T032 [US3] Add `AdjustedLeft` and `AdjustedTop` bindable properties to `HintViewModel` (default to `Hint.BoundingRectangle.Left/Top` when no adjustment applied) in `src/Vimium/ViewModels/HintViewModel.cs`
- [X] T033 [US3] Update `OverlayView.xaml` `ItemContainerStyle` to bind `Canvas.Top` to `HintViewModel.AdjustedTop` and `Canvas.Left` to `HintViewModel.AdjustedLeft` instead of `Hint.BoundingRectangle.Top/Left` in `src/Vimium/Views/OverlayView.xaml`

### Tests for User Story 3

- [X] T034 [P] [US3] Create unit tests for `OverlapResolver` — verify spiral offsetting positions are tried in correct priority order, verify collision detection against previously placed labels, verify stacking fallback when all five positions collide, verify labels stay within 20px max offset, verify deterministic output (same input → same output) in `src/Vimium.Tests/Services/OverlapResolverTest.cs`

**Checkpoint**: Hint labels no longer overlap on dense UIs. Each label is clearly readable and visually associated with its target element.

---

## Phase 6: User Story 4 - Per-Action Hint Filtering (Priority: P3)

**Goal**: Filter hints at overlay-open time based on the default action slot's configured action type. When default is a click-based action (Invoke, LeftClick, RightClick), only show elements supporting interactive patterns. When default is MoveMouse or Hover, show all visible elements.

**Independent Test**: Configure default action to Invoke, activate hints — only clickable elements have labels. Change default to MoveMouse, activate hints — all visible elements (including static text) have labels.

### Implementation for User Story 4

- [X] T035 [US4] Implement filter mode selection in `UiAutomationHintProviderService` — read default action slot from `ConfigService`; when default action is Invoke/LeftClick/RightClick, use `InvokeFiltered` mode (pattern-availability condition filtering as implemented in T007); when default is MoveMouse/Hover, use `AllElements` mode (skip pattern-availability conditions, use only ControlView+Enabled+OnScreen) in `src/Vimium/Services/UiAutomationHintProviderService.cs`
- [X] T036 [US4] Ensure filter mode is fixed at overlay-open time — store the filter mode used during enumeration and do not re-evaluate when alternate modifiers are held during hint matching in `src/Vimium/Services/UiAutomationHintProviderService.cs`
- [X] T037 [US4] Pass `FilterMode` to `BenchmarkLogEntry` so each benchmark entry records whether `InvokeFiltered` or `AllElements` mode was active in `src/Vimium/Services/UiAutomationHintProviderService.cs`

### Tests for User Story 4

- [X] T038 [P] [US4] Extend `UiAutomationHintProviderServiceTest` with per-action filtering tests — verify Invoke default produces pattern-availability condition tree, verify MoveMouse default produces all-elements condition tree, verify filter mode is fixed at overlay-open (changing modifiers during hint matching doesn't re-filter) in `src/Vimium.Tests/Services/UiAutomationHintProviderServiceTest.cs`

**Checkpoint**: Hints are filtered based on the intended default action. Non-clickable elements are hidden when the user intends to click, reducing visual noise.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Integration validation, documentation, and final verification.

- [X] T039 [US1] [US4] Run full benchmark procedure per quickstart.md VS-006 — build Vimium, open Wikipedia/Singapore in Chrome at 1920×1080, run 20 cold-start repetitions with `parse-benchmark-log.ps1`, verify P95 < 750ms and zero missing interactive elements
- [X] T040 [US2] [US3] Run cross-story validation — verify custom modifier actions work correctly with non-overlapping labels (e.g., hint labels repositioned by spiral offsetting are still clickable with the correct action when selected with modifier) per quickstart.md VS-002, VS-003, VS-004
- [X] T041 Run full quickstart.md validation — execute VS-001 through VS-005 smoke tests, verify all acceptance criteria pass, verify `dotnet test src\Vimium.sln` passes with no regressions
- [X] T042 Verify constitution compliance — confirm all five principles pass (MVVM separation, interface-driven services, testing standards, UX consistency, non-blocking UI); verify `dotnet build src\Vimium.sln` succeeds with zero warnings in `specs/002-hint-overlay-improvements/checklists/requirements.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No tasks — skip to Foundational
- **Foundational (Phase 2)**: No dependencies — can start immediately. BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) — specifically T001 (ActionSlot) and T002 (VimiumConfig extensions)
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) — uses ActionSlot from T001, VimiumConfig from T002. Independent of US1.
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) only — no dependency on US1, US2, or US4. Fully independent.
- **User Story 4 (Phase 6)**: Depends on Foundational (Phase 2) AND User Story 1 (Phase 3) — extends T007's pattern-availability filtering with conditional mode selection in the same file (`UiAutomationHintProviderService.cs`)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational — No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational — Independent of US1, US3, US4
- **User Story 3 (P2)**: Can start after Foundational — Independent of US1, US2, US4
- **User Story 4 (P3)**: Can start after Foundational + US1 — Extends US1's `UiAutomationHintProviderService.cs`

### Within Each User Story

- Models/interfaces before service implementations
- Core implementation before integration
- Tests can be written in parallel with implementation (same phase, different files)
- Story complete and independently testable before moving to next priority

### File Conflict Notes

> ⚠️ **Same-file modifications**: The following files are modified by multiple phases. Task ordering within these files matters:
> - `src/Vimium/Services/UiAutomationHintProviderService.cs` — US1 (T007–T010, T012) then US4 (T035–T037). US4 MUST follow US1 completion.
> - `src/Vimium/ViewModels/OverlayViewModel.cs` — US2 (T019) and US3 (T031). These modify different methods (MatchString vs PopulateHints) and can be merged sequentially.
> - `src/Vimium/ViewModels/ShellViewModel.cs` — US1 (T014) and US2 (T024). Both modify `_keyListener_OnHotKeyActivated`; merge carefully or implement in sequence.
> - `src/Vimium/Views/OverlayView.xaml` — US3 (T033). Only modified by US3.
> - `src/Vimium/Views/OptionsView.xaml` — US2 (T021). Only modified by US2.

### Parallel Opportunities

- All Phase 2 tasks marked [P] can run in parallel
- Once Foundational phase completes, US1, US2, and US3 can start in parallel (different files except as noted above)
- All test tasks within a story marked [P] can run in parallel
- US3 is completely independent of US1 and US2 — can be implemented concurrently
- US2 is completely independent of US1 and US3 — can be implemented concurrently
- US4 must wait for US1 (same-file dependency)

---

## Parallel Example: User Story 1

```bash
# Launch all models/interfaces for US1 together:
Task: "Create BenchmarkLogEntry model in src/Vimium/Models/BenchmarkLogEntry.cs"
Task: "Create IBenchmarkService interface in src/Vimium/Services/Interfaces/IBenchmarkService.cs"

# Then implement services (sequential — same file):
Task: "Implement pattern-availability filtering in UiAutomationHintProviderService.cs"
Task: "Implement tree trimming in UiAutomationHintProviderService.cs"
Task: "Implement result caching in UiAutomationHintProviderService.cs"

# Launch all tests for US1 in parallel:
Task: "Unit tests for UiAutomationHintProviderService filtering in src/Vimium.Tests/Services/"
Task: "Unit tests for BenchmarkService in src/Vimium.Tests/Services/"
Task: "BenchmarkLogEntry roundtrip tests in src/Vimium.Tests/Models/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (T001–T002)
2. Complete Phase 3: User Story 1 (T003–T018)
3. **STOP and VALIDATE**: Run benchmark procedure — verify P95 < 750ms on Wikipedia/Singapore
4. Deploy MVP — performance improvement alone delivers immediate user value

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP! 🎯)
3. Add User Story 2 → Test independently → Deploy/Demo (customizable actions)
4. Add User Story 3 → Test independently → Deploy/Demo (no more overlapping labels)
5. Add User Story 4 → Test independently → Deploy/Demo (filtered hints)
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Foundational together (T001–T002)
2. Once Foundational is done:
   - Developer A: User Story 1 (performance + benchmarking)
   - Developer B: User Story 2 (action customization)
   - Developer C: User Story 3 (overlap resolution)
3. After US1 completes: Developer A picks up US4 (per-action filtering)
4. Stories complete and integrate independently
5. Final integration testing in Phase 7

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks within the same phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Tests follow xUnit AAA pattern per constitution Principle III — test names: `MethodName_Scenario_ExpectedBehavior`
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same-file conflicts across parallel phases, cross-story dependencies that break independence
- Pattern-availability properties for element-mode patterns (Invoke, Toggle, SelectionItem, ExpandCollapse, Value, RangeValue) are confirmed reliable — the TextPattern quirk documented in `FindTextProviderService` does not apply
- UIA COM is STA-threaded — no parallel enumeration. Performance gains come from provider-side filtering and result caching
- Benchmark log is local-only (constitution: no telemetry)
- The existing `Hint.MovePointerToCenter()` method is used unchanged for both MoveMouse and Hover actions
- Wrap `Hint.Invoke()`, `Click()`, `RightClick()`, `MovePointerToCenter()` calls in `Task.Run` to avoid blocking the keyboard hook thread (existing pattern in `OverlayViewModel.MatchString`)
