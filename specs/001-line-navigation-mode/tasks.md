# Tasks: Line Navigation Mode

**Input**: Design documents from `/specs/001-line-navigation-mode/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: INCLUDED — TDD per Constitution Principle III. Test tasks are written BEFORE implementation tasks within each user story phase.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Source: `src/Vimium/` for production code, `src/Vimium.Tests/` for tests
- Models: `src/Vimium/Models/`
- Services: `src/Vimium/Services/`, interfaces in `src/Vimium/Services/Interfaces/`
- ViewModels: `src/Vimium/ViewModels/`
- Views: `src/Vimium/Views/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Extend configuration model and register new hotkey — minimal surface area, no new files yet

- [X] T001 [P] Add `LineNavigationModifier` and `CopyModifier` properties to `VimiumConfig` in `src/Vimium/Models/VimiumConfig.cs` with defaults `"Ctrl+."` and `"Ctrl"`
- [X] T002 [P] Add `LineNavigationModifier` and `CopyModifier` convenience properties to `ConfigService` in `src/Vimium/Services/ConfigService.cs` (auto-save pattern matching existing properties)
- [X] T003 Register `LineNavigationHotKey` property and `OnLineNavigationHotKeyActivated` event in `KeyListenerService` in `src/Vimium/Services/KeyListenerService.cs` (follow existing `HotKey`/`OnHotKeyActivated` pattern)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core models and service interfaces that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundational

- [X] T004 [P] Write tests for `TextLineHint` model in `src/Vimium.Tests/Models/TextLineHintTest.cs` — verify construction, validation, non-null text, positive rect
- [X] T005 [P] Write tests for `LineNavigationSession` model in `src/Vimium.Tests/Models/LineNavigationSessionTest.cs` — verify hint collection, window handle binding
- [X] T006 [P] Write tests for extended `VimiumConfig` in `src/Vimium.Tests/Models/VimiumConfigTest.cs` — verify new fields serialize/deserialize with camelCase, absent keys → defaults

### Implementation for Foundational

- [X] T007 [P] Create `TextLineHint` model in `src/Vimium/Models/TextLineHint.cs` — extend `Hint` base class, add `TextContent` string property
- [X] T008 [P] Create `LineNavigationSession` model in `src/Vimium/Models/LineNavigationSession.cs` — mirror `HintSession` with `IList<TextLineHint> Hints`, `OwningWindow`, `OwningWindowBounds`
- [X] T009 [P] Create `ILineHintProviderService` interface in `src/Vimium/Services/Interfaces/ILineHintProviderService.cs` — `EnumLineHints()` (foreground), `EnumLineHints(IntPtr)`, `EnumLineHintsAsync(IntPtr)` returning `LineNavigationSession`
- [X] T010 [P] Create `ClipboardService` in `src/Vimium/Services/ClipboardService.cs` — `SetText(string)` with retry loop (3 attempts, 50ms delay) on `COMException`
- [X] T011 Wire `LineNavigationHotKey` in `App.xaml.cs` in `src/Vimium/App.xaml.cs` — subscribe to `OnLineNavigationHotKeyActivated`, trigger `ShowLineNavigationOverlay()` (stub for now, filled in US1)

**Checkpoint**: Foundation ready — models, interfaces, clipboard service, hotkey registration in place. All foundational tests pass. User story implementation can now begin.

---

## Phase 3: User Story 1 - Activate Line Navigation Mode (Priority: P1) 🎯 MVP

**Goal**: Press `Ctrl+.` → overlay appears with hint labels on each visible text line. Escape dismisses. Element mode (`Ctrl+;`) unchanged.

**Independent Test**: Press `Ctrl+.` in Notepad with multi-line text → hint labels appear on each visible line. Press `Ctrl+;` → element hints still work.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T012 [P] [US1] Write tests for `UiAutomationLineHintProviderService` in `src/Vimium.Tests/Services/UiAutomationLineHintProviderServiceTest.cs` — mock UIA to return known `TextRange` objects, verify hints have correct `TextContent` and `BoundingRectangle`
- [X] T013 [P] [US1] Write tests for `LineNavigationOverlayViewModel` in `src/Vimium.Tests/ViewModels/LineNavigationOverlayViewModelTest.cs` — verify `PopulateHints` assigns labels, `IsLoading` transitions, `MatchString` filters hints

### Implementation for User Story 1

- [X] T014 [US1] Create `UiAutomationLineHintProviderService` in `src/Vimium/Services/UiAutomationLineHintProviderService.cs` — implement `ILineHintProviderService` using `IUIAutomationTextPattern.GetVisibleRanges()`, `CacheRequest` for batched property retrieval, background-thread `EnumLineHintsAsync` pattern matching existing `EnumHintsAsync`
- [X] T015 [P] [US1] Create `LineNavigationOverlayViewModel` in `src/Vimium/ViewModels/LineNavigationOverlayViewModel.cs` — constructor overloads (loading + ready), `PopulateHints(LineNavigationSession, IHintLabelService)`, `MatchString` property for progressive hint filtering, `IsLoading` property, `CloseOverlay`/`OnHintResolved` actions
- [X] T016 [US1] Create `LineNavigationOverlayView` XAML in `src/Vimium/Views/LineNavigationOverlayView.xaml` — transparent WPF window with `WS_EX_TRANSPARENT`, `WindowStyle=None`, `AllowsTransparency=True`, `Topmost=True`, `ShowInTaskbar=False`. Hint labels rendered as `TextBlock` elements positioned at each `TextLineHint.BoundingRectangle` using `Canvas`. Loading indicator shown when `IsLoading` is true.
- [X] T017 [US1] Create `LineNavigationOverlayView` code-behind in `src/Vimium/Views/LineNavigationOverlayView.xaml.cs` — minimal: `DataContext` binding, window positioning from `OwningWindowBounds`, close on `Escape` key
- [X] T018 [US1] Implement `ShowLineNavigationOverlay()` in `src/Vimium/App.xaml.cs` — enumerate on background thread via `EnumLineHintsAsync`, show loading overlay immediately, then call `PopulateHints`. Wire `KeyboardHookService` for hint-character input. Close on `Escape`.

**Checkpoint**: `Ctrl+.` shows text-line hints. `Escape` dismisses. `Ctrl+;` still shows element hints. All US1 tests pass.

---

## Phase 4: User Story 2 - Navigate Lines by Typing Hint Labels (Priority: P1)

**Goal**: Type a hint label (without Ctrl held) → cursor moves to that line's center. Progressive filtering as user types.

**Independent Test**: Activate line overlay, type a hint label → cursor jumps to the center of the corresponding text line. Type partial prefix → only matching hints highlighted.

### Tests for User Story 2

- [X] T019 [P] [US2] Extend `LineNavigationOverlayViewModelTest` in `src/Vimium.Tests/ViewModels/LineNavigationOverlayViewModelTest.cs` — verify `MatchString` setter: unique match triggers `OnHintResolved` with `copyModifierHeld=false`, partial match highlights subset, no match keeps all visible

### Implementation for User Story 2

- [X] T020 [US2] Implement hint resolution in `LineNavigationOverlayViewModel.MatchString` setter in `src/Vimium/ViewModels/LineNavigationOverlayViewModel.cs` — unique match without copy modifier → fire `OnHintResolved(hint, copyModifierHeld: false)`, close overlay, move cursor via `hint.MovePointerToCenter()` (reuse existing method since `TextLineHint` extends `Hint`)
- [X] T021 [US2] Wire `OnHintResolved` in `src/Vimium/App.xaml.cs` — for navigation case (copyModifierHeld=false): close overlay, move cursor to hint center, no clipboard action
- [X] T022 [US2] Implement `OnHintResolved` handler for navigation in `src/Vimium/App.xaml.cs` — close line overlay, invoke `hint.MovePointerToCenter()` on background thread

**Checkpoint**: Typing a full hint label moves cursor to the correct line. Partial typing filters hints. All US1+US2 tests pass.

---

## Phase 5: User Story 3 - Copy Text from a Line with Sub-Line Selection (Priority: P1)

**Goal**: Hold Ctrl + type hint → enter selection mode. Search (incremental), Tab-cycle matches, arrow-key cursor movement, Shift+arrow selection, Enter copies (whole line or selection), Esc cancels.

**Independent Test**: Ctrl + hint on a line → Enter copies whole line. Ctrl + hint, type search → cursor jumps, Tab cycles, Shift+Ctrl+arrow selects, Enter copies selection. Esc cancels without copy.

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T023 [P] [US3] Write tests for `SelectionState` model in `src/Vimium.Tests/Models/SelectionStateTest.cs` — verify cursor movement (char, word, Home/End), selection extension (Shift+arrow, Ctrl+Shift+arrow), search match generation, Tab cycling (forward, backward, wrap), selected text extraction, empty search → no matches, zero-match search → cursor unchanged
- [X] T024 [P] [US3] Write tests for `SelectionModeViewModel` in `src/Vimium.Tests/ViewModels/SelectionModeViewModelTest.cs` — verify `HandleCharacter` appends to search and updates cursor, `HandleBackspace`, `HandleEnter` copies selected text (or whole line if no selection), `HandleEscape` closes without copy, `HandleTab`/`HandleShiftTab` cycles matches, cursor bounds enforcement
- [X] T025 [P] [US3] Write tests for `ClipboardService` in `src/Vimium.Tests/Services/ClipboardServiceTest.cs` — verify `SetText` succeeds, retry on `COMException`, throws `InvalidOperationException` after all retries exhausted

### Implementation for User Story 3

- [X] T026 [US3] Create `SelectionState` model in `src/Vimium/Models/SelectionState.cs` — all fields from data-model.md: `TargetedLine`, `VisibleText`, `AllVisibleLines`, `CursorPosition`, `SelectionStart`, `SelectionEnd`, `SearchQuery`, `SearchMatches`, `ActiveMatchIndex`. Implement derived properties `SelectedText`, `HasSelection`, `CursorLineIndex`, `CursorLinePosition`. Implement all mutation methods for cursor movement (char step, word step via `Ctrl+Arrow` boundary detection), selection extension, search (case-insensitive `IndexOf` across `VisibleText`), and Tab cycling (circular wrap).
- [X] T027 [US3] Create `SelectionModeViewModel` in `src/Vimium/ViewModels/SelectionModeViewModel.cs` — constructor takes `TextLineHint targetedLine`, `IReadOnlyList<TextLineHint> allLines`, `Rect windowBounds`, `ClipboardService`. Expose public properties for data binding. Implement all `Handle*` methods: `HandleCharacter`, `HandleBackspace`, `HandleArrow`, `HandleCtrlArrow`, `HandleShiftArrow`, `HandleCtrlShiftArrow`, `HandleHome`, `HandleEnd`, `HandleTab`, `HandleEnter` (copy + close), `HandleEscape` (close no copy). `HandleEnter` with no selection → copies `TargetedLine.TextContent` (whole line fast path).
- [X] T028 [US3] Create `SelectionModeOverlayView` XAML in `src/Vimium/Views/SelectionModeOverlayView.xaml` — transparent WPF window (same style as `LineNavigationOverlayView`). Render: text cursor indicator (`|` or thin vertical line at cursor position), selection highlight (semi-transparent blue rectangle over selected range), search match highlights (semi-transparent yellow rectangles for all matches, distinct color for active match). Use `Canvas` for pixel-positioned elements based on `AllVisibleLines` bounding rectangles + character offsets.
- [X] T029 [US3] Create `SelectionModeOverlayView` code-behind in `src/Vimium/Views/SelectionModeOverlayView.xaml.cs` — minimal: `DataContext` binding to `SelectionModeViewModel`, window positioning from `windowBounds`, close on `Escape` via ViewModel
- [X] T030 [US3] Wire `OnHintResolved` with `copyModifierHeld=true` in `src/Vimium/App.xaml.cs` — close line overlay, immediately open `SelectionModeOverlayView` with the targeted line + all visible lines + `ClipboardService`. Pass selection-mode keys from `KeyboardHookService` to `SelectionModeViewModel.Handle*` methods. On copy feedback: brief flash/tooltip showing "Copied!"
- [X] T031 [US3] Extend `KeyboardHookService` in `src/Vimium/Services/KeyboardHookService.cs` — add mode flag (`IsSelectionModeActive`). When active, swallow navigation keys (`←`, `→`, `Ctrl+←`, `Ctrl+→`, `Shift+←`, `Shift+→`, `Ctrl+Shift+←`, `Ctrl+Shift+→`, `Home`, `End`, `Tab`, `Shift+Tab`, `Enter`, `Escape`, `Backspace`) and dispatch to `SelectionModeViewModel`. Pass all other keys through to underlying application.

**Checkpoint**: Full copy workflow works: Ctrl+hint → Enter (whole line), Ctrl+hint → search → Tab → Shift+arrow → Enter (portion). All US1–US3 tests pass.

---

## Phase 6: User Story 4 - Configure Navigation Mode (Priority: P2)

**Goal**: Line-navigation hotkey and copy modifier are configurable in Options → Keyboard. Changes take effect immediately (auto-save).

**Independent Test**: Open Options → Keyboard, change line-navigation hotkey to `Ctrl+/`, press new hotkey → line overlay appears. Change copy modifier → new modifier works.

### Tests for User Story 4

- [X] T032 [P] [US4] Write tests for `ConfigService` line-navigation properties in `src/Vimium.Tests/Services/ConfigServiceTest.cs` — verify `LineNavigationModifier` and `CopyModifier` get/set persist to JSON, `IsDirty` tracking, cancel/restore, defaults

### Implementation for User Story 4

- [X] T033 [US4] Add `LineNavigationModifier` and `CopyModifier` bindings to `KeyboardSettingsViewModel` in `src/Vimium/ViewModels/KeyboardSettingsViewModel.cs` — expose properties that delegate to `ConfigService.Instance`. Include duplicate hotkey validation (must not equal `OverlayModifier` or `TaskbarModifier`).
- [X] T034 [US4] Add "Line Navigation" hotkey field and "Copy Modifier" dropdown to `KeyboardSettingsView` XAML in `src/Vimium/Views/OptionsView.xaml` — new row in Keyboard tab for line-navigation activation hotkey (text input with hotkey format hint `Ctrl+.`), and dropdown for copy modifier (`Ctrl`/`Alt`/`Shift`)
- [X] T035 [US4] Wire `ConfigService.PropertyChanged` in `KeyListenerService` in `src/Vimium/Services/KeyListenerService.cs` — when `LineNavigationModifier` changes, unregister old hotkey and register new one (same pattern as existing `HotKey` property setter). When `CopyModifier` changes, update the modifier check in `App.xaml.cs`.

**Checkpoint**: Options → Keyboard shows line-nav settings. Changes apply immediately. All US1–US4 tests pass.

---

## Phase 7: User Story 5 - Toggle Between Element and Line Modes (Priority: P2)

**Goal**: Users can seamlessly switch between element mode (`Ctrl+;`) and line mode (`Ctrl+.`) without opening settings. Each mode's overlay is visually distinct and operates independently.

**Independent Test**: Activate element mode → dismiss → activate line mode → dismiss → repeat 3 times. No interference, no visual corruption, no memory growth.

### Tests for User Story 5

- [X] T036 [P] [US5] Write integration-style verification in `src/Vimium.Tests/ViewModels/` — verify `LineNavigationOverlayViewModel` and `OverlayViewModel` do not share mutable state, each has independent `Hints` collections, closing one does not affect the other

### Implementation for User Story 5

- [X] T037 [US5] Add duplicate hotkey guard in `KeyboardSettingsViewModel` in `src/Vimium/ViewModels/KeyboardSettingsViewModel.cs` — when setting `LineNavigationModifier`, validate it does not equal `OverlayModifier` or `TaskbarModifier`. Show inline validation error if duplicate.
- [X] T038 [US5] Ensure mode isolation in `src/Vimium/App.xaml.cs` — verify element-mode overlay open/close lifecycle does not touch line-navigation state, and vice versa. Ensure both overlays can be opened/closed in any sequence without state leakage. Add guard: `ShowLineNavigationOverlay()` is no-op if element overlay is currently visible (and vice versa) — only one overlay at a time.

**Checkpoint**: Both modes coexist peacefully. Repeated toggling works. All tests pass (full suite).

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, edge-case hardening, and user feedback improvements

- [X] T039 [P] Handle zero-text window gracefully in `src/Vimium/Services/UiAutomationLineHintProviderService.cs` — return `LineNavigationSession` with empty `Hints`; show "No text lines found" tooltip and auto-dismiss after 1s in `LineNavigationOverlayViewModel`
- [X] T040 [P] Add copy confirmation feedback in `src/Vimium/Views/SelectionModeOverlayView.xaml` — brief "Copied!" text that fades out after 500ms when `OnCopied` callback fires
- [X] T041 [P] Ensure selection mode highlight colors use theme resources in `src/Vimium/Views/SelectionModeOverlayView.xaml` — cursor color, selection highlight, search match highlight, active match highlight all from theme resource dictionary
- [X] T042 Run `dotnet test src\Vimium.sln` — verify ALL tests pass (existing + new) with zero failures
- [X] T043 Run quickstart validation in `specs/001-line-navigation-mode/quickstart.md` — execute all 9 manual validation scenarios (VS-1 through VS-9) and confirm pass
- [X] T044 [P] Run `dotnet build src\Vimium.sln` — verify zero warnings
- [X] T045 Update `CHANGELOG.md` and `README.md` — document line-navigation feature, new hotkeys, copy workflow

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (models need config fields; hotkey wiring needs registration) — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion
- **User Story 2 (Phase 4)**: Depends on Phase 3 (needs line overlay to exist)
- **User Story 3 (Phase 5)**: Depends on Phase 3 (needs line overlay and `OnHintResolved` hook). Independent of US2.
- **User Story 4 (Phase 6)**: Depends on Phase 2 (config model + hotkey registration). Independent of US1-US3 implementation (can configure before features exist, or after).
- **User Story 5 (Phase 7)**: Depends on Phase 3 + Phase 4 + Phase 5 (needs both modes to be functional)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 1 (Setup) ──▶ Phase 2 (Foundational) ──┬──▶ Phase 3 (US1) ──┬──▶ Phase 4 (US2)
                                              │                     │
                                              ├──▶ Phase 6 (US4)    └──▶ Phase 5 (US3)
                                              │                              │
                                              └──────────────────────────────┤
                                                                             ▼
                                                                   Phase 7 (US5) ──▶ Phase 8 (Polish)
```

- **US1 (P1)**: Foundation for US2 and US3. Can start immediately after Phase 2.
- **US2 (P1)**: Needs US1 overlay. Adds navigation action.
- **US3 (P1)**: Needs US1 overlay. Adds selection mode. Can be developed in parallel with US2 (different ViewModels/Views).
- **US4 (P2)**: Config-only. Can be done in parallel with US1-US3.
- **US5 (P2)**: Integration — needs everything else complete.

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Models before services
- Services before ViewModels (for US3)
- ViewModels before Views
- Core implementation before integration wiring

### Parallel Opportunities

- **Phase 1**: T001, T002 can run in parallel (different files)
- **Phase 2**: T004, T005, T006 (all tests) can run in parallel. T007, T008, T009, T010 can run in parallel after tests written.
- **Phase 3 (US1)**: T012 and T013 (tests) can run in parallel. T014 and T015 can run in parallel after tests.
- **Phase 4 (US2)**: T019 is extension of existing test.
- **Phase 5 (US3)**: T023, T024, T025 (tests) all in parallel. T026 and T027 in parallel after tests.
- **Phase 6 (US4)**: T032 (test) can run separately. T033 and T034 are sequential (ViewModel → View).
- **Phase 7 (US5)**: T036 (test) first, then T037 and T038 sequential.
- **Phase 8**: T039, T040, T041, T044 all in parallel.
- **Cross-phase**: US2 and US3 can be implemented in parallel (both depend on US1, different files). US4 can be done alongside US1-US3.

---

## Parallel Example: User Story 3 (most complex story)

```bash
# Step 1: Launch all US3 tests together (must fail first):
Task: "Write tests for SelectionState model in src/Vimium.Tests/Models/SelectionStateTest.cs"
Task: "Write tests for SelectionModeViewModel in src/Vimium.Tests/ViewModels/SelectionModeViewModelTest.cs"
Task: "Write tests for ClipboardService in src/Vimium.Tests/Services/ClipboardServiceTest.cs"

# Step 2: After tests written, launch models + clipboard in parallel:
Task: "Create SelectionState model in src/Vimium/Models/SelectionState.cs"
Task: "Ensure ClipboardService in src/Vimium/Services/ClipboardService.cs passes tests"

# Step 3: ViewModel depends on SelectionState:
Task: "Create SelectionModeViewModel in src/Vimium/ViewModels/SelectionModeViewModel.cs"

# Step 4: Views + integration depend on ViewModel:
Task: "Create SelectionModeOverlayView XAML..."
Task: "Create SelectionModeOverlayView code-behind..."
Task: "Wire OnHintResolved with copyModifierHeld=true..."
Task: "Extend KeyboardHookService for selection mode keys..."
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T011)
3. Complete Phase 3: User Story 1 (T012–T018)
4. **STOP and VALIDATE**: `Ctrl+.` shows text-line hints. Element mode still works. Run quickstart VS-1.
5. Demo if ready — line hints visible is a demonstrable increment.

### Incremental Delivery (Recommended)

1. Setup + Foundational → foundation ready (all tests pass)
2. Add US1 → line hints visible (demo: text lines labeled)
3. Add US2 → cursor navigation works (demo: jump to any line with keyboard)
4. Add US3 → copy + selection mode works (demo: copy commands from tutorials)
5. Add US4 → hotkey configurable in Options (demo: change settings, see effect)
6. Add US5 → mode isolation verified (demo: seamless switching)
7. Polish → hardened edge cases, all quickstart scenarios pass

### Suggested MVP Scope

**MVP = Phase 1 + Phase 2 + Phase 3 + Phase 4 + Phase 5** (US1, US2, US3 — all P1 stories). This delivers the complete core value proposition: navigate text lines, jump cursor, copy whole lines or portions via search + selection. US4 (config UI) and US5 (toggle validation) can ship in a follow-up release with the default hotkeys working immediately.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing (TDD red-green-refactor)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Element mode (`OverlayViewModel`, `OverlayView`) is **untouched** — all new files are parallel
