# Task Pin and Ignore Design

## Goal

Bring the macOS `d77db4c feat(tasks): add pin and ignore controls` behavior to the Windows App without adding later macOS ideas. Users can pin a primary task, temporarily ignore it, restore ignored tasks, and keep those preferences across launches. Codex data remains read-only.

## Scope

- Right-click a primary task to pin/unpin or ignore it.
- Status priority stays above pinning; pinned tasks lead only within the same status.
- Ignoring removes any pin and hides the task immediately.
- A temporary ignore clears when that task has a `task_started` later than the ignore timestamp.
- A toolbar button appears only while ignored tasks exist and opens a compact restore popup with one-task and restore-all actions.
- Store task IDs, ignore timestamps, and the single `UntilNextTurn` rule type locally. Do not persist task titles.
- Keep the visible list at eight tasks and suppress notifications from ignored tasks.

Out of scope: permanent ignore, title/status/project rules, custom rule editing, task data writes, and changes to window-level always-on-top behavior.

## Approaches Considered

1. Filter and reorder only the current eight rows in the App. This is small, but a pinned or ignored task can fall out of the recent query and become impossible to manage reliably.
2. Load a broadly enlarged recent list. This avoids a repository API but changes the established recent-eight contract and does unnecessary rollout/log work.
3. Mirror the macOS candidate pipeline: load recent candidates, explicitly include preference IDs, then apply a pure list policy. This preserves the visible-eight contract, is independently testable, and is the selected approach.

## Core Model and Persistence

`ThreadListPreferences` contains an ordinal set of pinned task IDs and an ordinal dictionary of ignored rules. `IgnoredThreadRule` contains only the task ID, UTC ignore timestamp, and `UntilNextTurn` mode. A JSON repository stores this separately from sound and window settings under the existing local application-data directory. Missing, malformed, or older files degrade to empty preferences; failed saves do not break monitoring.

`ThreadListPolicy` is a WPF-independent pure Core component. It first removes temporary ignore rules when `LatestTaskStartedAt > IgnoredAt`, partitions candidates into visible and ignored sets, then sorts by status priority, pinned state, latest event time, and task ID. It returns updated preferences so automatic restores can be persisted.

## Data Loading

Extend `IThreadRepository` with a read-only `LoadByIds` operation and implement it with parameterized SQLite placeholders through the existing read-only connection. Archived tasks and Subagents remain excluded exactly as in the recent query.

`ThreadStatusLoader` accepts a request containing recent limit, included task IDs, and expanded task IDs. It merges recent and explicitly included records by ordinal task ID before reading rollout, titles, incidents, tokens, and requested Subagents. Missing included IDs are tolerated. Existing source-health degradation remains intact.

The App requests `8 + ignored count` recent candidates, capped safely, and explicitly includes pinned and ignored IDs. The extra recent candidates compensate for hidden rows, keeping up to eight visible tasks.

## App State and Interaction

`MainWindowViewModel` owns the candidate snapshots and preferences, while `ThreadListPolicy` produces visible and ignored rows. Preference changes update the list immediately and save locally. Refresh applies the policy before passing visible snapshots to the notification coordinator, so ignored completions and incidents do not sound.

Each task row exposes `IsPinned`, pin/unpin, and ignore commands. The WPF row context menu uses Segoe Fluent Icons and Chinese labels matching the macOS meaning. A subtle pin glyph appears before the task title when pinned.

When ignored tasks exist, an eye-slash button appears in the existing header toolbar between the window pin and sound buttons. Its popup lists the current title from in-memory candidates, falling back to the first eight task-ID characters; titles are never written to settings. Each row has a restore icon, and more than one ignored task adds a restore-all command. The popup closes when the final rule is restored or when focus moves elsewhere.

## Failure and Compatibility Behavior

- Preference load/save failure falls back or continues in memory without failing the task list.
- A missing or archived explicitly included task removes a stale pin after a successful healthy load; ignored rules remain recoverable until explicitly restored unless the macOS behavior proves the task unavailable in the candidate set.
- Database access remains `ReadOnly` with `PRAGMA query_only = ON` and parameter binding.
- Existing settings files require no migration; this feature uses a new versioned file.
- Existing expansion, two-second refresh, pause/manual refresh, title resolution, service incidents, and notification baselines remain unchanged.

## Verification

Core tests cover preference serialization, malformed-file fallback, read-only ID loading, candidate merging, sorting, visible limit, automatic restore, stale pin pruning, and ignore partitioning. App tests cover commands, immediate row updates, notification suppression, toolbar visibility, restore behavior, and refresh requests. Final verification includes the full Release test/build suite, dependency vulnerability scan, runtime right-click/restore checks, and the mandatory pre-push security audit.
