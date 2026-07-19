# Favorites and Archived Watchlist Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add macOS-aligned task favorites, a persisted favorites-only filter, and read-only visibility for archived favorite tasks.

**Architecture:** Extend the existing preference and pure-list-policy model, then add a distinct parameterized repository path that may return archived favorite IDs without changing the recent-eight query. The WPF ViewModel merges this candidate stream, clears archived lifecycle/incident notification fields, and exposes star controls while preserving current pin/ignore behavior.

**Tech Stack:** .NET 9, C#, WPF/MVVM, Microsoft.Data.Sqlite read-only access, System.Text.Json, xUnit

---

### Task 1: Favorite preferences and pure filtering policy

**Files:**
- Modify: `src/ThreadBeacon.Core/Models/ThreadListPreferences.cs`
- Modify: `src/ThreadBeacon.Core/Services/ThreadListPolicy.cs`
- Modify: `src/ThreadBeacon.App/Settings/JsonThreadListPreferenceStore.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Services/ThreadListPolicyTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/Settings/JsonThreadListPreferenceStoreTests.cs`

- [x] Add failing Core tests proving favorites-only mode includes only favorite candidates, favorite status does not reorder the all-tasks list, missing favorite IDs remain persisted, and ignore rules still win over favorites.

```csharp
var preferences = new ThreadListPreferences(
    favoriteThreadIds: ["favorite"],
    showsFavoritesOnly: true);
ThreadListResult result = ThreadListPolicy.Evaluate(candidates, preferences, limit: 8);
Assert.Equal(["favorite"], result.VisibleSnapshots.Select(x => x.Id));
```

- [x] Add failing store tests proving favorite IDs and filter state round-trip, old pin/ignore-only JSON defaults favorites to empty/off, and serialized JSON contains no title or Codex path.
- [x] Run focused policy/store tests; expect missing constructor members and properties.
- [x] Add ordinal `FavoriteThreadIds`, mutable `ShowsFavoritesOnly`, clone support, DTO fields with empty/off defaults, and the policy filter before visible sorting.
- [x] Re-run focused tests, full Core tests, and full App tests with zero failures.
- [x] Commit `feat(favorites): add favorite list preferences`.

### Task 2: Archived-capable read-only favorite loading

**Files:**
- Modify: `src/ThreadBeacon.Core/Models/ThreadRecord.cs`
- Modify: `src/ThreadBeacon.Core/Models/ThreadSnapshot.cs`
- Modify: `src/ThreadBeacon.Core/Models/ThreadLoadRequest.cs`
- Modify: `src/ThreadBeacon.Core/Services/IThreadRepository.cs`
- Modify: `src/ThreadBeacon.Core/Services/SQLiteThreadRepository.cs`
- Modify: `src/ThreadBeacon.Core/Services/ThreadStatusLoader.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Services/SQLiteThreadRepositoryTests.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Services/ThreadStatusLoaderTests.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Notifications/CompletionNotificationTrackerTests.cs`

- [x] Add failing repository tests showing ordinary `LoadByIds` excludes archived rows while `LoadByIdsIncludingArchived` returns only requested primary rows with the correct archive flag and parameterized hostile IDs.
- [x] Add failing loader tests proving recent/included/favorite records merge without duplicates, archived favorites retain rename/Token data, use neutral idle state, clear task-start/completion/incident fields, and are excluded from incident-log queries.

```csharp
ThreadSnapshot archived = Assert.Single(loader.Load(new ThreadLoadRequest(
    RecentLimit: 8,
    IncludedThreadIds: new HashSet<string>(StringComparer.Ordinal),
    FavoriteThreadIds: new HashSet<string>(["archived"], StringComparer.Ordinal),
    ExpandedThreadIds: new HashSet<string>(StringComparer.Ordinal))).Threads);
Assert.True(archived.IsArchived);
Assert.Equal(ThreadStatus.Idle, archived.Status);
Assert.Null(archived.CompletionEventAt);
Assert.Null(archived.ServiceIncident);
```

- [x] Add a failing notification test proving archived snapshots never produce completion or warning candidates even if a fixture contains lifecycle fields.
- [x] Run focused tests; expect missing APIs/archive members.
- [x] Add `IsArchived = false` compatibility defaults, `FavoriteThreadIds` in the request, archived-capable parameterized SQL, archive-column reading in every main-thread query, and the three-source Loader merge. Query incidents only for active IDs and sanitize archived lifecycle output.
- [x] Re-run focused tests and the full solution tests; require zero failures.
- [x] Commit `feat(favorites): load archived favorite tasks`.

### Task 3: ViewModel commands and WPF star presentation

**Files:**
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowCollection.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/MainWindowViewModelTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowCollectionTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowViewModelTests.cs`

- [ ] Add failing ViewModel tests proving refresh forwards every favorite ID, toggle-favorite persists without reordering all mode, toggle-filter immediately changes rows and persists, a missing favorite is not pruned, and notifications receive sanitized visible snapshots.
- [ ] Add failing row tests for favorite/archive state, context-menu label, commands, neutral archived label, and collection reconciliation. Add XAML structure assertions for toolbar star state, menu order, favorite/archive glyphs, and favorites empty state.
- [ ] Run focused App tests; expect missing commands/properties/controls.
- [ ] Implement favorite/filter commands and request wiring. Keep favorites independent of pins and ignores; recompute rows and header/status counts immediately after each action.
- [ ] Add the toolbar star before window pin, context favorite action before pin, gold row star, neutral archive glyph/status, and filtered empty-state text. Tooltips and automation names switch between `仅显示收藏` and `显示全部任务`.
- [ ] Re-run focused tests, full solution tests, and Release build; require zero failures, warnings, or errors.
- [ ] Launch Release and verify favorite/unfavorite, all/favorites filter, persistence across restart, pin/ignore coexistence, two-second refresh stability, and archived presentation when local archived favorite data is available.
- [ ] Commit `feat(favorites): add archived task watchlist`.

### Task 4: Documentation, security audit, and delivery

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `PRIVACY.md`
- Modify: `ROADMAP.md`
- Modify: `docs/superpowers/plans/2026-07-19-favorites-archived-watchlist.md`

- [ ] Document favorite/filter semantics, archived neutral behavior, notification suppression, stored fields, and read-only scope in Chinese and English. Remove only this feature from deferred scope.
- [ ] Run `dotnet test ThreadBeacon.slnx --configuration Release`, `dotnet build ThreadBeacon.slnx --configuration Release --no-restore`, and `dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive`; require zero failures, warnings, errors, or known vulnerabilities.
- [ ] Run mandatory tracked-file audits for private keys/tokens, sensitive extensions, personal absolute paths, network APIs, writable SQLite modes, Core write APIs, unexpected binaries, and `git diff --check origin/main...HEAD`.
- [ ] Commit documentation, repeat the security audit after the final commit, push `main`, fetch the remote, verify `HEAD == origin/main`, and leave the verified Release App running.
