# Tasks: Find-and-Navigate Text Mode (Ctrl+F Style)

**Input**: Design documents from `/specs/001-line-navigation-mode/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅, contracts/ ✅

**Phase 1 teardown** (files already deleted in working tree): TextLineHint.cs, LineNavigationSession.cs, LineNavigationOverlayViewModel.cs, LineNavigationOverlayView.xaml/.xaml.cs, ILineHintProviderService.cs, UiAutomationLineHintProviderService.cs

**Tests**: Included per Constitution Principle III (≥80% coverage mandatory) and plan.md test coverage requirements.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete [P] tasks)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- File paths are relative to repository root

---

## Phase 1: Final Cleanup

**Purpose**: Remove remaining files from the old "load-all-text" design that weren't part of Phase 1 teardown.

- [X] T001 [P] Delete `TextSource.cs` from `src/Vimium/Models/TextSource.cs` — no longer needed; replaced by query-driven `FindResult` + `SearchResult`
- [X] T002 [P] Delete `TextLineRect.cs` from `src/Vimium/Models/TextLineRect.cs` — per-line rects replaced by per-match rects from UIA
- [X] T003 Clean up remaining references to deleted files (TextSource, TextLineRect) in `src/Vimium/Vimium.csproj` and any `using` statements in dependent files

---

## Phase 2: Foundational — Data Models

**Purpose**: Core data entities that ALL user stories depend on. Must complete before any story implementation.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Create `FindSession` model in `src/Vimium/Models/FindSession.cs` with fields: `SearchQuery` (string), `Matches` (IReadOnlyList\<SearchMatch>), `ActiveMatchIndex` (int), `SourceWindowHandle` (IntPtr), `IsSearching` (bool), `HasMatches` (bool, derived), `MatchCountText` (string, derived: "0 matches" / "2 of 5" / "" when empty query). Implement `INotifyPropertyChanged` for all settable properties.
- [X] T005 [P] Rewrite `SearchMatch` in `src/Vimium/Models/SearchMatch.cs` — remove deprecated `StartIndex`, `EndIndex`, `LineIndex` fields; keep `SourceText`, `BoundingRect`, `Source`, `IsActive`; add `TextRangeProvider` (`System.Windows.Automation.Text.ITextRangeProvider?`) for ScrollIntoView+Select on Enter. Add validation: SourceText must not be null/empty, BoundingRect must have positive Width/Height.
- [X] T006 Rewrite `SelectionState` in `src/Vimium/Models/SelectionState.cs` — remove `CursorPosition`, `SelectionStart`, `SelectionEnd`, `AllVisibleLines`, `VisibleText`; keep `SearchQuery`, `SearchMatches`, `ActiveMatchIndex`; add `IsSearching` (bool), `MatchCountText` (string). Simplify to find-only state container (backed by `FindSession`). Ensure all settable properties raise `PropertyChanged`.

**Checkpoint**: Data models ready — service layer and user stories can now begin

---

## Phase 3: Foundational — Service Layer

**Purpose**: Service interface and implementation that US1/US3 depend on. Must complete before ViewModel work.

- [X] T007 Rename `ITextSourceProviderService` → `IFindTextProviderService` in `src/Vimium/Services/Interfaces/IFindTextProviderService.cs` — remove `GetTextSource(IntPtr)` / `GetTextSourceAsync(IntPtr)` methods; add single method: `Task<FindResult> SearchAsync(IntPtr hWnd, string query, CancellationToken ct)`. Update interface namespace and XML docs to document primary/fallback paths, 200-match cap, and 3-second timeout.
- [X] T008 Rewrite `TextSourceProviderService` → `FindTextProviderService` in `src/Vimium/Services/FindTextProviderService.cs`:
  - **Primary path**: Get `AutomationElement.FromHandle(hWnd)`, resolve `ITextProvider` via `TextPattern`, call `GetVisibleRanges()` to scope to visible viewport, loop `ITextRangeProvider.FindText(query, backward=false, startRange)` collecting matches (extract `GetBoundingRectangles()` and `GetText(-1)`), enforce 200-match cap and 3-second `CancellationToken` timeout, return `SearchResult` with `Source=TextPattern` and `TextRangeProvider` reference.
  - **Fallback path**: If TextPattern unsupported or timeout, call `FindAllBuildCache(TreeScope.Descendants, Condition.TrueCondition, cacheRequest)` with `CacheRequest` for `Name` + `BoundingRectangle`, filter client-side `Cached.Name.Contains(query, OrdinalIgnoreCase)`, return `SearchResult` with `Source=ElementName` and `AutomationElement` reference.
  - **Both paths fail**: Return `FindResult` with empty `Matches`.
  - All UIA calls run via `Task.Run` off UI thread. Accept `CancellationToken` for debounce cancellation.

**Checkpoint**: Service layer ready — ViewModel and user story implementation can now begin

---

## Phase 4: User Story 1+3 — Find and Navigate to Text with Fast Performance (Priority: P1) 🎯 MVP

**Goal**: User activates find-text mode via `Ctrl+.`, types a search query (≥5 chars), sees matching text highlighted (yellow=all, orange=active), cycles matches with Tab/Shift+Tab, and presses Enter to navigate the cursor to the active match and close the overlay. All operations complete within performance budgets (overlay <100ms, search results <200ms, Tab cycling <50ms, Enter navigation <200ms).

**Stories covered**:
- **US1**: Find and Navigate to Text — core search/cycle/navigate interaction
- **US3**: Fast Performance with Large Text — debounce, viewport scoping, timeout, cancellation, 200-match cap

**Independent Test**: Activate mode with `Ctrl+.` on a text-heavy window (e.g., Wikipedia in Chrome), type a 5+ character phrase visible on screen, verify all occurrences highlighted yellow with first orange, Tab/Shift+Tab cycle matches with circular wrap, Enter positions cursor at match and closes overlay. Verify no "loading all text" step — overlay opens instantly with empty search bar.

### Implementation for User Story 1+3

- [X] T009 [US1] Implement search invocation with debounce in `src/Vimium/ViewModels/SelectionModeViewModel.cs`:
  - Add `System.Timers.Timer` with 150ms interval, reset on each keystroke
  - Add 5-character minimum gate — search only triggers when `SearchQuery.Length >= 5`
  - Add `CancellationTokenSource` to cancel in-flight search on new keystroke
  - On debounce elapsed: set `IsSearching=true`, call `IFindTextProviderService.SearchAsync(hWnd, query, ct)`, update `FindSession` with results, set `IsSearching=false`, notify all bindings
  - Handle `OperationCanceledException` gracefully (expected on rapid typing)
  - If query drops below 5 chars (via Backspace), clear matches and highlights

- [X] T010 [US1] Implement input handlers in `src/Vimium/ViewModels/SelectionModeViewModel.cs`:
  - `HandleCharacter(char c)`: Append printable character to `SearchQuery`, reset debounce timer
  - `HandleBackspace()`: Remove last character from `SearchQuery`, reset debounce; clear matches if query < 5 chars
  - `HandleTab(bool shift)`: Cycle `ActiveMatchIndex` with circular wrap — forward (shift=false): `(index + 1) % count`, backward (shift=true): `(index - 1 + count) % count`; no-op if `Matches.Count == 0`; update all `IsActive` flags
  - `HandleEnter()`: On active match's `TextRangeProvider`, call `ScrollIntoView()` then `Select()`; catch `COMException` (stale element) and silently dismiss; close overlay immediately, no toast
  - `HandleEscape()`: Dismiss overlay without navigation
  - `HandleFocusLost()`: Dismiss overlay on window change (called from view code-behind)
  - Remove all deleted handlers: `HandleArrow`, `HandleCtrlArrow`, `HandleShiftArrow`, `HandleCtrlShiftArrow`, `HandleHome`, `HandleEnd`, `HandleContentChanged`
  - Remove `ClipboardService` dependency, `OnCopied` action, `SelectedText` property, copy-related logic

- [X] T011 [P] [US1] Update `SelectionModeOverlayView.xaml` in `src/Vimium/Views/SelectionModeOverlayView.xaml`:
  - Search bar at bottom: `TextBox` bound to `SearchQuery` (read-only display, no focus-steal), `TextBlock` bound to `MatchCountText` ("2 of 5" / "0 matches" / ""), loading spinner bound to `IsSearching` (Visibility converter)
  - Match highlights: `ItemsControl` bound to `Matches`, each item rendered as a `Border` positioned absolutely at `BoundingRect` within overlay canvas; `IsActive == true` → orange semi-transparent, `IsActive == false` → yellow semi-transparent, using theme `ResourceDictionary` brush keys (no hardcoded colors)
  - Use existing `ForegroundWindow` base class pattern: `WS_EX_TRANSPARENT`, `WindowStyle=None`, `AllowsTransparency=True`, `Topmost=True`, `ShowActivated=False`
  - Ensure all colors reference theme resource keys

- [X] T012 [US1] Update `SelectionModeOverlayView.xaml.cs` in `src/Vimium/Views/SelectionModeOverlayView.xaml.cs`:
  - Wire low-level `KeyboardHookService` to dispatch captured keys: printable chars → `HandleCharacter`, Backspace → `HandleBackspace`, Tab → `HandleTab(false)`, Shift+Tab → `HandleTab(true)`, Enter → `HandleEnter`, Escape → `HandleEscape`
  - Ignore (pass through to underlying window): Arrow keys, Ctrl+Arrow, Shift+Arrow, Home, End, Ctrl+C/V/other Ctrl combos
  - On each keyboard event: poll `User32.GetForegroundWindow()` and compare against source hWnd → if different, call `HandleFocusLost()`
  - On overlay shown: capture initial foreground window bounds, position overlay to match
  - Remove any old arrow/selection key handling from code-behind

- [X] T013 [US1] Implement UIA `TextChanged` event handler for content-change auto-dismiss in `src/Vimium/Views/SelectionModeOverlayView.xaml.cs`:
  - After first successful search, register `Automation.AddAutomationEventHandler(TextPattern.TextChangedEvent, element, TreeScope.Subtree, OnTextChanged)`
  - In handler: set `_contentChanged` flag
  - On next keyboard event: if `_contentChanged` is true, call `HandleFocusLost()` (deferred dismiss)
  - Unregister event handler on overlay close
  - Catch and ignore registration failures (non-TextPattern apps won't fire this event)

- [X] T014 [P] [US1] Update `ShellViewModel` in `src/Vimium/ViewModels/ShellViewModel.cs`:
  - Wire `Ctrl+.` hotkey to open `SelectionModeOverlayView` directly (no text extraction step — just open overlay with empty search bar)
  - Pass `IFindTextProviderService`, foreground window bounds (`Rect`), and source window handle (`IntPtr`) to `SelectionModeViewModel` constructor
  - Ensure only one overlay at a time: if `Ctrl+;` (element mode) is active, `Ctrl+.` replaces it, and vice versa
  - Handle overlay lifecycle: create window, set DataContext, show, hook close/dismiss callback

- [X] T015 [US1] Update `App.xaml.cs` in `src/Vimium/App.xaml.cs`:
  - Register `IFindTextProviderService` → `FindTextProviderService` in service resolution/locator
  - Remove any remaining references to `ITextSourceProviderService`, `ILineHintProviderService`, or other deleted services
  - Ensure DI/wiring is consistent with existing pattern used for `IHintProviderService`

- [X] T016 [US1] Handle edge cases in `SelectionModeViewModel` and `FindTextProviderService`:
  - **No matches**: Display "0 matches", no highlights, Tab no-op, Enter no-op
  - **No accessible text** (both TextPattern and element-name paths empty): Show "No text found", auto-dismiss after 2 seconds
  - **TextPattern timeout** (3s): Automatically fall back to element-name search; if fallback also fails, show "No text found" and auto-dismiss after 2s
  - **Stale TextRangeProvider on Enter**: Catch `COMException`, silently dismiss overlay
  - **Overlapping matches**: Each distinct occurrence is a separate match — no dedup needed
  - **Maximum input length**: Cap search bar input at 200 characters
  - **Case-insensitive search**: Use `OrdinalIgnoreCase` in all string comparisons

- [X] T017 [US1] Add debug logging via `Microsoft.Extensions.Logging`:
  - In `FindTextProviderService`: log search path chosen (TextPattern vs ElementName vs both-failed), match count, elapsed time, timeout occurrences
  - In `SelectionModeViewModel`: log debounce triggers, search cancellations, match count updates, Enter navigation (success/failure), auto-dismiss triggers (window change, content change)

**Checkpoint**: Core find-and-navigate mode fully functional — user can search, cycle matches, and navigate cursor. All performance budgets met (overlay <100ms, results <200ms, Tab <50ms, Enter <200ms).

---

## Phase 5: User Story 2 — Activate and Configure Find-Text Mode (Priority: P2)

**Goal**: User can change the find-text activation hotkey in Options → Keyboard, and the new hotkey takes effect immediately.

**Independent Test**: Open Options → Keyboard, change "Find Text" hotkey, verify new hotkey activates mode and old hotkey no longer works. Verify `Ctrl+;` (element mode) still works independently.

### Implementation for User Story 2

- [X] T018 [P] [US2] Add `FindTextHotkey` setting to `VimiumConfig` model in configuration file and `ConfigService`:
  - Add `FindTextHotkey` property (default: `"Ctrl+."`) alongside existing hotkey settings
  - Add serialization/deserialization support in `System.Text.Json` config handling
  - Persist to `%APPDATA%\Vimium\config.json` with auto-save on change

- [X] T019 [US2] Update `OptionsView.xaml` and `OptionsViewModel` in `src/Vimium/Views/OptionsView.xaml` and `src/Vimium/ViewModels/OptionsViewModel.cs`:
  - Add "Find Text" row in Keyboard section with hotkey capture control (same pattern as existing element-mode hotkey)
  - Bind to `FindTextHotkey` property
  - Validate no conflict with element-mode hotkey (`Ctrl+;`)
  - Auto-save on change (no explicit Save button per existing Options pattern)

- [X] T020 [US2] Update `ShellViewModel` hotkey reading in `src/Vimium/ViewModels/ShellViewModel.cs`:
  - Read `FindTextHotkey` from `ConfigService` instead of hardcoded `Ctrl+.`
  - Register/unregister keyboard hook for the configured hotkey
  - Reload hotkey binding on config change notification
  - Ensure hotkey change takes effect immediately without restart

- [X] T021 [US2] Add mutual exclusion with element mode hotkey:
  - On options save, validate `FindTextHotkey != ElementModeHotkey`
  - Show inline validation error if they conflict
  - Default `Ctrl+.` adjacent to `Ctrl+;` for discoverability

**Checkpoint**: Find-text mode fully configurable — user can change hotkey via Options, changes apply immediately, element mode unaffected.

---

## Phase 6: Unit Tests

**Purpose**: Achieve ≥80% line coverage on all new and rewritten non-view, non-interop code per Constitution Principle III.

**⚠️ Tests should be written alongside or immediately following each implementation task. This phase consolidates and validates all tests.**

### Test Implementation

- [X] T022 [P] Create `FindTextProviderServiceTest` in `src/Vimium.Tests/Services/FindTextProviderServiceTest.cs`:
  - Test: `SearchAsync_TextPatternAvailable_ReturnsMatches` — mock `ITextProvider`/`ITextRangeProvider`, verify results with `Source=TextPattern`
  - Test: `SearchAsync_TextPatternUnavailable_FallsBackToElementNames` — simulate missing TextPattern, mock `FindAllBuildCache`, verify results with `Source=ElementName`
  - Test: `SearchAsync_Timeout_FallsBackToElementNames` — simulate slow FindText exceeding 3s, verify fallback triggers
  - Test: `SearchAsync_BothPathsFail_ReturnsEmptyFindResult` — TextPattern unavailable AND no matching element names, verify empty result
  - Test: `SearchAsync_RespectsCancellationToken` — cancel mid-search, verify `OperationCanceledException`/early exit
  - Test: `SearchAsync_Respects200MatchCap` — generate 300 matches in mock, verify result capped at 200
  - Test: `SearchAsync_VisibleViewportOnly` — verify `GetVisibleRanges()` is called, not full document

- [X] T023 [P] Rewrite `SelectionStateTest` in `src/Vimium.Tests/Models/SelectionStateTest.cs`:
  - Test: `SelectionState_InitialState_EmptyQueryNoMatches` — verify fresh state has empty query, empty matches, ActiveMatchIndex=0
  - Test: `SelectionState_UpdateMatches_NotifiesPropertyChanged` — verify match count and property change notifications
  - Test: `SelectionState_ActiveMatchIndex_WrapsCircularly` — verify index cycling logic (done at ViewModel level, test state transitions)
  - Test: `SelectionState_NoCursorOrSelectionProperties` — verify old cursor/selection properties are gone
  - Test: `SelectionState_IsSearching_FlagToggles` — verify IsSearching transitions
  - Test: `SelectionState_MatchCountText_FormatsCorrectly` — verify "0 matches", "2 of 5", "" (empty)

- [X] T024 [P] Create `FindSessionTest` in `src/Vimium.Tests/Models/FindSessionTest.cs`:
  - Test: `FindSession_Constructor_InitializesDefaults` — query empty, matches empty, IsSearching=false
  - Test: `FindSession_HasMatches_FalseWhenEmpty` — verify derived property
  - Test: `FindSession_MatchCountText_FormatsAllStates` — test "0 matches", "1 of 1", "2 of 5", "" (empty query)
  - Test: `FindSession_PropertyChanged_RaisedOnSetters` — verify INotifyPropertyChanged for all settable fields

- [X] T025 [P] Create `SearchMatchTest` in `src/Vimium.Tests/Models/SearchMatchTest.cs`:
  - Test: `SearchMatch_Validation_RejectsEmptySourceText` — verify constructor/validation throws on null/empty SourceText
  - Test: `SearchMatch_Validation_RejectsZeroSizeBoundingRect` — verify validation on BoundingRect dimensions
  - Test: `SearchMatch_IsActive_TogglesCorrectly` — verify state transitions
  - Test: `SearchMatch_DeprecatedFieldsDoNotExist` — verify StartIndex, EndIndex, LineIndex are removed
  - Test: `SearchMatch_TextRangeProvider_CanBeNull` — verify null TextRangeProvider for ElementName source

- [X] T026 Rewrite `SelectionModeViewModelTest` in `src/Vimium.Tests/ViewModels/SelectionModeViewModelTest.cs`:
  - Test: `HandleCharacter_Below5Chars_NoSearchTriggered` — type 4 chars, wait >150ms, verify no search call
  - Test: `HandleCharacter_At5Chars_SearchTriggeredAfterDebounce` — type 5 chars, wait 150ms, verify search called
  - Test: `HandleCharacter_RapidTyping_CancelsInFlightSearch` — type fast, verify only last search completes
  - Test: `HandleTab_CyclesForwardWithWrap` — with 3 matches, press Tab 4 times, verify circular wrap
  - Test: `HandleTab_Shift_CyclesBackwardWithWrap` — Shift+Tab from index 0 wraps to last match
  - Test: `HandleTab_NoMatches_NoOp` — verify no crash/state change with empty matches
  - Test: `HandleEnter_WithActiveMatch_NavigatesAndCloses` — verify ScrollIntoView+Select called, overlay closes
  - Test: `HandleEnter_StaleElement_CatchesExceptionAndCloses` — mock COMException, verify silent dismiss
  - Test: `HandleEnter_NoMatches_NoOp` — verify no action with empty matches
  - Test: `HandleEscape_DismissesWithoutNavigation` — verify no navigation calls, overlay closes
  - Test: `HandleBackspace_ReducesQueryAndUpdatesMatches` — verify debounce reset, match update
  - Test: `HandleBackspace_Below5Chars_ClearsMatches` — backspace reduces to 4 chars, verify matches cleared
  - Test: `HandleFocusLost_DismissesImmediately` — verify overlay close called
  - Test: `MatchCountText_UpdatesWithSearchResults` — verify binding-visible string updates
  - Test: `IsSearching_TrueDuringSearch` — verify loading indicator state

- [X] T027 [P] Add find-text activation tests to existing ShellViewModel tests in `src/Vimium.Tests/ViewModels/ShellViewModelTest.cs`:
  - Test: `ActivateFindText_CtrlDot_OpensSelectionOverlay` — verify overlay created with SelectionModeViewModel
  - Test: `ActivateFindText_WhileElementModeActive_ReplacesOverlay` — verify only one overlay
  - Test: `ActivateFindText_UsesConfiguredHotkey` — verify hotkey read from config, not hardcoded

- [X] T028 Run `dotnet test src/Vimium.sln` with code coverage:
  - Run: `dotnet test src/Vimium.sln --collect:"XPlat Code Coverage"`
  - Verify all tests pass with zero failures
  - Verify ≥80% line coverage on all non-view, non-interop new/rewritten code
  - Generate coverage report for review

**Checkpoint**: All tests passing, coverage ≥80% on new code. Core logic verified in isolation.

---

## Phase 7: Integration & Polish

**Purpose**: Build, validate against quickstart scenarios, verify zero regressions, final cleanup.

- [X] T029 Build solution and fix all warnings/errors:
  - Run: `dotnet build src/Vimium.sln` — must succeed with zero warnings
  - Fix any compilation errors from renamed interfaces, removed types
  - Verify all `using` directives are clean

- [X] T030 Run quickstart.md validation scenarios (VS-1 through VS-15):
  - VS-1 through VS-8, VS-11 through VS-15: ✅ Validated and working
  - VS-9 (Notepad) & VS-10 (VS Code): ⚠ Known limitation — these editors expose limited UIA text. Feature is best-effort; users can navigate via their editor's own Ctrl+F.
  - **Timeout tip**: When a search exceeds the 3s timeout (e.g. massive Wikipedia pages), the overlay now shows "Search timed out. Try the app's built-in Ctrl+F for better results." Noted in README.
  
- [X] T031 Verify element mode zero regressions and final polish:
  - Element mode (Ctrl+;) works with zero regressions
  - Themes (Light/Dark/Skadi) render correctly for find-text overlay
  - Rapid open/close cycles show no memory leaks
  - Build succeeds with zero errors
  - README updated with best-effort notice

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Final Cleanup)**: No dependencies — can start immediately
- **Phase 2 (Foundational — Models)**: No dependencies — can start immediately, in parallel with Phase 1
- **Phase 3 (Foundational — Service)**: Depends on Phase 2 (needs `FindResult`, `SearchResult` models) — BLOCKS all user stories
- **Phase 4 (US1+US3, P1)**: Depends on Phase 3 (needs `IFindTextProviderService`) — core MVP
- **Phase 5 (US2, P2)**: Depends on Phase 4 completion (needs `ShellViewModel` wiring) — can start after Phase 4
- **Phase 6 (Unit Tests)**: Can run in parallel with implementation tasks; tests for each component should be written alongside it
- **Phase 7 (Integration & Polish)**: Depends on all prior phases complete

### User Story Dependencies

- **User Story 1+3 (P1)**: Can start after Foundational Phase 3 — no dependencies on other stories. US3 (performance) is inseparable from US1 implementation — same ViewModel, same service.
- **User Story 2 (P2)**: Can start after Phase 4 US1 ViewModel/ShellViewModel is stable. Configuration is additive — adds hotkey setting to existing Options UI.

### Within Each Phase

- Phase 2: T004 and T005 can run in parallel (different files); T006 depends on T004 (uses `FindSession`)
- Phase 3: T007 (interface) before T008 (implementation)
- Phase 4: T009 → T010 (sequential, same file), T011 and T014 can parallel with T009/T010 (different files), T012 after T011, T013 after T012, T015 after T009, T016/T017 after core implementation is stable
- Phase 5: T018 [P] (independent config model), then T019/T020/T021 (sequential or parallel, different files)
- Phase 6: All test tasks can run in parallel (different test files for different components)
- Phase 7: Sequential — build first, then validate, then final polish

### Parallel Opportunities

- Phase 1: T001 and T002 (different files)
- Phase 2: T004 and T005 (different model files)
- Phase 6: T022 through T027 (different test files)
- Across phases: Phase 1 and Phase 2 can run concurrently
- Within Phase 4: T011 (XAML), T014 (ShellViewModel), T015 (App.xaml.cs) can be developed in parallel with T009/T010 (ViewModel) since they touch different files

---

## Parallel Example: Phase 4 (US1+US3)

```bash
# Start ViewModel core (sequential within file):
Task: "T009 Implement search invocation with debounce in SelectionModeViewModel.cs"
# After T009 done:
Task: "T010 Implement input handlers in SelectionModeViewModel.cs"

# In parallel with T009/T010 (different files):
Task: "T011 Update SelectionModeOverlayView.xaml"
Task: "T014 Update ShellViewModel.cs — wire Ctrl+. hotkey"
Task: "T015 Update App.xaml.cs — register new service"

# After T011 and T010:
Task: "T012 Update SelectionModeOverlayView.xaml.cs"
Task: "T013 Implement UIA TextChanged auto-dismiss"
Task: "T016 Handle edge cases"
Task: "T017 Add debug logging"
```

---

## Implementation Strategy

### MVP First (US1+US3 Only — Phase 1→2→3→4→7)

1. Complete Phase 1: Delete TextSource.cs, TextLineRect.cs (2 tasks)
2. Complete Phase 2: Foundational models — FindSession, SearchMatch rewrite, SelectionState rewrite (3 tasks)
3. Complete Phase 3: Service layer — IFindTextProviderService + FindTextProviderService (2 tasks)
4. Complete Phase 4: Core find-and-navigate — ViewModel, View, ShellViewModel wiring (9 tasks)
5. **STOP and VALIDATE**: Test with quickstart scenarios VS-1 through VS-10
6. Build → test → deploy/demo (MVP!)

### Incremental Delivery

1. Complete Setup (Phase 1) + Foundational (Phase 2 + 3) → Foundation ready
2. Add US1+US3 (Phase 4) → Test independently → Deploy/Demo (MVP!)
3. Add US2 (Phase 5) → Hotkey configurable via Options → Deploy/Demo
4. Add Tests (Phase 6) → Coverage ≥80% → Merge
5. Each story adds value without breaking previous stories

### Suggested Execution Order

```
T001 ─┬─ T003 ─────────────────────────────────────────────────────────────┐
T002 ─┘                                                                      │
                                                                             │
T004 ─┬─ T006 ─── T007 ─── T008 ─── T009 ─── T010 ─── T012 ─── T013 ───┐  │
T005 ─┘                                      │                           │  │
                                             ├── T011 (parallel)         │  │
                                             ├── T014 (parallel)         │  │
                                             └── T015 (parallel)         │  │
                                                    │                    │  │
                                                    └── T016 ─── T017 ───┤  │
                                                                          │  │
T018 ─── T019 ─── T020 ─── T021 ─────────────────────────────────────────┤  │
                                                                          │  │
T022 ─── T023 ─── T024 ─── T025 ─── T026 ─── T027 ─── T028 ─────────────┤  │
                                                                          │  │
                                                                          ├──┤
                                                    T029 ─── T030 ─── T031┘  │
```

---

## Notes

- [P] tasks = different files, no dependencies — can run concurrently
- [US1], [US2] labels map tasks to user stories from spec.md for traceability
- US3 (Fast Performance) tasks are embedded in US1 phase — same components, inseparable implementation
- Phase 1 teardown (TextLineHint, LineNavigationSession, etc.) was already completed before task generation
- Constitution Principle IV note: The "Text selection & copy contract" paragraph in constitution.md still describes the OLD design. Constitution update is a follow-up tracked separately.
- Each user story checkpoint is independently testable
- Verify tests fail before implementing (TDD: Red → Green → Refactor)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
