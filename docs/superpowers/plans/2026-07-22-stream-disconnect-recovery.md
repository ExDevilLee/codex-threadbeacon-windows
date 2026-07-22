# Stream Disconnect Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add privacy-bounded terminal stream-disconnect recognition and a dedicated opt-in recovery rule.

**Architecture:** Extend the existing read-only log pipeline with one exact terminal shape, combine it with same-turn exhausted retry evidence in Core, and reuse the existing recovery and WPF settings surfaces. Do not add a second sender or retain raw log text.

**Tech Stack:** .NET 9, C#, Microsoft.Data.Sqlite, WPF, xUnit, Windows UI Automation.

---

### Task 1: Parser and repository

**Files:**
- Modify: `tests/ThreadBeacon.Core.Tests/Services/LogEventParserTests.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/Services/SQLiteLogEventRepositoryTests.cs`
- Modify: `src/ThreadBeacon.Core/Models/ServiceIncident.cs`
- Modify: `src/ThreadBeacon.Core/Services/LogEventParser.cs`
- Modify: `src/ThreadBeacon.Core/Services/SQLiteLogEventRepository.cs`

- [x] Add failing tests for exhausted warning, matched final disconnect, unmatched final disconnect, and repository selection.
- [x] Run focused tests and verify they fail because the new incident kind/query is absent.
- [x] Add `StreamDisconnected`, exact repository selection, and same-turn exhausted-retry gating.
- [x] Run focused tests and verify they pass.

### Task 2: Recovery rule and migration

**Files:**
- Modify: `tests/ThreadBeacon.Core.Tests/AutoRecovery/AutoRecoverySettingsTests.cs`
- Modify: `tests/ThreadBeacon.Core.Tests/AutoRecovery/AutoRecoveryTrackerTests.cs`
- Modify: `src/ThreadBeacon.Core/AutoRecovery/AutoRecoverySettings.cs`
- Modify: `src/ThreadBeacon.Core/AutoRecovery/AutoRecoveryTracker.cs`

- [x] Add failing tests for defaults, localization, missing-rule migration, and candidate mapping.
- [x] Add the independently enabled rule and localized default prompts.
- [x] Run focused tests and verify they pass.

### Task 3: WPF presentation

**Files:**
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowViewModelTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/AutoRecoverySettingsViewModelTests.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/AutoRecoverySettingsViewModel.cs`

- [x] Add failing tests for the localized row detail and sixth settings rule.
- [x] Add connection-interrupted labels and keep `Retry 5/5` visible for the terminal incident.
- [x] Run focused tests and verify they pass.

### Task 4: Release verification and delivery

**Files:**
- Modify: `VERSION`
- Modify: `CHANGELOG.md`
- Modify: `ROADMAP.md`
- Modify: `README.md`
- Modify: `README-EN.md`

- [x] Document the exact evidence requirement and privacy boundary; bump to `0.13.0`.
- [x] Run the full tests, Release build, and diff checks.
- [x] Publish and install the new build to the fixed local install directory.
- [x] Verify the installed ThreadBeacon UI in Chinese and English without changing automatic-recovery switches.
- [x] Scan staged changes for credentials, private paths, task IDs, URLs, and raw log bodies.
- [x] Commit `feat(recovery): handle exhausted stream disconnects` and push `main`.

The privacy-safe Windows POC found seven reconnect retry records, two exhausted reconnect records,
one terminal disconnect record, and one same-turn exhausted/final pair. It emitted counts only.
