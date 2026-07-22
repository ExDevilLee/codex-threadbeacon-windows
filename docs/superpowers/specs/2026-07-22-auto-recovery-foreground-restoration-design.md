# Auto-recovery foreground restoration

## Goal

After unattended auto recovery brings Codex to the foreground, restore the application that was foreground before recovery only when doing so cannot override a user focus change.

## Safety contract

- Capture the original foreground window and process identity immediately before a serialized recovery attempt.
- Capture the unique Codex process identity without activating it.
- Run the same restoration check after success, failure, or cancellation.
- Restore only when the current foreground process is still the captured Codex process and the original window still belongs to the captured original process.
- Skip when either identity is missing, the original process is Codex, the original window/process ended, a PID was reused, or the user selected another application.
- Restoration failure never changes the recovery send result.
- Double-click task opening and manual UI actions are outside this lifecycle.

No application identity is persisted or added to recovery history.
