# Windows README Restructure Design

## Goal

Restructure the Simplified Chinese and English Windows READMEs around the current macOS README information architecture while keeping every platform statement accurate for the Windows implementation.

## Information Architecture

Both documents use the same sequence:

1. product identity, release/platform/license badges, and community disclaimer;
2. dedicated small-screen use case;
3. 30-second quick start;
4. paired interface screenshots;
5. four concise feature groups;
6. Windows download and portable installation;
7. data and privacy boundaries;
8. known limitations;
9. development and feedback links;
10. App icon and platform repositories.

The existing chronological POC narrative is removed from the primary README. Detailed implementation, validation, parity, and design records remain under `docs/`, `CHANGELOG.md`, and `ROADMAP.md`.

## Windows Adaptation

- Describe the portable `win-x64` ZIP and optional single-file EXE instead of Homebrew or an App bundle.
- Describe SmartScreen without recommending system-wide security changes.
- Describe Windows UI Automation and fail-closed composer checks instead of macOS Accessibility permission.
- Retain Windows-specific launch-at-login, WPF, Hook Bridge, and `%LOCALAPPDATA%` behavior only where useful to users.
- Do not claim system tray support or any other deferred feature.

## Content Rules

- Chinese and English headings, feature groups, links, and limitations must correspond.
- Use only screenshots already stored in `docs/assets/readme`.
- Keep privacy claims consistent with `PRIVACY.md` and read-only Codex data access.
- Keep build and release instructions reproducible from repository scripts.
- Avoid version-specific feature inventory that will quickly become stale.

## Verification

- Check that all relative Markdown links and image paths resolve.
- Compare heading order and feature bullet coverage between languages.
- Run repository tests and a Release build because README release commands and repository-readiness tests are executable contracts.
- Scan the staged diff for secrets and machine-specific paths before commit.
