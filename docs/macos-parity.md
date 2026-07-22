# macOS Parity Ledger

This file records the macOS reference checkpoint used for Windows feature
comparison. The roadmap describes intended Windows work; this ledger records
which macOS commits have been inspected and what they contained.

## Current Checkpoint

- Reference repository: `ExDevilLee/codex-threadbeacon-macos`
- Last analyzed macOS commit: `a25b6cb` (`v0.1.8`)
- Last analyzed on: 2026-07-23
- Windows repository commit at checkpoint: `98a012f`
- Next analysis starts at: commits after `a25b6cb`

When the macOS repository advances, inspect only commits after the recorded
checkpoint, then update this section even when no Windows change is required.

## Analysis Through `a25b6cb`

### Implemented on Windows

- Read-only task monitoring, Rename titles, status derivation, Token totals,
  refresh controls, placement persistence, pin/ignore, favorites, sounds,
  themes, language switching, About, health diagnostics, and launch at login.
- Main-task model and reasoning details with stable Token detail popovers.
- Inline Subagent expansion with active/total counts, archived-parent filtering,
  and semantic aliases derived from `agent_path`.
- HTTP 400/429/503 and other service incidents, model-capacity failures, and
  exhausted stream-disconnect detection.
- Configurable automatic recovery with guarded Windows UI Automation, local
  recovery history, and safe foreground-app restoration.
- Double-click opening of an unarchived Codex task through its task ID.
- Interrupted-turn status with neutral localized text and correct lifecycle
  precedence.
- Color-blind-safe status indicators and the macOS-aligned shield health icon.
- Optional WeChat Pay and Alipay sponsorship assets on the external support
  page; no payment UI or feature unlocks in the main window.

### Reference-Only or Platform-Specific Differences

- Windows uses WPF, Windows UI Automation, registry startup integration, and
  Windows-native window/resource APIs instead of AppKit, Accessibility APIs,
  LaunchAgents, and macOS menu-bar behavior.
- macOS distribution details, signing, notarization, Homebrew packaging, and
  App Store behavior are not part of the Windows parity target.

### Not Yet Published by macOS at This Checkpoint

- Compression history/observability remains a macOS roadmap candidate, so it is
  deliberately not implemented ahead of the reference version.

## Update Procedure

1. Fetch the macOS reference repository.
2. List commits after `Last analyzed macOS commit` and classify each as
   implemented, platform-specific, documentation-only, or pending.
3. Add the analysis result and update the checkpoint commit above.
4. Implement pending Windows behavior one feature at a time, with tests, a
   Release build, installed-EXE UI verification, and a sensitive-data scan.
5. Record the Windows commit that completed the feature beside the entry.

Do not rewrite older checkpoints; append a dated analysis entry when the
reference advances.

## Analysis Log

### 2026-07-23 - Through `a25b6cb`

- Inspected macOS commits through `v0.1.8`.
- Confirmed Windows coverage for interrupted turns, running-first ordering,
  Subagent active/total visibility, archived-parent filtering, and semantic
  `agent_path` aliases.
- Confirmed no new published macOS feature remains to implement at this
  checkpoint.
- Windows completion commit: `98a012f`.
