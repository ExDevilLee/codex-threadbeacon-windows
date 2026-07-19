# Windows Theme Preferences Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add persisted System/Light/Dark themes to the Windows app with immediate updates across all open WPF surfaces.

**Architecture:** Keep the saved preference in `DisplaySettings`, expose it through `DisplaySettingsViewModel`, and let an application-owned `AppThemeState` resolve System to the current Windows app appearance. Theme dictionaries with identical resource keys are loaded into `Application.Resources`; all user-facing brushes use `DynamicResource` so existing windows and popups update without recreation.

**Tech Stack:** .NET 9, WPF, xUnit, `System.Text.Json`, `Microsoft.Win32.Registry`, `Microsoft.Win32.SystemEvents`.

---

### Task 1: Add the theme model and stable persistence value

**Files:**
- Create: `src/ThreadBeacon.App/Theme/AppTheme.cs`
- Create: `src/ThreadBeacon.App/Theme/AppThemeJsonConverter.cs`
- Modify: `src/ThreadBeacon.App/Settings/DisplaySettings.cs`
- Test: `tests/ThreadBeacon.App.Tests/Theme/AppThemeTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/Settings/JsonDisplaySettingsStoreTests.cs`

- [ ] **Step 1: Write failing model and persistence tests**

Add tests for enum order/default, stable values (`system`, `light`, `dark`), unsupported JSON fallback to System, missing theme property fallback, and round-tripping a Light setting without changing refresh interval, maximum task count, or language.

- [ ] **Step 2: Run the focused tests and confirm the expected failures**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --no-restore --filter "FullyQualifiedName~AppTheme|FullyQualifiedName~JsonDisplaySettingsStoreTests"
```

Expected: compilation or assertion failures because `AppTheme` and `DisplaySettings.Theme` do not exist.

- [ ] **Step 3: Implement the minimal model and converter**

Use a string-backed enum and converter that maps unknown or null values to `AppTheme.System`. Add `Theme` to `DisplaySettings` with a default constructor value of System and preserve the existing constructor call sites through an optional final parameter.

- [ ] **Step 4: Run the focused tests and confirm they pass**

Run the same command. Expected: all AppTheme and display-store tests pass.

- [ ] **Step 5: Commit the model boundary**

```powershell
git add src/ThreadBeacon.App/Theme src/ThreadBeacon.App/Settings/DisplaySettings.cs tests/ThreadBeacon.App.Tests/Theme tests/ThreadBeacon.App.Tests/Settings/JsonDisplaySettingsStoreTests.cs
git commit -m "feat(theme): add persisted theme preference"
```

### Task 2: Add application theme resolution and system detection

**Files:**
- Create: `src/ThreadBeacon.App/Theme/AppThemeState.cs`
- Create: `src/ThreadBeacon.App/Theme/WindowsSystemThemeDetector.cs`
- Modify: `src/ThreadBeacon.App/ThreadBeacon.App.csproj`
- Test: `tests/ThreadBeacon.App.Tests/Theme/AppThemeStateTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/Theme/WindowsSystemThemeDetectorTests.cs`

- [ ] **Step 1: Write failing state and detector tests**

Test that explicit Light/Dark never changes when the system detector changes, System resolves to the detector result, detector failures return Light, and `Changed` fires only when the effective theme changes. The detector tests must use an injected registry reader/`Func<bool?>`, not the real machine registry.

- [ ] **Step 2: Run the focused tests and confirm they fail for missing types**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --no-restore --filter "FullyQualifiedName~AppThemeState|FullyQualifiedName~WindowsSystemThemeDetector"
```

- [ ] **Step 3: Implement the state and detector**

`WindowsSystemThemeDetector` reads `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`; value `1` means Light, `0` means Dark, missing/errors mean Light. Subscribe to `Microsoft.Win32.SystemEvents.UserPreferenceChanged` and expose `ThemeChanged`; detach it in `Dispose`.

`AppThemeState` owns the saved preference, the detector, and the effective theme. `SetPreference` persists through an injected callback and raises `Changed` only when the resolved theme changes. Keep System as the saved preference even when its current resolution is Light.

- [ ] **Step 4: Run focused tests and verify green**

Run the two filters above. Expected: all tests pass without reading the real registry.

- [ ] **Step 5: Commit theme resolution**

```powershell
git add src/ThreadBeacon.App/Theme src/ThreadBeacon.App/ThreadBeacon.App.csproj tests/ThreadBeacon.App.Tests/Theme
git commit -m "feat(theme): resolve Windows system appearance"
```

### Task 3: Add light/dark resource dictionaries and convert brush bindings

**Files:**
- Create: `src/ThreadBeacon.App/Resources/Theme.Light.xaml`
- Create: `src/ThreadBeacon.App/Resources/Theme.Dark.xaml`
- Modify: `src/ThreadBeacon.App/App.xaml`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Modify: `src/ThreadBeacon.App/SettingsWindow.xaml`
- Modify: `src/ThreadBeacon.App/Controls/DataSourceHealthControl.xaml`
- Modify: `src/ThreadBeacon.App/Controls/TokenInfoControl.xaml`
- Test: `tests/ThreadBeacon.App.Tests/Theme/ThemeResourceTests.cs`

- [ ] **Step 1: Write failing resource and XAML tests**

Test that both dictionaries expose exactly the shared keys `WindowBackgroundBrush`, `SurfaceBrush`, `PrimaryTextBrush`, `SecondaryTextBrush`, and `ControlBorderBrush`; test that the main/settings/control XAML uses `DynamicResource` for those keys and no longer uses `StaticResource` for theme-dependent brushes.

- [ ] **Step 2: Run focused tests and confirm the expected failures**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --no-restore --filter "FullyQualifiedName~ThemeResourceTests"
```

- [ ] **Step 3: Implement the dictionaries and binding migration**

Move the current light values into `Theme.Light.xaml`. Define a dark palette with sufficient contrast, keeping status brushes outside the theme dictionary. Remove duplicate color brush definitions from `App.xaml`; leave shared typography/style definitions there with `Foreground="{DynamicResource PrimaryTextBrush}"`. Replace theme-dependent `StaticResource` references in all four XAML surfaces with `DynamicResource`.

- [ ] **Step 4: Run resource tests and the existing XAML tests**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --no-restore --filter "FullyQualifiedName~ThemeResourceTests|FullyQualifiedName~Views"
```

Expected: all resource-key and existing layout tests pass.

- [ ] **Step 5: Commit resource migration**

```powershell
git add src/ThreadBeacon.App/App.xaml src/ThreadBeacon.App/MainWindow.xaml src/ThreadBeacon.App/SettingsWindow.xaml src/ThreadBeacon.App/Controls src/ThreadBeacon.App/Resources/Theme.*.xaml tests/ThreadBeacon.App.Tests/Theme
git commit -m "feat(theme): add light and dark resource dictionaries"
```

### Task 4: Bind theme selection in Settings and apply resources at startup

**Files:**
- Modify: `src/ThreadBeacon.App/App.xaml.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/DisplaySettingsViewModel.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/MonitoringSettingsCoordinator.cs`
- Modify: `src/ThreadBeacon.App/SettingsWindow.xaml`
- Test: `tests/ThreadBeacon.App.Tests/ViewModels/DisplaySettingsViewModelTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/ViewModels/MonitoringSettingsCoordinatorTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/Views/SettingsWindowXamlTests.cs`

- [ ] **Step 1: Write failing ViewModel and XAML tests**

Test that theme options are ordered System/Light/Dark, names follow the active UI language, setting Light persists immediately, and Settings contains a dedicated theme row bound to `Display.Theme`.

- [ ] **Step 2: Run the focused tests and verify red**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --no-restore --filter "FullyQualifiedName~DisplaySettingsViewModel|FullyQualifiedName~MonitoringSettingsCoordinator|FullyQualifiedName~SettingsWindowXamlTests"
```

- [ ] **Step 3: Implement binding and startup integration**

Add `ThemeOptions`, `Theme`, and `EffectiveTheme` to `DisplaySettingsViewModel`. Extend every save path to preserve the theme field. Add a ComboBox row to the general settings grid without reusing the fixed spacer row that previously caused overlap. In `App.OnStartup`, create `AppThemeState` from the loaded preference, apply the selected dictionary before creating `MainWindow`, subscribe to state changes, and dispose the detector on app exit. Pass the shared state to the Settings ViewModel.

- [ ] **Step 4: Run focused tests and verify green**

Run the same command. Expected: all theme binding, coordinator, and settings layout tests pass.

- [ ] **Step 5: Commit settings integration**

```powershell
git add src/ThreadBeacon.App/App.xaml.cs src/ThreadBeacon.App/ViewModels/DisplaySettingsViewModel.cs src/ThreadBeacon.App/ViewModels/MonitoringSettingsCoordinator.cs src/ThreadBeacon.App/SettingsWindow.xaml tests/ThreadBeacon.App.Tests/ViewModels tests/ThreadBeacon.App.Tests/Views/SettingsWindowXamlTests.cs
git commit -m "feat(theme): add theme selector to settings"
```

### Task 5: Verify app-wide switching and persistence

**Files:**
- Modify: `tests/ThreadBeacon.App.Tests/Views/MainWindowXamlTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/Theme/AppThemeStateTests.cs`
- Modify: `ROADMAP.md`
- Modify: `README.md`
- Modify: `README-EN.md`

- [ ] **Step 1: Add integration assertions**

Assert that all theme-dependent surfaces reference DynamicResource, that switching the state changes the application dictionary source, and that the saved JSON still contains language, refresh interval, maximum task count, and theme after a theme change.

- [ ] **Step 2: Run the complete test suite**

```powershell
dotnet test --no-restore --logger "console;verbosity=minimal"
```

Expected: all App and Core tests pass; no build warnings or test failures.

- [ ] **Step 3: Publish and manually verify with Computer Use**

Publish with:

```powershell
dotnet publish src/ThreadBeacon.App/ThreadBeacon.App.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish
```

Install into `C:\Users\Administrator\AppData\Local\ThreadBeacon`, launch the installed EXE, and use the Settings window to select Light, Dark, System, and English. Verify the main window, Settings, Token popup, health popup, and context menu update without restarting; close/reopen the app and verify the selected theme persists.

- [ ] **Step 4: Update documentation**

Document the three theme modes and the default System behavior in Chinese and English README files. Mark the Windows theme milestone complete in ROADMAP without claiming custom themes or high-contrast support.

- [ ] **Step 5: Run security and diff checks**

```powershell
git diff --check
rg -n -i "password|secret|api[_-]?key|BEGIN (RSA|OPENSSH|EC) PRIVATE KEY" --glob '!bin/**' --glob '!obj/**' --glob '!artifacts/**' .
rg -n "C:\\Users|D:\\Coding" --glob '!bin/**' --glob '!obj/**' --glob '!artifacts/**' .
```

Only documentation examples may match; no actual credentials or machine-specific paths may be introduced.

- [ ] **Step 6: Commit, push, and reinstall the verified build**

```powershell
git add README.md README-EN.md ROADMAP.md tests
git commit -m "feat(theme): complete Windows theme preferences"
git push origin main
```

Republish/copy/start the installed EXE after the final commit and verify `git rev-parse HEAD` matches `git ls-remote origin refs/heads/main`.
