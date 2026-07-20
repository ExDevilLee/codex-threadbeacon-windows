# ThreadBeacon for Windows Troubleshooting

[简体中文](troubleshooting.md)

This guide applies to the Windows 11 technical preview downloaded from GitHub Releases. Confirm
that Codex Desktop or Codex CLI has run at least one task before troubleshooting.

## Windows Warns About The EXE

The preview does not yet use a commercial code-signing certificate, so SmartScreen may identify an
unknown publisher. Download only from this repository's GitHub Release and verify the tag. Do not
disable Defender or SmartScreen and do not run security-bypass commands.

## No Tasks Appear

1. Run a real primary task in Codex Desktop or Codex CLI.
2. Turn off the favorites-only filter and check ignored tasks.
3. Resume monitoring or use manual refresh.
4. Open data-source health and check the task database.

ThreadBeacon does not list Subagents as independent primary rows. If the database is unavailable,
confirm `%USERPROFILE%\.codex\state_5.sqlite` exists. Never edit, replace, or upload it. If a custom
Codex directory is used, verify `CODEX_HOME` or `CODEX_SQLITE_HOME`.

## Titles, Status, Or Tokens Do Not Update

- Rename failures fall back to the database title and report degraded health.
- An unresolved turn without events for 120 seconds becomes `unknown`.
- `justCompleted` becomes `idle` after about 60 seconds.
- Paused monitoring still permits manual refresh.
- Rollout reads are tail-only and safely degrade during format changes or rotation.

Report health categories and success/failure counts only. Do not attach session indexes or rollouts.

## Incidents Or Sounds Are Missing

Incidents require allowlisted structured HTTP 400/429/503 or explicit model-capacity evidence.
ThreadBeacon does not infer them from conversation text, silence, or ordinary timeouts. For sounds,
check Settings switches, preview playback, and system volume. Missing or invalid custom WAV files
fall back to the selected built-in sound. Startup and manual refresh never replay old events.

## Launch At Login Does Not Work

The setting writes only
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ThreadBeacon`. Confirm the installed executable
remains at `%LOCALAPPDATA%\ThreadBeacon\ThreadBeacon.App.exe`, then disable and re-enable the setting.
Do not replace it with an administrator-level task or untrusted startup helper.

## Upgrade, Roll Back, Or Uninstall

ThreadBeacon only notifies about updates. Quit it, download the target GitHub Release, and replace
the files in `%LOCALAPPDATA%\ThreadBeacon`. Settings are local JSON files; compatibility with every
future version is not guaranteed.

Before uninstalling, disable launch at login, quit the app, and delete
`%LOCALAPPDATA%\ThreadBeacon`. No driver, daemon, or administrator-level service is installed.

## Before Opening An Issue

Safe details include the Release version, Windows 11 version, CPU architecture, Codex version,
health categories, and redacted steps reproducible from a blank environment. Never post task
titles, IDs, conversation or reasoning content, `state_5.sqlite`, `logs_2.sqlite`, rollouts,
usernames, absolute paths, request IDs, provider URLs, tokens, cookies, credentials, complete logs,
or unredacted screenshots. Follow [`SECURITY.md`](../SECURITY.md) for vulnerabilities.
