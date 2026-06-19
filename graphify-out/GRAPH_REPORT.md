# Graph Report - Vim_with_mouse  (2026-06-19)

## Corpus Check
- 58 files · ~17,102 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 364 nodes · 410 edges · 41 communities (33 shown, 8 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `efe92899`
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

## God Nodes (most connected - your core abstractions)
1. `User32` - 25 edges
2. `DllImport` - 21 edges
3. `IntPtr` - 18 edges
4. `UiAutomationHintProviderService` - 13 edges
5. `ShellViewModel` - 11 edges
6. `App` - 10 edges
7. `KeyListenerService` - 10 edges
8. `OverlayView` - 8 edges
9. `HuntAndPeck.Tests` - 7 edges
10. `IntPtr` - 7 edges

## Surprising Connections (you probably didn't know these)
- `UiAutomationHintProviderService` --implements--> `IDebugHintProviderService`  [EXTRACTED]
  src/HuntAndPeck/Services/UiAutomationHintProviderService.cs → src/HuntAndPeck/ViewModels/ShellViewModel.cs
- `KeyListenerService` --implements--> `IKeyListenerService`  [EXTRACTED]
  src/HuntAndPeck/Services/KeyListenerService.cs → src/HuntAndPeck/Services/Interfaces/IKeyListenerService.cs
- `UiAutomationExpandCollapseHint` --inherits--> `Hint`  [EXTRACTED]
  src/HuntAndPeck/Models/UiAutomationExpandCollapseHint.cs → src/HuntAndPeck/Services/UiAutomationHintProviderService.cs
- `UiAutomationFocusHint` --inherits--> `Hint`  [EXTRACTED]
  src/HuntAndPeck/Models/UiAutomationFocusHint.cs → src/HuntAndPeck/Services/UiAutomationHintProviderService.cs
- `UiAutomationInvokeHint` --inherits--> `Hint`  [EXTRACTED]
  src/HuntAndPeck/Models/UiAutomationInvokeHint.cs → src/HuntAndPeck/Services/UiAutomationHintProviderService.cs

## Import Cycles
- None detected.

## Communities (41 total, 8 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.14
Nodes (11): MarshalAs, HuntAndPeck.NativeMethods, User32, POINT, DllImport, int, IntPtr, LowLevelKeyboardProc (+3 more)

### Community 1 - "Community 1"
Cohesion: 0.07
Nodes (19): Hint, IUIAutomationExpandCollapsePattern, IUIAutomationInvokePattern, IUIAutomationSelectionItemPattern, IUIAutomationTogglePattern, DebugHint, HuntAndPeck.Models, HuntAndPeck.Models (+11 more)

### Community 2 - "Community 2"
Cohesion: 0.06
Nodes (22): INotifyPropertyChanged, NotifyPropertyChanged, ObservableCollection, PropertyChangedEventArgs, Rect, bool, string, string (+14 more)

### Community 3 - "Community 3"
Cohesion: 0.10
Nodes (15): EventArgs, HuntAndPeck, SingleLaunchMutex, IDisposable, KeyModifier, Keys, Mutex, HotKeyEventArgs (+7 more)

### Community 4 - "Community 4"
Cohesion: 0.18
Nodes (13): DebugHint, Func, IDebugHintProviderService, IHintProviderService, IUIAutomation, List, HuntAndPeck.Services, UiAutomationHintProviderService (+5 more)

### Community 5 - "Community 5"
Cohesion: 0.13
Nodes (12): HuntAndPeck.Tests, Hardcodet.NotifyIcon.Wpf (1.1.0), Microsoft.NET.Test.Sdk (17.11.1), System.Data.DataSetExtensions (4.5.0), xunit (2.9.0), xunit.runner.visualstudio (2.8.0), net10.0-windows, Microsoft.NET.Sdk (+4 more)

### Community 6 - "Community 6"
Cohesion: 0.12
Nodes (11): CancelEventArgs, DrawingContext, bool, EventArgs, ForegroundWindow, HuntAndPeck.Views, HuntAndPeck.Views, OptionsView (+3 more)

### Community 7 - "Community 7"
Cohesion: 0.15
Nodes (9): Action, Application, bool, EventArgs, IDebugHintProviderService, IHintLabelService, IHintProviderService, HuntAndPeck.ViewModels (+1 more)

### Community 8 - "Community 8"
Cohesion: 0.13
Nodes (10): DebugOverlayViewModel, HintLabelService, App, HuntAndPeck, KeyListenerService, OptionsViewModel, OverlayViewModel, SingleLaunchMutex (+2 more)

### Community 9 - "Community 9"
Cohesion: 0.14
Nodes (10): Form, HotKey, IKeyListenerService, int, HotKey, HuntAndPeck.Services.Interfaces, IKeyListenerService, Message (+2 more)

### Community 10 - "Community 10"
Cohesion: 0.17
Nodes (8): DispatcherTimer, KeyboardHookService, KeyDownEventArgs, RoutedEventArgs, EventArgs, string, HuntAndPeck.Views, OverlayView

### Community 11 - "Community 11"
Cohesion: 0.22
Nodes (5): ICommand, Predicate, Action, DelegateCommand, HuntAndPeck.ViewModels

### Community 12 - "Community 12"
Cohesion: 0.25
Nodes (7): Added, Added, Changed, Changelog, Fixed, v1.0 — Mouse interaction, v1.1 — Elevated, popup-friendly, faster

### Community 13 - "Community 13"
Cohesion: 0.25
Nodes (7): Download, How to change font size, hunt-n-peck, Interaction modes, Screenshots, Supported Elements, To use

### Community 14 - "Community 14"
Cohesion: 0.33
Nodes (4): HuntAndPeck.Extensions, RectExtensions, IntPtr, Rect

### Community 15 - "Community 15"
Cohesion: 0.33
Nodes (4): IHintLabelService, HintLabelService, HuntAndPeck.Services, IList

### Community 17 - "Community 17"
Cohesion: 0.33
Nodes (4): HuntAndPeck.NativeMethods, Kernel32, DllImport, IntPtr

### Community 18 - "Community 18"
Cohesion: 0.33
Nodes (4): HuntAndPeck.Extensions, IEnumerableTExtensions, IEnumerable, T

### Community 19 - "Community 19"
Cohesion: 0.33
Nodes (4): HuntAndPeck.Services.Interfaces, IDebugHintProviderService, HintSession, IntPtr

### Community 20 - "Community 20"
Cohesion: 0.32
Nodes (5): HuntAndPeck.Services.Interfaces, IHintProviderService, HintSession, IntPtr, Task

### Community 21 - "Community 21"
Cohesion: 0.40
Nodes (4): CultureInfo, HuntAndPeck.Properties, Resources, ResourceManager

### Community 22 - "Community 22"
Cohesion: 0.40
Nodes (3): Fact, HintLabelServiceTest, HuntAndPeck.Tests.Services

### Community 23 - "Community 23"
Cohesion: 0.40
Nodes (3): HuntAndPeck.Services.Interfaces, IHintLabelService, IList

### Community 24 - "Community 24"
Cohesion: 0.40
Nodes (3): RoutedEventArgs, DebugOverlayView, HuntAndPeck.Views

### Community 25 - "Community 25"
Cohesion: 0.67
Nodes (3): ApplicationSettingsBase, HuntAndPeck.Properties, Settings

### Community 26 - "Community 26"
Cohesion: 0.50
Nodes (3): Dictionary, HuntAndPeck.Services, UiAutomationPatternIds

### Community 27 - "Community 27"
Cohesion: 0.50
Nodes (3): Constants, HuntAndPeck.NativeMethods, UInt32

### Community 28 - "Community 28"
Cohesion: 0.50
Nodes (3): AssemblyVersionInformation, string, System

## Knowledge Gaps
- **145 isolated node(s):** `net10.0-windows`, `Microsoft.NET.Test.Sdk (17.11.1)`, `xunit (2.9.0)`, `xunit.runner.visualstudio (2.8.0)`, `Microsoft.NET.Sdk` (+140 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `UiAutomationHintProviderService` connect `Community 4` to `Community 7`?**
  _High betweenness centrality (0.028) - this node is a cross-community bridge._
- **Why does `Hint` connect `Community 1` to `Community 4`?**
  _High betweenness centrality (0.026) - this node is a cross-community bridge._
- **What connects `net10.0-windows`, `Microsoft.NET.Test.Sdk (17.11.1)`, `xunit (2.9.0)` to the rest of the system?**
  _145 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.14015151515151514 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.07311827956989247 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.06451612903225806 - nodes in this community are weakly interconnected._
- **Should `Community 3` be split into smaller, more focused modules?**
  _Cohesion score 0.09523809523809523 - nodes in this community are weakly interconnected._