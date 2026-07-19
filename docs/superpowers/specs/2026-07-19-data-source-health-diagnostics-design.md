# Data Source Health Diagnostics Design

Date: 2026-07-19
Status: Approved by the macOS-alignment direction

## Goal

Show whether the current task list is based on complete local Codex data or is
running with a degraded source. The feature diagnoses the same read-only refresh
that builds the list; it does not add a second probe, expose local content, or try
to repair Codex data.

## Product Behavior

The footer always shows a compact health button at the right, immediately before
the existing refresh time. Its icon, color, tooltip, and accessible name express
one of three overall states:

- `正常`: all sources used by the refresh succeeded;
- `部分降级`: the task list remains usable, but at least one optional source failed;
- `不可用`: the task database could not produce a trustworthy refresh.

Clicking the button opens a dismissible popover aligned with the macOS layout. It
shows the last successful refresh time and four rows:

| Source | Purpose | Failure behavior |
| --- | --- | --- |
| Task database | task rows, archive state, Token totals, relationships | overall unavailable; retain the previous visible list |
| Rename index | user-renamed titles | degrade and use the SQLite title |
| Rollout | status, activity, and Token detail | degrade and show aggregate success/failure read counts |
| Service logs | HTTP 429/503 retry and failure state | degrade and omit service incidents |

Each row uses a familiar icon plus text and color. The popover closes when the user
clicks elsewhere. It never replaces the existing footer status text, pause state,
or refresh timestamp.

## Health Model

`ThreadBeacon.Core` owns the health semantics so WPF does not infer them. Add a
`DataSourceHealthReport` containing:

- task database, rename index, rollout, and service-log source states;
- rollout success and failure counts;
- the last successful refresh timestamp;
- derived overall state and stable Chinese display text.

Individual sources use `Healthy`, `Degraded`, `Unavailable`, or `NotUsed`. Detail
messages come from a fixed mapping of existing status enums; raw exception text is
never retained. The task database is the only source that can make the overall
state unavailable. Optional failures make it degraded.

The existing repository result enums remain the low-level contracts. The loader
maps them into the health report while preserving their existing fallback behavior.
Service-log loading gains a result type so a missing, busy, incompatible, or
unavailable log database is distinguishable from a healthy query with no incidents.

## Refresh Data Flow

One `ThreadStatusLoader.Load` call produces task snapshots and the health report:

1. Aggregate the recent, explicitly included, favorite, and requested direct-child
   task-database reads into the task source state.
2. Map the session-index result while continuing to resolve titles from SQLite.
3. Count successful and failed rollout reads for main rows and any direct Subagents
   actually loaded in this refresh.
4. Load service incidents through the status-bearing result and degrade to an empty
   incident set on failure.
5. Return snapshots and health together without a second file or database read.

On a successful core refresh, `MainWindowViewModel` records the refresh time as the
last successful time and reconciles rows as today. On a task-database failure it
publishes the unavailable health report but keeps the previous candidate and visible
rows. Completion and service-incident sounds are evaluated only for successful core
refreshes; health transitions themselves never play sounds.

`NotUsed` applies when a source genuinely did not participate, such as service logs
with no active visible task IDs. An empty but successfully queried source is healthy.

## WPF Structure

Add a focused health detail view model and a reusable `DataSourceHealthControl`.
The control owns only popover interaction and presentation; it receives the current
report from `MainWindowViewModel`. The existing two-second reconciliation updates
the report object without replacing the control, so an open popover does not close
on refresh.

The footer becomes three columns: status text, health button, and update time. The
button remains visually quiet when healthy and uses orange/red emphasis only for
degraded or unavailable states. Layout must remain coherent at the existing minimum
window width.

## Privacy And Failure Boundaries

- Keep SQLite connections short-lived, non-pooled, and read-only with
  `PRAGMA query_only = ON`.
- Do not add network access, telemetry, history, export, automatic repair, or Codex
  writes.
- Do not retain or display task IDs, titles, rollout paths, database paths, log
  bodies, conversation content, or raw exceptions in health state.
- A missing or changing optional source cannot fail the task list.
- Do not add separate health categories for Subagents, sound assets, preferences,
  or the deferred archive-restore CLI.

## Tests And Acceptance

Core tests cover all-healthy, optional-source degradation, accurate rollout counts,
service-log status mapping, task-database unavailability, and stable privacy-safe
detail text. Application tests cover last-successful-time updates, retention of rows
on core failure, no health-triggered notifications, report updates on the existing
view model, popover dismissal, accessibility labels, and the three-column footer.

Final acceptance requires:

- normal local data shows the always-visible healthy footer button;
- clicking it shows the four source rows and last successful refresh time;
- a two-second refresh does not close the open popover;
- full tests and Release build pass without warnings;
- dependency vulnerability and pre-push sensitive-data checks pass;
- public documentation describes the diagnostic without exposing local data.
