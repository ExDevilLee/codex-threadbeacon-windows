# Favorites and Archived Watchlist Design

## Goal

Bring the behavior of macOS commit `e71376e feat(favorites): add archived task watchlist` to the Windows App without implementing later archive-management ideas. Users can favorite primary tasks, persist a favorites-only filter, and continue seeing favorite tasks after Codex archives them.

## Scope

- Add `favoriteThreadIds` and `showsFavoritesOnly` to the existing task-list preferences.
- Right-click a primary row to favorite or unfavorite it independently of pinning and ignoring.
- Add a star toolbar button before the window always-on-top button. An outline star means all tasks; a filled accent star means favorites only.
- Show a filled gold star on favorite rows and a neutral archive glyph on archived favorite rows.
- In favorites-only mode, show only favorite candidates. Favorites do not otherwise alter status/pin/recency sorting.
- Load favorite IDs through a separate read-only repository path that may include archived primary tasks.
- Archived favorites use the neutral `Archived` label, keep available rename titles and Token data, and never expose running/completed/incident state or emit completion/incident sounds.
- Keep missing favorite IDs in preferences so the watchlist remains durable across temporary data unavailability.

Out of scope: unarchive actions, direct Codex writes, batch favorite management, custom favorite ordering, automatic pinning, project grouping, favorite counts, or new settings pages.

## Approaches Considered

1. Extend the existing candidate pipeline with a distinct archived-capable favorite load. This reuses list policy, notification filtering, and refresh health behavior while preserving the recent-eight query. Selected.
2. Build a separate favorites page and loader. This separates the view but duplicates status, title, Token, Subagent, and notification logic and would diverge from macOS.
3. Include archived rows in every recent query. This is simpler at the repository layer but changes the established recent-eight contract and can crowd normal tasks out of the default list.

## Models and Persistence

`ThreadListPreferences` gains an ordinal favorite-ID set and a Boolean favorites-only filter. Existing versioned JSON remains compatible: absent fields normalize to an empty set and `false`. The store continues to persist only stable task IDs, ignore timestamps, rule types, and the filter flag; it never stores titles or Codex paths.

`ThreadRecord` and `ThreadSnapshot` gain `IsArchived`, defaulting to `false` so existing callers and fixtures remain compatible. Favorites are never pruned merely because a load does not return them.

`ThreadListPolicy` applies automatic ignore restoration first, builds the ignored list as before, optionally filters candidates by favorite IDs, then applies the existing status, pin, event-time, and ordinal-ID ordering. Favorites do not change sort order.

## Read-Only Data Flow

`ThreadLoadRequest` adds `FavoriteThreadIds`. `IThreadRepository` adds `LoadByIdsIncludingArchived`; its SQLite implementation reuses the existing short-lived, non-pooled, `Mode=ReadOnly`, `PRAGMA query_only = ON` connection and parameter binding. Only this favorite-specific path omits the archive filter; recent and ordinary included-ID reads continue excluding archived rows and Subagents.

All thread queries return the archive column. `ThreadStatusLoader` merges recent, ordinary included, and favorite records by ordinal ID. It queries service incidents only for non-archived candidates. Archived favorites may still read the rollout tail for available Token data, but the resulting snapshot forces neutral idle state internally, uses the database update time, clears task-start/completion/incident fields, and sets `IsArchived = true`.

## App State and UI

`MainWindowViewModel` includes favorite IDs in every refresh request, exposes `ShowsFavoritesOnly`, and provides toggle-favorite and toggle-filter commands. Both operations update the current candidate list immediately and persist preferences. The notification observer receives only policy-visible snapshots; archived snapshots carry no notification events even in the all-tasks view.

Each row exposes `IsFavorite`, `IsArchived`, `FavoriteCommandLabel`, and a favorite command. The row context menu order is favorite, pin, separator, ignore. The title line shows pin, favorite, and archive glyphs before the title. Archived rows display `已归档` instead of the derived status label and use the existing neutral idle color.

The star toolbar button sits before window always-on-top, matching macOS. Its tooltip and accessible name switch between `仅显示收藏` and `显示全部任务`. When the filtered list is empty, the existing empty panel switches to `暂无收藏任务` and a star glyph; all-tasks mode keeps the existing empty state.

## Failure and Compatibility Behavior

- Missing or malformed preference JSON loads existing empty defaults.
- Older preference JSON preserves pins and ignores while defaulting favorites to empty and the filter to off.
- Favorite-load degradation does not discard healthy recent tasks; source health remains visible through the existing degraded-state UI.
- A missing favorite stays stored and can reappear when Codex data becomes available again.
- Archived favorites never query 429/503 logs, never appear as running/completed, and never produce notification candidates.
- SQLite and Codex files remain strictly read-only; only the App-owned preference JSON changes.

## Verification

Core tests cover backward-compatible preferences, favorites filtering without reorder, archived-capable parameterized loading, archive flags, loader merge precedence, active-only incident queries, and archived notification suppression. App tests cover commands, immediate filter/favorite behavior, persistence, toolbar labels/state, row glyph/status presentation, and favorites empty state. Final checks include the full Release test/build suite, dependency vulnerability scan, runtime right-click/filter/archived presentation checks where local data permits, and the mandatory pre-push security audit.
