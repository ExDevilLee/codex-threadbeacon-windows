# Subagent Count Badge Design

Date: 2026-07-19
Status: Approved by standing feature-direction authorization

## Goal

Show whether a primary task has created direct Subagents, and how many, without
changing the compact task-list hierarchy or implying that those Subagents are still
running.

## Existing Data Contract

The Windows data path already implements the macOS count contract:

- `SQLiteThreadRepository` counts direct `thread_spawn_edges` relationships in one
  read-only query;
- known child thread IDs are excluded from the primary task list even when historical
  rows do not carry `thread_source = 'subagent'`;
- a missing relationship table falls back to zero counts rather than failing the list;
- `ThreadRecord.SubagentCount` is passed unchanged to
  `ThreadSnapshot.SubagentCount`.

This feature therefore changes only the application presentation layer.

## Display Rules

- Place the indicator immediately after the task title and before cumulative Token.
- Show it only when `SubagentCount > 0`; zero reserves no layout space.
- Use the Segoe Fluent branch icon plus the full invariant decimal count.
- Use the existing secondary text color and compact 11px typography.
- Do not add a pill, colored background, or status color.
- The tooltip and accessibility name are `N 个 Subagent`.
- The indicator is not clickable and has no context menu.
- The title keeps priority for remaining width and uses the existing single-line
  ellipsis behavior.

The count is a historical total of direct relationships. It is not a running count,
does not distinguish `open` from `closed`, and does not summarize child status or
Token usage.

## View Model

`ThreadRowViewModel` exposes:

- `SubagentCount`;
- `HasSubagents`;
- `SubagentCountText`;
- `SubagentAccessibilityLabel`.

`Update` refreshes the count on the existing row instance so the two-second
reconciliation cycle does not replace rows or create stale labels.

## Layout

Replace the standalone title `TextBlock` with a two-column title area. The title uses
the star column; the optional indicator uses an auto column. The existing Token and
duration columns remain unchanged.

At the 480px minimum width, long titles truncate before the indicator and numeric
columns. The indicator never overlaps Token details or duration.

## Failure, Privacy, and Scope

- Invalid non-positive counts are hidden defensively.
- No child task ID, title, role, path, status, Token value, or body is displayed or
  persisted.
- SQLite remains read-only and no additional query is introduced.
- The feature performs no network access and does not modify Codex data.

## Testing

- A zero count exposes no visible indicator state.
- A positive count exposes the exact number and Chinese accessibility label.
- Reconciliation updates a count on the existing row instance.
- Existing title, Token detail, status, duration, sound notification, and refresh
  tests remain green.
- Runtime inspection covers tasks with and without counts at 620px and 480px widths.

## Out of Scope

- expanding Subagent rows;
- active versus completed Subagent counts;
- Subagent status colors or alerts;
- recursive descendant counts;
- parent-plus-child Token aggregation.
