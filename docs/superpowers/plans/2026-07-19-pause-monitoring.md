# Pause and Resume Monitoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a global header control that pauses and resumes ThreadBeacon's two-second automatic refresh while preserving manual refresh and current task data.

**Architecture:** A testable `MonitoringState` in `ThreadBeacon.App` owns the process-local active/paused mode. `MainWindowViewModel` exposes the state and derives footer feedback, while `MainWindow` remains responsible for starting and stopping its existing `DispatcherTimer`. The Core read-only data pipeline remains unchanged.

**Tech Stack:** .NET 9, WPF, `INotifyPropertyChanged`, `DispatcherTimer`, xUnit, Windows UI Automation.

---

### Task 1: Model process-local monitoring state

**Files:**

- Create: `src/ThreadBeacon.App/ViewModels/MonitoringState.cs`
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/MonitoringStateTests.cs`

- [ ] **Step 1: Write the failing state tests**

Create `MonitoringStateTests.cs`:

```csharp
using ThreadBeacon.App.ViewModels;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class MonitoringStateTests
{
    [Fact]
    public void Constructor_DefaultsToActiveMonitoring()
    {
        var state = new MonitoringState();

        Assert.False(state.IsPaused);
        Assert.True(state.ShouldAutoRefresh);
    }

    [Fact]
    public void ToggleCommand_PausesThenResumesMonitoring()
    {
        var state = new MonitoringState();

        state.ToggleCommand.Execute(null);
        Assert.True(state.IsPaused);
        Assert.False(state.ShouldAutoRefresh);

        state.ToggleCommand.Execute(null);
        Assert.False(state.IsPaused);
        Assert.True(state.ShouldAutoRefresh);
    }
}
```

- [ ] **Step 2: Run the focused tests and verify RED**

Run:

```powershell
dotnet test tests\ThreadBeacon.App.Tests\ThreadBeacon.App.Tests.csproj --filter "FullyQualifiedName~MonitoringStateTests"
```

Expected: compilation fails because `MonitoringState` does not exist.

- [ ] **Step 3: Implement the minimal state model**

Create `MonitoringState.cs`:

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ThreadBeacon.App.Commands;

namespace ThreadBeacon.App.ViewModels;

public sealed class MonitoringState : INotifyPropertyChanged
{
    private bool isPaused;

    public MonitoringState()
    {
        ToggleCommand = new RelayCommand(Toggle);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsPaused
    {
        get => isPaused;
        private set
        {
            if (isPaused == value)
            {
                return;
            }

            isPaused = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShouldAutoRefresh));
        }
    }

    public bool ShouldAutoRefresh => !IsPaused;

    public ICommand ToggleCommand { get; }

    private void Toggle() => IsPaused = !IsPaused;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

- [ ] **Step 4: Run the focused tests and verify GREEN**

Run the command from Step 2.

Expected: both `MonitoringStateTests` pass.

- [ ] **Step 5: Commit the state checkpoint**

```powershell
git add src/ThreadBeacon.App/ViewModels/MonitoringState.cs tests/ThreadBeacon.App.Tests/ViewModels/MonitoringStateTests.cs
git diff --cached --check
git commit -m "feat: model pause and resume monitoring"
```

### Task 2: Derive paused footer feedback

**Files:**

- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`

- [ ] **Step 1: Write failing footer tests with deterministic fake sources**

Create `MainWindowViewModelTests.cs` with these tests and helpers:

```csharp
using ThreadBeacon.App.Settings;
using ThreadBeacon.App.ViewModels;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.App.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task MonitoringPause_ShowsLastUpdateWithoutBlockingManualRefresh()
    {
        var monitoring = new MonitoringState();
        MainWindowViewModel viewModel = CreateViewModel(
            monitoring,
            ThreadRepositoryStatus.Healthy);

        monitoring.ToggleCommand.Execute(null);
        Assert.Equal("监听已暂停 · 尚未更新", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.UpdatedText);

        await viewModel.RefreshAsync();

        Assert.Equal("监听已暂停 · 上次更新", viewModel.StatusText);
        Assert.Matches("^\\d{2}:\\d{2}:\\d{2}$", viewModel.UpdatedText);
    }

    [Fact]
    public async Task MonitoringPause_DoesNotHideSourceError()
    {
        var monitoring = new MonitoringState();
        MainWindowViewModel viewModel = CreateViewModel(
            monitoring,
            ThreadRepositoryStatus.Missing);

        await viewModel.RefreshAsync();
        monitoring.ToggleCommand.Execute(null);

        Assert.Equal("未找到 Codex 状态数据库", viewModel.StatusText);
    }

    private static MainWindowViewModel CreateViewModel(
        MonitoringState monitoring,
        ThreadRepositoryStatus repositoryStatus)
    {
        var loader = new ThreadStatusLoader(
            new FakeThreadRepository(repositoryStatus),
            new HealthyTitleRepository(),
            new UnusedRolloutParser());
        var windowPin = new WindowPinState(new MemorySettingsStore());
        return new MainWindowViewModel(loader, windowPin, monitoring);
    }

    private sealed class FakeThreadRepository(ThreadRepositoryStatus status)
        : IThreadRepository
    {
        public ThreadLoadResult LoadRecent(int limit = 8) => new(status, []);
    }

    private sealed class HealthyTitleRepository : ISessionIndexTitleRepository
    {
        public TitleLoadResult LoadLatestTitles() =>
            new(SessionIndexStatus.Healthy, new Dictionary<string, string>());
    }

    private sealed class UnusedRolloutParser : IRolloutTailParser
    {
        public RolloutLoadResult Parse(string filePath) =>
            throw new InvalidOperationException("No empty-source rollout should be parsed.");
    }

    private sealed class MemorySettingsStore : IAppSettingsStore
    {
        public AppSettings Load() => new();

        public bool Save(AppSettings settings) => true;
    }
}
```

- [ ] **Step 2: Run the focused tests and verify RED**

```powershell
dotnet test tests\ThreadBeacon.App.Tests\ThreadBeacon.App.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests"
```

Expected: tests fail because the view model does not expose monitoring-aware footer behavior.

- [ ] **Step 3: Inject monitoring state and derive footer text**

Change the constructor to:

```csharp
public MainWindowViewModel(
    ThreadStatusLoader loader,
    WindowPinState windowPin,
    MonitoringState monitoring)
```

Expose `public MonitoringState Monitoring { get; }`. Keep the last source status text and whether it represents an error in private fields. Subscribe to `Monitoring.PropertyChanged`; when `IsPaused` changes, raise `PropertyChanged` for `StatusText` and `UpdatedText`.

Derive `StatusText` with this order:

```csharp
if (hasSourceError)
{
    return sourceStatusText;
}

if (Monitoring.IsPaused)
{
    return string.IsNullOrEmpty(updatedText)
        ? "监听已暂停 · 尚未更新"
        : "监听已暂停 · 上次更新";
}

return sourceStatusText;
```

Keep `UpdatedText` empty before the first successful refresh and retain the last `HH:mm:ss` value while paused. Manual `RefreshCommand` must remain enabled whenever `IsRefreshing` is false.

- [ ] **Step 4: Run focused and existing App tests**

```powershell
dotnet test tests\ThreadBeacon.App.Tests\ThreadBeacon.App.Tests.csproj --filter "FullyQualifiedName~MainWindowViewModelTests"
dotnet test tests\ThreadBeacon.App.Tests\ThreadBeacon.App.Tests.csproj
```

Expected: the new footer tests and all existing App tests pass.

- [ ] **Step 5: Commit the footer checkpoint**

```powershell
git add src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs
git diff --cached --check
git commit -m "feat: expose paused monitoring status"
```

### Task 3: Wire the timer lifecycle and header control

**Files:**

- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml.cs`

- [ ] **Step 1: Construct and subscribe to monitoring state**

In `MainWindow`, construct one `MonitoringState`, pass it into `MainWindowViewModel`, and subscribe to its `PropertyChanged` event. Unsubscribe in `OnClosed`.

- [ ] **Step 2: Stop and resume the timer**

Add an async handler for `MonitoringState.IsPaused` changes:

```csharp
private async void OnMonitoringPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName != nameof(MonitoringState.IsPaused))
    {
        return;
    }

    if (viewModel.Monitoring.IsPaused)
    {
        refreshTimer.Stop();
        return;
    }

    await viewModel.RefreshAsync();
    if (viewModel.Monitoring.ShouldAutoRefresh)
    {
        refreshTimer.Start();
    }
}
```

After the initial `OnLoaded` refresh, start the timer only when `ShouldAutoRefresh` remains true. Leave `RefreshCommand` unchanged so manual refresh works while paused.

- [ ] **Step 3: Add the macOS-aligned header button**

Insert a 32 x 32 button between the pin and refresh buttons. Give it `Margin="0,0,8,0"`, `Command="{Binding Monitoring.ToggleCommand}"`, and a style that switches:

- active: Tooltip and automation name `暂停监听`, glyph `&#xE769;`, secondary text color.
- paused: Tooltip and automation name `恢复监听`, glyph `&#xE768;`, foreground `#0067C0`.

Keep the existing pin button and refresh button behavior unchanged.

- [ ] **Step 4: Build the WPF application**

```powershell
dotnet build ThreadBeacon.slnx --configuration Release --no-restore
```

Expected: build succeeds with zero warnings and zero errors.

- [ ] **Step 5: Commit the UI checkpoint**

```powershell
git add src/ThreadBeacon.App/MainWindow.xaml src/ThreadBeacon.App/MainWindow.xaml.cs
git diff --cached --check
git commit -m "feat: add pause and resume control"
```

### Task 4: Document and verify the completed feature

**Files:**

- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`

- [ ] **Step 1: Update user documentation**

Document that the middle header button pauses/resumes automatic monitoring, manual refresh remains available while paused, resuming refreshes immediately, and app restart always returns to active monitoring. Add the completed feature to Phase 2 in `ROADMAP.md`.

- [ ] **Step 2: Run full automated verification**

```powershell
dotnet build ThreadBeacon.slnx --configuration Release --no-restore
dotnet test ThreadBeacon.slnx --configuration Release --no-build
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
```

Expected: build has zero warnings/errors, all tests pass, and no project has a known vulnerable package.

- [ ] **Step 3: Run real timing acceptance**

Launch the Release executable and use Windows UI Automation to verify:

1. The header exposes `暂停监听` between pin and refresh.
2. Click pause and wait at least five seconds; the right footer time does not change.
3. Invoke manual refresh once; the time advances once and remains unchanged for another three seconds.
4. Click resume; the time advances immediately and advances again across at least two subsequent two-second cycles.
5. Restart the process; the header exposes `暂停监听`, proving active is the default.
6. Resize to 480 pixels and capture a screenshot proving the three controls do not overlap the title.

- [ ] **Step 4: Commit documentation**

```powershell
git add README.md README-EN.md ROADMAP.md
git diff --cached --check
git commit -m "docs: document pause and resume monitoring"
```

- [ ] **Step 5: Run the mandatory pre-push privacy audit**

Inspect `origin/main...HEAD` for tracked `bin`, `obj`, settings, certificates, private keys, credentials, API tokens, absolute user paths, Codex task IDs/titles, rollout paths, and conversation content. Confirm `git diff --check origin/main...HEAD` and a clean working tree.

- [ ] **Step 6: Push and confirm remote equality**

```powershell
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
```

Expected: local and remote commit IDs are identical. Leave the verified Release App running for user inspection.
