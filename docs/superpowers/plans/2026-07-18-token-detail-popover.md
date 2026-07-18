# Token Detail Popover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the macOS-compatible Token detail popover to the Windows task list and keep it stable through two-second data refreshes.

**Architecture:** Pure formatter and detail view-model types convert existing `TokenUsageSnapshot` data into display text. A focused row reconciler preserves `ThreadRowViewModel` identity across refreshes, while a WPF `TokenInfoControl` owns hover, click-to-pin, popup, and accessibility behavior without reading Codex data.

**Tech Stack:** .NET 9, WPF, System.Windows.Controls.Primitives.Popup, xUnit

---

## File Structure

- Create `src/ThreadBeacon.App/Formatting/TokenUsageFormatter.cs`: compact counts, percentages, turn deltas, and times.
- Create `src/ThreadBeacon.App/ViewModels/TokenDetailViewModel.cs`: ordered Token detail rows.
- Create `src/ThreadBeacon.App/ViewModels/ThreadRowCollection.cs`: ID-based row reconciliation.
- Create `src/ThreadBeacon.App/Controls/TokenInfoControl.xaml`: info trigger and 270px popup.
- Create `src/ThreadBeacon.App/Controls/TokenInfoControl.xaml.cs`: hover timers, click pinning, and popup close behavior.
- Modify `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`: observable in-place row updates and Token detail exposure.
- Modify `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`: reconcile rows instead of clearing them.
- Modify `src/ThreadBeacon.App/MainWindow.xaml`: place the info control after cumulative Token.
- Create formatter, detail-model, row reconciliation, and popup-state tests under `tests/ThreadBeacon.App.Tests`.
- Create `docs/validation/2026-07-18-windows-30-minute-soak.md`: privacy-safe soak summary.
- Modify `README.md`, `README-EN.md`, and `ROADMAP.md`: document Token details and validation status.

### Task 1: Token Formatting and Detail Model

**Files:**
- Create: `tests/ThreadBeacon.App.Tests/Formatting/TokenUsageFormatterTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/TokenDetailViewModelTests.cs`
- Create: `src/ThreadBeacon.App/Formatting/TokenUsageFormatter.cs`
- Create: `src/ThreadBeacon.App/ViewModels/TokenDetailViewModel.cs`

- [ ] **Step 1: Write failing formatter tests**

Cover compact boundaries and missing values:

```csharp
[Theory]
[InlineData(999, "999")]
[InlineData(1_000, "1K")]
[InlineData(1_250, "1.3K")]
[InlineData(1_000_000, "1M")]
public void FormatCount_UsesCompactMacCompatibleText(long value, string expected) =>
    Assert.Equal(expected, TokenUsageFormatter.FormatCount(value));

[Fact]
public void FormatCurrentTurn_PrefixesKnownValue() =>
    Assert.Equal("+1.5K", TokenUsageFormatter.FormatCurrentTurn(1_500));

[Fact]
public void FormatPercent_UsesWholePercentage() =>
    Assert.Equal("40%", TokenUsageFormatter.FormatPercent(0.4));
```

- [ ] **Step 2: Write failing detail-model tests**

Construct a real `TokenUsageSnapshot` and assert the exact label order:

```csharp
string[] expectedLabels =
[
    "会话总量", "输入", "缓存输入", "非缓存输入", "输出",
    "Reasoning", "当前 turn", "缓存率", "更新时间",
];
Assert.Equal(expectedLabels, details.Rows.Select(row => row.Label));
Assert.Equal("+500", details.Rows[6].Value);
```

Add a total-only snapshot test that expects dashes for breakdown, turn, cache
rate, and update time.

- [ ] **Step 3: Run the focused tests and verify RED**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "TokenUsageFormatterTests|TokenDetailViewModelTests"
```

Expected: compile failure because the formatter and detail types do not exist.

- [ ] **Step 4: Implement the formatter and ordered detail model**

Expose these formatter methods:

```csharp
public static string FormatCount(long? value);
public static string FormatCurrentTurn(long? value);
public static string FormatPercent(double? ratio);
public static string FormatTime(DateTimeOffset? value);
```

Use invariant compact counts, em dash for unavailable or negative values,
whole-number percentages, and local `HH:mm:ss` time. Define:

```csharp
public sealed record TokenDetailRow(string Label, string Value);

public sealed class TokenDetailViewModel
{
    public TokenDetailViewModel(TokenUsageSnapshot snapshot);
    public IReadOnlyList<TokenDetailRow> Rows { get; }
    public string Note => "缓存输入已包含在输入中；Reasoning 已包含在输出中。";
}
```

- [ ] **Step 5: Run focused and complete app tests**

Run the focused command, then:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
```

Expected: all tests pass with zero warnings.

- [ ] **Step 6: Commit**

```powershell
git add src/ThreadBeacon.App/Formatting src/ThreadBeacon.App/ViewModels/TokenDetailViewModel.cs tests/ThreadBeacon.App.Tests/Formatting tests/ThreadBeacon.App.Tests/ViewModels/TokenDetailViewModelTests.cs
git commit -m "feat: format Token usage details"
```

### Task 2: Stable Task Row Reconciliation

**Files:**
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowCollectionTests.cs`
- Create: `src/ThreadBeacon.App/ViewModels/ThreadRowCollection.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Write failing reconciliation tests**

Use real `ThreadSnapshot` values and assert:

```csharp
collection.Reconcile([firstSnapshot], now);
ThreadRowViewModel original = collection.Items.Single();

collection.Reconcile([updatedFirstSnapshot], now.AddSeconds(2));

Assert.Same(original, collection.Items.Single());
Assert.Equal("2K", original.TokenText);
```

Add tests proving new rows are inserted in loader order, removed rows disappear,
and the same IDs move without object replacement.

- [ ] **Step 2: Run the focused tests and verify RED**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter ThreadRowCollectionTests
```

Expected: compile failure because `ThreadRowCollection` does not exist.

- [ ] **Step 3: Make task rows observable and updateable**

Change `ThreadRowViewModel` to `INotifyPropertyChanged`. Preserve immutable `Id`,
and add:

```csharp
public TokenDetailViewModel? TokenDetails { get; private set; }
public bool HasTokenDetails => TokenDetails is not null;
public void Update(ThreadSnapshot snapshot, DateTimeOffset now);
```

`Update` refreshes title, status, brush, total, duration, and Token details, then
raises notifications only for changed display properties.

- [ ] **Step 4: Implement ID-based reconciliation**

`ThreadRowCollection` owns an `ObservableCollection<ThreadRowViewModel> Items`.
For each latest snapshot, reuse a row with the same ID, update it, and move it to
the latest index. Create missing rows and remove IDs absent from the new result.

Replace `MainWindowViewModel.ReplaceThreads` clear-and-add behavior with:

```csharp
threadRows.Reconcile(result.Threads, result.RefreshedAt);
```

Keep the public `Threads` collection and visibility notifications unchanged.

- [ ] **Step 5: Run app and full solution tests**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
dotnet test ThreadBeacon.slnx
```

Expected: identity, order, removal, and update tests pass; all existing tests
remain green.

- [ ] **Step 6: Commit**

```powershell
git add src/ThreadBeacon.App/ViewModels tests/ThreadBeacon.App.Tests/ViewModels
git commit -m "refactor: preserve task rows across refreshes"
```

### Task 3: Token Info Popup Control

**Files:**
- Create: `tests/ThreadBeacon.App.Tests/Controls/TokenPopoverStateTests.cs`
- Create: `src/ThreadBeacon.App/Controls/TokenPopoverState.cs`
- Create: `src/ThreadBeacon.App/Controls/TokenInfoControl.xaml`
- Create: `src/ThreadBeacon.App/Controls/TokenInfoControl.xaml.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`

- [ ] **Step 1: Write failing popup-state tests**

Test a pure state object:

```csharp
state.OpenForHover();
Assert.True(state.IsOpen);
Assert.False(state.IsPinned);

state.TogglePinned();
Assert.True(state.IsOpen);
Assert.True(state.IsPinned);

state.RequestHoverDismiss();
Assert.True(state.IsOpen);

state.Close();
Assert.False(state.IsOpen);
Assert.False(state.IsPinned);
```

- [ ] **Step 2: Run the focused test and verify RED**

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter TokenPopoverStateTests
```

Expected: compile failure because `TokenPopoverState` does not exist.

- [ ] **Step 3: Implement popup state**

Expose `IsOpen`, `IsPinned`, `OpenForHover`, `TogglePinned`,
`RequestHoverDismiss`, and `Close`. A dismiss request closes only when the
surface is not click-pinned.

- [ ] **Step 4: Build the WPF control**

Create a 16x16 borderless info button using `Segoe Fluent Icons`. Bind a
`TokenDetailViewModel` dependency property. The popup must:

- be 270px wide;
- open to the left of the right-edge trigger;
- render the nine bound rows in order with monospaced right-aligned values;
- render the explanatory note below a divider;
- use `StaysOpen="False"` for outside-click dismissal;
- use 300 ms open and 150 ms dismiss `DispatcherTimer` values;
- expose tooltip and automation name `查看 Token 详情`.

Wire mouse enter/leave on both trigger and popup content to the timers. A click
toggles pinned state. Closing the popup resets both timers and state.

- [ ] **Step 5: Integrate after cumulative Token**

Add the controls namespace to `MainWindow.xaml`. Replace the Token cell with a
right-aligned horizontal group containing the existing total followed by:

```xml
<controls:TokenInfoControl Margin="4,0,0,0"
                           Details="{Binding TokenDetails}"
                           Visibility="{Binding HasTokenDetails, Converter={StaticResource BooleanToVisibilityConverter}}" />
```

Add one shared `BooleanToVisibilityConverter` resource and widen the Token column
only enough for the 16px control. Apply the same track width to the header and
row grids so columns remain aligned at the 480px minimum window width.

- [ ] **Step 6: Build and run all tests**

```powershell
dotnet build ThreadBeacon.slnx
dotnet test ThreadBeacon.slnx --no-build
```

Expected: zero warnings, zero errors, and all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src/ThreadBeacon.App/Controls src/ThreadBeacon.App/MainWindow.xaml tests/ThreadBeacon.App.Tests/Controls
git commit -m "feat: add Token detail popover"
```

### Task 4: Soak Record, Documentation, and Runtime Verification

**Files:**
- Create: `docs/validation/2026-07-18-windows-30-minute-soak.md`
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`

- [ ] **Step 1: Record the privacy-safe soak result**

Record duration, sample interval, probe failures, source downgrades, task-count
range, concurrent-running sample count, app crashes, and maximum probe duration.
Do not record task IDs, titles, paths, or content. Mark concurrent acceptance
complete only if at least two tasks were running during the test.

- [ ] **Step 2: Update documentation**

Document the info control and nine detail fields in both READMEs. Mark the
30-minute validation and compact Token detail as complete in `ROADMAP.md` only
when supported by the recorded results.

- [ ] **Step 3: Run final automated verification**

```powershell
dotnet build ThreadBeacon.slnx
dotnet test ThreadBeacon.slnx --no-build
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
git diff --check
```

Expected: zero warnings/errors, all tests pass, no vulnerable packages, and no
whitespace errors.

- [ ] **Step 4: Run visual and interaction verification**

Launch the app with real tasks and verify the design checklist at normal width
and the 480px minimum. Capture screenshots with the popup closed and open. Keep
one popup click-pinned for at least three automatic refresh cycles and confirm
its values update without closing. Verify hover timing, outside click, keyboard
focus, tooltip, and no text overlap.

- [ ] **Step 5: Commit documentation**

```powershell
git add docs/validation README.md README-EN.md ROADMAP.md
git commit -m "docs: record Token detail and soak validation"
```

- [ ] **Step 6: Run the mandatory pre-push security and privacy audit**

Inspect every commit and file added since `origin/main`:

```powershell
git status --short
git diff --name-status origin/main...HEAD
git diff --check origin/main...HEAD
git ls-files | rg '(^|/)(bin|obj)/|\.user$|\.suo$|settings\.json$'
rg -n --hidden -g '!**/bin/**' -g '!**/obj/**' -e 'sk-[A-Za-z0-9_-]{20,}|github_pat_[A-Za-z0-9_]+|ghp_[A-Za-z0-9]+|BEGIN (RSA |OPENSSH |EC )?PRIVATE KEY|password\s*[:=]' .
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
```

Review the complete diff directly. Confirm that no API keys, SSH material,
credentials, local settings, absolute user paths, Codex task IDs, task titles,
rollout paths, or conversation content are present. The soak document must
contain aggregate counts and timings only. Treat any unexpected match as a
release blocker and remove it before continuing.

- [ ] **Step 7: Push the completed stage**

Only after the complete verification and security audit succeed, push `main` to
`origin` and confirm that local `HEAD` matches `origin/main`.
