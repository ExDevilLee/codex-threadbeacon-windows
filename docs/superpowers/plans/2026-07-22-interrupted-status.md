# Interrupted Status Implementation Plan

1. Add parser contract tests for valid, invalid, reordered, tied, and malformed interruption events.
2. Add model, localization, glyph, notification, and recovery boundary tests.
3. Implement the new status and timestamp precedence in ThreadBeacon.Core.
4. Implement the localized neutral presentation in ThreadBeacon.App.
5. Run focused and full automated tests, publish and install the release build, then verify the rendered UI with isolated read-only fixture data.
6. Scan the staged diff for secrets, commit the feature, and push `main`.
