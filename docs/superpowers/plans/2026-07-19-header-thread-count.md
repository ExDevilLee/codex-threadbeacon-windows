# Header Thread Count Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Display the number of running tasks over the number of visible primary tasks beside the header subtitle.

**Architecture:** A pure application formatter converts derived task statuses into immutable display and accessibility text. `MainWindowViewModel` updates the label only after successful snapshot reconciliation, while XAML renders the count beside the existing subtitle.

**Tech Stack:** .NET 9, C#, WPF, xUnit

---

## File Map

- Create `src/ThreadBeacon.App/Formatting/ThreadCountFormatter.cs`: pure count wording.
- Create `tests/ThreadBeacon.App.Tests/Formatting/ThreadCountFormatterTests.cs`: exact count semantics.
- Modify `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`: latest successful count label.
- Modify `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`: refresh and failure behavior.
- Modify `src/ThreadBeacon.App/MainWindow.xaml`: subtitle count presentation.
- Modify `README.md`, `README-EN.md`, and `ROADMAP.md`: delivered count behavior.

### Task 1: Pure Thread Count Formatter

**Files:**
- Create: `tests/ThreadBeacon.App.Tests/Formatting/ThreadCountFormatterTests.cs`
- Create: `src/ThreadBeacon.App/Formatting/ThreadCountFormatter.cs`

- [ ] **Step 1: Write failing formatter tests**

Assert the exact macOS-aligned contract:

```csharp
ThreadCountLabel mixed = ThreadCountFormatter.Format(
    [
        ThreadStatus.Running,
        ThreadStatus.Running,
        ThreadStatus.Running,
        ThreadStatus.Idle,
        ThreadStatus.JustCompleted,
        ThreadStatus.Unknown,
        ThreadStatus.NeedsAction,
        ThreadStatus.Error,
    ]);

Assert.Equal("3/8", mixed.DisplayText);
Assert.Equal("3 个任务正在运行，共显示 8 个任务", mixed.AccessibilityLabel);
```

Add an empty-input case expecting `0/0` and its exact Chinese explanation. Add a null
case expecting `ArgumentNullException`.

- [ ] **Step 2: Run focused tests and confirm failure**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter ThreadCountFormatterTests
```

Expected: compilation fails because `ThreadCountFormatter` and `ThreadCountLabel` do
not exist.

- [ ] **Step 3: Implement the formatter**

Create:

```csharp
public sealed record ThreadCountLabel(
    string DisplayText,
    string AccessibilityLabel);

public static class ThreadCountFormatter
{
    public static ThreadCountLabel Format(IEnumerable<ThreadStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        int runningCount = 0;
        int visibleCount = 0;
        foreach (ThreadStatus status in statuses)
        {
            visibleCount++;
            if (status is ThreadStatus.Running)
            {
                runningCount++;
            }
        }

        return new ThreadCountLabel(
            $"{runningCount}/{visibleCount}",
            $"{runningCount} 个任务正在运行，共显示 {visibleCount} 个任务");
    }
}
```

- [ ] **Step 4: Run focused and application tests**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter ThreadCountFormatterTests
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
```

Expected: formatter tests and the complete application test suite pass.

- [ ] **Step 5: Commit**

```powershell
git add src/ThreadBeacon.App/Formatting/ThreadCountFormatter.cs tests/ThreadBeacon.App.Tests/Formatting/ThreadCountFormatterTests.cs
git commit -m "feat(ui): format header thread count"
```

### Task 2: Successful Refresh State and Header Layout

**Files:**
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`

- [ ] **Step 1: Write failing view-model refresh tests**

Use a loader that returns two `Running` snapshots and one `Idle` snapshot. Assert:

```csharp
Assert.Equal("0/0", viewModel.ThreadCountText);
await viewModel.RefreshAsync();
Assert.Equal("2/3", viewModel.ThreadCountText);
Assert.Equal(
    "2 个任务正在运行，共显示 3 个任务",
    viewModel.ThreadCountAccessibilityLabel);
```

Use a mutable repository that succeeds once, then throws. Assert the second refresh
keeps the first successful label rather than clearing it.

- [ ] **Step 2: Run focused tests and confirm failure**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter MainWindowViewModelTests
```

Expected: compilation fails because the count properties do not exist.

- [ ] **Step 3: Update the label only after successful reconciliation**

Initialize:

```csharp
private ThreadCountLabel threadCountLabel = ThreadCountFormatter.Format([]);
```

Expose:

```csharp
public string ThreadCountText => threadCountLabel.DisplayText;
public string ThreadCountAccessibilityLabel => threadCountLabel.AccessibilityLabel;
```

After `threadRows.Reconcile`, format `result.Threads.Select(thread => thread.Status)`.
When the record changes, assign it and raise both property names. Do not change the
label in the existing catch block.

- [ ] **Step 4: Add the subtitle count**

Replace the subtitle `TextBlock` with:

```xml
<StackPanel Margin="0,2,0,0" Orientation="Horizontal">
  <TextBlock FontSize="12"
             Foreground="{StaticResource SecondaryTextBrush}"
             Text="Codex 任务状态" />
  <TextBlock Margin="6,0,0,0"
             FontFamily="Cascadia Mono, Consolas"
             FontSize="11"
             Foreground="{StaticResource SecondaryTextBrush}"
             Text="{Binding ThreadCountText}"
             ToolTip="{Binding ThreadCountAccessibilityLabel}"
             AutomationProperties.Name="{Binding ThreadCountAccessibilityLabel}" />
</StackPanel>
```

- [ ] **Step 5: Run all tests and Release build**

Close the running Release instance, then run:

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
```

Expected: Core and application tests pass; build reports zero warnings and zero
errors.

- [ ] **Step 6: Inspect runtime behavior**

Launch the Release executable. Verify the count matches visible running rows, remains
stable while paused, updates after resume/manual refresh, exposes the explanation in
the accessibility tree, and does not overlap controls at 620px or 480px.

- [ ] **Step 7: Commit**

```powershell
git add src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs src/ThreadBeacon.App/MainWindow.xaml tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs
git commit -m "feat(ui): show running task count in header"
```

### Task 3: Documentation, Security Audit, and Push

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`

- [ ] **Step 1: Document exact count semantics**

State that the header displays `running/visible`, only derived `Running` contributes
to the numerator, and the count covers the same primary task snapshots shown in the
list.

- [ ] **Step 2: Run final verification**

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
```

Expected: all tests pass, Release build has zero errors, and no vulnerable packages
are reported.

- [ ] **Step 3: Perform mandatory pre-push security review**

Inspect all changes against `origin/main`, tracked paths, and added text for private
keys, credentials, tokens, absolute user paths, local settings, Codex content, build
output, and temporary files. Confirm no persistence, data-source, network, or write
path changed.

- [ ] **Step 4: Commit documentation**

```powershell
git add README.md README-EN.md ROADMAP.md
git commit -m "docs: document header thread count"
```

- [ ] **Step 5: Push and verify remote parity**

```powershell
git push origin main
git fetch origin main
git status --short --branch
git rev-parse HEAD
git rev-parse origin/main
```

Expected: push succeeds, the worktree is clean, and local `HEAD` equals
`origin/main`.
