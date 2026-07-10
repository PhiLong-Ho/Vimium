<!--
  Sync Impact Report
  ==================
  Version change: 1.1.0 → 1.2.0
  Bump rationale: MINOR — added "Documentation after feature" requirement to
  Development Workflow: README.md MUST be updated after every user-facing feature
  completion. No principles modified or removed — purely additive.

  Modified sections:
    Development Workflow — added documentation-update gate.

  Added sections: none

  Removed sections: none

  Templates requiring updates:
    ✅ .specify/memory/constitution.md — filled (this file)
    ✅ memory/constitution.md — mirrored (same content)
    ⚠ templates/plan-template.md — no changes needed.

  Follow-up TODOs: none — all placeholders resolved.
-->

# Vimium Constitution

> A lightweight keyboard-driven UI overlay for Windows. Press a hotkey, type a hint,
> and interact with anything on the screen — no mouse required. Built on the Windows
> UI Automation framework (like screen readers), so it works with almost any Windows
> desktop application.

## Core Principles

### I. MVVM Separation & Code Quality

**All UI logic MUST live in ViewModels, never in code-behind.**

- View code-behind files (`.xaml.cs`) are restricted to view-only concerns: focus
  management, window lifecycle hooks, and `DependencyProperty` bindings that cannot
  be expressed in XAML. No business logic, no state mutation, no service calls.
- ViewModels MUST be unit-testable in isolation — no dependency on `Dispatcher`,
  `Window`, or any WPF visual-tree types. Use `DelegateCommand` for all user
  actions.
- Every bound property MUST raise `PropertyChanged` via a shared base
  (`NotifyPropertyChanged`). No silent state drift between View and ViewModel.
- XAML bindings MUST use `{Binding}` and `{RelativeSource}` patterns; no
  named-element references in bindings that couple the view to itself.

**Rationale**: MVVM is the architectural backbone. Code-behind logic is untestable
with unit tests and couples behavior to windowing infrastructure. Strict separation
ensures ViewModels remain testable and the view can be redesigned independently.

### II. Interface-Driven Services

**Every service MUST expose a contract through an interface.**

- All service classes in `Vimium/Services/` MUST implement an interface defined in
  `Vimium/Services/Interfaces/`. No direct `new Service()` in ViewModels — inject
  via constructor or resolve through a service locator pattern agreed upon in
  implementation.
- New services introduced without an interface require explicit justification in
  the implementation plan (Complexity Tracking table).
- Existing interfaces (`IHintProviderService`, `IHintLabelService`,
  `IKeyListenerService`, `IDebugHintProviderService`) define the pattern; follow
  their naming convention: `I{Feature}Service`.
- Services that wrap Win32 / UI Automation calls (e.g., `KeyboardHookService`,
  `UiAutomationHintProviderService`) MUST isolate platform interop behind the
  interface so that consumers are testable with mock implementations.

**Rationale**: Interfaces enable unit testing with mock/stub implementations,
support future DI-container wiring, and make service boundaries explicit for
code review.

### III. Testing Standards

**Core logic MUST have automated tests. Tests MUST pass before merge.**

- **Scope**: All services, models, converters, and extension methods in
  `Vimium/` require unit tests in `Vimium.Tests/`. ViewModels SHOULD be tested
  where testable in isolation (no UI Automation or `Dispatcher` dependency).
- **Coverage target**: ≥80% line coverage on all non-view, non-interop code
  (services, models, converters, ViewModels). Use `dotnet-coverage` or equivalent
  tooling; report coverage in PR descriptions.
- **Test framework**: xUnit (`Vimium.Tests.csproj`). Follow AAA pattern
  (Arrange, Act, Assert). Test names MUST describe the scenario:
  `MethodName_Scenario_ExpectedBehavior`.
- **Gate**: `dotnet test src\Vimium.sln` MUST pass with zero failures before any
  branch is merged to `master`. CI should enforce this automatically.
- **Red-Green-Refactor**: New features follow TDD — write the test first, verify
  it fails, implement, verify it passes, then refactor. Bug fixes MUST include a
  regression test that reproduces the bug.
- Views (`*.xaml`, `*.xaml.cs`) and Win32 interop (`NativeMethods/`,
  `KeyboardHookService`) are exempt from unit test requirements but MUST be
  covered by manual test scenarios documented in the feature spec.

**Rationale**: Vimium runs elevated and injects into other processes via UI
Automation. Untested changes can cause crashes that lose user work or break
keyboard input system-wide. Tests are the safety net.

### IV. User Experience Consistency

**Every interaction MUST be keyboard-accessible and theme-consistent. Vimium
supports two first-class interaction modalities: element interaction and text
navigation/copy.**

- **Keyboard-first**: All UI (options window, overlay, context menus) MUST
  support full keyboard navigation — Tab order, arrow keys, Escape to dismiss,
  access keys (Alt+letter) on labeled controls. Focus indicators MUST be visible
  at all times (never hidden).
- **Theme consistency**: Every visual element MUST derive colors from the active
  theme's `ResourceDictionary` (Light / Dark / Skadi). No hardcoded color values
  in XAML or C#. New themes added in the future MUST define the same set of
  resource keys.
- **Element interaction contract** (existing, immutable): Press an element-mode
  hotkey (e.g., `Ctrl+;`) → hints appear on interactive elements → type hint
  string → element is invoked/clicked/toggled. This flow MUST NOT change; it is
  the backbone Vimium interaction.
- **Text selection & copy contract** (search-first modality, redesigned 2026-07-09):
  Press a text-selection hotkey (e.g., `Ctrl+.`) → overlay appears with a search bar
  over the foreground window → type a visible phrase to find and highlight matches
  (yellow for all, orange for active) → `Tab`/`Shift+Tab` to cycle matches circularly
  → arrow keys (`←`/`→`/`↑`/`↓`), `Ctrl+←/→` (word jumps), `Home`/`End` for cursor
  navigation → `Shift+Arrow`/`Ctrl+Shift+Arrow` for text selection → `Enter` to copy
  (selected text or current line fallback) with "Copied!" toast → `Esc` or
  foreground-window change cancels without copy. Search is optional — cursor starts
  at offset 0 and navigation/copy work immediately without typing a search phrase.
  Character-to-screen mapping uses proportional-width estimation
  (average-char-width with CJK 2× Latin width multiplier); RTL bidirectional text
  is deferred (documented limitation). Auto-dismiss on window change (poll
  `GetForegroundWindow()` on each keyboard event) and UIA content change.
- **Interaction mode isolation**: Element mode (`Ctrl+;`) and text selection mode
  (`Ctrl+.`) MUST use distinct, user-configurable hotkeys. Switching between modes
  MUST NOT require opening the settings window. Only one overlay at a time —
  activating one mode while the other is visible is a no-op. Each mode's overlay
  MUST be visually distinct (element hints vs. transparent text selection overlay
  with search bar).
- **Feedback SLA**: Overlay MUST appear within 100ms of hotkey activation
  (loading indicator counts as "appeared"). Settings changes in options MUST
  apply immediately (auto-save, no explicit Save button). Every user action MUST
  produce visible feedback (hint highlight, cursor position, search match
  highlight, clipboard confirmation).
- **Copy feedback**: When text is copied via the text navigation mode, the
  overlay MUST provide a brief visible confirmation (e.g., a flash, a tooltip,
  or a status text) so the user knows the clipboard was updated.
- **Accessibility**: High-contrast themes MUST remain usable. Font size MUST be
  user-configurable (8–24pt). Color-only information MUST have a non-color
  alternative (text labels, shapes). Selection mode cursor and match highlights
  MUST be visible in all themes.

**Rationale**: Vimium's core value proposition is replacing the mouse with the
keyboard — not just for clicking buttons, but for consuming and capturing text
as well. Users who depend on Vimium (accessibility, RSI prevention, productivity)
must have a consistent and predictable interaction model across both modalities.
The text navigation modality mirrors familiar Windows text-editing conventions
(arrows, Ctrl+arrow, Shift+arrow) to ensure non-vim users can adopt it
immediately.

### V. Performance & Non-Blocking UI

**The UI thread MUST never block. The overlay MUST beat human perception.**

- **Overlay latency**: Hotkey press to overlay window visible MUST complete in
  <100ms. Hint enumeration (UI Automation tree walk) MUST run on a background
  thread or async task. The overlay MUST display a loading indicator immediately
  while hints populate.
- **No synchronous cross-process calls on UI thread**: UI Automation
  `FindAllBuildCache` and pattern resolution (`InvokePattern`, `TogglePattern`,
  `TextPattern`, etc.) MUST run off the UI thread. Violations cause perceptible
  freezes.
- **Batch operations**: UI Automation data retrieval MUST use cached requests
  (`CacheRequest`) to minimize cross-process COM calls. Per-element round-trips
  are prohibited.
- **Memory**: Steady-state memory footprint MUST remain under 100MB. Hint
  enumeration objects MUST be eligible for GC immediately after overlay closes
  (no retained references from long-lived ViewModels).
- **Startup time**: Cold-start to tray icon visible MUST be <2 seconds. Scheduled
  task auto-start MUST not perceptibly delay Windows logon.

**Rationale**: Vimium competes with muscle memory. If the overlay takes longer to
appear than reaching for the mouse, users abandon it. Background enumeration was
the v1.2 headline feature for this reason — it must never regress.

## Technical Standards

- **Platform**: .NET 10, Windows only (Windows 10+, Windows 11). WPF for all UI.
- **Dependencies**: Pure WPF — no third-party UI libraries (no MahApps,
  MaterialDesign, etc.). Theme system is custom-implemented via WPF
  `ResourceDictionary` switching.
- **Interop**: Win32 API calls isolated in `NativeMethods/` project. UI Automation
  via `System.Windows.Automation` managed namespace — element patterns
  (`InvokePattern`, `TogglePattern`, `SelectionItemPattern`, `ExpandCollapsePattern`,
  `ValuePattern`, `RangeValuePattern`) plus text patterns (`TextPattern`,
  `TextRange`) for text discovery and selection. Low-level keyboard hook via
  `SetWindowsHookEx` with `WH_KEYBOARD_LL`.
- **Configuration**: JSON-based settings (`%APPDATA%\Vimium\config.json`) via
  `System.Text.Json`. No dependency on .NET Framework `Settings.settings` for new
  settings.
- **Elevation**: Application runs as `requireAdministrator`. All changes MUST
  consider elevated-process security implications (UIPI, cross-privilege COM).
- **No telemetry**: Vimium collects zero data. No analytics, no phoning home, no
  crash reports without explicit user action.

## Development Workflow

- **Spec-driven development**: Features begin with a specification
  (`spec.md`) → implementation plan (`plan.md`) → task breakdown
  (`tasks.md`) → implementation → verification. Use `/speckit-*` skills for
  each phase.
- **Documentation after feature**: After completing any user-facing feature,
  the README.md MUST be updated to reflect the new capabilities, interaction
  modes, and configuration options. User-facing changes include: new hotkeys,
  new interaction modes, new settings pages, new overlay behaviors, and
  performance improvements visible to the user. Spec documents in `specs/`
  serve as the canonical design record; README.md is the user-facing summary.
- **Constitution compliance**: Every PR MUST pass a constitution check against
  all five principles. Violations require explicit justification in the
  Complexity Tracking table of the implementation plan.
- **Code review gate**: At least one review of the diff is required before merge.
  Use `detect_changes` and `get_review_context` from code-review-graph MCP tools
  to identify risk areas.
- **Test gate**: `dotnet test` must pass. Coverage report SHOULD be included for
  PRs that add or modify core logic.
- **Build gate**: `dotnet build src\Vimium.sln` must succeed with zero warnings
  (treat warnings as errors where practical).
- **Commit convention**: Use conventional commits (`feat:`, `fix:`, `docs:`,
  `chore:`, `refactor:`, `test:`). Reference spec/issue numbers when applicable.

## Governance

This constitution supersedes all other development practices and conventions for
the Vimium project. Where a practice conflicts with a principle herein, the
constitution takes precedence.

**Amendment procedure**:
1. Propose amendment via PR with rationale and impact analysis.
2. Amendment MUST document which principles are affected and why the change is
   necessary.
3. Amendment requires approval from the project maintainer.
4. Templates and guidance files (`.specify/templates/`, `CLAUDE.md`) MUST be
   updated to reflect the amendment.

**Versioning policy**:
- MAJOR: Principle removal, redefinition, or breaking change to governance.
- MINOR: New principle or section added; material expansion of guidance.
- PATCH: Clarifications, wording fixes, non-semantic refinements.

**Compliance review**: All PRs and spec documents MUST include a "Constitution
Check" section verifying alignment with each principle. The maintainer is
responsible for enforcing compliance at review time. Use the complexity tracking
mechanism in `plan-template.md` to document and justify any intentional
violations.

**Version**: 1.2.0 | **Ratified**: 2026-07-05 | **Last Amended**: 2026-07-10
