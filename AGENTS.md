# ThreadBeacon for Windows Agent Instructions

## macOS Feature Parity

- Use `docs/macos-parity.md` as the authoritative parity checkpoint.
- Before comparing the macOS reference repository, read `Last analyzed macOS
  commit` from that file and inspect only commits after that checkpoint.
- After every comparison, append the dated analysis result to
  `docs/macos-parity.md` and advance its checkpoint, including when no Windows
  implementation is needed.
- Do not implement a feature that is only a macOS roadmap candidate or an
  unpublished macOS behavior unless the user explicitly requests it.
- When a parity feature is completed, record the Windows commit beside the
  corresponding entry in the parity ledger.

## Feature Completion

- Keep the Windows implementation behaviorally aligned with macOS while using
  native Windows APIs and WPF conventions where the platforms differ.
- Add or update focused tests before considering a feature complete.
- Run the full test suite and a Release build.
- Publish and install the Release EXE to the fixed local location used by the
  project, then perform UI-level verification of the changed behavior.
- Scan the working tree and staged diff for secrets and machine-specific data
  before committing.
- Commit each completed feature independently and push `main` after verification.

## Data and Privacy Boundaries

- Keep Codex SQLite and rollout access read-only.
- Do not read or display conversation bodies, reasoning summaries, or complete
  requests unless a future user request explicitly changes the product scope.
- Keep `ThreadBeacon.Core` independent of WPF.
