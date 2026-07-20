# Detached Subagent Promotion Design

Date: 2026-07-21
Status: Approved by user

## Goal

Show Codex tasks that are user-visible independent tasks but remain marked as
`thread_source = 'subagent'`, without allowing genuine background Subagents into the
primary task list.

## Evidence

The affected user-visible task is active and present in `session_index.jsonl`, but its
SQLite row is marked as a Subagent. It has no child entry in
`thread_spawn_edges`. The current database contains one linked Subagent and one such
detached Subagent. Windows and macOS currently apply the same strict source and edge
filters, so both would omit this data shape.

## Selected Rule

A detached Subagent is promoted to a primary candidate only when all conditions hold:

- it is not archived;
- `thread_source` is `subagent`;
- it is not referenced by `thread_spawn_edges.child_thread_id`;
- the healthy Rename index contains a non-empty title for its thread ID.

Normal primary queries retain their existing source filter. Linked Subagents remain
available only through inline expansion. If the Rename index or relationship table is
missing or incompatible, promotion is skipped and the current strict behavior remains.

## Architecture

`IThreadRepository` gains a bounded `LoadDetachedSubagentCandidates` operation with a
default empty implementation. `SQLiteThreadRepository` implements it with a separate,
read-only, relationship-aware query. `ThreadStatusLoader` loads Rename titles, requests
detached candidates only while that source is healthy, keeps only IDs present in the
Rename map, merges them with normal/included/favorite records, and reuses the existing
status, Token, incident, sorting, and list-policy pipeline.

The detached query is supplemental. Its failure degrades task-database health without
discarding the normal task list. The request is bounded by the configured recent-task
limit, so the two-second refresh cannot turn into an unbounded database or rollout scan.

## Privacy And Safety

- SQLite remains read-only, query-only, non-pooled, and short-lived.
- No conversation body or raw delegation payload is displayed or persisted.
- No Codex data or settings are modified.
- No network access is added.
- Rename failure fails closed: no detached Subagent is promoted.

## Acceptance

- linked Subagents remain excluded from the main list;
- an unlinked, renamed Subagent is displayed as a normal primary candidate;
- an unlinked Subagent without a Rename entry remains excluded;
- archived detached Subagents remain excluded;
- missing relationship or Rename data preserves strict filtering;
- the installed App displays the affected user-visible detached task;
- all Core and App tests, Release build, packaging, and security checks pass.
