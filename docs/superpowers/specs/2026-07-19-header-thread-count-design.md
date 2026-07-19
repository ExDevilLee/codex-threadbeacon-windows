# Header Thread Count Design

Date: 2026-07-19
Status: Approved by standing feature-direction authorization

## Goal

Show how many currently displayed primary tasks are running without requiring the
user to scan every status light.

## User Experience

- Keep the `ThreadBeacon` product title unchanged.
- Keep the `Codex 任务状态` subtitle and append a compact `running/visible` count on
  the same line.
- Use monospaced digits, 11px text, and the existing secondary text color.
- The tooltip and accessibility name are
  `N 个任务正在运行，共显示 M 个任务`.
- Initial state and a successfully loaded empty list display `0/0`.
- The count is informational and is not clickable.

Examples:

- one running task among seven visible tasks: `1/7`;
- three running tasks among eight visible tasks: `3/8`;
- no visible tasks: `0/0`.

## Counting Contract

`running` is an exact count of snapshots whose derived status is
`ThreadStatus.Running`.

The numerator does not include:

- `JustCompleted`;
- `NeedsAction`;
- `Warning`;
- `Error`;
- `Idle`;
- `Unknown`.

The denominator is the number of snapshots in the current successfully loaded main
list. Subagents remain excluded by the existing repository contract.

## Refresh and Failure Behavior

- Every successful startup, automatic, manual, or resume refresh recalculates the
  label from the same snapshot batch used to reconcile the list.
- Pausing monitoring leaves the last successful label visible.
- Manual refresh while paused updates the label.
- If a refresh throws before producing a result, keep the previous label because the
  visible list is also unchanged.
- A successful degraded or empty result updates the label to match the displayed
  snapshots.

This feature does not alter completion-notification policy or refresh scheduling.

## Architecture

Add a pure application formatter:

```csharp
public sealed record ThreadCountLabel(string DisplayText, string AccessibilityLabel);

public static class ThreadCountFormatter
{
    public static ThreadCountLabel Format(IEnumerable<ThreadStatus> statuses);
}
```

`MainWindowViewModel` owns the latest immutable label. After successful list
reconciliation it formats the result statuses and raises property changes for
`ThreadCountText` and `ThreadCountAccessibilityLabel` only when the label changes.

The formatter belongs to `ThreadBeacon.App.Formatting`: it is presentation wording,
does not change Core status derivation, and has no WPF dependency.

## Layout

Replace the second subtitle `TextBlock` with a horizontal `StackPanel` containing:

- the existing `Codex 任务状态` label;
- the bound count with a six-pixel left margin.

The product title, traffic lights, and four header buttons keep their current
positions. The subtitle row remains a single line at the 480px minimum width.

## Privacy and Scope

- The count is derived in memory from already loaded main-task statuses.
- No new SQLite, rollout, session-index, network, or write operation is introduced.
- No count history or task metadata is persisted.

## Testing

- Formatting eight mixed statuses with three `Running` values returns `3/8` and the
  exact Chinese explanation.
- An empty input returns `0/0`.
- A successful refresh updates the view-model label from the reconciled snapshots.
- A thrown refresh preserves the previous label.
- Existing list, pause/resume, sound, Token, and Subagent badge tests remain green.
- Runtime inspection covers normal, paused, and 480px layouts.

## Out of Scope

- counts by warning, error, attention, or completion state;
- counting hidden or archived tasks;
- counting Subagents;
- changing task ordering or status derivation;
- historical charts or persisted metrics.
