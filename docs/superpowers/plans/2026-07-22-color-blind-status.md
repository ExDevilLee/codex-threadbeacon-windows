# Color-blind-safe status indicators implementation plan

1. Add failing tests for the display-setting contract and status-glyph mapping.
2. Persist the new boolean preference without disturbing language, theme, refresh interval, or task count.
3. Expose the preference in General settings with localized label and helper text.
4. Render fixed-width colored status glyphs for main tasks and Subagents while retaining the current dots when disabled.
5. Update version and user documentation.
6. Run all tests and a Release build, publish and install the app, then verify the real UI before a sensitive-data scan, commit, and push.
