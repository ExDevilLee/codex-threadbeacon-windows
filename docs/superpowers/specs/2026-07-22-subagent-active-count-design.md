# Subagent Active Count Design

## Contract

- Badge text is `active/total` for direct Subagents only.
- `active` counts only children whose rollout is confirmed `Running` by the existing 120-second freshness policy.
- `total` remains the direct relationship count from `thread_spawn_edges`.
- Archived parents never query or display active children.
- Missing or unreadable rollout data is never guessed to be active.

## Cost control

Every refresh queries only direct children updated within the running-freshness window. Only those rollout tails are parsed while a parent is collapsed. Expanded rows reuse observations parsed earlier in the same refresh, so an active child is not read twice.

## UI

- Show `0/N` when a parent has history but no currently running child.
- Hide the badge when total is zero.
- Clamp invalid active values to `0...total`.
- Localized accessible text includes both counts and the expand/collapse action.
