# Compaction History Implementation Plan

> **For agentic workers:** Implement this plan task-by-task with tests before production code.

**Goal:** Show read-only rollout compression count and last-completed time in Windows Token details, matching the macOS checkpoint `648719f` without implementing live Hooks yet.

**Architecture:** Extend the existing rollout parser with a privacy-safe `CompactionHistory` value object. Carry it through `ThreadSnapshot` into the existing Token detail view model. Unknown or malformed compression records are ignored, and no source text is retained.

**Tech Stack:** .NET 9, WPF, System.Text.Json, xUnit.

---

### Task 1: Add the failing parser and model tests

**Files:**
- Create: `src/ThreadBeacon.Core/Models/CompactionHistory.cs`
- Modify: `src/ThreadBeacon.Core/Models/RolloutObservation.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/RolloutTailParserTests.cs`

- [ ] Add tests for one top-level `compacted`, one `event_msg/context_compacted`, adjacent pair deduplication, independent events, and malformed records.
- [ ] Run `dotnet test tests/ThreadBeacon.Core.Tests/ThreadBeacon.Core.Tests.csproj --filter RolloutTailParserTests`; confirm the new assertions fail because the model and parser do not expose history yet.

### Task 2: Implement privacy-safe compression history parsing

**Files:**
- Modify: `src/ThreadBeacon.Core/Models/CompactionHistory.cs`
- Modify: `src/ThreadBeacon.Core/Models/RolloutObservation.cs`
- Modify: `src/ThreadBeacon.Core/Services/RolloutTailParser.cs`

- [ ] Add immutable count/last-time fields and parse only event type and timestamp.
- [ ] Deduplicate adjacent top-level and event-message pairs within a small bounded time window.
- [ ] Run the focused parser tests and confirm all pass.

### Task 3: Carry history into task details

**Files:**
- Modify: `src/ThreadBeacon.Core/Models/ThreadSnapshot.cs`
- Modify: `src/ThreadBeacon.Core/Services/ThreadStatusLoader.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/TokenDetailViewModel.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/ThreadStatusLoaderTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/ViewModels/TokenDetailViewModelTests.cs`

- [ ] Add a snapshot test proving history reaches a task detail model.
- [ ] Keep details available when history is the only non-empty detail.
- [ ] Run Core and App focused tests.

### Task 4: Render localized history rows

**Files:**
- Modify: `src/ThreadBeacon.App/Resources/Strings.zh-Hans.xaml`
- Modify: `src/ThreadBeacon.App/Resources/Strings.en.xaml`
- Modify: `src/ThreadBeacon.App/Controls/TokenInfoControl.xaml`
- Test: `tests/ThreadBeacon.App.Tests/Controls/TokenInfoControlTests.cs`

- [ ] Add localized `压缩次数` / `Compactions` and `最近压缩` / `Last compaction` labels.
- [ ] Render `0` and `-` when no history exists, preserving the current popover layout.
- [ ] Verify the XAML and control tests.

### Task 5: Document and verify the feature

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `CHANGELOG.md`
- Modify: `VERSION`

- [ ] Update the version and document the read-only history boundary and current lack of live Hook status.
- [ ] Run the full Core/App tests, Release build, publish script, and install the fixed EXE.
- [ ] Verify the Token details popover in the installed UI with an isolated rollout fixture.
- [ ] Scan the working tree and staged diff for secrets and machine-specific data.
- [ ] Commit and push this feature independently.
