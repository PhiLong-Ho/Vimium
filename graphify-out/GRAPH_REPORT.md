# Graph Report - Vimium  (2026-07-05)

## Corpus Check
- 101 files · ~53,378 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 875 nodes · 992 edges · 84 communities (74 shown, 10 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `93cb9719`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]
- [[_COMMUNITY_Community 6|Community 6]]
- [[_COMMUNITY_Community 7|Community 7]]
- [[_COMMUNITY_Community 8|Community 8]]
- [[_COMMUNITY_Community 9|Community 9]]
- [[_COMMUNITY_Community 10|Community 10]]
- [[_COMMUNITY_Community 11|Community 11]]
- [[_COMMUNITY_Community 12|Community 12]]
- [[_COMMUNITY_Community 13|Community 13]]
- [[_COMMUNITY_Community 14|Community 14]]
- [[_COMMUNITY_Community 15|Community 15]]
- [[_COMMUNITY_Community 16|Community 16]]
- [[_COMMUNITY_Community 17|Community 17]]
- [[_COMMUNITY_Community 18|Community 18]]
- [[_COMMUNITY_Community 19|Community 19]]
- [[_COMMUNITY_Community 20|Community 20]]
- [[_COMMUNITY_Community 21|Community 21]]
- [[_COMMUNITY_Community 22|Community 22]]
- [[_COMMUNITY_Community 23|Community 23]]
- [[_COMMUNITY_Community 24|Community 24]]
- [[_COMMUNITY_Community 25|Community 25]]
- [[_COMMUNITY_Community 26|Community 26]]
- [[_COMMUNITY_Community 27|Community 27]]
- [[_COMMUNITY_Community 28|Community 28]]
- [[_COMMUNITY_Community 29|Community 29]]
- [[_COMMUNITY_Community 30|Community 30]]
- [[_COMMUNITY_Community 31|Community 31]]
- [[_COMMUNITY_Community 32|Community 32]]
- [[_COMMUNITY_Community 33|Community 33]]
- [[_COMMUNITY_Community 34|Community 34]]
- [[_COMMUNITY_Community 35|Community 35]]
- [[_COMMUNITY_Community 41|Community 41]]
- [[_COMMUNITY_Community 42|Community 42]]
- [[_COMMUNITY_Community 44|Community 44]]
- [[_COMMUNITY_Community 45|Community 45]]
- [[_COMMUNITY_Community 46|Community 46]]
- [[_COMMUNITY_Community 47|Community 47]]
- [[_COMMUNITY_Community 48|Community 48]]
- [[_COMMUNITY_Community 49|Community 49]]
- [[_COMMUNITY_Community 50|Community 50]]
- [[_COMMUNITY_Community 51|Community 51]]
- [[_COMMUNITY_Community 52|Community 52]]
- [[_COMMUNITY_Community 53|Community 53]]
- [[_COMMUNITY_Community 54|Community 54]]
- [[_COMMUNITY_Community 55|Community 55]]
- [[_COMMUNITY_Community 56|Community 56]]
- [[_COMMUNITY_Community 57|Community 57]]
- [[_COMMUNITY_Community 58|Community 58]]
- [[_COMMUNITY_Community 59|Community 59]]
- [[_COMMUNITY_Community 60|Community 60]]
- [[_COMMUNITY_Community 61|Community 61]]
- [[_COMMUNITY_Community 62|Community 62]]
- [[_COMMUNITY_Community 63|Community 63]]
- [[_COMMUNITY_Community 64|Community 64]]
- [[_COMMUNITY_Community 65|Community 65]]
- [[_COMMUNITY_Community 66|Community 66]]
- [[_COMMUNITY_Community 67|Community 67]]
- [[_COMMUNITY_Community 68|Community 68]]
- [[_COMMUNITY_Community 69|Community 69]]
- [[_COMMUNITY_Community 70|Community 70]]
- [[_COMMUNITY_Community 71|Community 71]]
- [[_COMMUNITY_Community 72|Community 72]]
- [[_COMMUNITY_Community 73|Community 73]]
- [[_COMMUNITY_Community 74|Community 74]]
- [[_COMMUNITY_Community 75|Community 75]]
- [[_COMMUNITY_Community 76|Community 76]]
- [[_COMMUNITY_Community 77|Community 77]]
- [[_COMMUNITY_Community 78|Community 78]]
- [[_COMMUNITY_Community 79|Community 79]]
- [[_COMMUNITY_Community 80|Community 80]]

## God Nodes (most connected - your core abstractions)
1. `User32` - 25 edges
2. `DllImport` - 21 edges
3. `IntPtr` - 18 edges
4. `Hands-On Guide: Spec-Driven Development with GitHub Spec Kit and Claude Code` - 17 edges
5. `ShellViewModel` - 16 edges
6. `ConfigService` - 15 edges
7. `Tasks: [FEATURE NAME]` - 13 edges
8. `UiAutomationHintProviderService` - 13 edges
9. `HotKeyTest` - 12 edges
10. `OverlayViewModel` - 12 edges

## Surprising Connections (you probably didn't know these)
- `OverlayViewModel` --references--> `ConfigService`  [EXTRACTED]
  src/Vimium/ViewModels/OverlayViewModel.cs → src/Vimium/ViewModels/OptionsViewModel.cs
- `UiAutomationHintProviderService` --implements--> `IHintProviderService`  [EXTRACTED]
  src/Vimium/Services/UiAutomationHintProviderService.cs → src/Vimium/ViewModels/ShellViewModel.cs
- `UiAutomationHintProviderService` --implements--> `IDebugHintProviderService`  [EXTRACTED]
  src/Vimium/Services/UiAutomationHintProviderService.cs → src/Vimium/ViewModels/ShellViewModel.cs
- `UiAutomationHintProviderService` --implements--> `IDebugHintProviderService`  [EXTRACTED]
  src/Vimium/Services/UiAutomationHintProviderService.cs → src/HuntAndPeck/ViewModels/ShellViewModel.cs
- `OptionsViewModel` --references--> `string`  [EXTRACTED]
  src/Vimium/ViewModels/OptionsViewModel.cs → src/HuntAndPeck/ViewModels/OptionsViewModel.cs

## Import Cycles
- None detected.

## Communities (84 total, 10 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.14
Nodes (11): MarshalAs, User32, Vimium.NativeMethods, POINT, DllImport, int, IntPtr, LowLevelKeyboardProc (+3 more)

### Community 1 - "Community 1"
Cohesion: 0.05
Nodes (44): DebugHint, Func, Hint, IUIAutomation, IUIAutomationExpandCollapsePattern, IUIAutomationInvokePattern, IUIAutomationSelectionItemPattern, IUIAutomationTogglePattern (+36 more)

### Community 2 - "Community 2"
Cohesion: 0.15
Nodes (10): Brush, ObservableCollection, PropertyChangedEventArgs, Rect, bool, Rect, bool, HuntAndPeck.ViewModels (+2 more)

### Community 3 - "Community 3"
Cohesion: 0.09
Nodes (16): HuntAndPeck, SingleLaunchMutex, IDisposable, Mutex, HuntAndPeck.Services, KeyboardHookService, KeyDownEventArgs, Vimium.Services (+8 more)

### Community 4 - "Community 4"
Cohesion: 0.07
Nodes (26): Dependencies & Execution Order, Format: `[ID] [P?] [Story] Description`, Implementation for User Story 1, Implementation for User Story 2, Implementation for User Story 3, Implementation Strategy, Incremental Delivery, MVP First (User Story 1 Only) (+18 more)

### Community 5 - "Community 5"
Cohesion: 0.10
Nodes (17): HuntAndPeck.Tests, Hardcodet.NotifyIcon.Wpf (1.1.0), Microsoft.NET.Test.Sdk (17.11.1), System.Data.DataSetExtensions (4.5.0), xunit (2.9.0), xunit.runner.visualstudio (2.8.0), net10.0-windows, Microsoft.NET.Sdk (+9 more)

### Community 6 - "Community 6"
Cohesion: 0.08
Nodes (25): 1. Initialize Analysis Context, 2. Load Artifacts (Progressive Disclosure), 3. Build Semantic Models, 4. Detection Passes (Token-Efficient Analysis), 5. Severity Assignment, 6. Produce Compact Analysis Report, 7. Provide Next Actions, 8. Offer Remediation (+17 more)

### Community 7 - "Community 7"
Cohesion: 0.12
Nodes (14): Action, EventArgs, IDebugHintProviderService, IHintProviderService, bool, EventArgs, IDebugHintProviderService, IHintLabelService (+6 more)

### Community 8 - "Community 8"
Cohesion: 0.12
Nodes (13): Application, DebugOverlayViewModel, HintLabelService, App, HuntAndPeck, KeyListenerService, OptionsViewModel, OverlayViewModel (+5 more)

### Community 9 - "Community 9"
Cohesion: 0.11
Nodes (13): Form, HotKey, IKeyListenerService, int, HotKey, HuntAndPeck.Services.Interfaces, IKeyListenerService, Vimium.Services.Interfaces (+5 more)

### Community 10 - "Community 10"
Cohesion: 0.05
Nodes (27): CancelEventArgs, DispatcherTimer, DrawingContext, KeyboardHookService, KeyDownEventArgs, KeyEventArgs, MouseButtonEventArgs, RoutedEventArgs (+19 more)

### Community 11 - "Community 11"
Cohesion: 0.18
Nodes (7): ICommand, Predicate, Action, Action, DelegateCommand, HuntAndPeck.ViewModels, Vimium.ViewModels

### Community 12 - "Community 12"
Cohesion: 0.16
Nodes (16): Added, Added, Added, Added, Changed, Changed, Changed, Changelog (+8 more)

### Community 13 - "Community 13"
Cohesion: 0.19
Nodes (12): Building, Command-line, Download, Features, How to configure, How to use, Install (auto-start + Start menu shortcut), Interaction modes (+4 more)

### Community 14 - "Community 14"
Cohesion: 0.24
Nodes (7): HuntAndPeck.Extensions, RectExtensions, Vimium.Extensions, IntPtr, Rect, IntPtr, Rect

### Community 15 - "Community 15"
Cohesion: 0.40
Nodes (4): HuntAndPeck.Properties, Resources, Vimium.Properties, ResourceManager

### Community 16 - "Community 16"
Cohesion: 0.32
Nodes (3): Hint, HuntAndPeck.Models, Vimium.Models

### Community 17 - "Community 17"
Cohesion: 0.33
Nodes (4): Kernel32, Vimium.NativeMethods, DllImport, IntPtr

### Community 18 - "Community 18"
Cohesion: 0.18
Nodes (8): HuntAndPeck.Extensions, IEnumerableTExtensions, Vimium.Extensions, IEnumerable, Lazy, ConfigService, T, VimiumConfig

### Community 19 - "Community 19"
Cohesion: 0.22
Nodes (7): HuntAndPeck.Services.Interfaces, IDebugHintProviderService, Vimium.Services.Interfaces, HintSession, IntPtr, HintSession, IntPtr

### Community 20 - "Community 20"
Cohesion: 0.23
Nodes (9): HuntAndPeck.Services.Interfaces, IHintProviderService, Vimium.Services.Interfaces, HintSession, IntPtr, Task, HintSession, IntPtr (+1 more)

### Community 21 - "Community 21"
Cohesion: 0.38
Nodes (4): HexToColorConverter, CultureInfo, IValueConverter, Type

### Community 22 - "Community 22"
Cohesion: 0.33
Nodes (4): Fact, HintLabelServiceTest, HuntAndPeck.Tests.Services, Vimium.Tests.Services

### Community 23 - "Community 23"
Cohesion: 0.29
Nodes (5): HuntAndPeck.Services.Interfaces, IHintLabelService, Vimium.Services.Interfaces, IList, IList

### Community 24 - "Community 24"
Cohesion: 0.29
Nodes (5): RoutedEventArgs, RoutedEventArgs, DebugOverlayView, HuntAndPeck.Views, Vimium.Views

### Community 25 - "Community 25"
Cohesion: 0.50
Nodes (4): ApplicationSettingsBase, HuntAndPeck.Properties, Settings, Vimium.Properties

### Community 26 - "Community 26"
Cohesion: 0.40
Nodes (4): Dictionary, HuntAndPeck.Services, UiAutomationPatternIds, Vimium.Services

### Community 27 - "Community 27"
Cohesion: 0.50
Nodes (3): Constants, Vimium.NativeMethods, UInt32

### Community 28 - "Community 28"
Cohesion: 0.50
Nodes (3): AssemblyVersionInformation, string, System

### Community 30 - "Community 30"
Cohesion: 0.50
Nodes (3): HintSession, HuntAndPeck.Models, Vimium.Models

### Community 31 - "Community 31"
Cohesion: 0.50
Nodes (3): DebugHintViewModel, HuntAndPeck.ViewModels, Vimium.ViewModels

### Community 32 - "Community 32"
Cohesion: 0.33
Nodes (5): Development Workflow: Spec-Driven, Key Tools, MCP Tools: code-review-graph, When to use graph tools FIRST, Workflow

### Community 41 - "Community 41"
Cohesion: 0.07
Nodes (29): 1. Keyboard Navigation, 2. Font Size & Localization, 3. Modernized Layout, 4. Theme System, 5. JSON Configuration, 6. Immediate Apply (Live Settings), 7. Project Rename: HuntAndPeck → Vimium, Color Palette by Theme (+21 more)

### Community 44 - "Community 44"
Cohesion: 0.29
Nodes (4): SolidColorBrush, ConfigService, PropertyChangedEventArgs, OverlaySettingsViewModel

### Community 47 - "Community 47"
Cohesion: 0.22
Nodes (4): ConfigService, string, HuntAndPeck.ViewModels, OptionsViewModel

### Community 49 - "Community 49"
Cohesion: 0.40
Nodes (4): KeyModifier, Keys, HotKeyEventArgs, Vimium.NativeMethods

### Community 50 - "Community 50"
Cohesion: 0.40
Nodes (4): Debug Issue, Steps, Tips, Token Efficiency Rules

### Community 51 - "Community 51"
Cohesion: 0.22
Nodes (8): 1. SPEC — Create or update the spec document, 2. PLAN — Review the spec with the user, 3. BUILD — Implement phase by phase, 4. CHECK — Verify against the spec, New Requirements Mid-Cycle, Spec-Driven Development, The Cycle, Token Efficiency

### Community 52 - "Community 52"
Cohesion: 0.40
Nodes (4): Explore Codebase, Steps, Tips, Token Efficiency Rules

### Community 53 - "Community 53"
Cohesion: 0.40
Nodes (4): Refactor Safely, Safety Checks, Steps, Token Efficiency Rules

### Community 54 - "Community 54"
Cohesion: 0.40
Nodes (4): Output Format, Review Changes, Steps, Token Efficiency Rules

### Community 56 - "Community 56"
Cohesion: 0.29
Nodes (6): bool, string, string, HintViewModel, HuntAndPeck.ViewModels, Vimium.ViewModels

### Community 57 - "Community 57"
Cohesion: 0.29
Nodes (5): HintLabelService, HuntAndPeck.Services, Vimium.Services, IList, IList

### Community 58 - "Community 58"
Cohesion: 0.33
Nodes (4): INotifyPropertyChanged, HuntAndPeck.ViewModels, NotifyPropertyChanged, Vimium.ViewModels

### Community 59 - "Community 59"
Cohesion: 0.40
Nodes (3): ConfigService, PropertyChangedEventArgs, GeneralSettingsViewModel

### Community 60 - "Community 60"
Cohesion: 0.33
Nodes (5): Rect, Rect, DebugOverlayViewModel, HuntAndPeck.ViewModels, Vimium.ViewModels

### Community 61 - "Community 61"
Cohesion: 0.40
Nodes (4): HintSession, HintSession, IHintLabelService, IHintLabelService

### Community 62 - "Community 62"
Cohesion: 0.33
Nodes (4): NotifyPropertyChanged, ConfigService, PropertyChangedEventArgs, KeyboardSettingsViewModel

### Community 63 - "Community 63"
Cohesion: 0.08
Nodes (24): Commit everything, Hands-On Guide: Spec-Driven Development with GitHub Spec Kit and Claude Code, Initialise the project for Claude Code, Install the Spec Kit CLI, Install uv, Review and customise `CLAUDE.md`, Specifying Features 2, 3, and 4, Step 10: Phase 7 — Tasks (+16 more)

### Community 64 - "Community 64"
Cohesion: 0.08
Nodes (24): 1. Keyboard Navigation, 2. Font Size & Localization, 3. Modernized Layout, 4. Theme System, 5. JSON Configuration, 6. Immediate Apply (Live Settings), 7. Hotkey Configuration, 8. Project Rename: HuntAndPeck → Vimium (+16 more)

### Community 65 - "Community 65"
Cohesion: 0.12
Nodes (15): 1. Initialize Convergence Context, 2. Load Artifacts (Progressive Disclosure), 3. Build the Intent Inventory, 4. Assess the Codebase and Classify Findings, 5. Assign Severity, 6. Present the In-Session Findings Summary, 7. Append Convergence Tasks (or report converged), 8. Provide Next Actions (Handoff) (+7 more)

### Community 66 - "Community 66"
Cohesion: 0.22
Nodes (10): Find-SpecifyRoot(), Format-SpecKitCommand(), Get-CurrentBranch(), Get-FeaturePathsEnv(), Get-InvokeSeparator(), Get-Python3Command(), Get-RepoRoot(), Resolve-SpecifyInitDir() (+2 more)

### Community 67 - "Community 67"
Cohesion: 0.15
Nodes (12): Assumptions, Edge Cases, Feature Specification: [FEATURE NAME], Functional Requirements, Key Entities *(include if feature involves data)*, Measurable Outcomes, Requirements *(mandatory)*, Success Criteria *(mandatory)* (+4 more)

### Community 68 - "Community 68"
Cohesion: 0.18
Nodes (10): Core Principles, Governance, [PRINCIPLE_1_NAME], [PRINCIPLE_2_NAME], [PRINCIPLE_3_NAME], [PRINCIPLE_4_NAME], [PRINCIPLE_5_NAME], [PROJECT_NAME] Constitution (+2 more)

### Community 69 - "Community 69"
Cohesion: 0.18
Nodes (10): Completion Report, Done When, Key rules, Mandatory Post-Execution Hooks, Outline, Phase 0: Outline & Research, Phase 1: Design & Contracts, Phases (+2 more)

### Community 70 - "Community 70"
Cohesion: 0.18
Nodes (10): Completion Report, Done When, For AI Generation, Mandatory Post-Execution Hooks, Outline, Pre-Execution Checks, Quick Guidelines, Section Requirements (+2 more)

### Community 71 - "Community 71"
Cohesion: 0.18
Nodes (10): Checklist Format (REQUIRED), Completion Report, Done When, Mandatory Post-Execution Hooks, Outline, Phase Structure, Pre-Execution Checks, Task Generation Rules (+2 more)

### Community 72 - "Community 72"
Cohesion: 0.18
Nodes (10): Core Principles, Governance, [PRINCIPLE_1_NAME], [PRINCIPLE_2_NAME], [PRINCIPLE_3_NAME], [PRINCIPLE_4_NAME], [PRINCIPLE_5_NAME], [PROJECT_NAME] Constitution (+2 more)

### Community 73 - "Community 73"
Cohesion: 0.22
Nodes (8): Complexity Tracking, Constitution Check, Documentation (this feature), Implementation Plan: [FEATURE], Project Structure, Source Code (repository root), Summary, Technical Context

### Community 74 - "Community 74"
Cohesion: 0.25
Nodes (7): Anti-Examples: What NOT To Do, Checklist Purpose: "Unit Tests for English", Example Checklist Types & Sample Items, Execution Steps, Post-Execution Checks, Pre-Execution Checks, User Input

### Community 75 - "Community 75"
Cohesion: 0.29
Nodes (6): Completion Report, Done When, Mandatory Post-Execution Hooks, Outline, Pre-Execution Checks, User Input

### Community 76 - "Community 76"
Cohesion: 0.29
Nodes (6): Completion Report, Done When, Mandatory Post-Execution Hooks, Outline, Pre-Execution Checks, User Input

### Community 77 - "Community 77"
Cohesion: 0.40
Nodes (4): Outline, Post-Execution Checks, Pre-Execution Checks, User Input

### Community 78 - "Community 78"
Cohesion: 0.40
Nodes (4): Outline, Post-Execution Checks, Pre-Execution Checks, User Input

### Community 79 - "Community 79"
Cohesion: 0.40
Nodes (4): [Category 1], [Category 2], [CHECKLIST TYPE] Checklist: [FEATURE NAME], Notes

## Knowledge Gaps
- **432 isolated node(s):** `User Input`, `Pre-Execution Checks`, `Goal`, `Operating Constraints`, `1. Initialize Analysis Context` (+427 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ShellViewModel` connect `Community 7` to `Community 9`?**
  _High betweenness centrality (0.071) - this node is a cross-community bridge._
- **Why does `ConfigService` connect `Community 18` to `Community 58`, `Community 10`?**
  _High betweenness centrality (0.059) - this node is a cross-community bridge._
- **Why does `Action` connect `Community 7` to `Community 18`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._
- **What connects `User Input`, `Pre-Execution Checks`, `Goal` to the rest of the system?**
  _432 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.14015151515151514 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.05048076923076923 - nodes in this community are weakly interconnected._
- **Should `Community 3` be split into smaller, more focused modules?**
  _Cohesion score 0.09420289855072464 - nodes in this community are weakly interconnected._