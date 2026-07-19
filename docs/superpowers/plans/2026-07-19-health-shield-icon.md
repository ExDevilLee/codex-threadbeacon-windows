# Health Shield Icon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the healthy data-source status entry glyph with a Windows Fluent shield containing a check mark while preserving the macOS state mapping and all existing interaction behavior.

**Architecture:** `DataSourceHealthViewModel` will expose button-specific base and overlay glyph strings derived from the overall health state. `DataSourceHealthControl` will render those two glyphs in a fixed 16×16 grid; the existing `OverallGlyph` remains unchanged for the popup header and row icons.

**Tech Stack:** .NET 9, WPF XAML, C#, Segoe Fluent Icons, xUnit

---

### Task 1: Specify the button glyph state mapping

**Files:**
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/DataSourceHealthViewModelTests.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/DataSourceHealthViewModel.cs`

- [x] **Step 1: Write the failing ViewModel test**

Add a test that verifies all three overall states:

```csharp
[Fact]
public void HealthButtonGlyphs_MatchMacOsOverallStateMapping()
{
    var viewModel = new DataSourceHealthViewModel();

    Assert.Equal("\uEA18", viewModel.HealthButtonBaseGlyph);
    Assert.Equal("\uE73E", viewModel.HealthButtonOverlayGlyph);

    viewModel.Update(new DataSourceHealthReport(
        DataSourceHealthStatus.Healthy,
        DataSourceHealthStatus.Degraded("Rename unavailable"),
        DataSourceHealthStatus.Healthy,
        DataSourceHealthStatus.Healthy,
        1,
        0,
        DateTimeOffset.Now));

    Assert.Equal("\uE7BA", viewModel.HealthButtonBaseGlyph);
    Assert.Equal(string.Empty, viewModel.HealthButtonOverlayGlyph);

    viewModel.Update(new DataSourceHealthReport(
        DataSourceHealthStatus.Unavailable("Database unavailable"),
        DataSourceHealthStatus.NotUsed,
        DataSourceHealthStatus.NotUsed,
        DataSourceHealthStatus.NotUsed,
        0,
        0,
        null));

    Assert.Equal("\uEA39", viewModel.HealthButtonBaseGlyph);
    Assert.Equal(string.Empty, viewModel.HealthButtonOverlayGlyph);
}
```

- [x] **Step 2: Run the focused test and verify RED**

Run:

```powershell
dotnet test tests\ThreadBeacon.App.Tests\ThreadBeacon.App.Tests.csproj --filter "HealthButtonGlyphs_MatchMacOsOverallStateMapping"
```

Expected: compilation fails because `HealthButtonBaseGlyph` and `HealthButtonOverlayGlyph` do not exist.

- [x] **Step 3: Add the minimal ViewModel properties**

Add these properties beside `OverallGlyph`:

```csharp
public string HealthButtonBaseGlyph => OverallStatus switch
{
    OverallDataSourceHealth.Healthy => "\uEA18",
    OverallDataSourceHealth.Degraded => "\uE7BA",
    OverallDataSourceHealth.Unavailable => "\uEA39",
    _ => string.Empty,
};

public string HealthButtonOverlayGlyph => OverallStatus is OverallDataSourceHealth.Healthy
    ? "\uE73E"
    : string.Empty;
```

In `Update`, notify both new properties:

```csharp
OnPropertyChanged(nameof(HealthButtonBaseGlyph));
OnPropertyChanged(nameof(HealthButtonOverlayGlyph));
```

- [x] **Step 4: Run the focused test and verify GREEN**

Run the command from Step 2.

Expected: one matching test passes with no warning or error.

### Task 2: Render the layered shield inside the existing button

**Files:**
- Modify: `tests/ThreadBeacon.App.Tests/Controls/DataSourceHealthControlTests.cs`
- Modify: `src/ThreadBeacon.App/Controls/DataSourceHealthControl.xaml`

- [x] **Step 1: Write the failing control assertions**

After locating `HealthButton`, add:

```csharp
var baseGlyph = Assert.IsType<TextBlock>(control.FindName("HealthButtonBaseGlyph"));
var overlayGlyph = Assert.IsType<TextBlock>(control.FindName("HealthButtonOverlayGlyph"));
Assert.Equal("\uEA18", baseGlyph.Text);
Assert.Equal("\uE73E", overlayGlyph.Text);
Assert.Equal(16d, baseGlyph.FontSize);
Assert.Equal(9d, overlayGlyph.FontSize);
```

- [x] **Step 2: Run the focused control test and verify RED**

Run:

```powershell
dotnet test tests\ThreadBeacon.App.Tests\ThreadBeacon.App.Tests.csproj --filter "Construct_UsesDismissiblePopupAndKeepsItOpenAcrossReportUpdates"
```

Expected: the test fails because the named layered glyph elements do not exist.

- [x] **Step 3: Replace the button's single glyph with a fixed layered grid**

Replace the direct button `TextBlock` with:

```xml
<Grid Width="16" Height="16">
    <TextBlock x:Name="HealthButtonBaseGlyph"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"
               FontFamily="Segoe Fluent Icons"
               FontSize="16"
               Foreground="{Binding Details.OverallBrush, ElementName=Root}"
               Text="{Binding Details.HealthButtonBaseGlyph, ElementName=Root}" />
    <TextBlock x:Name="HealthButtonOverlayGlyph"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"
               FontFamily="Segoe Fluent Icons"
               FontSize="9"
               Foreground="{Binding Details.OverallBrush, ElementName=Root}"
               Text="{Binding Details.HealthButtonOverlayGlyph, ElementName=Root}" />
</Grid>
```

- [x] **Step 4: Run the focused control and ViewModel tests**

Run:

```powershell
dotnet test tests\ThreadBeacon.App.Tests\ThreadBeacon.App.Tests.csproj --filter "DataSourceHealth"
```

Expected: all matching tests pass and clicking the test button still opens the dismissible popup.

### Task 3: Verify the complete application and deliver

**Files:**
- Verify only: all solution projects

- [x] **Step 1: Run all automated tests**

```powershell
dotnet test ThreadBeacon.slnx --configuration Release
```

Expected: all tests pass with no failures.

- [x] **Step 2: Build the Release application**

```powershell
dotnet build ThreadBeacon.slnx --configuration Release --no-restore
```

Expected: build succeeds with zero warnings and zero errors.

- [x] **Step 3: Run dependency and repository safety checks**

```powershell
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
git diff --check
git diff -- src tests docs
```

Expected: no known vulnerable packages, no whitespace errors, no secrets, local paths, credentials, new network access, or data-write behavior in the diff.

- [x] **Step 4: Launch and visually verify the real control**

Start `src/ThreadBeacon.App/bin/Release/net9.0-windows/ThreadBeacon.App.exe`, verify the lower-right healthy status uses a green shield with a centered check mark, click it, and confirm the popup renders and the process remains responsive without a new `.NET Runtime` crash event.

- [x] **Step 5: Commit and push the feature**

```powershell
git add src/ThreadBeacon.App/Controls/DataSourceHealthControl.xaml src/ThreadBeacon.App/ViewModels/DataSourceHealthViewModel.cs tests/ThreadBeacon.App.Tests/Controls/DataSourceHealthControlTests.cs tests/ThreadBeacon.App.Tests/ViewModels/DataSourceHealthViewModelTests.cs docs/superpowers/plans/2026-07-19-health-shield-icon.md
git commit -m "feat(health): match macOS shield icon"
git push origin main
```

Expected: local `HEAD` and `origin/main` resolve to the same commit, while `.superpowers/` remains untracked and uncommitted.
