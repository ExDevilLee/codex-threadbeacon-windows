# ThreadBeacon for Codex on Windows

[简体中文](README.md) | English

[![GitHub Release](https://img.shields.io/github/v/release/ExDevilLee/codex-threadbeacon-windows?include_prereleases&sort=semver)](https://github.com/ExDevilLee/codex-threadbeacon-windows/releases)
![Windows 11 x64](https://img.shields.io/badge/Windows_11-x64-0078D4)
[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

ThreadBeacon is a native Windows status window for monitoring primary Codex Desktop and Codex CLI
tasks at a glance. It reduces repeated context switching while several tasks are running and works
well pinned on the desktop or placed on a dedicated portrait display.

This is an unofficial community project. It is not affiliated with or endorsed by OpenAI.
`Codex` is a trademark of its respective owner.

## A Dedicated Small-Screen Status Board

ThreadBeacon's compact list fits a portrait secondary display, including a 7-inch screen. Keep
Codex interactions on the computer, code and diffs on the main monitor, and task states continuously
visible on the small display.

![ThreadBeacon used as a Codex task status board on a portrait small display](docs/assets/readme/threadbeacon-small-display-workspace.jpg)

> AI-generated workspace concept. On-screen content illustrates the layout and workflow; refer to
> the screenshots below for the actual app UI.

## 30-Second Quick Start

Before starting, make sure that:

- You are running Windows 11 x64.
- Codex Desktop or Codex CLI is installed and has run at least one task.
- The current download is a portable technical preview, so Microsoft Defender SmartScreen may
  display a warning.

Install and launch:

1. Download `ThreadBeacon-vX.Y.Z-win-x64.zip` from
   [GitHub Releases](https://github.com/ExDevilLee/codex-threadbeacon-windows/releases).
2. Extract the entire ZIP to a stable directory. Do not run the app from inside the archive.
3. Double-click `ThreadBeacon.App.exe`. If SmartScreen displays a warning, first verify that the
   file came from this repository's Release, then select **More info** and **Run anyway**.

ThreadBeacon automatically reads recent local Codex primary tasks. No account, API token, or data
path is required. If no tasks appear or the footer reports a source problem, see
[`Troubleshooting`](docs/troubleshooting-en.md).

## Interface Preview

| Primary task status and inline Subagents | Token usage details |
| :---: | :---: |
| ![ThreadBeacon primary task status and inline Subagents](docs/assets/readme/en/threadbeacon-main.png) | ![ThreadBeacon Token usage details](docs/assets/readme/en/threadbeacon-token-details.png) |

| General Settings | About ThreadBeacon |
| :---: | :---: |
| ![ThreadBeacon General Settings](docs/assets/readme/en/threadbeacon-settings.png) | ![About ThreadBeacon](docs/assets/readme/en/threadbeacon-about.png) |

| Notification and custom sounds | Auto-recovery rules and history |
| :---: | :---: |
| ![ThreadBeacon notification and custom sounds](docs/assets/readme/en/threadbeacon-sounds.png) | ![ThreadBeacon auto-recovery rules and history](docs/assets/readme/en/threadbeacon-auto-recovery.png) |

## Core Features

### Glanceable Task States

- Refreshes every 2 seconds by default, with `1 / 2 / 5 / 10 seconds`, pause, and manual refresh
  options.
- Shows the latest renamed task title, status duration, and running tasks over visible tasks.
- Distinguishes running, just completed, interrupted, service incident, idle, and unknown states.
  Color-blind-safe shapes are enabled by default.
- The just-completed state can remain visible for `1-5 minutes`; status priority always outranks
  manual pinning.
- The window can stay above other apps and restores its display, position, and size.

### Subagent And Token Overview

- Primary tasks show direct Subagents as `running/history total`, such as `2/27`, with inline
  expansion.
- Expanded rows show the Agent name, task title, state, recent activity, model, reasoning effort,
  and Token usage.
- The main list keeps cumulative Token usage compact; Task details adds input, cached input, output,
  reasoning, current-turn usage, cache ratio, model, and reasoning effort.
- Token details also show cumulative compaction count and latest completion time. Live compacting
  status requires an optional Codex Hook explicitly installed from Settings.
- The app never reads or displays conversation bodies and does not aggregate second-level or deeper
  Subagents. See the Chinese
  [`live compaction status design`](docs/superpowers/specs/2026-07-23-compaction-hook-design.md) for
  Hook and privacy boundaries.

### Incident Monitoring And Sounds

- Reads allowlisted local structured logs for HTTP 4xx/5xx retries and terminal failures,
  exhausted reconnection failures, and explicit model-capacity incidents.
- Active retries appear as warnings and terminal incidents as errors. A confirmed incident cannot
  be overwritten by a generic completion event.
- Completion and incident notifications use separate default sounds. Either can be disabled,
  preview one of eight built-in sounds, or select a local WAV file.
- See [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) for sound sources and licenses, and the
  [`service incident monitoring design`](docs/superpowers/specs/2026-07-19-service-incident-monitoring-design.md)
  for detailed state rules.

### Optional Auto Recovery

- Auto recovery is off by default. HTTP 400, HTTP 429, HTTP 503, other HTTP failures,
  model-capacity incidents, and connection interruptions have separate rules and prompts; HTTP 503
  remains off by default.
- Sending uses the visible input field in the installed Codex App. Windows UI Automation must
  confirm one Codex window, the exact task title, an empty composer, and one send button. Any
  ambiguity fails closed.
- Each incident type defaults to `3` consecutive attempts. Settings accepts `1-20` or unlimited
  attempts. A normal completion resets the count, and an individual open circuit can be cleared.
- Local recovery records distinguish not sent, sending, sent, failed, and circuit-open results.

### Everyday Controls And Settings

- Favorite, show only favorites, pin, temporarily ignore, and restore tasks. Archived favorites
  remain visible with an explicit archived state.
- Double-click an unarchived primary task to open it in Codex App after the same unique-window,
  identity, and draft-safety checks. Navigation never types or sends text.
- Settings supports Follow System, Simplified Chinese, and English, plus System, Light, and Dark
  themes.
- Configure visible task count, refresh interval, completion retention, sounds, auto recovery, and
  launch at login.
- A compact health control reports SQLite, Rename, Rollout, and service-log data-source status.
- The app checks GitHub Releases and shows an update link without downloading or installing it.

## Download And Install

Download from [GitHub Releases](https://github.com/ExDevilLee/codex-threadbeacon-windows/releases):

```text
ThreadBeacon-vX.Y.Z-win-x64.zip
ThreadBeacon-vX.Y.Z-win-x64.exe
```

The ZIP is recommended. Extract it completely and run `ThreadBeacon.App.exe`; it includes bundled
sound assets and the Hook Bridge used by optional live compaction status. The single-file EXE
extracts the same Bridge at runtime and is suitable for portable use.

To upgrade, exit ThreadBeacon and replace all program files with the new version. Keep the JSON
settings under `%LOCALAPPDATA%\ThreadBeacon` to preserve preferences. Before uninstalling, disable
**Launch at login** in Settings, exit the app, and remove its program directory. Delete the local
data directory as well only when you also want to remove preferences and recovery records. See
[`Troubleshooting`](docs/troubleshooting-en.md) for complete steps.

## Data And Privacy

- The app reads only local Codex task SQLite, rename index, rollout tails, and three allowlisted log
  targets to derive states, Tokens, model details, and incidents.
- It does not read reasoning summaries, conversation bodies, full requests, provider URLs, or
  request IDs. It does not upload Codex data or start a network service.
- It does not directly modify Codex SQLite, session index, or rollout files; these sources are
  always accessed read-only.
- Auto recovery is off by default. The app sends a user-configured continuation prompt through the
  visible Codex composer only after the user enables the matching rule.
- Live compaction state is optional. Only explicit user opt-in allows a structured update to local
  Codex Hook configuration; unrelated Hooks are preserved and Settings can remove it.
- Update checks request only public GitHub Release metadata and include no Codex data, local paths,
  settings, or device identifier.
- See [`PRIVACY.md`](PRIVACY.md) for local persistence, recovery records, and Hook behavior.

## Known Limitations

- An unresolved task with no recent rollout events may temporarily appear as unknown even while a
  quiet tool call is still running.
- Read-only sources cannot reliably distinguish approval waiting from user-input waiting. The app
  does not infer these states from silence or conversation text.
- Codex SQLite, session index, rollout, and log formats are not stable public APIs and may require
  adaptation after Codex updates.
- Auto recovery and double-click navigation require the installed Codex Desktop app. They refuse to
  act when the window, title, or composer cannot be uniquely confirmed.
- This is a portable technical preview without an installer or system tray. SmartScreen may appear
  on first launch.

See [`Troubleshooting`](docs/troubleshooting-en.md) for explanations and recovery steps.

## Development And Feedback

Build locally:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet run --project src/ThreadBeacon.App
```

Create a self-contained `win-x64` release package:

```powershell
.\script\publish_release.ps1
```

- Changes: [`CHANGELOG.md`](CHANGELOG.md)
- Future candidates: [`ROADMAP.md`](ROADMAP.md)
- macOS parity: [`docs/macos-parity.md`](docs/macos-parity.md)
- Contributing: [`CONTRIBUTING.md`](CONTRIBUTING.md)
- Ordinary reports: use GitHub Issue Forms and never upload task titles, conversations, databases,
  or local paths.
- Security reports: see [`SECURITY.md`](SECURITY.md).

## App Icon

<img src="Resources/AppIcon-1024.png" alt="ThreadBeacon app icon" width="160">

The `B1 Graphite / Code Beacon` icon uses a graphite tile, white code braces, and vertically stacked
red, amber, and green lights. The Windows App uses the multi-size
[`Resources/AppIcon.ico`](Resources/AppIcon.ico).

## Platform Repositories

- macOS: [`ExDevilLee/codex-threadbeacon-macos`](https://github.com/ExDevilLee/codex-threadbeacon-macos)
- Windows: [`ExDevilLee/codex-threadbeacon-windows`](https://github.com/ExDevilLee/codex-threadbeacon-windows)

Each platform has an independent repository, implementation, and release process. They share state
semantics, feature contracts, and test scenarios without a source dependency.
