# Subagent Inline Expansion Design

Date: 2026-07-19
Status: Approved by standing macOS-alignment authorization

## Goal

Let a user inspect a primary task's direct Subagents without leaving the compact
task list, while preserving ThreadBeacon's read-only, local-only data boundary.

## Considered Approaches

1. **On-demand inline expansion (selected).** Click the existing count indicator to
   expand direct children below the parent. This matches macOS, keeps context visible,
   and avoids reading child rollout files while collapsed.
2. **Popover-only list.** Keeps the list height stable but obscures comparison and
   repeats the dismissal problems already seen with Token popovers.
3. **Always load and display children.** Simplifies state, but increases SQLite and
   rollout work every two seconds and makes the compact list noisy.

## Data Contract

- Add a `SubagentRecord` containing child ID, parent ID, title, rollout path, updated
  time, SQLite Token fallback, nickname, role, model, and reasoning effort.
- Add `IThreadRepository.LoadDirectSubagents(parentIds)` and implement it as one
  parameterized, read-only SQLite query against `thread_spawn_edges` joined to
  `threads`.
- An empty parent set or missing relationship table returns an empty result.
- Only visible IDs that are currently expanded are requested.
- Child rollout paths are handled through existing Windows path APIs and the existing
  tail parser. Conversation bodies are never read.
- Rename overrides from `session_index.jsonl` apply to child IDs using the same title
  repository as primary tasks.

## Core Model And Loading

`SubagentSnapshot` mirrors the display-safe portion of a primary snapshot and adds
optional nickname, role, model, and reasoning effort. `ThreadSnapshot` gains a
`Subagents` collection.

`ThreadStatusLoader.Load` accepts an optional expanded-parent ID set. It first loads
the visible primary tasks, intersects their IDs with that set, and then requests and
parses only those direct children. Child status, completion retention, running
freshness, Token fallback, and sorting reuse primary-task rules. Child failures
degrade that child to existing fallback behavior and never fail the primary list.

## Interaction And Layout

- The neutral branch/count indicator becomes a button with an expanded/collapsed
  state and an accessible label such as `展开 3 个 Subagent`.
- Expanding triggers an immediate baseline refresh. Collapsing removes the child rows
  immediately and triggers no unnecessary child parsing.
- Expansion state is in memory only and is not written to settings.
- Direct children appear below the parent with an indented turn-arrow, status dot,
  `Agent 别名 | 标题`, cumulative Token, and `状态 · 最近活动`.
- Each child has an info icon whose hover/click detail uses the existing stable
  popover behavior and shows role, model, reasoning, Token details, and activity.
- Only one child level is shown. Child rows do not expose their own descendants.
- Parent and child rows remain stable across the two-second refresh reconciliation so
  an open detail popover is not dismissed by refresh.

## Failure States

While an expanded parent is loading, its child region displays a compact loading
row. If child data cannot be read, that region displays a degraded message while the
last successful primary list remains usable. A parent with a historical count but no
readable current child records displays `暂无可读取的 Subagent` rather than spinning
forever.

## Privacy And Security

- SQLite is opened read-only with query-only mode and no pooling.
- No Codex data, settings, or session files are modified.
- No network access is added.
- No body text, prompts, responses, tool output, or deeper task tree is loaded.
- Expansion state and child metadata are not persisted.

## Testing And Acceptance

- Repository tests cover parameterized parent filtering, ordering, missing tables,
  optional metadata, and read-only behavior.
- Loader tests prove only expanded visible parents are loaded, child rename/status/
  Token fallback is applied, and children are sorted consistently.
- View-model tests cover toggle state, reconciliation identity, labels, empty/error
  states, and detail formatting.
- Runtime acceptance verifies expansion/collapse, two-second refresh stability,
  hover and pinned detail dismissal, and layout at 620px and 480px.

## Out Of Scope

- recursive descendants;
- active-child counts derived from `open` or `closed`;
- parent-plus-child Token aggregation;
- child completion sounds;
- context-compaction progress or app-server integration.
