# Window Always-on-Top Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a macOS-compatible window pin control that keeps ThreadBeacon above normal windows and restores the selection after restart.

**Architecture:** A focused JSON settings store in `ThreadBeacon.App` owns persistence under `%LOCALAPPDATA%`. A testable `WindowPinState` owns the current Boolean and toggle command, while WPF binds `Window.Topmost` and the header icon directly to that state.

**Tech Stack:** .NET 9, WPF, System.Text.Json, xUnit, Segoe Fluent Icons

---

## File Structure

- Create `src/ThreadBeacon.App/Settings/AppSettings.cs`: versioned app preferences model.
- Create `src/ThreadBeacon.App/Settings/IAppSettingsStore.cs`: read/write boundary for preferences.
- Create `src/ThreadBeacon.App/Settings/JsonAppSettingsStore.cs`: tolerant JSON persistence in Local AppData.
- Create `src/ThreadBeacon.App/ViewModels/WindowPinState.cs`: pin state, notification, and toggle behavior.
- Create `src/ThreadBeacon.App/Commands/RelayCommand.cs`: synchronous command used by the pin button.
- Modify `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`: expose `WindowPinState` to WPF.
- Modify `src/ThreadBeacon.App/MainWindow.xaml.cs`: construct the default settings store.
- Modify `src/ThreadBeacon.App/MainWindow.xaml`: bind `Topmost` and add the pin button before refresh.
- Create `tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj`: Windows-targeted app behavior tests.
- Create `tests/ThreadBeacon.App.Tests/Settings/JsonAppSettingsStoreTests.cs`: persistence and failure tests.
- Create `tests/ThreadBeacon.App.Tests/ViewModels/WindowPinStateTests.cs`: state and save-failure tests.
- Modify `ThreadBeacon.slnx`: include the new app test project.
- Modify `README.md`, `README-EN.md`, and `ROADMAP.md`: document the completed behavior.

### Task 1: JSON App Settings Store

**Files:**
- Create: `tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj`
- Create: `tests/ThreadBeacon.App.Tests/Settings/JsonAppSettingsStoreTests.cs`
- Create: `src/ThreadBeacon.App/Settings/AppSettings.cs`
- Create: `src/ThreadBeacon.App/Settings/IAppSettingsStore.cs`
- Create: `src/ThreadBeacon.App/Settings/JsonAppSettingsStore.cs`
- Modify: `ThreadBeacon.slnx`

- [ ] **Step 1: Add the app test project and failing storage tests**

Create a `net9.0-windows` xUnit project referencing `ThreadBeacon.App`. Tests must assert:

```csharp
[Fact]
public void Load_WhenFileIsMissing_ReturnsDefaults()
{
    var store = new JsonAppSettingsStore(Path.Combine(temp.Path, "settings.json"));
    Assert.False(store.Load().IsWindowPinned);
}

[Fact]
public void SaveAndLoad_RoundTripsPinnedState()
{
    var store = new JsonAppSettingsStore(Path.Combine(temp.Path, "settings.json"));
    Assert.True(store.Save(new AppSettings { IsWindowPinned = true }));
    Assert.True(store.Load().IsWindowPinned);
}

[Fact]
public void Load_WhenJsonIsInvalid_ReturnsDefaults()
{
    File.WriteAllText(settingsPath, "not-json");
    Assert.False(new JsonAppSettingsStore(settingsPath).Load().IsWindowPinned);
}

[Fact]
public void Save_WhenParentPathIsAFile_ReturnsFalse()
{
    File.WriteAllText(blockedParent, "blocked");
    var store = new JsonAppSettingsStore(Path.Combine(blockedParent, "settings.json"));
    Assert.False(store.Save(new AppSettings { IsWindowPinned = true }));
}
```

- [ ] **Step 2: Run the focused tests and verify they fail**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter JsonAppSettingsStoreTests
```

Expected: build fails because the settings types do not exist.

- [ ] **Step 3: Implement the settings boundary and JSON store**

Use this public contract:

```csharp
public sealed record AppSettings
{
    public int Version { get; init; } = 1;
    public bool IsWindowPinned { get; init; }
}

public interface IAppSettingsStore
{
    AppSettings Load();
    bool Save(AppSettings settings);
}
```

`JsonAppSettingsStore` must accept an explicit path for tests and provide:

```csharp
public static JsonAppSettingsStore CreateDefault()
{
    string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return new JsonAppSettingsStore(Path.Combine(root, "ThreadBeacon", "settings.json"));
}
```

`Load` catches file, permission, and JSON exceptions and returns `new AppSettings()`.
`Save` creates the parent directory, serializes camel-case indented JSON, returns
`true` on success, and returns `false` for storage or serialization exceptions.

- [ ] **Step 4: Run the storage tests and verify they pass**

Run the command from Step 2. Expected: four passing tests.

- [ ] **Step 5: Commit the settings store**

```powershell
git add ThreadBeacon.slnx src/ThreadBeacon.App/Settings tests/ThreadBeacon.App.Tests
git commit -m "feat: persist app settings as local JSON"
```

### Task 2: Testable Window Pin State

**Files:**
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/WindowPinStateTests.cs`
- Create: `src/ThreadBeacon.App/Commands/RelayCommand.cs`
- Create: `src/ThreadBeacon.App/ViewModels/WindowPinState.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml.cs`

- [ ] **Step 1: Write failing pin-state tests**

Use a recording fake `IAppSettingsStore` and verify:

```csharp
[Fact]
public void Constructor_RestoresPinnedState()
{
    var state = new WindowPinState(new FakeStore(new AppSettings { IsWindowPinned = true }));
    Assert.True(state.IsPinned);
}

[Fact]
public void ToggleCommand_TogglesAndSavesState()
{
    var store = new FakeStore(new AppSettings());
    var state = new WindowPinState(store);
    state.ToggleCommand.Execute(null);
    Assert.True(state.IsPinned);
    Assert.True(store.LastSaved!.IsWindowPinned);
}

[Fact]
public void ToggleCommand_WhenSaveFails_KeepsCurrentState()
{
    var state = new WindowPinState(new FakeStore(new AppSettings(), saveResult: false));
    state.ToggleCommand.Execute(null);
    Assert.True(state.IsPinned);
}
```

- [ ] **Step 2: Run the focused tests and verify they fail**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter WindowPinStateTests
```

Expected: build fails because `WindowPinState` is missing.

- [ ] **Step 3: Implement the pin state and synchronous command**

`WindowPinState` implements `INotifyPropertyChanged`, loads once in its
constructor, and exposes:

```csharp
public bool IsPinned { get; private set; }
public ICommand ToggleCommand { get; }
```

Its toggle action flips `IsPinned`, raises `PropertyChanged`, then calls:

```csharp
settingsStore.Save(new AppSettings { IsWindowPinned = IsPinned });
```

Do not revert `IsPinned` if `Save` returns false. Add a small `RelayCommand`
matching the existing `AsyncRelayCommand` conventions.

Expose the state from `MainWindowViewModel` through a constructor parameter and
property:

```csharp
public MainWindowViewModel(ThreadStatusLoader loader, WindowPinState windowPin)
public WindowPinState WindowPin { get; }
```

Construct it in `MainWindow.xaml.cs` with `JsonAppSettingsStore.CreateDefault()`.

- [ ] **Step 4: Run all app tests and verify they pass**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
```

Expected: all app settings and pin-state tests pass.

- [ ] **Step 5: Commit the pin state**

```powershell
git add src/ThreadBeacon.App/Commands/RelayCommand.cs src/ThreadBeacon.App/ViewModels src/ThreadBeacon.App/MainWindow.xaml.cs tests/ThreadBeacon.App.Tests/ViewModels
git commit -m "feat: add persistent window pin state"
```

### Task 3: WPF Pin Control

**Files:**
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`

- [ ] **Step 1: Bind the window level**

Add this attribute to the root `Window`:

```xml
Topmost="{Binding WindowPin.IsPinned}"
```

- [ ] **Step 2: Add the pin button immediately before refresh**

Split the header action area into a horizontal `StackPanel`. Add a 32x32 pin
button followed by the existing 32x32 refresh button with 8px spacing. Bind the
pin command to `WindowPin.ToggleCommand`.

The icon uses the Windows 11 native glyphs documented by Microsoft:

```xml
<TextBlock FontFamily="Segoe Fluent Icons" FontSize="15">
    <TextBlock.Style>
        <Style TargetType="TextBlock">
            <Setter Property="Text" Value="&#xE718;" />
            <Setter Property="Foreground" Value="{StaticResource SecondaryTextBrush}" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding WindowPin.IsPinned}" Value="True">
                    <Setter Property="Text" Value="&#xE841;" />
                    <Setter Property="Foreground" Value="#0067C0" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </TextBlock.Style>
</TextBlock>
```

Set the button tooltip through a `DataTrigger`: `钉在最前面` when false and
`取消钉住` when true. Preserve the current refresh command and tooltip.

- [ ] **Step 3: Build and run the complete automated suite**

```powershell
dotnet build ThreadBeacon.slnx
dotnet test ThreadBeacon.slnx --no-build
```

Expected: zero build errors and all tests pass.

- [ ] **Step 4: Commit the WPF control**

```powershell
git add src/ThreadBeacon.App/MainWindow.xaml
git commit -m "feat: add always-on-top window control"
```

### Task 4: Documentation and Manual Verification

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`

- [ ] **Step 1: Document the completed feature**

Add the following behavior to both READMEs: the top-right pin keeps the window
above normal apps and the choice survives restart. Move Always-on-top from the
ROADMAP future list to the completed POC list without changing unrelated items.

- [ ] **Step 2: Run final non-visual verification**

```powershell
dotnet build ThreadBeacon.slnx
dotnet test ThreadBeacon.slnx --no-build
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
git diff --check
```

Expected: zero warnings/errors, all tests pass, no vulnerable packages, and no
whitespace errors.

- [ ] **Step 3: Launch and manually verify**

Start `src/ThreadBeacon.App/bin/Debug/net9.0-windows/ThreadBeacon.App.exe` and
verify:

1. The outline pin appears immediately before refresh.
2. Clicking it shows the filled accent pin and keeps ThreadBeacon above Notepad.
3. Clicking it again restores normal stacking.
4. Pinning, closing, and restarting restores the pinned state.
5. `%LOCALAPPDATA%\ThreadBeacon\settings.json` contains only the version and pin Boolean.

- [ ] **Step 4: Commit documentation and verification-ready state**

```powershell
git add README.md README-EN.md ROADMAP.md
git commit -m "docs: describe window pin behavior"
```

- [ ] **Step 5: Report the commit range and leave push to explicit approval**

```powershell
git status --short --branch
git log --oneline origin/main..HEAD
```

Expected: a clean worktree, with local commits listed ahead of `origin/main`.
