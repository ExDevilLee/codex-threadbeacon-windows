# ThreadBeacon for Windows Roadmap

## Product Principles

- Keep the app focused on understanding task state at a glance.
- Prefer local read-only data and preserve Codex write availability.
- Keep parsing and status rules independent from WPF.
- Treat Codex local schemas and JSONL formats as versioned compatibility inputs, not stable APIs.
- Use the macOS implementation as a behavioral reference rather than translating Swift line by line.

## Phase 1: Core Data POC

- [x] Resolve Codex paths with overrides for `CODEX_HOME` and `CODEX_SQLITE_HOME`.
- [x] Read the 8 most recent unarchived primary threads from SQLite in read-only mode.
- [x] Exclude Subagents using thread metadata and parent-child relationships.
- [x] Resolve renamed titles from `session_index.jsonl` with SQLite title fallback.
- [x] Read at most the final 2 MiB of each rollout for status and Token fields.
- [x] Derive task status with completion retention and stale-running expiry.
- [x] Merge task, title, rollout, status, and Token data into unified snapshots.
- [x] Display the derived task status in WPF.
- [x] Display cumulative Token usage.
- [x] Show a macOS-aligned Token detail popover with hover and click-to-pin behavior.
- [x] Show a neutral direct-Subagent count beside the primary task title when non-zero.
- [x] Expand direct Subagents inline on demand with status, activity, Token, and metadata details.
- [x] Show running primary tasks over currently visible tasks in the window header.
- [x] Refresh every 2 seconds and support manual refresh.
- [x] Degrade safely when the state database is missing, locked, or schema-incompatible.
- [x] Validate read-only behavior for 30 minutes while Codex is continuously writing.

Acceptance: with several concurrent Codex tasks, run for 30 minutes and verify task count, renamed titles, status transitions, and Token values against the real sessions without blocking Codex writes.

Result: completed on Win11 with 900 samples, no probe failures, no source degradations, and no App crashes. See [the validation record](docs/validation/2026-07-18-windows-30-minute-soak.md).

## Phase 2: Window Controls

- [x] Keep the ThreadBeacon window above normal windows with a header pin button.
- [x] Persist the always-on-top selection and restore it after restart.
- [x] Pause and resume automatic monitoring while keeping manual refresh available.

## Phase 3: Completion Sounds

- [x] Detect reliable task completion events without reading conversation bodies.
- [x] Notify only for new completions observed by automatic refresh.
- [x] Prevent startup, manual refresh, and monitoring resume from replaying history.
- [x] Coalesce multiple completions in one refresh batch into one sound.
- [x] Bundle the macOS-aligned Beacon, Chime, Pulse, Alert, Resolve, and Knock tones.
- [x] Default new settings to Chime for completion and Alert for 429/503 incidents while preserving existing selections.
- [x] Persist sound preferences and bounded derived event history locally.

## Phase 4: HTTP 429/503 Service Incidents

- [x] Query only visible primary task IDs from `logs_2.sqlite` with a strict target and event allow-list.
- [x] Parse retry episodes without retaining raw log bodies in task snapshots.
- [x] Show active retries as warnings and exhausted retries as failures with HTTP/retry detail.
- [x] Clear stale incidents after same-turn recovery or newer rollout lifecycle evidence.
- [x] Suppress misleading completion sounds while an incident is active.
- [x] Play one independently configurable warning sound per incident episode.
- [x] Degrade to normal task monitoring when the log database is unavailable or incompatible.

## Phase 5: Task Pin and Ignore Controls

- [x] Right-click a primary task to pin/unpin or temporarily ignore it.
- [x] Keep status priority above task pinning and prioritize pins only within the same status.
- [x] Restore temporary ignores automatically after a newer turn starts.
- [x] Restore one ignored task or all ignored tasks from the conditional header control.
- [x] Persist only task IDs, ignore timestamps, and rule types without modifying Codex data.

## Phase 6: Favorites and Archived Watchlist

- [x] Favorite or unfavorite a primary task independently of pin and ignore.
- [x] Persist favorite task IDs and the all/favorites-only filter locally.
- [x] Keep the existing status, pin, and recency order when favorites are not filtered.
- [x] Load specified archived favorites through a separate parameterized read-only query.
- [x] Show archived favorites with a neutral state while retaining available rename and Token data.
- [x] Exclude archived favorites from service-incident queries and completion/incident notifications.

## Phase 7: Data Source Health Diagnostics

- [x] Build task-database, Rename-index, rollout, and service-log health from the same refresh that produces task snapshots.
- [x] Distinguish healthy, degraded, unavailable, and unused sources without retaining raw errors or local paths.
- [x] Show aggregate rollout success/failure counts and the last successful refresh time.
- [x] Keep the previous visible task list when the core task database is unavailable.
- [x] Keep the health popover open while the two-second refresh updates its contents in place.
- [x] Expose an always-visible, accessible footer entry aligned with the macOS behavior.

## Phase 8: Settings Window

- [x] Replace the sound-only popover with a single-instance, non-modal settings window.
- [x] Provide macOS-aligned General and Sounds tabs through native WPF controls.
- [x] Configure 1, 2, 5, or 10-second refresh intervals and apply changes immediately.
- [x] Configure maximum visible task counts of 4, 8, 12, or 20 without replaying notifications.
- [x] Keep paused monitoring paused while preferences change.
- [x] Persist display preferences separately from window pin and sound settings.

## Deferred

- Other failure, warning, and attention sounds.
- Subagent alerts, reliable active-child counts, and parent-child Token aggregation.
- System tray integration.
- Packaging, signing, and automatic updates.
