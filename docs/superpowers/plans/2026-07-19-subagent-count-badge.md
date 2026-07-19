# Subagent Count Badge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Display a neutral direct-Subagent count after each primary task title when the count is greater than zero.

**Architecture:** Reuse the existing `ThreadSnapshot.SubagentCount` data contract and add derived presentation properties to the existing row view model. Replace only the title cell with a star/auto layout so the optional indicator does not alter Token or duration columns.

**Tech Stack:** .NET 9, C#, WPF, Segoe Fluent Icons, xUnit

---

## File Map

- Create `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowViewModelTests.cs`: count display contract.
- Modify `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowCollectionTests.cs`: reconciliation contract.
- Modify `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`: count and derived labels.
- Modify `src/ThreadBeacon.App/MainWindow.xaml`: neutral inline branch indicator.
- Modify `README.md`, `README-EN.md`, and `ROADMAP.md`: count semantics and delivered scope.

### Task 1: Row View Model Count Contract

**Files:**
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowViewModelTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowCollectionTests.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`

- [ ] **Step 1: Write failing zero and positive count tests**

Construct rows from snapshots and assert:

```csharp
var empty = new ThreadRowViewModel(Snapshot(subagentCount: 0), Now);
Assert.Equal(0, empty.SubagentCount);
Assert.False(empty.HasSubagents);
Assert.Equal(string.Empty, empty.SubagentCountText);
Assert.Equal(string.Empty, empty.SubagentAccessibilityLabel);

var populated = new ThreadRowViewModel(Snapshot(subagentCount: 3), Now);
Assert.Equal(3, populated.SubagentCount);
Assert.True(populated.HasSubagents);
Assert.Equal("3", populated.SubagentCountText);
Assert.Equal("3 个 Subagent", populated.SubagentAccessibilityLabel);
```

Add a negative-count case that is defensively normalized to zero.

- [ ] **Step 2: Write a failing reconciliation test**

Create a row with one Subagent, reconcile the same thread with four, and assert the
row instance is preserved while all four count properties update.

- [ ] **Step 3: Run focused tests and confirm failure**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "ThreadRowViewModelTests|ThreadRowCollectionTests"
```

Expected: compilation fails because the count presentation properties do not exist.

- [ ] **Step 4: Implement the count properties**

Add a backing field and these properties:

```csharp
public int SubagentCount
{
    get => subagentCount;
    private set
    {
        value = Math.Max(0, value);
        if (SetField(ref subagentCount, value))
        {
            OnPropertyChanged(nameof(HasSubagents));
            OnPropertyChanged(nameof(SubagentCountText));
            OnPropertyChanged(nameof(SubagentAccessibilityLabel));
        }
    }
}

public bool HasSubagents => SubagentCount > 0;
public string SubagentCountText => HasSubagents
    ? SubagentCount.ToString(CultureInfo.InvariantCulture)
    : string.Empty;
public string SubagentAccessibilityLabel => HasSubagents
    ? $"{SubagentCountText} 个 Subagent"
    : string.Empty;
```

Assign `SubagentCount = snapshot.SubagentCount` inside `Update`.

- [ ] **Step 5: Run focused and full application tests**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "ThreadRowViewModelTests|ThreadRowCollectionTests"
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
```

Expected: all focused tests and all application tests pass.

- [ ] **Step 6: Commit**

```powershell
git add src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs tests/ThreadBeacon.App.Tests/ViewModels
git commit -m "feat(subagent): expose count badge state"
```

### Task 2: Neutral Inline Indicator

**Files:**
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`

- [ ] **Step 1: Replace the title cell with a two-column layout**

Keep the outer task-row columns unchanged. In column 2 use:

```xml
<Grid Grid.Column="2" Margin="0,0,12,0">
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*" />
    <ColumnDefinition Width="Auto" />
  </Grid.ColumnDefinitions>
  <TextBlock VerticalAlignment="Center"
             FontSize="13"
             Text="{Binding Title}"
             TextTrimming="CharacterEllipsis"
             ToolTip="{Binding Title}" />
  <StackPanel Grid.Column="1"
              Margin="8,0,0,0"
              VerticalAlignment="Center"
              Orientation="Horizontal"
              ToolTip="{Binding SubagentAccessibilityLabel}"
              AutomationProperties.Name="{Binding SubagentAccessibilityLabel}"
              Visibility="{Binding HasSubagents, Converter={StaticResource BooleanToVisibilityConverter}}">
    <TextBlock VerticalAlignment="Center"
               FontFamily="Segoe Fluent Icons"
               FontSize="11"
               Foreground="{StaticResource SecondaryTextBrush}"
               Text="&#xE8AC;" />
    <TextBlock Margin="3,0,0,0"
               VerticalAlignment="Center"
               FontFamily="Cascadia Mono, Consolas"
               FontSize="11"
               Foreground="{StaticResource SecondaryTextBrush}"
               Text="{Binding SubagentCountText}" />
  </StackPanel>
</Grid>
```

- [ ] **Step 2: Run all tests and Release build**

Close the running Release instance, then run:

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
```

Expected: Core and application tests pass; Release build has zero warnings and zero
errors.

- [ ] **Step 3: Inspect runtime layout**

Launch the Release executable and verify tasks with a positive direct count show the
branch icon and exact number; zero-count tasks reserve no space; tooltip and
accessibility name contain `N 个 Subagent`; long titles, count, Token info, and
duration do not overlap at 620px or 480px.

- [ ] **Step 4: Commit**

```powershell
git add src/ThreadBeacon.App/MainWindow.xaml
git commit -m "feat(subagent): show direct count badge"
```

### Task 3: Documentation, Security Audit, and Push

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`

- [ ] **Step 1: Document exact semantics**

State that positive primary-task rows show direct historical Subagent count, not a
live running count. Mark the neutral count indicator complete while leaving inline
expansion, child status, alerts, and Token aggregation deferred.

- [ ] **Step 2: Run final verification**

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
```

Expected: all tests pass, build has zero errors, and no vulnerable packages are
reported.

- [ ] **Step 3: Perform mandatory pre-push security review**

Inspect the full diff against `origin/main`, tracked paths, and added text for keys,
tokens, credentials, absolute user paths, local settings, Codex content, build output,
and temporary files. Confirm this feature persists no new data and adds no network or
write access.

- [ ] **Step 4: Commit documentation**

```powershell
git add README.md README-EN.md ROADMAP.md
git commit -m "docs: document Subagent count badges"
```

- [ ] **Step 5: Push and verify remote parity**

```powershell
git push origin main
git fetch origin main
git status --short --branch
git rev-parse HEAD
git rev-parse origin/main
```

Expected: push succeeds, worktree is clean, and local `HEAD` equals `origin/main`.
