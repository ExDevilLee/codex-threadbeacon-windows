# Main Window Placement Implementation Plan

## Task 1: Pure placement model, resolver, and JSON store

- Add failing tests for saved-display restore, primary-display fallback, bounds clamping, empty
  display lists, store round-trip, malformed input, and failed writes.
- Add immutable placement/display geometry models and a resolver with no WPF dependency.
- Add a dedicated JSON store rooted at `%LOCALAPPDATA%\ThreadBeacon`.
- Run focused and full App tests, then commit.

## Task 2: Native Windows placement adapter

- Add failing coordinator tests with a fake platform for restore, resolved-placement rewrite,
  missing placement, and capture persistence.
- Add a Win32 adapter using monitor working areas, window rectangles, and `SetWindowPos`.
- Keep all native failures non-fatal and avoid Codex or network access.
- Run focused and full App tests, then commit.

## Task 3: Main window lifecycle integration

- Add source/XAML tests for centered fallback, native-handle restore, and normal-state-only saves.
- Wire the placement coordinator only to `MainWindow`; do not attach it to `SettingsWindow`.
- Update bilingual README, roadmap, and privacy documentation.
- Run all Release tests and build, scan dependencies for known vulnerabilities, audit the full
  diff for secrets and private data, push, and verify remote parity.
