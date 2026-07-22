# Subagent Active Count Implementation Plan

1. Add failing repository tests for fresh direct-child candidates.
2. Add failing loader tests for collapsed active counting and archived-parent exclusion.
3. Add activity models and a read-only parameterized repository query.
4. Parse candidate rollouts once per refresh and reuse observations for expanded children.
5. Add active count to snapshots and the `active/total` WPF presentation.
6. Update documentation and version, then run all tests and Release build.
7. Publish, install, and validate light/dark, Chinese/English, collapsed/expanded UI.
8. Scan staged changes for sensitive data, commit, and push `main`.
