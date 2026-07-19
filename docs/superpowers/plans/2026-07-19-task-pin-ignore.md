# Task Pin and Ignore Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add macOS-aligned task pin, temporary ignore, automatic next-turn restore, and ignored-task recovery controls to the Windows App.

**Architecture:** Core owns serializable preference records, a pure candidate-list policy, and the read-only candidate-loading contract. The WPF App owns resilient JSON persistence, commands, popup state, and row presentation; `MainWindowViewModel` applies the Core policy before notification observation so hidden tasks cannot sound.

**Tech Stack:** .NET 9, C#, WPF/MVVM, Microsoft.Data.Sqlite read-only access, System.Text.Json, xUnit

---

### Task 1: Core preferences and list policy

**Files:**
- Create: `src/ThreadBeacon.Core/Models/ThreadListPreferences.cs`
- Create: `src/ThreadBeacon.Core/Models/ThreadListResult.cs`
- Create: `src/ThreadBeacon.Core/Services/ThreadListPolicy.cs`
- Create: `tests/ThreadBeacon.Core.Tests/Services/ThreadListPolicyTests.cs`

- [x] Add failing tests proving automatic restore uses strict `LatestTaskStartedAt > IgnoredAt`, ignored candidates are partitioned, visible rows are limited to eight, status outranks pinning, pinning outranks recency within one status, and task ID is the final deterministic tie-breaker.

```csharp
ThreadListResult result = ThreadListPolicy.Evaluate(
    candidates,
    new ThreadListPreferences(["pinned"], ignoredRules),
    limit: 8);
Assert.Equal(["running", "pinned-idle"], result.VisibleSnapshots.Select(x => x.Id));
Assert.DoesNotContain("resumed", result.Preferences.IgnoredRules.Keys);
```

- [x] Run `dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --configuration Release --filter FullyQualifiedName~ThreadListPolicyTests`; expect compilation failures because the policy types do not exist.
- [x] Implement immutable `IgnoredThreadRule`, `ThreadIgnoreMode.UntilNextTurn`, cloneable `ThreadListPreferences`, `ThreadListResult`, and pure `ThreadListPolicy.Evaluate`. Use `StringComparer.Ordinal` collections and do not reference WPF.
- [x] Re-run the focused tests; expect all policy tests to pass, then run the full Core test project with zero failures.
- [x] Commit `feat(tasks): add task list preference policy`.

### Task 2: Read-only candidate loading and preference persistence

**Files:**
- Create: `src/ThreadBeacon.Core/Models/ThreadLoadRequest.cs`
- Modify: `src/ThreadBeacon.Core/Services/IThreadRepository.cs`
- Modify: `src/ThreadBeacon.Core/Services/SQLiteThreadRepository.cs`
- Modify: `src/ThreadBeacon.Core/Services/ThreadStatusLoader.cs`
- Create: `src/ThreadBeacon.App/Settings/IThreadListPreferenceStore.cs`
- Create: `src/ThreadBeacon.App/Settings/JsonThreadListPreferenceStore.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Services/SQLiteThreadRepositoryTests.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Services/ThreadStatusLoaderTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/Settings/JsonThreadListPreferenceStoreTests.cs`

- [ ] Add failing tests for `LoadByIds`: de-duplicate IDs, exclude archived/Subagent rows, bind hostile text as data, return missing IDs harmlessly, and retain `Mode=ReadOnly` plus `PRAGMA query_only = ON`.
- [ ] Add failing loader tests showing recent and explicitly included records merge by ordinal ID without duplicates, and included-task failure degrades without discarding healthy recent candidates.

```csharp
ThreadSnapshotLoadResult result = loader.Load(new ThreadLoadRequest(
    RecentLimit: 9,
    IncludedThreadIds: new HashSet<string>(["pinned", "ignored"], StringComparer.Ordinal),
    ExpandedThreadIds: []));
Assert.Equal(["recent", "pinned", "ignored"], result.Threads.Select(x => x.Id));
```

- [ ] Add failing App tests showing missing/malformed JSON loads empty preferences, valid data round-trips IDs/timestamps/mode, and saved JSON contains no task titles.
- [ ] Run the three focused test classes; expect missing API/type failures.
- [ ] Implement parameterized `LoadByIds`, merge candidates in `ThreadStatusLoader`, and add the resilient versioned JSON preference store under the existing local application-data directory. All database connections must use the existing read-only opener.
- [ ] Re-run focused tests, full Core tests, and full App tests; require zero failures.
- [ ] Commit `feat(tasks): load and persist task preferences`.

### Task 3: ViewModel behavior and WPF controls

**Files:**
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowCollection.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`
- Create: `src/ThreadBeacon.App/ViewModels/IgnoredThreadRowViewModel.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowCollectionTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowViewModelTests.cs`

- [ ] Add failing ViewModel tests proving refresh requests include pinned/ignored IDs and `8 + ignored count`, a pin immediately reorders only within status, ignore immediately removes the row and its pin/expansion, notifications receive visible rows only, new-turn refresh clears and persists the rule, and one/all restore commands repopulate rows.
- [ ] Add failing row tests for `IsPinned`, pin/unpin label, and commands. Add a XAML structure test or direct control assertions for the conditional eye-slash button, row context menu, pin glyph, restore-one, and restore-all controls.
- [ ] Run focused App tests; expect missing members and controls.
- [ ] Implement ViewModel candidate state and commands. Persist only when preferences change, prune stale pins after a healthy successful candidate load, and preserve refresh baseline semantics for command-triggered refreshes.
- [ ] Add the row context menu (`置顶任务`/`取消置顶`, `忽略此任务`), subtle Fluent pin glyph, conditional ignored-task toolbar button, and a focus-dismissable popup. Show in-memory titles with an eight-character ID fallback and close after the last restore.
- [ ] Re-run focused App tests and the full solution tests; require zero failures.
- [ ] Build Release and launch the App. Verify right-click pin/unpin, status-first ordering, immediate ignore, one/all restore, popup focus dismissal, next-turn auto-restore, two-second refresh stability, and no sound from ignored rows.
- [ ] Commit `feat(tasks): add pin and ignore controls`.

### Task 4: Documentation, security audit, and delivery

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `PRIVACY.md`
- Modify: `ROADMAP.md`
- Modify: `docs/superpowers/plans/2026-07-19-task-pin-ignore.md`

- [ ] Document task-level pinning, temporary ignore, automatic next-turn restore, recovery controls, stored fields, and the distinction from window always-on-top. Remove the feature from deferred lists without introducing later macOS scope.
- [ ] Run `dotnet test ThreadBeacon.slnx --configuration Release`, `dotnet build ThreadBeacon.slnx --configuration Release --no-restore`, and `dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive`; require zero failures, warnings, errors, or known vulnerabilities.
- [ ] Run mandatory tracked-file audits for private keys/tokens, sensitive extensions, personal absolute paths, network APIs, writable SQLite modes, Core write APIs, unexpected binaries, and `git diff --check origin/main...HEAD`.
- [ ] Commit documentation, repeat the security audit after the final commit, push `main`, fetch the remote, verify `HEAD == origin/main`, and leave the verified Release App running.
