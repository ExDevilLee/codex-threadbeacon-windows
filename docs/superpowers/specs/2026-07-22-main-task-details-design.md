# Main Task Details Design

## Goal

Show the main task model and reasoning effort in the existing details popover while keeping the main list compact and read-only.

## Data contract

- Read `threads.model` and `threads.reasoning_effort` from the Codex SQLite database.
- Parse `model` and `effort` from valid `turn_context.payload` rollout records as a per-field fallback.
- A later blank rollout value must not erase the latest non-empty value.
- SQLite wins independently for each field; rollout fills only a missing field.
- No conversation body, prompt, response, or reasoning content is retained.

## UI

- Rename the popover to **Task details / 任务详情**.
- Show Model and Reasoning before Token details.
- Keep the info button visible when any of model, reasoning effort, or Token usage exists.
- Hide the Token section and its explanatory note when Token usage is unavailable.
- Normalize known reasoning effort values for display while preserving unknown non-empty values.

## Compatibility

The Codex database and rollout formats are observed implementation details, not a public stable contract. Existing read-only, busy, missing, and incompatible degradation behavior remains unchanged.
