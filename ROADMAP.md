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
- [x] Refresh every 2 seconds and support manual refresh.
- [x] Degrade safely when the state database is missing, locked, or schema-incompatible.
- [ ] Validate behavior while Codex is continuously writing.

Acceptance: with several concurrent Codex tasks, run for 30 minutes and verify task count, renamed titles, status transitions, and Token values against the real sessions without blocking Codex writes.

## Phase 2: Window Controls

- [x] Keep the ThreadBeacon window above normal windows with a header pin button.
- [x] Persist the always-on-top selection and restore it after restart.

## Deferred

- Completion and incident sounds.
- Thread pin and ignore rules.
- Subagent expansion.
- HTTP 429/503 incident monitoring.
- System tray integration.
- Packaging, signing, and automatic updates.
