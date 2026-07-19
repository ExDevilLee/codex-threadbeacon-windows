# Expanded Sound Library Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the macOS-authored Alert, Resolve, and Knock WAV assets, expose all six sounds in Windows settings, and align new-install defaults to Chime for completion and Alert for incidents.

**Architecture:** Extend the existing `CompletionSound` enum and mapping without changing notification rules or WPF layout. Copy the deterministic macOS WAV files byte-for-byte, validate their format and hashes in tests, and preserve JSON compatibility through string enum serialization and property defaults.

**Tech Stack:** .NET 9, C# 13, WPF, System.Media.SoundPlayer, System.Text.Json, xUnit

---

### Task 1: Six-sound API and defaults

**Files:**
- Modify: `src/ThreadBeacon.App/Sounds/CompletionSound.cs`
- Modify: `src/ThreadBeacon.App/Sounds/SoundNotificationSettings.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/SoundSettingsViewModel.cs`
- Modify: `tests/ThreadBeacon.App.Tests/ViewModels/SoundSettingsViewModelTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/Sounds/JsonSoundNotificationSettingsStoreTests.cs`

- [x] Add failing tests asserting the six-value order, Chime completion default, Alert incident default, old three-value JSON compatibility, and Alert/Resolve/Knock round-trip serialization.
- [x] Run the focused settings tests and verify failures are caused by missing enum values or old defaults.
- [x] Add `Alert`, `Resolve`, and `Knock` to `CompletionSound`, expose them after the existing three options, and change only property defaults to Chime and Alert.
- [x] Run focused and full App tests; require all tests to pass.
- [x] Commit with `feat(sound): expose six notification sounds`.

### Task 2: Cross-platform WAV assets and playback mapping

**Files:**
- Create: `src/ThreadBeacon.App/Resources/Sounds/Done-Alert.wav`
- Create: `src/ThreadBeacon.App/Resources/Sounds/Done-Resolve.wav`
- Create: `src/ThreadBeacon.App/Resources/Sounds/Done-Knock.wav`
- Modify: `src/ThreadBeacon.App/Sounds/WavSoundPlaybackService.cs`
- Modify: `tests/ThreadBeacon.App.Tests/Sounds/WavSoundPlaybackServiceTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/Sounds/BundledSoundAssetTests.cs`

- [x] Add failing path-mapping tests for Alert, Resolve, and Knock and asset tests requiring all six WAV files with 44.1 kHz mono 16-bit PCM headers and the three macOS SHA-256 hashes.
- [x] Run the focused sound tests and verify failures for missing mappings/assets.
- [x] Map the three enum values to `Done-Alert.wav`, `Done-Resolve.wav`, and `Done-Knock.wav`.
- [x] Copy the three files byte-for-byte from macOS `f213fad` and verify SHA-256 hashes `A294A8142F636F5641AA04F6974A304F46E5148068DD06E42BA3D1002654D497`, `F2E077E6E926FF315EABD11D26B53716E29DB5B7A1D96334B988867D1554B6AE`, and `127AA68C18EB0A419627D33FAA8395A1A5CA601BA8B5ABA3181EAE894CD4889D`.
- [x] Run focused and full App tests, then build Release and confirm all six WAV files are present in `bin/Release/net9.0-windows/Resources/Sounds`.
- [x] Commit with `feat(sound): bundle expanded sound library`.

### Task 3: Documentation, runtime verification, and delivery

**Files:**
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`
- Modify: `docs/superpowers/plans/2026-07-19-expanded-sound-library.md`

- [x] Document all six deterministic built-in sounds and the Chime/Alert defaults in Chinese and English; update the roadmap without changing later feature scope.
- [x] Run the complete Release test/build suite and package vulnerability audit with zero failures, warnings, errors, or known vulnerabilities.
- [x] Launch the Release App, open sound settings, inspect both six-item selectors, and preview Alert, Resolve, and Knock through the normal UI.
- [x] Run the mandatory tracked-file secret, private-key, absolute-path, network/write API, binary asset, and diff audits.
- [x] Mark the plan complete, commit documentation, push `main`, verify `HEAD == origin/main`, and leave the Release App running.
