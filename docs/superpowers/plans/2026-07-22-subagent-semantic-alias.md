# Subagent Semantic Alias Implementation Plan

1. Add failing formatter, repository compatibility, loader, view-model, and XAML layout tests.
2. Extend Subagent records and snapshots with optional `AgentPath` metadata.
3. Feature-detect the SQLite column and preserve old-schema behavior.
4. Humanize the final path component and prefer it in Subagent rows.
5. Run all tests, publish and install the next version, then verify an expanded row with isolated read-only fixture data.
6. Scan the staged diff for secrets, commit, and push `main`.
