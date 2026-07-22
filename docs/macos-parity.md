# macOS Parity Ledger

This file records the macOS reference checkpoint used for Windows feature
comparison. The roadmap describes intended Windows work; this ledger records
which macOS commits have been inspected and what they contained.

## Current Checkpoint

- Reference repository: `ExDevilLee/codex-threadbeacon-macos`
- Last analyzed macOS commit: `648719f` (`origin/main` after `v0.1.8`)
- Last analyzed on: 2026-07-23
- Windows repository commit at checkpoint: `345edc4`
- Next analysis starts at: commits after `648719f`

When the macOS repository advances, inspect only commits after the recorded
checkpoint, then update this section even when no Windows change is required.

## Analysis Through `648719f`

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

### Newly Pending on Windows

- **Compression observability** (`8b4b441` through `50a6d7e`): macOS now shows
  historical completion count and last-completed time in Token details. An
  opt-in, user-level Codex Hook bridge can additionally show a live
  `Compacting` phase on the primary task row. The Hook uses structured JSON
  merge, backs up existing configuration, removes only ThreadBeacon entries on
  uninstall, and keeps conversation text, summaries, Reasoning, paths, and
  transcripts out of storage. Windows history is implemented in `663da08`;
  live Hook status is still pending.
- **Confirmed foreground recovery** (`648719f`): automatic recovery may proceed
  without navigating when the already-frontmost Codex task is the exact
  confirmed target and its composer is empty. Drafts and ambiguous composer
  states still fail closed. Recovery results also expose stable privacy-safe
  diagnostic codes. Windows currently keeps the stricter frontmost stop policy.

### Reference-Only or Not Yet Published by macOS at This Checkpoint

- Compression percentage, estimated remaining time, and live progress are not
  provided by macOS and should not be invented on Windows.

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

### 2026-07-23 - Through `648719f`

- Inspected macOS commits `2ad5d2b` through `648719f`.
- Found new compression observability: rollout history plus opt-in Codex
  `PreCompact`/`PostCompact` Hook lifecycle tracking.
- Found a recovery safety refinement: allow confirmed frontmost target recovery
  only with an empty composer, and add stable diagnostic codes.
- Windows implementation status: compression history is complete in `663da08`;
  the opt-in Hook phase and foreground recovery refinement remain pending.
