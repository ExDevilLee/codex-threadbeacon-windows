# Confirmed Foreground Recovery Design

## Goal

Allow unattended recovery to reuse the current Codex composer only when the
frontmost Codex task is the exact requested task and its composer is uniquely
identified, readable, and empty.

## Safety Boundary

- Keep the current single-Codex-window requirement.
- Confirm the current task with exactly one app-header title matching the
  expected Rename title.
- Require exactly one composer and require its existing empty-placeholder
  state. A draft, missing composer, multiple composers, or unreadable text
  fails closed.
- Skip deep-link navigation only for this confirmed foreground case. All other
  selections retain the existing guarded navigation flow.
- Interactive double-click opening remains explicit and uses the existing
  navigation behavior.
- Send is still invoked exactly once and still requires rollout evidence. No
  retry is added.

## Diagnostics And Privacy

Selection returns a typed result with a stable diagnostic code. Recovery
history may store only that code alongside its existing bounded identifiers and
timestamps. It must not store the task title, composer text, prompt, rollout
path, UI tree, window title, or exception message.

Codes distinguish invalid input, Codex availability, foreground target
confirmation, composer count/readability/draft failures, navigation failures,
target confirmation failures, and successful selection. Counts are bounded to
`0`, `1`, or `many` so diagnostics cannot become an unbounded data channel.

## Verification

- Unit-test selection result codes and recovery-history persistence.
- Unit-test recovery sender propagation without storing prompt or path.
- Exercise a pure foreground-selection policy for confirmed, ambiguous, draft,
  and unreadable composer states.
- Run the full Release test suite and build.
- Use Windows UI Automation against the installed Codex app to verify the
  current task title is uniquely detectable and that a non-empty draft is not
  changed. Do not send a real message during this verification.
