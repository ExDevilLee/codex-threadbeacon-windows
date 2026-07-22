# Main Task Details Implementation Plan

1. Add failing parser tests for latest valid rollout model and reasoning effort.
2. Add failing repository and loader tests for SQLite fields and per-field fallback precedence.
3. Extend Core models, SQL projections, rollout parsing, and snapshot composition.
4. Add failing view-model and XAML tests for metadata-only details and localized presentation.
5. Update the details view model, resources, and popover layout.
6. Update privacy/data documentation and bump the patch version.
7. Run all tests and a Release build, publish and install, then validate Chinese and English UI states.
8. Scan the staged diff for sensitive information, commit, and push `main`.
