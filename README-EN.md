# ThreadBeacon for Codex on Windows

[简体中文](README.md) | English

ThreadBeacon is a native Windows status window for monitoring primary Codex Desktop and Codex CLI tasks at a glance.

This repository is the independent Windows implementation of [ThreadBeacon for macOS](https://github.com/ExDevilLee/codex-threadbeacon-macos). It is an unofficial community project and is not affiliated with or endorsed by OpenAI. `Codex` is a trademark of its respective owner.

## Status

The project is in its Windows POC stage. A Win11 probe has verified the core local data path for the currently installed Codex version. These local formats are not a stable public API.

The first end-to-end POC is now implemented: short-lived, non-pooled, read-only SQLite connections load recent unarchived primary threads and exclude subagents; a shared read of `session_index.jsonl` selects the last valid renamed title; each rollout read is limited to the final 2 MiB and retains only event types, timestamps, and numeric Token fields to derive `running`, `justCompleted`, `idle`, and `unknown`. A unified loader merges these sources into snapshots, and the WPF window displays status lights, titles, cumulative Token usage, and status duration. It defaults to 8 tasks and a 2-second automatic refresh interval, with manual refresh also available. Each source degrades safely when unavailable or incompatible.

The WPF App is connected to real local task data. A Win11 read-only concurrent-task soak ran for more than 30 minutes: 900 samples completed with no probe failures, source degradations, or App crashes, and Codex writes remained available. See the [Windows 30-minute soak record](docs/validation/2026-07-18-windows-30-minute-soak.md).

An always-visible data-source health entry now sits at the bottom-right of the window. Its popover reports the task database, Rename index, rollout, and service-log sources, aggregate rollout read successes/failures, and the last successful refresh time. Optional-source failures keep the main list running in a degraded state; a task-database failure retains the previous successful list. Diagnostics keep only fixed status categories, counts, and timestamps in memory and never display paths, task IDs, titles, or raw errors.

The first window enhancement is complete: the pin button in the top-right keeps ThreadBeacon above other normal windows. The selection is stored locally and restored after restart.

The main window remembers its last display, position, and size across launches. If that display is disconnected, the window falls back to the primary display; oversized or off-screen geometry is constrained to the current working area. The settings window has no independent saved placement and remains centered on its owner. Matching the current macOS scope, display hot-plug changes are not handled while the App is running and there is no explicit display picker.

Right-click a primary task to pin or ignore it. Status priority always outranks task pinning, while pinned tasks lead within the same status; a normal ignore rule clears automatically when the task starts a newer turn. When ignored tasks exist, a header button restores one task or all tasks. These local rules store only task IDs, ignore timestamps, and the rule type, never titles, and do not modify Codex data. Task pinning is independent of window always-on-top.

Right-clicking also favorites a primary task independently of pin and ignore. The header star switches between all tasks and favorites only, and persists the filter with the favorite task IDs locally. Favorites do not alter the existing status, pin, or recency order. If Codex archives a favorite, it remains in the watchlist with a neutral `Archived` state while retaining any available renamed title and Token data. Archived favorites do not query 429/503 logs or emit completion or incident sounds.

The middle header button temporarily pauses or resumes automatic monitoring. Manual refresh remains available while paused; resuming refreshes immediately, and every App launch starts with monitoring active. This control only affects ThreadBeacon's local read-only refresh and does not pause Codex tasks.

The info button beside cumulative Token usage shows session total, input, cached input, non-cached input, output, Reasoning, current turn, cache rate, and update time. Hover opens a transient detail popover and clicking pins it; a pinned popover remains stable across automatic task refreshes.

The gear button opens a separate settings window. Its General tab offers 1, 2, 5, or 10-second refresh intervals, maximum task counts of 4, 8, 12, or 20, and a Launch at login switch; changes are saved and applied immediately without altering the paused state. Launch at login writes only the current-user `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ThreadBeacon` value and removes it when disabled; an unavailable registry is handled as a non-blocking settings failure. The Sounds tab provides the same Beacon, Chime, Pulse, Alert, Resolve, and Knock built-in tones as the macOS version, including preview playback. New installations default to Chime for task completion and Alert for 429/503 incidents; either notification can independently use any of the six sounds. A sound plays once only when an automatic refresh observes a new reliable `task_complete` event; multiple completions in one refresh batch are coalesced. App startup, manual refresh, monitoring resume, and task-count changes establish a baseline and never replay historical completions. Display preferences are stored in `%LOCALAPPDATA%\ThreadBeacon\display-settings.json`; sound preferences and at most 256 derived event IDs also stay local. These files contain no task titles, conversation bodies, Token details, or Codex paths.

The settings window also supports Simplified Chinese, English, and System language preferences. The preference is stored as a stable semantic value, and switching languages updates both the main and settings windows immediately. Task titles, Agent aliases, model names, HTTP status codes, and other raw Codex data remain unchanged. Unsupported system locales fall back to English, while missing or invalid language settings fall back to System.

The information button in the title bar opens a single-instance About window with the App icon, runtime version, project purpose, and independent-community disclaimer, plus GitHub, Releases, and Privacy links. Those links are handed to the default browser only after an explicit user click.

After startup, the App silently checks GitHub Releases once, including prereleases. If a newer release is found, an update icon appears in the footer; About also provides manual checking and retry. A failed check does not affect task monitoring, sounds, or data-source health, and the App never downloads or installs updates automatically.

Theme preferences are available in the General tab with `System`, `Light`, and `Dark` modes. New installations default to `System`, which follows the Windows app appearance setting. Choosing `Light` or `Dark` applies immediately to the main window, settings window, and open detail surfaces; the selected mode is stored locally and restored after restart. This milestone does not add custom colors or a dedicated high-contrast theme.

The App now also monitors HTTP 400/429/503 service incidents and model-capacity failures for currently visible primary tasks. Active 429/503 retries appear as a yellow “Service incident” with the HTTP status and retry progress; HTTP 400, exhausted retries, and model-capacity failures become a red “Service failure,” with capacity failures carrying a dedicated detail label. A later HTTP 200 in the same turn or a newer rollout lifecycle event clears the stale incident. Each incident episode can play one independently configurable warning sound and shares the baseline and 256-entry local derived-ID history with completion events.

The Sounds tab supports choosing, previewing, and clearing a local WAV file independently for completion and service-incident notifications. If a custom file is unavailable or invalid, playback automatically falls back to the selected built-in tone.

A primary task that created Subagents shows a neutral branch icon and its direct Subagent count after the title. This is a historical parent-child relationship count, not a live running count; zero reserves no space. Clicking the count expands direct children inline with `Agent alias | title`, derived status, recent activity, and cumulative Token usage. The detail button shows role, model, reasoning effort, and numeric Token fields. Child records and rollout tails are read only for visible expanded parents; collapsing stops those reads. Conversation bodies and deeper descendants are never read or displayed.

The window subtitle shows `running tasks/current visible tasks`, such as `1/7`. Only primary snapshots with the derived `Running` status contribute to the numerator, and the denominator matches the primary snapshots currently displayed. Pausing preserves the last successful count; manual refresh or monitoring resume recalculates it.

The first POC is deliberately limited to:

- Reading 8 recent unarchived primary threads by default, with configurable limits of 4, 8, 12, or 20, and excluding subagents.
- Using the latest renamed title from `session_index.jsonl`.
- Deriving task status from rollout JSONL tails.
- Displaying cumulative Token usage with a numeric-only detail popover.
- Playing a configurable built-in sound for new task completions observed by automatic refresh.
- Detecting HTTP 400/429/503 and model-capacity incidents for visible primary tasks from read-only local logs.
- Showing a non-zero historical direct-Subagent count and expanding direct children on demand.
- Showing running primary tasks over currently visible primary tasks in the subtitle.
- Pinning, temporarily ignoring, automatically restoring on a newer turn, and manually restoring primary tasks.
- Favoriting independently, filtering to favorites, and watching archived favorites.
- Showing four local data-source states, aggregate rollout read counts, and the last successful refresh time.
- Restoring the main window's last display, position, and size with safe disconnected-display fallback.
- Refreshing every 2 seconds by default, with configurable 1, 2, 5, or 10-second intervals and a manual refresh option.
- Opening SQLite databases in read-only mode.
- Never reading conversation bodies, accessing the network, or modifying Codex data.

Other failure/warning sounds, Subagent alerts and Token aggregation, and the system tray remain deferred.

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

## Versioned Release

The repository root `VERSION` file is the single source of truth for the app version. Stable releases use a matching Git tag such as `v0.1.0`. Generate a self-contained `win-x64` release package with:

```powershell
.\script\publish_release.ps1
```

The script writes a portable package ZIP and the published executable under `artifacts/release/<tag>`. The ZIP is the recommended distribution because it includes the bundled sound assets alongside the executable.

Pushing a `v*` tag also starts the repository GitHub Actions release workflow. It rebuilds the assets on a clean Windows runner and publishes both files to the matching GitHub Release.

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
