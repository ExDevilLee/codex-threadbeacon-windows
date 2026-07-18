# Token Detail Popover Design

## Goal

Complete the Windows Token usage experience by adding the same compact detail
popover already validated in the macOS app. Preserve the current read-only data
boundary and keep the Windows and macOS information hierarchy, control order,
and interaction behavior aligned.

Before implementation, run the existing Windows POC as a 30-minute soak test
while Codex is writing. Record task-source health, visible task count, status
counts, and rollout health without recording titles or conversation content.

## macOS Reference

The Windows behavior is derived from these macOS commits:

- `10f1783`: define the compact usage overview.
- `5dab150`: parse cumulative rollout usage.
- `daa9869`: carry usage into task snapshots.
- `a9d0145`: format compact Token values.
- `acdc32a`: add the compact Token detail popover.

Windows already implements the parsing, snapshot, and compact total portions.
This design completes the detail interaction without introducing a product
capability that is absent from macOS.

## UI Parity Rules

- Keep the cumulative Token value in its current right-side column.
- Add a small info icon immediately after the value, matching the macOS order.
- Use a secondary color in the resting state and a familiar Windows system info
  glyph.
- Show the detail surface after a 300 ms pointer hover.
- Dismiss an unpinned detail surface 150 ms after the pointer leaves both the
  trigger and surface.
- Clicking the info button keeps the surface open; clicking it again or clicking
  outside closes it.
- Keep the detail surface 270 device-independent pixels wide and align it to the
  right side of the triggering row.
- Use native WPF focus, shadow, border, and keyboard behavior. Do not imitate
  macOS chrome when Windows has an equivalent native convention.

## Detail Content

Display these rows in this exact order:

1. Session total
2. Input
3. Cached input
4. Uncached input
5. Output
6. Reasoning
7. Current turn
8. Cache rate
9. Updated time

Use compact monospaced numbers. Prefix a known current-turn value with `+`.
Show an em dash for unavailable values. Include the same explanatory note as
macOS: cached input is included in input, and Reasoning is included in output.

## Architecture

### Formatting

Extract Token display formatting from `ThreadRowViewModel` into a focused app
formatter. The formatter produces compact counts, percentages, current-turn
text, and local-time text. It has no WPF dependency and is covered by app tests.

### Detail Model

Add a Token detail view model that maps `TokenUsageSnapshot` into the nine
display rows and explanatory note. Missing cumulative or current-turn data must
not prevent the surface from opening.

### Popover Control

Add a focused WPF Token info control that owns the info button, popup, hover
timers, click-to-pin state, focus behavior, and accessibility name. The control
receives Token detail data through binding and does not read Codex data itself.

### Stable Row Identity

The current refresh path clears and recreates every task row every two seconds.
That would destroy an open popup. Change row refresh to reconcile rows by stable
thread ID and update existing row view models in place. Remove rows that are no
longer visible and insert or move rows to match the latest loader order.

`ThreadBeacon.Core` remains unchanged unless a test exposes a parsing defect.

## Data Flow

1. `ThreadStatusLoader` returns the same `TokenUsageSnapshot` already used for
   the compact total.
2. `MainWindowViewModel` reconciles the refreshed snapshots with existing rows.
3. `ThreadRowViewModel` updates its display fields and Token detail model.
4. The Token info control opens from the row's current detail model.
5. Subsequent two-second refreshes update values without replacing the row or
   closing a click-pinned popup.

## Failure Handling

- No Token snapshot: show the existing dash and do not show the info button.
- Total-only SQLite fallback: open the surface, show the total, and use dashes
  for unavailable breakdown fields.
- Invalid counters: preserve the existing parser behavior and show unavailable
  fields rather than guessing.
- Popup or hover state failures must not interrupt task refresh.
- A source health downgrade updates the list normally and does not retain stale
  Token values for a task that is still present.

## Testing

Automated tests cover:

- compact count boundaries;
- cache percentage and current-turn formatting;
- missing breakdown fields;
- the exact detail row order and labels;
- row reconciliation preserving object identity for an existing thread;
- row insertion, removal, and ordering after refresh;
- updated Token values reaching an existing row.

Manual UI verification compares Windows with the macOS source behavior:

- info icon placement and spacing;
- hover delay and delayed dismissal;
- click-to-pin and outside-click dismissal;
- popup content order, width, alignment, and missing-value display;
- popup remaining open through multiple two-second refreshes;
- keyboard focus and tooltip accessibility;
- no text overlap at the minimum 480-pixel window width.

## 30-Minute Soak Gate

The soak test runs the real Windows app and privacy-safe probe while Codex is
active. Sample every two seconds and summarize every minute. Acceptance requires:

- no process crash or unhandled exception;
- no SQLite write blocking attributable to ThreadBeacon;
- stable read-only source health aside from documented transient downgrades;
- task count, renamed titles, status transitions, and Token totals remaining
  consistent with the visible Codex sessions;
- no titles or conversation content written to the soak log.

If the machine does not have multiple active tasks during the run, record the
run as a partial soak and leave the concurrent-task acceptance item open.

## Out of Scope

- Token cost estimates or historical charts.
- Subagent Token aggregation.
- Additional Token data sources.
- Configurable columns or popup fields.
- Pause/resume monitoring, sounds, task pin/ignore, or other later macOS
  features.
