# Main Window Placement Design

## Goal

Remember the main ThreadBeacon window's display, position, and size across launches while
keeping recovery safe when the saved display or geometry is no longer usable.

## Behavior

- Persist only the main window. The settings window remains centered on its owner.
- Store a display identifier and the main window's normal-state pixel bounds.
- Restore an unchanged saved rectangle when its display is still connected and the rectangle
  fits that display's working area.
- If the saved display is missing, use the primary display, then the first available display.
- Clamp oversized or off-screen rectangles into the chosen display's working area.
- Keep the existing centered startup when there is no valid saved placement.
- Ignore minimized and maximized bounds so transient window states never replace the last
  normal rectangle.
- Match the macOS scope by not reacting to display hot-plug changes while the App is running.

## Storage And Privacy

Placement is stored separately at
`%LOCALAPPDATA%\ThreadBeacon\window-placement.json`. The record contains only a Windows
display device name and numeric window geometry. It contains no Codex path, task identifier,
title, conversation content, or Token data. Missing, malformed, or unwritable storage degrades
to centered startup without affecting monitoring.

## Windows Integration

Use Win32 window and monitor rectangles in physical pixels. This avoids mixing WPF
device-independent coordinates across monitors with different DPI scales. A pure resolver
selects the target display and clamps geometry; a native adapter enumerates monitor working
areas, captures the current window rectangle, and applies the resolved rectangle. The main
window invokes this adapter after its native handle exists and saves only while its state is
normal.

## Test Contract

- Preserve a visible placement on its saved display.
- Fall back to the primary display when the saved display is disconnected.
- Fit oversized and off-screen geometry inside the chosen working area.
- Return no placement when no valid display exists.
- Round-trip valid JSON and safely default on missing, malformed, or unwritable files.
- Verify the main window wires native-handle restore and normal-state move/resize saving while
  the settings window remains `CenterOwner`.
