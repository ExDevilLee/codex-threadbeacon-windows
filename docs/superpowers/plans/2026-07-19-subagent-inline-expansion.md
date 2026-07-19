# Subagent Inline Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add read-only, on-demand inline display of direct Subagents beneath visible primary tasks.

**Architecture:** Extend the Core repository and loader with an explicit expanded-parent contract, then project child snapshots into stable WPF row view models. Keep expansion state in `MainWindowViewModel`, load only visible expanded parents, and reuse the existing rollout parser, title resolver, Token formatting, and popover interaction patterns.

**Tech Stack:** .NET 9, C#, WPF, Microsoft.Data.Sqlite, xUnit

---

## File Map

- Create `src/ThreadBeacon.Core/Models/SubagentRecord.cs` and `SubagentSnapshot.cs`.
- Modify `IThreadRepository`, `SQLiteThreadRepository`, `ThreadSnapshot`, and
  `ThreadStatusLoader` for on-demand direct-child loading.
- Create `SubagentRowViewModel`, `SubagentDetailViewModel`, and focused formatters.
- Modify `ThreadRowViewModel`, `ThreadRowCollection`, `MainWindowViewModel`, and
  `MainWindow.xaml` for expansion, stable reconciliation, inline rows, and details.
- Extend Core and App tests before each production change.
- Update `README.md`, `README-EN.md`, `ROADMAP.md`, and `PRIVACY.md`.

### Task 1: Repository Child Records

- [ ] Add failing repository tests proving a request for one parent returns only its
  direct children in recency order with optional agent metadata.
- [ ] Add failing tests for empty parent input and a missing `thread_spawn_edges`
  table returning an empty healthy result.
- [ ] Run the focused tests and confirm they fail because the child API is absent.
- [ ] Add `SubagentRecord`, a status-bearing load result, and
  `LoadDirectSubagents(IReadOnlySet<string>)` to the repository boundary.
- [ ] Implement one parameterized read-only query and reuse the existing SQLite
  compatibility/error mapping.
- [ ] Run focused and full Core tests, then commit `feat(subagent): load direct children`.

### Task 2: Child Snapshot Loading

- [ ] Add failing loader tests proving only expanded visible parent IDs are requested.
- [ ] Add failing tests for child rename overrides, rollout-derived status, SQLite
  Token fallback, metadata retention, and status/activity sorting.
- [ ] Run focused tests and confirm the expanded-parent overload and child snapshots
  are absent.
- [ ] Add `SubagentSnapshot`, `ThreadSnapshot.Subagents`, and an expanded-parent
  parameter to `ThreadStatusLoader.Load`.
- [ ] Reuse primary display-state and Token rules for children while isolating child
  parse failures.
- [ ] Run focused and full Core tests, then commit `feat(subagent): build child snapshots`.

### Task 3: Expansion State And Stable Rows

- [ ] Add failing App tests for toggle labels, transient expanded IDs, immediate
  collapse, refresh requests, and preservation of existing parent/child row instances.
- [ ] Run the focused tests and confirm the new state and view models are absent.
- [ ] Add expansion commands/state to `MainWindowViewModel` and pass a snapshot of
  expanded IDs to background refreshes.
- [ ] Add `SubagentRowViewModel` and reconcile children by ID under their existing
  parent row so two-second refreshes do not replace detail owners.
- [ ] Run focused and full App tests, then commit `feat(subagent): manage inline expansion`.

### Task 4: WPF Inline UI And Details

- [ ] Add failing formatter/detail tests for alias suppression, relative activity,
  optional metadata, and Token detail values.
- [ ] Run focused tests and confirm the presentation helpers are absent.
- [ ] Convert the count badge to a keyboard-accessible toggle button with expanded
  state, tooltip, and automation name.
- [ ] Add indented child rows and a child detail popover using the existing hover plus
  click pinning behavior, including outside-click dismissal.
- [ ] Add compact loading, empty, and degraded child-region states without resizing
  unrelated parent rows.
- [ ] Run all tests and Release build, then commit `feat(ui): expand Subagents inline`.

### Task 5: Documentation, Verification, Security Audit, And Push

- [ ] Document direct-child fields, on-demand reads, transient expansion, and explicit
  exclusions in `README.md`, `README-EN.md`, `ROADMAP.md`, and `PRIVACY.md`.
- [ ] Run `dotnet test ThreadBeacon.slnx`, `dotnet build ThreadBeacon.slnx -c Release`,
  and the vulnerable-package audit; require clean output.
- [ ] Launch Release and verify expansion/collapse, automatic refresh stability,
  detail hover/click/outside dismissal, and 620px/480px layout.
- [ ] Inspect the complete diff and tracked files for credentials, private keys,
  absolute user paths, local settings, Codex content, build output, or network/write
  access.
- [ ] Commit documentation as `docs: document Subagent inline expansion`.
- [ ] Push `main`, fetch, confirm a clean worktree and `HEAD == origin/main`, then
  leave the verified Release build running for stage acceptance.
