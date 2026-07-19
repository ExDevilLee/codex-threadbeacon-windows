# HTTP 429/503 Service Incident Monitoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Detect current visible tasks' HTTP 429/503 retry episodes from Codex's read-only log database, surface them in the task list, and play one configurable warning sound per episode.

**Architecture:** Add a privacy-scoped log repository and pure parser to `ThreadBeacon.Core`, then merge sanitized incidents into `ThreadStatusLoader`. Reuse the existing snapshot refresh, row reconciliation, settings store, sound player, and notification history so the feature remains independent of WPF inside Core and does not disrupt refresh when logs are unavailable.

**Tech Stack:** .NET 9, C# 13, Microsoft.Data.Sqlite, WPF, xUnit

---

### Task 1: Sanitized incident model and pure parser

**Files:**
- Create: `src/ThreadBeacon.Core/Models/ServiceIncident.cs`
- Create: `src/ThreadBeacon.Core/Models/LogEventRecord.cs`
- Create: `src/ThreadBeacon.Core/Services/LogEventParser.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/LogEventParserTests.cs`

- [ ] Write parser tests for active 429 retry, exhausted 503 failure, same-turn HTTP 200 recovery, excluded transport target, missing turn ID, malformed retry progress, and latest episode per task.
- [ ] Run `dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --filter FullyQualifiedName~LogEventParserTests` and verify failure because the types do not exist.
- [ ] Add `ServiceIncidentPhase`, `ServiceIncident`, and transient `LogEventRecord`; retain only episode ID, phase, code, retry progress, and occurrence time in the incident.
- [ ] Implement `LogEventParser.LatestIncidents` with exact target allow-list, compiled regular expressions for `turn.id`/`turn_id`, `status[= ]`, and `(attempt/limit in ...)`, and deterministic chronological processing.
- [ ] Run the focused tests and the complete Core suite; expect all tests to pass with no warnings.
- [ ] Commit with `feat(status): parse service incidents`.

### Task 2: Read-only SQLite repository

**Files:**
- Create: `src/ThreadBeacon.Core/Services/ILogEventRepository.cs`
- Create: `src/ThreadBeacon.Core/Services/SQLiteLogEventRepository.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/SQLiteLogEventRepositoryTests.cs`

- [ ] Write temporary-database tests proving only requested task IDs and the three allowed target/body combinations are returned; add tests for empty ID sets and a missing database.
- [ ] Run the focused tests and verify failure because the repository is missing.
- [ ] Define `ILogEventRepository.LoadLatestIncidents(IReadOnlySet<string>)` and implement a `Mode=ReadOnly` SQLite connection, parameterized ID placeholders, strict SQL predicates, ordered rows, Unix seconds/nanoseconds conversion, and parser handoff.
- [ ] Represent repository failure as an empty incident result at the loader boundary; do not expose raw bodies outside this repository/parser call.
- [ ] Run repository tests and all Core tests; expect all to pass.
- [ ] Commit with `feat(status): read Codex incidents safely`.

### Task 3: Merge incidents into task snapshots

**Files:**
- Modify: `src/ThreadBeacon.Core/Models/ThreadSnapshot.cs`
- Modify: `src/ThreadBeacon.Core/Services/ThreadStatusLoader.cs`
- Modify: `src/ThreadBeacon.App/App.xaml.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/ThreadStatusLoaderTests.cs`

- [ ] Add failing loader tests for visible-ID-only queries, retry-to-Warning override, failure-to-Error override, newer `task_started` clear, newer completion clear for retry, active incident completion suppression, and repository exception degradation.
- [ ] Run the loader tests and verify the expected missing API/behavior failures.
- [ ] Add nullable `ServiceIncident` to `ThreadSnapshot`; inject `ILogEventRepository` into the loader; load incidents once for visible primary IDs and catch repository failures.
- [ ] Apply clearing rules against `LatestTaskStartedAt` and `CompletionEventAt`, override status/time, and set `CompletionEventAt` to null while an incident remains active.
- [ ] Wire `SQLiteLogEventRepository(CodexDataPaths.LogDatabase)` in application composition.
- [ ] Run Core and App tests; expect all to pass.
- [ ] Commit with `feat(status): overlay service incidents`.

### Task 4: Present incident detail in existing task rows

**Files:**
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Test: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowViewModelTests.cs`

- [ ] Add failing tests for `服务异常`, `服务失败`, optional `HTTP 429/503`, optional `重试 n/limit`, and normal-row behavior.
- [ ] Run the focused ViewModel tests and verify the new assertions fail.
- [ ] Add a single incident detail text property derived from sanitized snapshot data and bind it in the existing status line without creating a new column or replacing row objects.
- [ ] Run focused and full App tests; expect all to pass.
- [ ] Commit with `feat(ui): show service incident status`.

### Task 5: One warning sound per episode

**Files:**
- Create: `src/ThreadBeacon.Core/Notifications/SoundNotificationEvent.cs`
- Modify: `src/ThreadBeacon.Core/Notifications/CompletionNotificationTracker.cs`
- Modify: `src/ThreadBeacon.App/Sounds/SoundNotificationSettings.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/SoundSettingsViewModel.cs`
- Modify: `src/ThreadBeacon.App/Sounds/CompletionNotificationCoordinator.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Test: `tests/ThreadBeacon.Core.Tests/Notifications/CompletionNotificationTrackerTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/ViewModels/SoundSettingsViewModelTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/Sounds/CompletionNotificationCoordinatorTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/Sounds/JsonSoundNotificationSettingsStoreTests.cs`

- [ ] Add failing tracker/coordinator tests for `warning:{threadId}:{episodeId}`, baseline recording, retry-to-failure deduplication, multiple episodes, disabled settings, selected warning sound, incident-over-completion priority, and 256-entry shared history.
- [ ] Run focused tests and verify failures for missing warning settings and events.
- [ ] Generalize the tracker result to distinguish completion and warning while preserving existing event IDs and persisted settings compatibility.
- [ ] Add `IsWarningEnabled` (default true) and `SelectedWarningSound` (default Chime), ViewModel properties/commands, and a separate settings section labeled `429/503 服务异常`.
- [ ] Update the coordinator to persist all observed events and play at most one sound per refresh, preferring a new incident over completion when both appear.
- [ ] Run all Core and App tests; expect all to pass.
- [ ] Commit with `feat(sound): alert once per service incident`.

### Task 6: Documentation, runtime validation, and delivery

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `PRIVACY.md`
- Modify: `ROADMAP.md`
- Modify: `docs/superpowers/plans/2026-07-19-service-incident-monitoring.md`

- [ ] Document the 429/503 boundary, strict log target allow-list, transient body handling, read-only degradation, status display, and warning sound controls in Chinese and English.
- [ ] Mark this plan's completed checkboxes and commit with `docs: document service incident monitoring`.
- [ ] Run `dotnet test ThreadBeacon.slnx --configuration Release`, `dotnet build ThreadBeacon.slnx --configuration Release --no-restore`, and `dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive`; require zero failures, warnings, and known vulnerabilities.
- [ ] Run the Release app against the real Codex database and verify normal refresh, manual refresh, Token detail persistence, Subagent expansion, and Codex write activity remain responsive.
- [ ] Run repository status, tracked-file secret/private-key scans, absolute user-path scans, network/write API scans, and staged diff review. Do not push if any sensitive material or unexpected data path is present.
- [ ] Commit any verification-only plan updates, push `main`, verify `main == origin/main`, and leave the Release app running for user evaluation.
