# Interrupted Status Design

## Goal

Represent a structured Codex user interruption as its own read-only task state instead of leaving the task displayed as running.

## Data contract

- Accept only `event_msg` / `turn_aborted` events whose `reason` is exactly `interrupted`.
- Use the envelope timestamp as the reliable boundary.
- If `completed_at` is a parseable ISO timestamp, use the later of it and the envelope timestamp.
- Ignore numeric or malformed `completed_at` values and retain the envelope timestamp.
- Never retain the reason, task body, or message content.

## State precedence

- A completion/final boundary wins ties with an interruption.
- An interruption wins ties with a running boundary.
- A later running boundary returns the task to Running.
- Service incident overlays continue to take precedence in presentation.

Interrupted sorts after Running and before JustCompleted. It uses a neutral gray brush, a distinct stop glyph when color-blind-safe indicators are enabled, and localized labels `已中断` / `Interrupted`.

## Side effects

Interrupted is observational only. It does not create completion sounds, service-warning sounds, or automatic-recovery candidates.
