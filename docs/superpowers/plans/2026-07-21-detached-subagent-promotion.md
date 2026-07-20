# Detached Subagent Promotion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Promote renamed, unlinked Subagent records into the primary task candidate set while retaining strict exclusion for real child tasks.

**Architecture:** Add a bounded supplemental repository query for detached candidates, then let `ThreadStatusLoader` cross-check those IDs against a healthy Rename index before merging them into the existing snapshot pipeline. Fail closed whenever either supporting source is unavailable.

**Tech Stack:** .NET 9, C#, Microsoft.Data.Sqlite, WPF, xUnit, PowerShell

---

## File Map

- Modify `src/ThreadBeacon.Core/Services/IThreadRepository.cs` to expose the optional candidate operation.
- Modify `src/ThreadBeacon.Core/Services/SQLiteThreadRepository.cs` to query detached candidates read-only.
- Modify `src/ThreadBeacon.Core/Services/ThreadStatusLoader.cs` to cross-check Rename IDs, merge candidates, and report health.
- Modify `tests/ThreadBeacon.Core.Tests/Services/SQLiteThreadRepositoryTests.cs` and `ThreadStatusLoaderTests.cs` with regression coverage.
- Modify `README.md`, `README-EN.md`, `CHANGELOG.md`, `ROADMAP.md`, and `VERSION` for `v0.10.1`.

### Task 1: Repository Candidate Query

- [x] Add a failing SQLite test whose fixture contains a linked Subagent, a detached active Subagent, and an archived detached Subagent.
- [x] Assert that `LoadDetachedSubagentCandidates(8)` returns only the active detached record and that a missing `thread_spawn_edges` table returns an empty healthy result.
- [x] Run `dotnet test tests/ThreadBeacon.Core.Tests --filter FullyQualifiedName~SQLiteThreadRepositoryTests` and confirm compilation fails because the method is absent.
- [x] Add the default interface method:

```csharp
ThreadLoadResult LoadDetachedSubagentCandidates(int limit)
{
    ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
    return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, []);
}
```

- [x] Implement a parameterized, read-only query requiring `thread_source = 'subagent'`, `archived = 0`, no child edge, recency ordering, and `$limit`; return empty when the relationship table is absent.
- [x] Re-run the focused repository tests and require all to pass.

### Task 2: Loader Promotion Policy

- [x] Add a failing loader test with one normal record and two detached candidates; give only one candidate a Rename entry and assert only that candidate is merged.
- [x] Add a failing loader test proving an unhealthy Rename source does not call the detached-candidate repository method.
- [x] Add a failing loader health test proving a supplemental candidate-query failure degrades task-database health without dropping normal records.
- [x] Run `dotnet test tests/ThreadBeacon.Core.Tests --filter FullyQualifiedName~ThreadStatusLoaderTests` and confirm the new expectations fail.
- [x] Load Rename titles before the optional candidate call, filter with `titleResult.Titles.ContainsKey(record.Id)`, and merge promoted records after recent/included/favorite records.
- [x] Include the supplemental status in `FirstUnhealthyStatus`, `CreateHealthReport`, and `TaskDatabaseHealth` only when the candidate source was used.
- [x] Re-run all Core tests and require all to pass.

### Task 3: Release Documentation And Runtime Verification

- [x] Set `VERSION` to `0.10.1` and add a Changelog Fixed entry explaining detached renamed task promotion.
- [x] Update both READMEs and the roadmap to describe the conservative promotion rule and fail-closed behavior.
- [x] Run `dotnet test ThreadBeacon.slnx --configuration Release`, `dotnet build ThreadBeacon.slnx --configuration Release`, and `dotnet list ThreadBeacon.slnx package --vulnerable --include-transitive`.
- [x] Run `script/publish_release.ps1`, install the complete portable package to `%LOCALAPPDATA%\ThreadBeacon`, and launch the fixed executable.
- [x] Verify in the UI that the affected detached task appears, linked Subagents remain inline-only, refresh is stable, and data-source health remains normal.
- [x] Scan the complete Git diff and tracked files for credentials, private keys, local Codex content, absolute user paths, build output, and unintended write/network behavior.
- [x] Commit, push `main`, tag `v0.10.1`, verify the GitHub Release assets, and confirm local `HEAD`, `origin/main`, and the tag match.
