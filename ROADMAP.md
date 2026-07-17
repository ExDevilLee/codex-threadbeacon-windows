# ThreadBeacon for Windows Roadmap

## Product Principles

- Keep the app focused on understanding task state at a glance.
- Prefer local read-only data and preserve Codex write availability.
- Keep parsing and status rules independent from WPF.
- Treat Codex local schemas and JSONL formats as versioned compatibility inputs, not stable APIs.
- Use the macOS implementation as a behavioral reference rather than translating Swift line by line.

## Phase 1: Core Data POC

- [ ] Resolve Codex paths with overrides for `CODEX_HOME` and `CODEX_SQLITE_HOME`.
- [ ] Read the 8 most recent unarchived primary threads from SQLite in read-only mode.
- [ ] Exclude Subagents using thread metadata and parent-child relationships.
- [ ] Resolve renamed titles from `session_index.jsonl` with SQLite title fallback.
- [ ] Read at most the rollout tail required for status and Token fields.
- [ ] Derive and display task status.
- [ ] Display cumulative Token usage.
- [ ] Refresh every 2 seconds and support manual refresh.
- [ ] Degrade safely when a data source is missing, locked, or being upgraded.
- [ ] Validate behavior while Codex is continuously writing.

Acceptance: with several concurrent Codex tasks, run for 30 minutes and verify task count, renamed titles, status transitions, and Token values against the real sessions without blocking Codex writes.

## Deferred

- Completion and incident sounds.
- Always-on-top window behavior.
- Thread pin and ignore rules.
- Subagent expansion.
- HTTP 429/503 incident monitoring.
- System tray integration.
- Packaging, signing, and automatic updates.

