# Monitoring Settings Window Design

Date: 2026-07-19
Status: Approved by the macOS-alignment direction

## Goal

Replace the narrow sound-only header popover with one native Windows settings
window that exposes the same general monitoring and sound preferences as macOS.
Changes persist locally and take effect immediately without restarting ThreadBeacon.

## Selected Approach

Use a single non-modal WPF `SettingsWindow`, opened by a gear button in the main
header. The window uses two tabs:

- `通用`: refresh interval and maximum visible task count;
- `提示音`: the existing global, completion, and HTTP 429/503 sound controls.

This follows the macOS settings hierarchy while retaining normal Windows window
behavior. Only one settings window may exist; reopening activates the existing
instance. Closing settings never closes the main window or stops monitoring.

The rejected alternatives are a larger header popup, which becomes fragile and
crowded, and an in-place main-window settings page, which hides the task list and
diverges from macOS navigation.

## General Settings Contract

`DisplaySettings` supports only these values:

| Preference | Values | Default |
| --- | --- | --- |
| Refresh interval | 1, 2, 5, 10 seconds | 2 seconds |
| Maximum visible tasks | 4, 8, 12, 20 | 8 tasks |

Unsupported or missing persisted values fall back independently to their defaults.
Changing either value saves immediately. A refresh-interval change updates the
current timer schedule without forcing a task refresh. A maximum-count change runs
one baseline refresh immediately so increasing the value can load additional tasks
and decreasing it can remove excess rows. This baseline refresh never replays sound
notifications.

Pausing continues to stop automatic monitoring regardless of the selected interval.
Manual refresh remains available while paused. Changing settings while paused does
not resume monitoring; the selected maximum count is applied by the one baseline
refresh, while the new interval is used only after monitoring resumes.

## Persistence

Add `IDisplaySettingsStore` and `JsonDisplaySettingsStore`, writing only
`display-settings.json` below the existing local ThreadBeacon settings directory.
The file contains version, refresh interval, and maximum task count. It contains no
task identity, title, content, Token value, Codex path, or health history.

Display preferences remain separate from:

- `settings.json`, currently owned by the always-on-top state;
- `sound-settings.json`, including its bounded notification-event history;
- the future main-window placement record.

This separation prevents independent features from overwriting fields in a shared
JSON object.

## Application State And Data Flow

`DisplaySettingsViewModel` loads, validates, saves, and exposes the supported option
lists. The same instance is shared by `MainWindow`, `MainWindowViewModel`, and the
settings window.

`MainWindowViewModel` replaces both hard-coded task limits with the current maximum:

- recent query limit = maximum visible count + current ignore-rule count;
- final `ThreadListPolicy` limit = maximum visible count.

`MainWindow` owns the `DispatcherTimer`. It observes display-setting changes,
updates `Interval`, and requests the one baseline refresh for maximum-count changes.
It also owns the single settings-window lifecycle.

## Sound Settings Migration

Move the existing controls and `SoundSettingsViewModel` into the `提示音` tab without
changing sound files, defaults, event deduplication, or notification semantics.

Match macOS enablement rules:

- the global switch disables all category controls;
- each category switch remains available when global sound is enabled;
- a category's picker and preview button require both global and category enablement;
- preview failures remain non-fatal.

The previous sound popup and its click handler are removed. The gear button is the
only settings entry in the compact header.

## Window And Layout

The settings window uses a stable 440 x 360 initial size, owner-centered startup,
normal taskbar ownership, and the shared ThreadBeacon icon and restrained palette.
It is not always-on-top merely because the main window is pinned. General controls
use labeled combo boxes; sound controls use checkboxes, combo boxes, and preview
buttons. Text must fit at the supported Windows scaling factors and minimum size.

This feature does not persist settings-window placement. Main-window placement is
the next independent feature and must not read or write settings-window geometry.

## Failure And Privacy Boundaries

- Invalid or unreadable display JSON falls back to defaults.
- Failed preference saves do not crash the App; the in-memory choice remains active
  for the current process, matching existing setting behavior.
- A failed baseline refresh leaves the previous task list and publishes the existing
  data-source health failure state.
- No network access, telemetry, Codex writes, schema probes, or new Codex reads are
  introduced beyond the user-selected task count.

## Tests And Acceptance

Automated tests cover validation/defaults, JSON round-trip and malformed input,
immediate persistence, timer interval updates, maximum-count refresh and list
behavior, pause/manual-refresh invariants, one-window lifecycle, sound-control
enablement, and XAML structure/accessibility.

Final acceptance requires:

- the gear opens one settings window with `通用` and `提示音` tabs;
- all four interval and count choices are present and persist across restart;
- changes take effect immediately and do not replay sounds;
- pause and manual refresh retain their current behavior;
- the old sound popup is absent;
- full tests, Release build, dependency vulnerability checks, runtime inspection,
  and pre-push sensitive-data review pass.
