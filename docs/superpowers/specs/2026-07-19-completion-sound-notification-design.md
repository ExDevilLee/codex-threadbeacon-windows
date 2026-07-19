# Task Completion Sound Notification Design

Date: 2026-07-19
Status: Approved by standing feature-direction authorization

## Goal

Add a Windows completion sound that follows the established macOS behavior without
changing the Codex data source or the existing two-second refresh contract.

The first sound milestone responds only to reliable `task_complete` rollout events.
It does not infer warning, failure, attention, rate-limit, or service states from task
text.

## User Experience

- Add a speaker button after the pin button and before pause/resume.
- Clicking the speaker opens a compact settings popover.
- The popover contains:
  - a global `Enable sounds` toggle;
  - a `Task completion` toggle;
  - a completion tone selector with Beacon, Chime, and Pulse;
  - a `Preview` command.
- Sounds are enabled by default, completion notifications are enabled by default,
  and Beacon is the default tone.
- The popover closes when the user clicks elsewhere.
- The three original WAV files from the macOS project are bundled with the Windows
  app so both versions share the same sound identity.

The visible Windows labels remain Chinese and align with the macOS layout. English
names in this document identify code concepts only.

## Notification Rules

Each completion is represented by a stable event ID:

`done:<thread-id>:<completion-timestamp-milliseconds>`

The tracker keeps at most the latest 256 IDs. It records all newly observed IDs but
returns at most one notification for a refresh batch, so several tasks completing
between refreshes produce one sound rather than overlapping sounds.

Refreshes have two policies:

- `Baseline`: record unseen completion IDs without playing a sound.
- `Notify`: record unseen completion IDs and request one sound when at least one new
  completion exists.

Policy mapping:

- application startup: Baseline;
- manual refresh: Baseline;
- refresh performed when monitoring resumes: Baseline;
- automatic two-second refresh: Notify;
- manual refresh while paused: Baseline.

This prevents historical completions from being replayed on startup, after a pause,
or when the user explicitly refreshes.

## Architecture

### Core

`ThreadBeacon.Core` owns the policy enum, notification event model, and pure
completion tracker. The tracker consumes `ThreadSnapshot.CompletionEventAt` and has
no dependency on WPF, audio APIs, or settings storage.

### Application

The WPF application owns:

- sound preferences and bounded seen-event history;
- JSON persistence under `%LOCALAPPDATA%\ThreadBeacon`;
- a coordinator that observes successful refresh results and requests playback;
- WAV resource lookup and Windows playback;
- the settings popover and preview command.

Sound settings are stored separately from the existing window-pin settings. This
keeps this feature isolated and prevents either settings owner from overwriting the
other owner's state.

The view model accepts an injected notification observer for deterministic tests.
The window assigns Baseline or Notify policy based on the refresh source.

## Failure and Privacy Behavior

- Missing, corrupt, or temporarily inaccessible sound settings fall back to defaults.
- Failure to save settings does not block interaction or refresh.
- Missing WAV resources or audio device/playback errors do not affect the task list.
- Only preferences and derived completion event IDs are persisted. Task titles,
  rollout contents, token details, and Codex paths are not copied into settings.
- SQLite and rollout access remain read-only. The feature performs no network access
  and does not modify Codex data.

## Testing

Core tests cover event identity, baseline versus notify, duplicate suppression,
batch coalescing, ordering, and the 256-ID bound.

Application tests cover settings defaults and persistence, coordinator filtering,
playback failure isolation, preview behavior, and refresh-policy forwarding.

Runtime verification covers successful Release build, bundled WAV presence, speaker
popover behavior, preference persistence, preview playback, pause/resume behavior,
and layout at the existing minimum window width.

## Out of Scope

- warning, failure, attention, 429, or 503 sounds;
- per-task mute or custom sound files;
- volume controls;
- Windows notifications or tray integration;
- reading task body text to classify events.
