# ThreadBeacon for Codex on Windows

[简体中文](README.md) | English

ThreadBeacon is a native Windows status window for monitoring primary Codex Desktop and Codex CLI tasks at a glance.

This repository is the independent Windows implementation of [ThreadBeacon for macOS](https://github.com/ExDevilLee/codex-threadbeacon-macos). It is an unofficial community project and is not affiliated with or endorsed by OpenAI. `Codex` is a trademark of its respective owner.

## Status

The project is in its Windows POC stage. A Win11 probe has verified the core local data path for the currently installed Codex version. These local formats are not a stable public API.

The first end-to-end POC is now implemented: short-lived, non-pooled, read-only SQLite connections load the 8 most recent unarchived primary threads and exclude subagents; a shared read of `session_index.jsonl` selects the last valid renamed title; each rollout read is limited to the final 2 MiB and retains only event types, timestamps, and numeric Token fields to derive `running`, `justCompleted`, `idle`, and `unknown`. A unified loader merges these sources into snapshots, and the WPF window displays status lights, titles, cumulative Token usage, and status duration with 2-second automatic refresh and manual refresh. Each source degrades safely when unavailable or incompatible.

The WPF App is connected to real local task data. A Win11 read-only concurrent-task soak ran for more than 30 minutes: 900 samples completed with no probe failures, source degradations, or App crashes, and Codex writes remained available. See the [Windows 30-minute soak record](docs/validation/2026-07-18-windows-30-minute-soak.md).

The first window enhancement is complete: the pin button in the top-right keeps ThreadBeacon above other normal windows. The selection is stored locally and restored after restart.

The middle header button temporarily pauses or resumes the 2-second automatic monitoring cycle. Manual refresh remains available while paused; resuming refreshes immediately, and every App launch starts with monitoring active. This control only affects ThreadBeacon's local read-only refresh and does not pause Codex tasks.

The info button beside cumulative Token usage shows session total, input, cached input, non-cached input, output, Reasoning, current turn, cache rate, and update time. Hover opens a transient detail popover and clicking pins it; a pinned popover remains stable across the 2-second task refresh cycle.

The speaker button configures notification sounds and provides the same Beacon, Chime, Pulse, Alert, Resolve, and Knock built-in tones as the macOS version, including preview playback. New installations default to Chime for task completion and Alert for 429/503 incidents; either notification can independently use any of the six sounds. A sound plays once only when an automatic refresh observes a new reliable `task_complete` event; multiple completions in one refresh batch are coalesced. App startup, manual refresh, and monitoring resume establish a baseline and never replay historical completions. Sound preferences and at most 256 derived event IDs are stored locally without task titles, conversation bodies, Token details, or Codex paths.

The App now also monitors HTTP 429/503 service incidents for currently visible primary tasks. Active retries appear as a yellow “Service incident” with the HTTP status and retry progress; exhausted retries become a red “Service failure.” A later HTTP 200 in the same turn or a newer rollout lifecycle event clears the stale incident. Each incident episode can play one independently configurable warning sound and shares the baseline and 256-entry local derived-ID history with completion events.

A primary task that created Subagents shows a neutral branch icon and its direct Subagent count after the title. This is a historical parent-child relationship count, not a live running count; zero reserves no space. Clicking the count expands direct children inline with `Agent alias | title`, derived status, recent activity, and cumulative Token usage. The detail button shows role, model, reasoning effort, and numeric Token fields. Child records and rollout tails are read only for visible expanded parents; collapsing stops those reads. Conversation bodies and deeper descendants are never read or displayed.

The window subtitle shows `running tasks/current visible tasks`, such as `1/7`. Only primary snapshots with the derived `Running` status contribute to the numerator, and the denominator matches the primary snapshots currently displayed. Pausing preserves the last successful count; manual refresh or monitoring resume recalculates it.

The first POC is deliberately limited to:

- Reading the 8 most recent unarchived primary threads and excluding subagents.
- Using the latest renamed title from `session_index.jsonl`.
- Deriving task status from rollout JSONL tails.
- Displaying cumulative Token usage with a numeric-only detail popover.
- Playing a configurable built-in sound for new task completions observed by automatic refresh.
- Detecting HTTP 429/503 retries and terminal failures for visible primary tasks from read-only local logs.
- Showing a non-zero historical direct-Subagent count and expanding direct children on demand.
- Showing running primary tasks over currently visible primary tasks in the subtitle.
- Refreshing every 2 seconds with a manual refresh option.
- Opening SQLite databases in read-only mode.
- Never reading conversation bodies, accessing the network, or modifying Codex data.

Other failure/warning sounds, task pin/ignore rules, Subagent alerts and Token aggregation, and the system tray remain deferred.

## Technology

- .NET 9
- WPF
- xUnit

## Repository Layout

- `src/ThreadBeacon.Core`: models, read-only data access, parsers, and status rules; no WPF dependency.
- `src/ThreadBeacon.App`: Windows UI and platform integration.
- `tests/ThreadBeacon.Core.Tests`: core behavior and compatibility tests.
- `tests/ThreadBeacon.App.Tests`: local settings and window interaction state tests.
- `tools/ThreadBeacon.Probe`: a local probe that only reports source health and thread count.
- `docs`: Windows probe and design notes.

The macOS repository is a behavioral reference only and is not a source dependency.

## Build and Run

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet run --project src/ThreadBeacon.App
dotnet run --project tools/ThreadBeacon.Probe --configuration Release
```

## App Icon

<p align="center">
  <img src="Resources/AppIcon-1024.png" width="160" alt="ThreadBeacon App icon">
</p>

The Windows App shares the `B1 Graphite / Code Beacon` icon with the macOS version: a graphite rounded square, white code braces, and a vertical red-yellow-green beacon.

- `Resources/AppIcon-1024.png`: the shared 1024px PNG master.
- `Resources/AppIcon.ico`: the Windows icon containing 16, 24, 32, 48, 64, 128, and 256px frames.

Regenerate the ICO in PowerShell:

```powershell
.\script\generate_app_icon.ps1
```

## Sound Assets

Beacon, Chime, Pulse, Alert, Resolve, and Knock are short sounds generated deterministically by the author's project scripts; they do not come from a third-party sound pack. Windows reuses the same 44.1 kHz mono 16-bit PCM WAV files as macOS, and the Release build copies them to `Resources/Sounds`.

## Privacy

Service-incident monitoring transiently parses only three allow-listed log targets and retains only the turn episode ID, HTTP status, retry progress, phase, and timestamp. It explicitly excludes transport logs that may contain request context. Conversation messages, responses, reasoning summaries, and complete requests are never read or displayed.

See [PRIVACY.md](PRIVACY.md) for the exact local data scope and processing boundaries.

## License

[MIT](LICENSE)
