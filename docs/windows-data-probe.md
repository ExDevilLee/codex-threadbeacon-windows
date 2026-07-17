# Windows Codex Data Probe

Probe date: 2026-07-18

## Conclusion

The core local data path used by ThreadBeacon for macOS is available on the tested Windows 11 machine. This is sufficient to start the Windows POC.

## Verified Sources

- `%USERPROFILE%\.codex\state_5.sqlite`
- `%USERPROFILE%\.codex\logs_2.sqlite`
- `%USERPROFILE%\.codex\session_index.jsonl`
- `%USERPROFILE%\.codex\sessions\...\*.jsonl`

The current `threads` table includes the rollout path, archive state, token usage, source, model, reasoning effort, and agent metadata needed by the planned POC. The `thread_spawn_edges` table is present for parent-child filtering.

The session index contains `id`, `thread_name`, and `updated_at`. Rollout samples contain the event types required for task start, completion, and Token observation.

Both SQLite databases could be opened read-only while Codex was running. All observed rollout paths were valid Windows absolute paths.

## Implemented POC Slice

`SQLiteThreadRepository` now opens `state_5.sqlite` with a short-lived, non-pooled, read-only connection and enforces `PRAGMA query_only`. It loads up to 8 recent unarchived primary threads, excludes Subagents using both `thread_source` and spawn edges, and counts direct child relationships.

The repository returns stable health states for a missing, busy, incompatible, or otherwise unavailable database. A local privacy-safe probe completed successfully while Codex was running and printed only source availability, returned thread count, and health status.

## Compatibility Boundary

This probe validates one Windows 11 machine and the currently installed Codex version. It does not establish a stable public contract. The implementation must use Windows path APIs, isolate schema assumptions, and degrade safely when sources change or disappear.
