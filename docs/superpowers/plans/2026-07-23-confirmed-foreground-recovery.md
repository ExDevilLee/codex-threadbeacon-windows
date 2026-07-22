# Confirmed Foreground Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Safely resume an exact frontmost Codex task with an empty composer and persist privacy-safe recovery diagnostics.

**Architecture:** Add a pure selection policy and a typed selection result at the automation boundary. Windows UI Automation gathers only counts, title-match state, and empty-composer state; the sender and bounded history store propagate a stable diagnostic code without retaining UI or conversation content.

**Tech Stack:** .NET 9, WPF UI Automation, xUnit, System.Text.Json

---

### Task 1: Selection Contract And Policy

**Files:**
- Modify: `src/ThreadBeacon.App/AutoRecovery/ICodexComposerAutomation.cs`
- Create: `src/ThreadBeacon.App/AutoRecovery/CodexTargetSelectionPolicy.cs`
- Create: `tests/ThreadBeacon.App.Tests/AutoRecovery/CodexTargetSelectionPolicyTests.cs`

- [ ] **Step 1: Write failing policy tests** for confirmed foreground empty
  composer, unconfirmed target, draft, unreadable value, and non-unique counts.
- [ ] **Step 2: Run the focused tests** and verify the new types are missing.
- [ ] **Step 3: Implement the typed result and pure policy** with bounded stable
  diagnostic codes and no title or composer content in results.
- [ ] **Step 4: Run the focused tests** and verify they pass.

### Task 2: Windows UI Automation Integration

**Files:**
- Modify: `src/ThreadBeacon.App/AutoRecovery/WindowsCodexComposerAutomation.cs`
- Modify: `src/ThreadBeacon.App/AutoRecovery/ICodexThreadOpener.cs`
- Modify: `src/ThreadBeacon.App/AutoRecovery/WindowsCodexRecoverySender.cs`
- Modify: `tests/ThreadBeacon.App.Tests/AutoRecovery/WindowsCodexRecoverySenderTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/AutoRecovery/WindowsCodexThreadOpenerTests.cs`

- [ ] **Step 1: Update fakes and write failing sender tests** asserting
  unattended selection mode and diagnostic propagation.
- [ ] **Step 2: Run focused tests** and verify contract failures.
- [ ] **Step 3: Integrate the policy** so only a confirmed foreground target
  bypasses deep-link navigation; preserve all existing navigation checks.
- [ ] **Step 4: Update sender and opener** to use unattended and interactive
  modes respectively.
- [ ] **Step 5: Run focused tests** and verify no typing or invocation occurs on
  any failed selection.

### Task 3: Bounded Recovery Diagnostics

**Files:**
- Modify: `src/ThreadBeacon.App/AutoRecovery/IAutoRecoveryHistoryStore.cs`
- Modify: `src/ThreadBeacon.App/AutoRecovery/AutoRecoveryCoordinator.cs`
- Modify: `tests/ThreadBeacon.App.Tests/AutoRecovery/AutoRecoveryCoordinatorTests.cs`
- Modify: `tests/ThreadBeacon.App.Tests/AutoRecovery/JsonAutoRecoveryHistoryStoreTests.cs`

- [ ] **Step 1: Write failing history tests** for stable diagnostic persistence
  and rejection of arbitrary detail strings.
- [ ] **Step 2: Add an optional diagnostic code** to history entries and allow
  only the finite selection-code vocabulary plus bounded count suffixes.
- [ ] **Step 3: Persist sender failure diagnostics** and use
  `unexpected_error` for caught exceptions instead of exception messages.
- [ ] **Step 4: Run all auto-recovery tests** and verify compatibility with
  existing history JSON.

### Task 4: Release And UI Verification

**Files:**
- Modify: `VERSION`
- Modify: `CHANGELOG.md`
- Modify: `docs/macos-parity.md`

- [ ] **Step 1: Set the next feature version and document safety/privacy.**
- [ ] **Step 2: Run `dotnet test ThreadBeacon.slnx --configuration Release`.**
- [ ] **Step 3: Run `dotnet build ThreadBeacon.slnx --configuration Release --no-restore`.**
- [ ] **Step 4: Publish and install the fixed-location executable.**
- [ ] **Step 5: Verify with Windows UI Automation** that an exact current title
  is detectable and a draft remains unchanged, without sending a message.
- [ ] **Step 6: Scan the diff for secrets and machine-specific data, commit,
  push `main`, and record the Windows completion commit in the parity ledger.**

