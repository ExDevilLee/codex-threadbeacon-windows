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

`SessionIndexTitleRepository` reads `session_index.jsonl` with Windows read/write/delete sharing enabled so Codex can continue appending. Malformed lines and entries with missing or blank IDs or titles are ignored. The last valid entry for a thread wins; tasks without a valid rename retain the SQLite title.

The local probe confirmed that the session index was healthy and that current visible primary threads had matching rename records. It reported only the match count and did not print IDs or titles.

`RolloutTailParser` opens rollout JSONL with Windows read/write/delete sharing and reads at most the final 2 MiB. When the read starts in the middle of a line, the truncated first line is discarded. Parsing retains only event types, timestamps, `turn_context` model/effort, and numeric Token counters; reasoning summaries, messages, and `last_agent_message` are never placed in the observation model. SQLite model and reasoning values take per-field precedence, while rollout metadata fills missing fields.

The status policy keeps a completion visible for the configured 1-5 minute duration (one minute by default) and changes it to idle afterward. An unresolved running turn becomes unknown after 120 seconds without a newer event. The local probe found every visible rollout available and produced aggregate status counts without printing thread identifiers, titles, paths, Token values, or message content.

`ThreadStatusLoader` now merges the SQLite records, session index title overrides, rollout observations, status policy, and SQLite Token fallback into immutable task snapshots. Snapshots sort by status priority, then latest event time, then stable thread ID.

For each visible unarchived parent, the repository also queries only direct Subagents updated within the 120-second running-freshness window. Their rollout observations derive the active numerator shown as `active/total`; expanded rows reuse the same per-refresh observations. A candidate-query failure degrades task-database health without discarding the last usable primary list.

The WPF POC displays the real snapshot list with status lights, renamed titles, cumulative Token totals, and status duration. It refreshes every 2 seconds on a background worker and provides a manual refresh command. A local launch verified that four real tasks rendered without layout overlap while Codex remained active.

## Compatibility Boundary

This probe validates one Windows 11 machine and the currently installed Codex version. It does not establish a stable public contract. The implementation must use Windows path APIs, isolate schema assumptions, and degrade safely when sources change or disappear.
