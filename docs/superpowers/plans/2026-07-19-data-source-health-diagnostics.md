# Data Source Health Diagnostics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an always-visible, privacy-safe footer diagnostic that reports the health of the task database, Rename index, rollout files, and service-log database from the same refresh that builds the task list.

**Architecture:** `ThreadBeacon.Core` maps existing source-specific result enums into one immutable health report and returns it with snapshots. `MainWindowViewModel` keeps the last successful timestamp and preserves existing rows when the core task query is unavailable; a focused WPF control displays the current report without being replaced during the two-second refresh cycle.

**Tech Stack:** .NET 9, C#, WPF, Microsoft.Data.Sqlite, xUnit

---

### Task 1: Health Model And Status-Bearing Service Logs

**Files:**
- Create: `src/ThreadBeacon.Core/Models/DataSourceHealth.cs`
- Create: `src/ThreadBeacon.Core/Models/ServiceLogLoadResult.cs`
- Modify: `src/ThreadBeacon.Core/Services/ILogEventRepository.cs`
- Modify: `src/ThreadBeacon.Core/Services/SQLiteLogEventRepository.cs`
- Create: `tests/ThreadBeacon.Core.Tests/Models/DataSourceHealthTests.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Services/SQLiteLogEventRepositoryTests.cs`

- [ ] **Step 1: Write failing health-model tests**

Add tests that construct reports directly and assert the complete public contract:

```csharp
[Fact]
public void OverallStatus_UsesUnavailableOnlyForTaskDatabase()
{
    DataSourceHealthReport report = Report(
        task: DataSourceHealthStatus.Healthy,
        rename: DataSourceHealthStatus.Unavailable("Rename 索引不可用"));

    Assert.Equal(OverallDataSourceHealth.Degraded, report.OverallStatus);
    Assert.Equal("部分数据源降级", report.Summary);

    report = Report(task: DataSourceHealthStatus.Unavailable("任务数据库不可用"));
    Assert.Equal(OverallDataSourceHealth.Unavailable, report.OverallStatus);
    Assert.Equal("任务数据不可用", report.Summary);
}

[Fact]
public void WithLastSuccessfulRefresh_PreservesOnlyStableDiagnosticFields()
{
    DateTimeOffset refreshedAt = DateTimeOffset.FromUnixTimeSeconds(100);
    DataSourceHealthReport report = Report(
        rolloutSuccessCount: 3,
        rolloutFailureCount: 2).WithLastSuccessfulRefresh(refreshedAt);

    Assert.Equal(refreshedAt, report.LastSuccessfulRefreshAt);
    Assert.Equal(3, report.RolloutSuccessCount);
    Assert.Equal(2, report.RolloutFailureCount);
    Assert.DoesNotContain(
        typeof(DataSourceHealthReport).GetProperties(),
        property => property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Error", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Thread", StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 2: Run the model tests and verify RED**

Run:

```powershell
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --filter DataSourceHealthTests
```

Expected: compilation fails because the health types do not exist.

- [ ] **Step 3: Implement the immutable health model**

Define these exact public types in `DataSourceHealth.cs`:

```csharp
public enum DataSourceHealthLevel { Healthy, Degraded, Unavailable, NotUsed }
public enum OverallDataSourceHealth { Healthy, Degraded, Unavailable }

public sealed record DataSourceHealthStatus(
    DataSourceHealthLevel Level,
    string DisplayText,
    string? DetailText)
{
    public static DataSourceHealthStatus Healthy { get; } =
        new(DataSourceHealthLevel.Healthy, "正常", null);
    public static DataSourceHealthStatus NotUsed { get; } =
        new(DataSourceHealthLevel.NotUsed, "未使用", null);
    public static DataSourceHealthStatus Degraded(string detail) =>
        new(DataSourceHealthLevel.Degraded, "部分降级", detail);
    public static DataSourceHealthStatus Unavailable(string detail) =>
        new(DataSourceHealthLevel.Unavailable, "不可用", detail);
}

public sealed record DataSourceHealthReport(
    DataSourceHealthStatus TaskDatabase,
    DataSourceHealthStatus RenameIndex,
    DataSourceHealthStatus Rollout,
    DataSourceHealthStatus ServiceLogs,
    int RolloutSuccessCount,
    int RolloutFailureCount,
    DateTimeOffset? LastSuccessfulRefreshAt)
{
    public OverallDataSourceHealth OverallStatus { get; }
    public string Summary { get; }
    public DataSourceHealthReport WithLastSuccessfulRefresh(DateTimeOffset value);
}
```

Normalize negative counts to zero in the record constructor. Derive `Unavailable`
only when `TaskDatabase.Level` is unavailable; otherwise any degraded or unavailable
optional source yields overall degraded. Treat `NotUsed` as neutral.

- [ ] **Step 4: Run the model tests and verify GREEN**

Run the focused test command from Step 2. Expected: all `DataSourceHealthTests` pass.

- [ ] **Step 5: Write failing service-log result tests**

Add repository tests asserting:

```csharp
[Fact]
public void LoadLatestIncidents_ReturnsNotUsedForEmptyRequest()
{
    ServiceLogLoadResult result = Repository("missing.db")
        .LoadLatestIncidents(new HashSet<string>());
    Assert.Equal(ServiceLogSourceStatus.NotUsed, result.Status);
    Assert.Empty(result.Incidents);
}

[Fact]
public void LoadLatestIncidents_ReturnsMissingWithoutCreatingDatabase()
{
    string path = Path.Combine(temporaryDirectory, "missing.db");
    ServiceLogLoadResult result = Repository(path)
        .LoadLatestIncidents(new HashSet<string> { "thread" });
    Assert.Equal(ServiceLogSourceStatus.Missing, result.Status);
    Assert.False(File.Exists(path));
}
```

Update existing healthy-query assertions to read `result.Incidents` and assert
`ServiceLogSourceStatus.Healthy`.

- [ ] **Step 6: Run service-log tests and verify RED**

Run:

```powershell
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --filter SQLiteLogEventRepositoryTests
```

Expected: compilation fails because `ServiceLogLoadResult` and
`ServiceLogSourceStatus` do not exist.

- [ ] **Step 7: Implement status-bearing service-log reads**

Create:

```csharp
public enum ServiceLogSourceStatus
{
    Healthy,
    Missing,
    Busy,
    Incompatible,
    Unavailable,
    NotUsed,
}

public sealed record ServiceLogLoadResult(
    ServiceLogSourceStatus Status,
    IReadOnlyDictionary<string, ServiceIncident> Incidents);
```

Change `ILogEventRepository.LoadLatestIncidents` to return this result. In
`SQLiteLogEventRepository`, return `NotUsed` for no requested IDs, `Missing` for an
absent file, `Healthy` after a successful query, `Busy` for SQLite busy/locked,
`Incompatible` for schema/data incompatibility, and `Unavailable` for I/O or other
SQLite failures. Never include exception text in the result.

- [ ] **Step 8: Verify Task 1 and commit**

Run:

```powershell
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --filter "DataSourceHealthTests|SQLiteLogEventRepositoryTests"
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj
git add src/ThreadBeacon.Core tests/ThreadBeacon.Core.Tests
git commit -m "feat(health): model local data source status"
```

Expected: focused and full Core suites pass before commit.

### Task 2: Build Health From The Existing Refresh

**Files:**
- Modify: `src/ThreadBeacon.Core/Models/ThreadSnapshotLoadResult.cs`
- Modify: `src/ThreadBeacon.Core/Services/ThreadStatusLoader.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Services/ThreadStatusLoaderTests.cs`

- [ ] **Step 1: Write failing loader-health tests**

Cover these behaviors with real stub results:

```csharp
[Fact]
public void Load_ReportsOptionalFailuresAndAccurateRolloutCounts()
{
    ThreadSnapshotLoadResult result = CreateLoader(
        titleStatus: SessionIndexStatus.Missing,
        rollouts: new Dictionary<string, RolloutLoadResult>
        {
            ["healthy"] = HealthyObservation(ThreadStatus.Running, Now, Now),
            ["missing"] = new(RolloutSourceStatus.Missing, RolloutObservation.Empty),
        },
        serviceLogStatus: ServiceLogSourceStatus.Missing).Load();

    Assert.Equal(OverallDataSourceHealth.Degraded, result.Health.OverallStatus);
    Assert.Equal(DataSourceHealthLevel.Unavailable, result.Health.RenameIndex.Level);
    Assert.Equal(DataSourceHealthLevel.Degraded, result.Health.Rollout.Level);
    Assert.Equal(1, result.Health.RolloutSuccessCount);
    Assert.Equal(1, result.Health.RolloutFailureCount);
    Assert.Equal(DataSourceHealthLevel.Unavailable, result.Health.ServiceLogs.Level);
    Assert.Equal(2, result.Threads.Count);
}

[Fact]
public void Load_ReportsTaskDatabaseUnavailableWithoutTreatingOptionalFailureAsCoreFailure()
{
    ThreadSnapshotLoadResult result = CreateLoader(
        recentStatus: ThreadRepositoryStatus.Missing).Load();
    Assert.Equal(OverallDataSourceHealth.Unavailable, result.Health.OverallStatus);

    result = CreateLoader(
        recentStatus: ThreadRepositoryStatus.Healthy,
        includedStatus: ThreadRepositoryStatus.Busy).Load(RequestWithIncludedId());
    Assert.Equal(OverallDataSourceHealth.Degraded, result.Health.OverallStatus);
}
```

Also assert no requested service-log IDs yields `NotUsed`, and expanded Subagent
rollout reads participate in the aggregate counts.

- [ ] **Step 2: Run loader tests and verify RED**

Run:

```powershell
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --filter ThreadStatusLoaderTests
```

Expected: compilation fails because `ThreadSnapshotLoadResult.Health` does not exist
and the log repository stubs still use the old return type.

- [ ] **Step 3: Add health to the snapshot result**

Extend the record constructor with `DataSourceHealthReport Health`. Keep
`ThreadSourceStatus`, `TitleSourceStatus`, and `IsHealthy` for existing callers, but
make `IsHealthy` delegate to `Health.OverallStatus is OverallDataSourceHealth.Healthy`.

- [ ] **Step 4: Accumulate source health in `ThreadStatusLoader`**

Use the existing result of each read. Do not call a repository or parser twice.
Track the core recent-task status separately from included/favorite/Subagent reads.
Map fixed details as follows:

```csharp
ThreadRepositoryStatus.Missing => "未找到 Codex 任务数据库"
ThreadRepositoryStatus.Busy => "Codex 任务数据库正忙"
ThreadRepositoryStatus.Incompatible => "Codex 任务数据库格式暂不兼容"
ThreadRepositoryStatus.Unavailable => "Codex 任务数据库暂不可用"
SessionIndexStatus.Missing => "未找到 Rename 索引"
SessionIndexStatus.Incompatible => "Rename 索引格式暂不兼容"
SessionIndexStatus.Unavailable => "Rename 索引暂不可用"
ServiceLogSourceStatus.Missing => "未找到服务日志数据库"
ServiceLogSourceStatus.Busy => "服务日志数据库正忙"
ServiceLogSourceStatus.Incompatible => "服务日志数据库格式暂不兼容"
ServiceLogSourceStatus.Unavailable => "服务日志数据库暂不可用"
```

For rollout, count every main and loaded direct-child parse result. Zero parses is
`NotUsed`; all healthy is `Healthy`; mixed success is `Degraded("部分 Rollout 无法读取")`;
all failed is `Unavailable("Rollout 数据不可用")`. Optional unavailable states still
produce only an overall degraded report.

- [ ] **Step 5: Verify Task 2 and commit**

Run:

```powershell
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --filter ThreadStatusLoaderTests
dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj
git add src/ThreadBeacon.Core tests/ThreadBeacon.Core.Tests
git commit -m "feat(health): report refresh source diagnostics"
```

Expected: focused and full Core suites pass.

### Task 3: Preserve Rows And Add The Stable WPF Popover

**Files:**
- Create: `src/ThreadBeacon.App/ViewModels/DataSourceHealthViewModel.cs`
- Create: `src/ThreadBeacon.App/Controls/DataSourceHealthControl.xaml`
- Create: `src/ThreadBeacon.App/Controls/DataSourceHealthControl.xaml.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Create: `tests/ThreadBeacon.App.Tests/ViewModels/DataSourceHealthViewModelTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/Controls/DataSourceHealthControlTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/Views/MainWindowXamlTests.cs`

- [ ] **Step 1: Write failing presentation-model tests**

Assert that one `DataSourceHealthViewModel` instance updates in place and exposes
stable rows, icon glyph/color keys, rollout count text, summary, accessibility text,
and formatted last-successful time. Use a fixed report and assert:

```csharp
Assert.Equal("数据源健康：部分数据源降级", viewModel.AccessibilityLabel);
Assert.Equal("成功 3 | 失败 2", viewModel.RolloutCountsText);
Assert.Equal("最后成功刷新：08:20:00", viewModel.LastSuccessfulRefreshText);
Assert.Equal(["任务数据库", "Rename 索引", "Rollout", "服务日志"],
    viewModel.Sources.Select(source => source.Title));
```

- [ ] **Step 2: Write failing refresh-retention tests**

In `MainWindowViewModelTests`, first load a healthy row, then make the repository
return `Missing` or throw. Assert the existing row instance and count remain,
`DataSourceHealth.OverallStatus` becomes unavailable, the previous successful time
is retained, and the completion observer is not invoked for the failed core refresh.
Also assert a later successful refresh updates the same `DataSourceHealthViewModel`
instance and advances the timestamp.

- [ ] **Step 3: Run view-model tests and verify RED**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "DataSourceHealthViewModelTests|MainWindowViewModelTests"
```

Expected: compilation fails because the presentation model and health property do
not exist.

- [ ] **Step 4: Implement the presentation model and refresh behavior**

`DataSourceHealthViewModel` implements `INotifyPropertyChanged`, owns four
`DataSourceHealthRowViewModel` instances, and provides `Update(report)` so bindings
change without replacing the object. It exposes only display-safe fields.

`MainWindowViewModel` creates one health view model. On a successful core task read,
attach `result.RefreshedAt` through `WithLastSuccessfulRefresh`, reconcile rows, and
notify completion as today. When the health is unavailable, update only health and
footer error state; retain candidate snapshots, rows, ignored rows, expanded state,
and the prior successful time. In the unexpected exception path, publish a fixed
unavailable report without exception text and keep the list.

- [ ] **Step 5: Run view-model tests and verify GREEN**

Run the focused command from Step 3. Expected: all focused tests pass.

- [ ] **Step 6: Write failing WPF control and footer tests**

Construct the control on an STA thread, apply its template, and assert the named
button and popup exist, `Popup.StaysOpen` is false, and changing `Details.Report`
does not replace or close the popup. Extend the XML fixture test to assert the footer
contains bindings in this order: `StatusText`, `DataSourceHealth`, `UpdatedText`.

- [ ] **Step 7: Run UI tests and verify RED**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj --filter "DataSourceHealthControlTests|MainWindowXamlTests"
```

Expected: compilation or assertions fail because the control and footer binding do
not exist.

- [ ] **Step 8: Implement the WPF control and footer integration**

Build `DataSourceHealthControl` as a transparent 22-pixel button with a fixed-size
Segoe Fluent icon and a click-opened `Popup` using `StaysOpen="False"`. The 320-pixel
popover contains the title, overall summary, last-successful time, divider, and four
source rows; the rollout row includes aggregate counts. Bind icon, text, tooltip,
automation name, and colors from the presentation model.

Change the footer to three columns (`*`, `Auto`, `Auto`), place the health control
before the update time, and preserve the existing 11-pixel compact typography.

- [ ] **Step 9: Verify Task 3 and commit**

Run:

```powershell
dotnet test tests/ThreadBeacon.App.Tests/ThreadBeacon.App.Tests.csproj
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
git add src/ThreadBeacon.App tests/ThreadBeacon.App.Tests
git commit -m "feat(health): show data source diagnostics"
```

Expected: all tests pass and Release build reports zero warnings and zero errors.

### Task 4: Runtime Acceptance, Documentation, Security Audit, And Push

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`
- Modify: `PRIVACY.md`

- [ ] **Step 1: Document the delivered behavior**

Document the always-visible footer entry, the four diagnostic sources, overall
states, rollout counts, last successful time, optional-source degradation, and the
fact that diagnostics contain no raw paths, task identity, content, or history.

- [ ] **Step 2: Run automated release verification**

Run:

```powershell
dotnet test ThreadBeacon.slnx
dotnet build ThreadBeacon.slnx -c Release
dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive
git diff --check
```

Expected: all tests pass, build has zero warnings/errors, no vulnerable packages are
reported, and the diff check is clean.

- [ ] **Step 3: Launch and inspect the Release executable**

Start `src/ThreadBeacon.App/bin/Release/net9.0-windows/ThreadBeacon.App.exe` and verify:

- healthy icon is visible at the footer right;
- clicking shows four source rows and a successful refresh time;
- leaving the popover open through multiple two-second refreshes does not close it;
- clicking elsewhere closes it;
- footer remains aligned at 620px and the 480px minimum width;
- normal task, Token, Subagent, favorites, pin/ignore, pause, and sound behavior remains intact.

- [ ] **Step 4: Perform the mandatory pre-push security review**

Inspect the complete range from `origin/main` through `HEAD` plus working-tree files.
Check tracked paths and added text for private keys, API tokens, credentials, email
secrets, absolute user paths, Codex task content, SQLite/JSONL/log data, build output,
temporary files, and generated diagnostic images. Confirm all SQLite access remains
read-only, no new network calls exist, and public health state contains only fixed
categories, counts, and timestamps.

- [ ] **Step 5: Commit documentation**

```powershell
git add README.md README-EN.md ROADMAP.md PRIVACY.md docs/superpowers/plans/2026-07-19-data-source-health-diagnostics.md
git commit -m "docs(health): document source diagnostics"
```

- [ ] **Step 6: Push and verify remote parity**

```powershell
git push origin main
git fetch origin main
git status --short --branch
git rev-parse HEAD
git rev-parse origin/main
```

Expected: push succeeds, the working tree is clean, and local `HEAD` exactly matches
`origin/main`.
