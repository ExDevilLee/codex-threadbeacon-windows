# Compaction Hook Implementation Plan

> **For agentic workers:** Implement this plan task-by-task with tests before production code.

**Goal:** Add an opt-in Windows Hook bridge and settings-managed live `Compacting` status while preserving the read-only default path.

**Architecture:** A console `ThreadBeacon.HookBridge` reads one Hook JSON document from stdin and updates a per-session marker store. Core reads markers through an injected repository, validates TTL and lifecycle evidence, and merges the activity into snapshots. WPF settings own structured `hooks.json` installation and removal.

**Tech Stack:** .NET 9, WPF, System.Text.Json, Windows file APIs, xUnit.

---

### Task 1: Activity marker model and repository

**Files:**
- Create: `src/ThreadBeacon.Core/Models/CompactionActivity.cs`
- Create: `src/ThreadBeacon.Core/Services/CompactionActivityRepository.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/CompactionActivityRepositoryTests.cs`

- [ ] Test atomic PreCompact write, matching PostCompact delete, session isolation, malformed input, future timestamps, and 15-minute expiry.
- [ ] Implement only the minimal marker schema and cleanup rules.
- [ ] Run focused tests and then the full Core suite.

### Task 2: Hook Bridge executable

**Files:**
- Create: `src/ThreadBeacon.HookBridge/ThreadBeacon.HookBridge.csproj`
- Create: `src/ThreadBeacon.HookBridge/Program.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/CompactionHookPayloadTests.cs`

- [ ] Test accepted PreCompact/PostCompact payload fields and reject missing or invalid session/turn IDs.
- [ ] Implement stdin parsing with no stdout output and a zero exit code on all non-blocking failures.
- [ ] Add the helper to the Release publish output without starting the WPF UI.

### Task 3: Merge live activity into snapshots

**Files:**
- Modify: `src/ThreadBeacon.Core/Models/ThreadSnapshot.cs`
- Modify: `src/ThreadBeacon.Core/Services/ThreadStatusLoader.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/ThreadRowViewModel.cs`
- Modify: `src/ThreadBeacon.App/Localization/AppLanguageText.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/ThreadStatusLoaderTests.cs`
- Test: `tests/ThreadBeacon.App.Tests/ViewModels/ThreadRowViewModelTests.cs`

- [ ] Add active compaction to snapshots and verify it is suppressed for archived tasks and service incidents.
- [ ] Display localized `Compacting` / `压缩中` with the existing Running visual semantics.
- [ ] Verify active status clears after completion or interruption evidence.

### Task 4: Structured Hook configuration manager

**Files:**
- Create: `src/ThreadBeacon.Core/Services/CompactionHookConfigurationManager.cs`
- Test: `tests/ThreadBeacon.Core.Tests/Services/CompactionHookConfigurationManagerTests.cs`

- [ ] Test backup, atomic merge, external modification detection, invalid JSON rejection, inline Hook rejection, and selective uninstall.
- [ ] Implement stable helper path resolution from `%LOCALAPPDATA%\ThreadBeacon\hooks\v1`.

### Task 5: Settings UI and release verification

**Files:**
- Modify: `src/ThreadBeacon.App/ViewModels/SettingsWindowViewModel.cs`
- Modify: `src/ThreadBeacon.App/SettingsWindow.xaml`
- Modify: `src/ThreadBeacon.App/Resources/Strings.zh-Hans.xaml`
- Modify: `src/ThreadBeacon.App/Resources/Strings.en.xaml`
- Modify: `script/publish_release.ps1`
- Modify: `README.md`, `README-EN.md`, `CHANGELOG.md`, `VERSION`

- [ ] Add a General-tab Hook section with status, enable/check/disable commands, trust and privacy disclosure, and safe errors.
- [ ] Publish and install the helper and WPF App.
- [ ] Run full tests, Release build, installed UI verification, and a real Hook stdin/marker smoke test.
- [ ] Scan for secrets, commit, push, and update `docs/macos-parity.md` with the Windows completion commit.
