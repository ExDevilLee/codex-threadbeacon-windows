# Archived Task Navigation Guard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent archived favorites from opening Codex on double-click while preserving active-task navigation.

**Architecture:** Add one guard to the existing WPF row event using the established `IsArchived` view-model state. Keep the opener and Codex automation unchanged.

**Tech Stack:** .NET 9, WPF, C#, xUnit, Windows UI Automation.

---

### Task 1: Regression and fix

**Files:**
- Modify: `tests/ThreadBeacon.App.Tests/Views/MainWindowXamlTests.cs`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml.cs`

- [x] Add a failing UI-wiring test requiring an archived-row guard before the opener call.
- [x] Run the test and verify it fails because the guard is absent.
- [x] Add the `row.IsArchived` short circuit.
- [x] Run navigation, row, and XAML tests.

### Task 2: Release and delivery

**Files:**
- Modify: `VERSION`
- Modify: `CHANGELOG.md`
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`

- [x] Bump to `0.13.1` and document the active-only navigation boundary.
- [x] Run full Release tests and build.
- [x] Publish, install, and inspect the ThreadBeacon UI without invoking Codex.
- [x] Scan staged changes for sensitive information.
- [x] Commit and push `fix(tasks): ignore archived task double-clicks`.

The installed UI had no archived favorite fixture at verification time. Validation therefore covered the
real main-list and favorites-filter rendering, then restored the all-tasks view. The negative double-click
path was verified through the WPF event-wiring regression test without opening or controlling Codex.
