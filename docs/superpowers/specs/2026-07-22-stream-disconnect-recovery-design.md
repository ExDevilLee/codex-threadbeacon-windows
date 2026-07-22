# Stream Disconnect Recovery Design

## Goal

Recognize a terminal Codex stream disconnect only when the same turn has exhausted its reconnect attempts, expose a dedicated failure state, and make that failure eligible for the existing opt-in automatic recovery pipeline.

## Evidence And Boundaries

- Accept records only from the existing `codex_core::responses_retry` and `codex_core::session::turn` allowlist.
- A retry record with `attempt == limit` remains a warning by itself.
- A final `Turn error: stream disconnected before completion:` record becomes terminal only when the same turn already has `attempt == limit`.
- A final disconnect without exhausted retry evidence is ignored rather than guessed to be terminal.
- Raw log bodies, URLs, titles, and local paths are not retained in snapshots, settings, history, or UI.

## Data Flow

`SQLiteLogEventRepository` selects the exact final-disconnect shape in read-only mode. `LogEventParser` combines it with retry progress for the same turn and emits `StreamDisconnected`. The existing status loader gives the terminal incident precedence over rollout completion. `AutoRecoveryTracker` maps the incident to a new independently configurable rule.

## UI

The task row displays a failed service state with localized connection-interrupted text and the exhausted retry count. The Auto recovery tab contains a sixth rule, enabled by default while the master automatic-recovery switch remains disabled. Existing settings files are migrated by filling the missing rule with its localized default.

## Verification

Tests cover warning-only, matched terminal failure, unmatched final error, repository filtering, settings migration, tracker mapping, localized row details, and settings UI bindings. Release verification includes the full test suite, build, install, and Windows UI automation against ThreadBeacon without sending a Codex message.
