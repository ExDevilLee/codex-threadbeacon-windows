# Task Completion Sound Notification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Play one configurable bundled sound when an automatic refresh observes one or more new reliable task-completion events.

**Architecture:** A pure Core tracker converts `ThreadSnapshot.CompletionEventAt` values into bounded, persisted event IDs and distinguishes baseline refreshes from notifying refreshes. The WPF application owns independent JSON sound preferences, safe WAV playback, and a compact speaker popover; `MainWindow` assigns the policy for each refresh source.

**Tech Stack:** .NET 9, C#, WPF, System.Text.Json, System.Media.SoundPlayer, xUnit

---

## File Map

- Create `src/ThreadBeacon.Core/Notifications/RefreshNotificationPolicy.cs`: baseline/notify policy.
- Create `src/ThreadBeacon.Core/Notifications/CompletionNotificationEvent.cs`: derived completion event.
- Create `src/ThreadBeacon.Core/Notifications/CompletionNotificationTracker.cs`: pure dedupe and bounded history.
- Create `tests/ThreadBeacon.Core.Tests/Notifications/CompletionNotificationTrackerTests.cs`: tracker contract.
- Create `src/ThreadBeacon.App/Sounds/CompletionSound.cs`: stable persisted sound IDs and filenames.
- Create `src/ThreadBeacon.App/Sounds/SoundNotificationSettings.cs`: persisted preferences and seen IDs.
- Create `src/ThreadBeacon.App/Sounds/ISoundNotificationSettingsStore.cs`: settings storage boundary.
- Create `src/ThreadBeacon.App/Sounds/JsonSoundNotificationSettingsStore.cs`: tolerant JSON storage.
- Create `tests/ThreadBeacon.App.Tests/Sounds/JsonSoundNotificationSettingsStoreTests.cs`: storage contract.
- Create `src/ThreadBeacon.App/Sounds/ISoundPlaybackService.cs`: playback boundary.
- Create `src/ThreadBeacon.App/Sounds/WavSoundPlaybackService.cs`: defensive Windows WAV playback.
- Create `src/ThreadBeacon.App/Sounds/CompletionNotificationCoordinator.cs`: tracker/settings/playback orchestration.
- Create `src/ThreadBeacon.App/ViewModels/SoundSettingsViewModel.cs`: bindable preferences and preview.
- Create `tests/ThreadBeacon.App.Tests/Sounds/CompletionNotificationCoordinatorTests.cs`: notification filtering and failure isolation.
- Create `tests/ThreadBeacon.App.Tests/ViewModels/SoundSettingsViewModelTests.cs`: settings and preview behavior.
- Modify `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`: observe successful refreshes with a supplied policy.
- Modify `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`: verify policy forwarding.
- Modify `src/ThreadBeacon.App/MainWindow.xaml.cs`: construct sound services and choose refresh policies.
- Modify `src/ThreadBeacon.App/MainWindow.xaml`: speaker button and settings popover.
- Modify `src/ThreadBeacon.App/ThreadBeacon.App.csproj`: copy WAV content to output.
- Create `src/ThreadBeacon.App/Resources/Sounds/Done-Beacon.wav`: shared macOS tone.
- Create `src/ThreadBeacon.App/Resources/Sounds/Done-Chime.wav`: shared macOS tone.
- Create `src/ThreadBeacon.App/Resources/Sounds/Done-Pulse.wav`: shared macOS tone.
- Modify `README.md`, `README-EN.md`, and `ROADMAP.md`: document completion sound support and limits.

### Task 1: Pure Completion Notification Tracker

**Files:**
- Create: `src/ThreadBeacon.Core/Notifications/RefreshNotificationPolicy.cs`
- Create: `src/ThreadBeacon.Core/Notifications/CompletionNotificationEvent.cs`
- Create: `src/ThreadBeacon.Core/Notifications/CompletionNotificationTracker.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Notifications/CompletionNotificationTrackerTests.cs`

- [x] **Step 1: Write failing tracker tests**

Create snapshots with and without `CompletionEventAt`, then assert:

```csharp
var tracker = new CompletionNotificationTracker();
CompletionNotificationEvent? result = tracker.Observe(
    [Completed("thread-1", At(10))],
    RefreshNotificationPolicy.Baseline);

Assert.Null(result);
Assert.Equal(["done:thread-1:10000"], tracker.SeenEventIds);
```

Add cases proving `Notify` returns a new event once, repeated observations return
null, two new completions in one batch return one event while recording both IDs,
snapshots without completion evidence do nothing, supplied history suppresses old
events, and 257 observations retain exactly the newest 256 IDs.

- [x] **Step 2: Run the tracker tests and confirm failure**

Run:

```powershell
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --filter CompletionNotificationTrackerTests
```

Expected: compilation fails because the notification types do not exist.

- [x] **Step 3: Implement the pure tracker**

Use these public contracts:

```csharp
public enum RefreshNotificationPolicy { Baseline, Notify }

public sealed record CompletionNotificationEvent(
    string EventId,
    string ThreadId,
    DateTimeOffset OccurredAt);

public sealed class CompletionNotificationTracker
{
    public const int MaximumHistory = 256;
    public CompletionNotificationTracker(IEnumerable<string>? seenEventIds = null);
    public IReadOnlyList<string> SeenEventIds { get; }
    public CompletionNotificationEvent? Observe(
        IReadOnlyList<ThreadSnapshot> snapshots,
        RefreshNotificationPolicy policy);
}
```

Generate IDs with `completion.ToUnixTimeMilliseconds()`. Record every unseen
candidate in input order, trim from the oldest end after every batch, and return the
first unseen candidate only when policy is `Notify`.

- [x] **Step 4: Run Core tests**

Run:

```powershell
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj
```

Expected: all Core tests pass.

- [x] **Step 5: Commit**

```powershell
git add src/ThreadBeacon.Core/Notifications tests/ThreadBeacon.Core.Tests/Notifications
git commit -m "feat(sound): track new completion events"
```

### Task 2: Independent Sound Settings Persistence

**Files:**
- Create: `src/ThreadBeacon.App/Sounds/CompletionSound.cs`
- Create: `src/ThreadBeacon.App/Sounds/SoundNotificationSettings.cs`
- Create: `src/ThreadBeacon.App/Sounds/ISoundNotificationSettingsStore.cs`
- Create: `src/ThreadBeacon.App/Sounds/JsonSoundNotificationSettingsStore.cs`
- Test: `tests/ThreadBeacon.App.Tests/Sounds/JsonSoundNotificationSettingsStoreTests.cs`

- [x] **Step 1: Write failing settings tests**

Assert missing and malformed files load these defaults:

```csharp
var expected = new SoundNotificationSettings
{
    IsEnabled = true,
    IsCompletionEnabled = true,
    SelectedCompletionSound = CompletionSound.Beacon,
    SeenEventIds = [],
};
```

Assert save/load round-trips disabled flags, `CompletionSound.Pulse`, and two event
IDs. Assert saving to a nested missing directory creates it. Assert an unavailable
path returns `false` rather than throwing.

- [x] **Step 2: Run the settings tests and confirm failure**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter JsonSoundNotificationSettingsStoreTests
```

Expected: compilation fails because the sound settings types do not exist.

- [x] **Step 3: Implement tolerant settings storage**

Define:

```csharp
public enum CompletionSound { Beacon, Chime, Pulse }

public sealed record SoundNotificationSettings
{
    public int Version { get; init; } = 1;
    public bool IsEnabled { get; init; } = true;
    public bool IsCompletionEnabled { get; init; } = true;
    public CompletionSound SelectedCompletionSound { get; init; } = CompletionSound.Beacon;
    public IReadOnlyList<string> SeenEventIds { get; init; } = [];
}
```

The JSON store mirrors `JsonAppSettingsStore` error handling and writes to
`%LOCALAPPDATA%\ThreadBeacon\sound-settings.json`. Add a string enum converter so
the file remains readable. Normalize a null `SeenEventIds` to an empty array after
deserialization.

- [x] **Step 4: Run application settings tests**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "JsonSoundNotificationSettingsStoreTests|JsonAppSettingsStoreTests"
```

Expected: all selected tests pass.

- [x] **Step 5: Commit**

```powershell
git add src/ThreadBeacon.App/Sounds tests/ThreadBeacon.App.Tests/Sounds
git commit -m "feat(sound): persist sound preferences"
```

### Task 3: Coordinator, Bindable Settings, and Refresh Policy

**Files:**
- Create: `src/ThreadBeacon.App/Sounds/ISoundPlaybackService.cs`
- Create: `src/ThreadBeacon.App/Sounds/ICompletionNotificationObserver.cs`
- Create: `src/ThreadBeacon.App/Sounds/CompletionNotificationCoordinator.cs`
- Create: `src/ThreadBeacon.App/ViewModels/SoundSettingsViewModel.cs`
- Create: `tests/ThreadBeacon.App.Tests/Sounds/CompletionNotificationCoordinatorTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/SoundSettingsViewModelTests.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`

- [x] **Step 1: Write failing coordinator and view-model tests**

Use in-memory settings and a recording player. Cover:

```csharp
coordinator.Observe([Completed("a", At(1))], RefreshNotificationPolicy.Baseline);
Assert.Empty(player.Played);
Assert.Single(store.Current.SeenEventIds);

coordinator.Observe([Completed("b", At(2))], RefreshNotificationPolicy.Notify);
Assert.Equal([CompletionSound.Beacon], player.Played);
```

Also prove global disable and completion disable prevent playback while still
recording IDs; batch completions play once; a throwing player does not escape;
property changes save immediately; and Preview plays the selected sound regardless
of completion history but respects the global enable toggle.

- [x] **Step 2: Write a failing refresh-policy forwarding test**

Inject a recording `ICompletionNotificationObserver` into `MainWindowViewModel`, call:

```csharp
await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);
Assert.Equal(RefreshNotificationPolicy.Notify, observer.LastPolicy);
```

Assert loader failure does not invoke the observer.

- [x] **Step 3: Run the focused tests and confirm failure**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "CompletionNotification|SoundSettingsViewModel|MainWindowViewModelTests"
```

Expected: compilation fails because the coordinator contracts and policy-aware
refresh overload do not exist.

- [x] **Step 4: Implement coordinator and settings view model**

Use these boundaries:

```csharp
public interface ISoundPlaybackService
{
    bool Play(CompletionSound sound);
}

public interface ICompletionNotificationObserver
{
    void Observe(IReadOnlyList<ThreadSnapshot> snapshots, RefreshNotificationPolicy policy);
}
```

`CompletionNotificationCoordinator` initializes the Core tracker from persisted
history, persists the tracker's history after every observation that changes it,
and calls `Play` only when the tracker returned an event and both toggles are enabled.
Catch playback exceptions at this boundary.

`SoundSettingsViewModel` exposes `IsEnabled`, `IsCompletionEnabled`,
`SelectedCompletionSound`, three display options, and `PreviewCommand`. Each setter
saves an immutable settings copy. Preview calls the player only when globally enabled
and catches playback exceptions.

- [x] **Step 5: Make refresh policy explicit**

Change the command construction to a parameterless lambda:

```csharp
refreshCommand = new AsyncRelayCommand(
    () => RefreshAsync(RefreshNotificationPolicy.Baseline),
    () => !IsRefreshing);
```

Change the method signature and invoke the observer only after a successful load:

```csharp
public async Task RefreshAsync(
    RefreshNotificationPolicy policy = RefreshNotificationPolicy.Baseline)
{
    // existing gated load and UI reconciliation
    completionObserver.Observe(result.Threads, policy);
}
```

- [x] **Step 6: Run application tests**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
```

Expected: all application tests pass.

- [x] **Step 7: Commit**

```powershell
git add src/ThreadBeacon.App/Sounds src/ThreadBeacon.App/ViewModels tests/ThreadBeacon.App.Tests
git commit -m "feat(sound): notify on automatic completions"
```

### Task 4: Bundle and Play the Original WAV Tones

**Files:**
- Create: `src/ThreadBeacon.App/Sounds/WavSoundPlaybackService.cs`
- Create: `src/ThreadBeacon.App/Resources/Sounds/Done-Beacon.wav`
- Create: `src/ThreadBeacon.App/Resources/Sounds/Done-Chime.wav`
- Create: `src/ThreadBeacon.App/Resources/Sounds/Done-Pulse.wav`
- Modify: `src/ThreadBeacon.App/ThreadBeacon.App.csproj`
- Test: `tests/ThreadBeacon.App.Tests/Sounds/WavSoundPlaybackServiceTests.cs`

- [x] **Step 1: Write failing resource-resolution tests**

Construct the player with a temporary base directory and assert each enum maps to:

```text
Resources/Sounds/Done-Beacon.wav
Resources/Sounds/Done-Chime.wav
Resources/Sounds/Done-Pulse.wav
```

Assert `Play` returns false when the file is missing and does not throw.

- [x] **Step 2: Run focused tests and confirm failure**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter WavSoundPlaybackServiceTests
```

Expected: compilation fails because `WavSoundPlaybackService` does not exist.

- [x] **Step 3: Implement defensive WAV playback**

Resolve a fixed filename from the enum, combine it with the injected or default
`AppContext.BaseDirectory`, verify existence, and use `System.Media.SoundPlayer` to
play asynchronously. Keep the active player in a field so it is not collected during
playback. Catch IO, invalid WAV, and platform/audio exceptions and return `false`.

- [x] **Step 4: Reuse and bundle the macOS WAV assets**

Copy the three author-owned WAV files from the local macOS reference checkout into
`src/ThreadBeacon.App/Resources/Sounds`. Add:

```xml
<Content Include="Resources\Sounds\*.wav">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

Compare SHA-256 hashes with the macOS source files and inspect WAV headers.

- [x] **Step 5: Run tests and Release build**

Run:

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
```

Expected: all tests pass; build has zero errors; all three WAV files exist under the
Release output `Resources\Sounds` directory.

- [x] **Step 6: Commit**

```powershell
git add src/ThreadBeacon.App/Sounds src/ThreadBeacon.App/Resources/Sounds src/ThreadBeacon.App/ThreadBeacon.App.csproj tests/ThreadBeacon.App.Tests/Sounds
git commit -m "feat(sound): bundle original completion tones"
```

### Task 5: Speaker Popover and Runtime Wiring

**Files:**
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml.cs`
- Test: `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`

- [x] **Step 1: Add policy coverage before window wiring**

Keep the existing default/manual refresh assertion as Baseline and the explicit
automatic call as Notify. Run the application tests before changing the window.

- [x] **Step 2: Construct sound services in the window**

Create one JSON store, WAV player, settings view model, and coordinator. Inject the
coordinator into `MainWindowViewModel`. Assign the settings panel's data context to
the sound settings view model.

Call:

```csharp
await viewModel.RefreshAsync(RefreshNotificationPolicy.Baseline); // Loaded
await viewModel.RefreshAsync(RefreshNotificationPolicy.Notify);   // Timer tick
await viewModel.RefreshAsync(RefreshNotificationPolicy.Baseline); // Resume
```

The bound manual command remains Baseline.

- [x] **Step 3: Add the speaker button and popover**

Insert a 32-by-32 Segoe Fluent Icons speaker button between pin and pause with an
eight-pixel right margin. Its tooltip and automation name are `提示音设置`.

Add a `Popup` placed below the speaker, `StaysOpen="False"`, containing a single
260-pixel-wide bordered surface with:

```xml
<CheckBox Content="启用提示音" IsChecked="{Binding IsEnabled}" />
<CheckBox Content="任务完成" IsChecked="{Binding IsCompletionEnabled}" />
<ComboBox ItemsSource="{Binding AvailableSounds}"
          DisplayMemberPath="DisplayName"
          SelectedValuePath="Value"
          SelectedValue="{Binding SelectedCompletionSound}" />
<Button Content="试听" Command="{Binding PreviewCommand}" />
```

Disable the completion controls when global sounds are disabled. Toggle the popup
from the speaker click handler; rely on `StaysOpen=False` for outside-click closing.

- [x] **Step 4: Run all automated verification**

Run:

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
```

Expected: all tests pass and the Release build has zero warnings and zero errors.

- [x] **Step 5: Run the app and inspect interaction**

Launch the Release executable. Verify the toolbar order is pin, sound, pause, refresh;
the popover closes on outside click; toggles and tone selection persist after restart;
Preview plays each enabled tone; disabled sounds do not preview; pause/resume still
works; and content does not overlap at 480-pixel width.

- [x] **Step 6: Commit**

```powershell
git add src/ThreadBeacon.App/MainWindow.xaml src/ThreadBeacon.App/MainWindow.xaml.cs tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs
git commit -m "feat(sound): add completion sound controls"
```

### Task 6: Documentation, Security Audit, and Push

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`

- [ ] **Step 1: Document the completed scope**

Add task-completion sound and selectable built-in tones to both READMEs. State that
startup, manual refresh, and resume do not replay historical completions. Mark only
the reliable completion-sound milestone complete in ROADMAP; leave warning/failure,
tray, and service-status sounds unclaimed.

- [ ] **Step 2: Run final verification from a clean process**

Close any old app instance, then run:

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
```

Expected: all tests pass, Release build has zero errors, and no vulnerable packages
are reported.

- [ ] **Step 3: Perform the mandatory pre-push security review**

Inspect `git status`, the full branch diff against `origin/main`, and tracked files.
Search added text for private keys, access tokens, credentials, machine-specific
absolute paths, temporary files, Codex task content, and local settings. Confirm WAV
files are the three intended author-owned assets and settings contain no user data.

- [ ] **Step 4: Commit documentation**

```powershell
git add README.md README-EN.md ROADMAP.md
git commit -m "docs: document completion sound notifications"
```

- [ ] **Step 5: Push and verify the remote**

```powershell
git push origin main
git status --short --branch
git rev-parse HEAD
git rev-parse origin/main
```

Expected: push succeeds, the worktree is clean, and local `HEAD` equals
`origin/main`.
