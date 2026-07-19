# Monitoring Settings Window Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a macOS-aligned native settings window with immediately applied, persistent refresh interval, maximum task count, and existing sound preferences.

**Architecture:** A dedicated JSON store and long-lived `DisplaySettingsViewModel` own validated monitoring preferences. The same instance feeds task-list limits, the main-window timer, and a single non-modal settings window; the existing `SoundSettingsViewModel` moves into the settings window without changing notification state or sound assets.

**Tech Stack:** .NET 9, C#, WPF, System.Text.Json, xUnit

---

### Task 1: Validated Display Preferences And Persistence

**Files:**
- Create: `src/ThreadBeacon.App/Settings/DisplaySettings.cs`
- Create: `src/ThreadBeacon.App/Settings/IDisplaySettingsStore.cs`
- Create: `src/ThreadBeacon.App/Settings/JsonDisplaySettingsStore.cs`
- Create: `src/ThreadBeacon.App/ViewModels/DisplaySettingsViewModel.cs`
- Create: `tests/ThreadBeacon.App.Tests/Settings/JsonDisplaySettingsStoreTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/DisplaySettingsViewModelTests.cs`

- [ ] **Step 1: Write failing settings-contract tests**

Assert the exact values and independent normalization:

```csharp
[Fact]
public void Constructor_NormalizesUnsupportedValuesIndependently()
{
    var settings = new DisplaySettings(3, 12);
    Assert.Equal(2, settings.RefreshIntervalSeconds);
    Assert.Equal(12, settings.MaximumTaskCount);
    Assert.Equal([1, 2, 5, 10], DisplaySettings.SupportedRefreshIntervalSeconds);
    Assert.Equal([4, 8, 12, 20], DisplaySettings.SupportedMaximumTaskCounts);
}
```

Add JSON tests for missing file defaults, valid round-trip, malformed JSON defaults,
unsupported persisted values, and a blocked parent path returning `false` on save.

- [ ] **Step 2: Run the focused tests and verify RED**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "DisplaySettings|JsonDisplaySettingsStore"
```

Expected: compilation fails because the display settings types do not exist.

- [ ] **Step 3: Implement the immutable model and JSON store**

Define `DisplaySettings` with version 1, defaults 2/8, the two supported arrays,
and a constructor that validates each value independently. Define:

```csharp
public interface IDisplaySettingsStore
{
    DisplaySettings Load();
    bool Save(DisplaySettings settings);
}
```

`JsonDisplaySettingsStore.CreateDefault()` resolves
`%LOCALAPPDATA%\ThreadBeacon\display-settings.json`. Match the existing settings
stores: camel-case indented JSON, caught I/O/JSON/argument failures, atomic model
validation on load, and no exception text or path exposure.

- [ ] **Step 4: Verify model/store GREEN**

Run the focused command from Step 2. Expected: all model and JSON tests pass.

- [ ] **Step 5: Write failing view-model tests**

Create a memory store and assert option labels, immediate persistence, no redundant
save for the same value, property-change notification, and active in-memory state
when save returns false:

```csharp
viewModel.RefreshIntervalSeconds = 5;
viewModel.MaximumTaskCount = 20;
Assert.Equal(2, store.SaveCount);
Assert.Equal(5, store.Current.RefreshIntervalSeconds);
Assert.Equal(20, store.Current.MaximumTaskCount);
Assert.Equal(TimeSpan.FromSeconds(5), viewModel.RefreshInterval);
```

- [ ] **Step 6: Run view-model tests and verify RED**

Run the focused command from Step 2. Expected: compilation fails because
`DisplaySettingsViewModel` does not exist.

- [ ] **Step 7: Implement and verify the display settings view model**

Implement `INotifyPropertyChanged`, stable option collections containing numeric
value plus localized label, `RefreshInterval`, and immediate saves. Run the focused
suite and full App suite, then commit:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "DisplaySettings|JsonDisplaySettingsStore"
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
git add src/ThreadBeacon.App/Settings src/ThreadBeacon.App/ViewModels/DisplaySettingsViewModel.cs tests/ThreadBeacon.App.Tests
git commit -m "feat(settings): persist monitoring preferences"
```

### Task 2: Apply Maximum Count And Refresh Schedule Immediately

**Files:**
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Create: `src/ThreadBeacon.App/ViewModels/MonitoringSettingsCoordinator.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/MonitoringSettingsCoordinatorTests.cs`

- [ ] **Step 1: Write failing task-limit tests**

Inject a display settings view model into `MainWindowViewModel`. Load 20 deterministic
records and assert the defaults show 8, selecting 4 then refreshing shows 4, and
selecting 20 then refreshing shows all 20. Assert the repository receives
`MaximumTaskCount + ignored-rule count`, and notification policy supplied to the
refresh remains `Baseline` for a settings-triggered refresh.

- [ ] **Step 2: Run task-limit tests and verify RED**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "MaximumTaskCount|DisplaySettings"
```

Expected: assertions fail because task loading and list policy still use 8.

- [ ] **Step 3: Replace both hard-coded list limits**

Add the shared `DisplaySettingsViewModel` as the final optional constructor argument
to `MainWindowViewModel`. Use:

```csharp
int recentLimit = checked(DisplaySettings.MaximumTaskCount + preferences.IgnoredRules.Count);
ThreadListPolicy.Evaluate(candidateSnapshots, preferences, DisplaySettings.MaximumTaskCount);
```

Clamp the sum to `int.MaxValue` without overflow. Keep the current default behavior
for tests/callers that omit the dependency by using an in-memory default state.

- [ ] **Step 4: Verify task-limit GREEN**

Run the focused test command. Expected: all maximum-count tests pass.

- [ ] **Step 5: Write failing schedule-coordinator tests**

Construct `MonitoringSettingsCoordinator` with a real display view model, an action
that records applied intervals, and an async callback that records baseline refreshes.
Assert changing to 5 seconds applies exactly one interval and no refresh; changing
maximum count invokes exactly one baseline refresh. The coordinator must not own or
toggle `MonitoringState`, so a paused state remains paused through both changes.

- [ ] **Step 6: Run schedule tests and verify RED**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter MonitoringSettingsCoordinatorTests
```

Expected: compilation fails because the coordinator does not exist.

- [ ] **Step 7: Implement the schedule integration**

Implement `MonitoringSettingsCoordinator : IDisposable`. It subscribes to the shared
display view model, calls the injected interval action only for
`RefreshIntervalSeconds`, and awaits the injected baseline-refresh callback only for
`MaximumTaskCount`. It never references or toggles monitoring state.

Share one display view model from `MainWindow` into the main view model and settings
UI. Construct the coordinator with `interval => refreshTimer.Interval = interval`
and `() => viewModel.RefreshAsync(Baseline)`. Disposing the main window unsubscribes
the coordinator; changing interval never starts or stops the timer.

- [ ] **Step 8: Verify and commit Task 2**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "MaximumTaskCount|MonitoringSettingsCoordinatorTests"
dotnet test ThreadBeacon.slnx
git add src/ThreadBeacon.App tests/ThreadBeacon.App.Tests
git commit -m "feat(settings): apply monitoring preferences"
```

Expected: focused and full test suites pass.

### Task 3: Native Settings Window And Sound Migration

**Files:**
- Create: `src/ThreadBeacon.App/ViewModels/SettingsWindowViewModel.cs`
- Create: `src/ThreadBeacon.App/SettingsWindow.xaml`
- Create: `src/ThreadBeacon.App/SettingsWindow.xaml.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/SoundSettingsViewModel.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/SoundSettingsViewModelTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/Views/MainWindowXamlTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/Views/SettingsWindowXamlTests.cs`

- [ ] **Step 1: Write failing sound enablement tests**

Assert category picker/preview enablement requires both global and category switches,
and commands do not preview a disabled category. Assert each relevant property raises
notifications when either controlling switch changes.

- [ ] **Step 2: Run sound tests and verify RED**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter SoundSettingsViewModelTests
```

Expected: new enablement properties or preview assertions fail.

- [ ] **Step 3: Implement macOS-aligned sound enablement**

Expose `IsCompletionCategoryEnabled`, `IsCompletionSoundEnabled`,
`IsWarningCategoryEnabled`, and `IsWarningSoundEnabled`. Global changes notify all;
category changes notify their sound-enabled property. Preview commands return unless
their corresponding sound-enabled property is true. Preserve settings, assets, and
seen-event IDs.

- [ ] **Step 4: Verify sound GREEN**

Run the focused sound suite. Expected: all tests pass.

- [ ] **Step 5: Write failing XAML and lifecycle tests**

Assert `SettingsWindow.xaml` has a 440 x 360 icon-bearing window, tabs named `通用`
and `提示音`, all supported bindings, localized labels, category enablement bindings,
and two preview commands. Assert `MainWindow.xaml` contains one named gear button and
no `SoundSettingsPopup` or `SoundButton`. Exercise the main-window open handler twice
and verify one settings-window instance is activated rather than duplicated.

- [ ] **Step 6: Run settings-window tests and verify RED**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "SettingsWindow|MainWindowXamlTests"
```

Expected: compilation/assertions fail because the window and gear entry do not exist.

- [ ] **Step 7: Implement the settings window**

Create `SettingsWindowViewModel(DisplaySettingsViewModel Display,
SoundSettingsViewModel Sound)`. Build a restrained native `TabControl` layout with
labeled combo boxes in `通用`, and global/completion/service sections in `提示音`.
Use the shared app resources and icon. Remove the sound popup and handler.

`MainWindow` stores a nullable settings-window field. The gear handler creates and
shows one owner-centered window when null, otherwise restores and activates the
existing window. Its `Closed` handler clears the field. Main-window close closes any
settings window and releases event subscriptions.

- [ ] **Step 8: Verify and commit Task 3**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
git add src/ThreadBeacon.App tests/ThreadBeacon.App.Tests
git commit -m "feat(settings): add native settings window"
```

Expected: all tests pass; Release has zero warnings and errors.

### Task 4: Runtime Acceptance, Documentation, Security Audit, And Push

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`
- Modify: `PRIVACY.md`

- [ ] **Step 1: Document settings behavior and data**

Describe the gear, two tabs, supported values/defaults, immediate application,
pause/manual-refresh behavior, separate local JSON, and unchanged sound privacy.

- [ ] **Step 2: Run complete automated verification**

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
git diff --check
```

Expected: all tests pass, Release has zero warnings/errors, no vulnerable package is
reported, and diff checks are clean.

- [ ] **Step 3: Perform runtime acceptance**

Launch Release and verify one gear opens one settings window with two tabs. Test all
four interval/count values, immediate task-count changes, pause/manual refresh,
sound toggles/pickers/previews, close/reopen, 480px main width, and persistence after
App restart. Confirm settings-window movement never changes main-window geometry.

- [ ] **Step 4: Perform mandatory pre-push review**

Inspect `origin/main..HEAD` and the working tree for credentials, keys, absolute user
paths, task identity/content, Codex database/JSONL/log data, local settings, binaries,
and temporary files. Confirm no new network access or Codex writes, bounded local
settings fields, and unchanged read-only SQLite guards.

- [ ] **Step 5: Commit documentation and push**

```powershell
git add README.md README-EN.md ROADMAP.md PRIVACY.md docs/superpowers/plans/2026-07-19-monitoring-settings-window.md
git commit -m "docs(settings): document monitoring preferences"
git push origin main
git fetch origin main
git status --short --branch
git rev-parse HEAD
git rev-parse origin/main
```

Expected: local and remote revisions match and the working tree is clean.
