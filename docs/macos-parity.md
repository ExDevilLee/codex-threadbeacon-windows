# macOS Parity Ledger

This file records the macOS reference checkpoint used for Windows feature
comparison. The roadmap describes intended Windows work; this ledger records
which macOS commits have been inspected and what they contained.

## Current Checkpoint

- Reference repository: `ExDevilLee/codex-threadbeacon-macos`
- Last analyzed macOS commit: `d18a18c` (`origin/main` after `v0.1.10`)
- Last analyzed on: 2026-07-23
- Windows repository commit at checkpoint: `6c2fb2b`
- Next analysis starts at: commits after `d18a18c`

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
  the opt-in live Hook status is implemented in `e48a057`.
- **Confirmed foreground recovery** (`648719f`): automatic recovery may proceed
  without navigating when the already-frontmost Codex task is the exact
  confirmed target and its composer is empty. Drafts and ambiguous composer
  states still fail closed. Recovery results also expose stable privacy-safe
  diagnostic codes. Windows implementation is complete in `73e5796`.

### Reference-Only or Not Yet Published by macOS at This Checkpoint

- Compression percentage, estimated remaining time, and live progress are not
  provided by macOS and should not be invented on Windows.

## Analysis Through `d18a18c`

### Newly Pending on Windows

- **Safe foreground navigation** (`c85db15`): when Codex is frontmost but the
  failed task is not currently selected, unattended recovery may deep-link to
  it only if the single readable source composer is empty. Windows
  implementation is complete in `a5b9019`.
- **Configurable completed-state retention** (`c5d6667`): Settings offers a
  persisted `1-5 minute` picker for the `Just completed` state. It applies to
  primary tasks and Subagents, and changing it refreshes the baseline without
  replaying completion notifications. Windows currently uses a fixed 60-second
  retention and exposes no setting.
- **Colon-formatted HTTP failures** (`3cea937`): incident parsing and the
  read-only SQLite filter accept final errors such as `last status: 429`.
  Windows implementation is complete in `9f8b93a`, with a digit-constrained
  SQL prefilter that preserves the strict read-only event boundary.
- **Accessible status symbols enabled by default** (`c227ac8`): new installs
  and settings files without an explicit value enable color-blind-safe status
  shapes; an explicitly saved disabled value remains disabled. Windows already
  implements the setting and shapes, but still defaults the option to off.
- **Configurable automatic-recovery circuit breaker** (`d713304`): each task
  and incident type tracks distinct recovery episodes, defaults to stopping
  after three attempts, supports a per-rule enabled flag and limit from 1 to
  20, records an `open circuit` history state, lists currently open circuits,
  supports manual reset, and resets after a newer confirmed completion.
  Windows implementation is complete in `ced5de6`.

### Documentation, Release, or Merge-Only Changes

- `d9de282`, `59433bc`, `880cee9`, and `20b9b98` are design or implementation
  planning commits for the features above.
- `32a5814` is the completed-retention merge commit.
- `6defb27` and `a847ade` prepare macOS releases `v0.1.9` and `v0.1.10`.
- `c7b4ef4`, `ca2d246`, `6ecf15c`, and `d18a18c` update screenshots, roadmap,
  project status, and README structure without adding another runtime feature.

### Recommended Windows Order

1. Add colon-formatted HTTP failure detection because it is a narrow parser
   compatibility fix and directly affects incident and recovery accuracy.
2. Add the automatic-recovery circuit breaker before expanding unattended
   navigation, limiting repeated sends if multiple failure episodes occur.
3. Add guarded foreground navigation with draft, ambiguity, focus-change, and
   target-identity UI tests.
4. Add the completed-state retention picker and baseline refresh behavior.
5. Enable accessible status symbols by default while preserving explicit user
   choices.

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
  the opt-in Hook phase is complete in `e48a057`; the foreground recovery
  refinement remains pending.

### 2026-07-23 - Windows compression Hook completion

- Added an opt-in, user-level `PreCompact`/`PostCompact` Hook bridge with
  structured configuration merge, backup, selective removal, and fail-closed
  handling for unknown or externally modified configurations.
- Added a privacy-minimized local activity marker and localized live
  `Compacting` status without storing prompts, summaries, Reasoning, transcript
  paths, or other Hook payload fields.
- Verified the packaged bridge lifecycle, installed Release UI in Chinese and
  English, and the primary-row transition into and out of the compacting state.
- Windows completion commit: `e48a057`.

### 2026-07-23 - Confirmed foreground recovery completion

- Implemented the macOS `648719f` behavior on Windows: an exact, unique,
  frontmost Codex task with one readable empty composer can reuse the current
  composer without navigation.
- Drafts, ambiguous targets, unreadable composers, changed runtime IDs, and
  focus changes fail closed. Interactive task opening remains separate from
  unattended recovery.
- Added allowlisted, bounded diagnostic codes to recovery history and sender
  results without persisting UI text, prompts, or paths.
- Windows completion commit: `73e5796`.

### 2026-07-23 - Through `d18a18c`

- Inspected macOS commits `c85db15` through `d18a18c` after the previous
  `648719f` checkpoint.
- Found five Windows deltas: guarded foreground navigation to an unselected
  task, configurable completed-state retention, colon-formatted HTTP failure
  parsing, color-blind-safe symbols enabled by default, and a configurable
  automatic-recovery circuit breaker.
- Classified the remaining commits as design, merge, release, screenshot,
  roadmap, project-status, or README-only changes.
- Advanced the next incremental comparison point to commits after `d18a18c`.

### 2026-07-23 - Colon-formatted HTTP completion

- Added `status: NNN` parsing and strict read-only SQLite prefilter support.
- Added positive parser/repository coverage and a negative SQL-boundary fixture.
- Windows completion commit: `9f8b93a`.

### 2026-07-23 - Automatic-recovery circuit-breaker completion

- Added a persisted per-task/per-incident circuit breaker that counts distinct
  recovery episodes and defaults to stopping after three attempts.
- Added per-rule enablement and 1-20 limits, a localized open-circuit list,
  manual reset, and automatic reset after a newer confirmed completion.
- Preflight selection and focus failures do not consume attempts; counting starts
  only after the exact target composer is selected and focused, before typing.
- Circuit storage excludes titles, prompts, rollout paths, composer content, UI
  trees, and raw errors, and degrades safely for duplicate or malformed state.
- Windows completion commit: `ced5de6`.

### 2026-07-23 - Safe foreground navigation completion

- Allowed unattended deep-link navigation away from a frontmost, unconfirmed
  Codex task only when its single readable source composer is empty.
- Kept source drafts, missing or multiple composers, and unreadable values
  fail-closed, while retaining all target identity, changed-composer, empty,
  focus, and unique-send-button checks after navigation.
- Windows completion commit: `a5b9019`.
