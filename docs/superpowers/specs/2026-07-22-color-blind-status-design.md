# Color-blind-safe status indicators

## Goal

Add an optional status presentation that distinguishes task states by color, shape, and text. The option is off by default, applies immediately to main tasks and Subagents, and persists with the other display settings.

## Behavior

- Keep the existing semantic color and localized status label.
- Use a fixed 18 x 18 status slot so toggling the option does not shift columns.
- Map Error, NeedsAction, Warning, Running, JustCompleted, Idle, and Unknown to distinct system glyphs.
- Treat archived tasks as Idle for presentation.
- Use the localized status label as the glyph tooltip and automation name.
- Do not change status inference, filtering, health indicators, or the brand icon.

## Verification

Unit tests cover defaults, persistence, setting preservation, and unique status mappings. UI verification covers the setting off/on, main and Subagent rows, Chinese and English, light and dark themes, and the minimum main-window width.
