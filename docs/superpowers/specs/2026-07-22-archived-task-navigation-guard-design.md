# Archived Task Navigation Guard Design

## Goal

Keep archived favorites as read-only watchlist rows and prevent their double-click gesture from navigating Codex.

## Design

The existing `ThreadRowViewModel.IsArchived` value is the authoritative presentation state. `MainWindow.OnTaskRowMouseLeftButtonDown` must return before marking the event handled or invoking `ICodexThreadOpener` when that value is true. The existing double-click count and button-ancestor protections remain unchanged.

No Codex database, rollout, preference, deep-link selector, or accessibility sender behavior changes. Active primary rows continue to use the existing navigation pipeline.

## Verification

A code-behind wiring regression test proves the UI event checks `row.IsArchived` before the opener call. Existing row tests prove archived snapshots expose the flag and neutral presentation. Release verification covers the full test suite, installed App rendering, and a security scan; automated UI validation does not invoke Codex.
