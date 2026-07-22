# Subagent Semantic Alias Design

## Goal

Prefer the meaningful task name stored in a Subagent's `agent_path` over its generated nickname, while retaining the existing fallback for older records.

## Data and compatibility

- Read only the `agent_path` metadata column from the local Codex state database.
- Detect the column before querying it. Databases without the column remain healthy and return a null path.
- Carry the path only through the in-memory Subagent record and snapshot; do not persist it in ThreadBeacon settings or logs.

## Presentation

- Take the final non-empty path component.
- Split snake_case and kebab-case into words and uppercase the first character.
- Prefer the semantic name, then fall back to the normalized nickname.
- Hide an alias that exactly equals the displayed title.
- Give the alias enough bounded width to remain useful in a narrow window without hiding the title.
