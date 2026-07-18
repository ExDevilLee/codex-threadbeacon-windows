# Window Always-on-Top Design

## Goal

Add a window-level always-on-top control to ThreadBeacon for Windows. Match the
macOS app's pin icon, placement, active state, tooltip wording, default state,
and persistence behavior. This feature does not include pinning individual
tasks.

## User Experience

- Add a pin icon button to the right side of the header, immediately before the
  existing refresh button.
- When the window is not pinned, show an outline pin in the secondary text
  color with the tooltip `钉在最前面`.
- When the window is pinned, show a filled pin in the app accent color with the
  tooltip `取消钉住`.
- Clicking the button immediately updates both the window level and the visual
  state.
- The default for a first launch is not pinned.
- The selected state is restored after the app restarts.

## Architecture

The WPF window uses its native `Topmost` property. No Win32 interop is needed.

The app project owns a small settings abstraction and JSON implementation. The
setting is stored at:

`%LOCALAPPDATA%\ThreadBeacon\settings.json`

The file contains a versioned object with an `isWindowPinned` Boolean. A
versioned object leaves room for future app preferences without coupling the
feature to a framework-generated settings class.

`ThreadBeacon.Core` remains independent from WPF and app preferences.

## Data Flow

1. During window initialization, the app loads settings.
2. Missing or invalid settings resolve to `isWindowPinned = false`.
3. The resolved value initializes the window's `Topmost` property and the pin
   button's visual state.
4. A pin-button click toggles the current value, applies it to `Topmost`, and
   saves the complete settings object.
5. A save failure does not undo the active window state or interrupt task
   monitoring.

## Failure Handling

- A missing settings file is normal and uses defaults.
- Invalid JSON, inaccessible paths, and read errors use defaults without
  preventing the app from starting.
- Directory creation or write errors leave the in-memory selection active for
  the current run.
- Settings failures must not affect Codex data loading or the two-second refresh
  timer.

## Testing

Automated tests cover:

- default settings when the file is absent;
- saving and restoring the pinned state;
- invalid JSON falling back to defaults;
- write failures not changing the in-memory window state.

Manual verification covers:

- icon placement immediately before refresh;
- outline/filled icon and tooltip changes;
- the window remaining above another normal desktop window;
- unpinning restoring normal window stacking;
- state restoration after restarting the app.

## Out of Scope

- Pinning, favoriting, or reordering individual tasks.
- System tray behavior.
- Global hotkeys.
- Forcing ThreadBeacon above secure desktop, full-screen exclusive apps, or
  other system-level topmost windows.
