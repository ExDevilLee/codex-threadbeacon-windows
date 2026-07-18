# Windows 30-Minute Read-Only Soak Validation

## Scope

This run validated the Windows POC while Codex was actively writing multiple
tasks. The ThreadBeacon app remained open while the privacy-safe probe sampled
the existing read-only data path every two seconds.

The run did not record task IDs, titles, rollout paths, user profile paths, or
conversation content.

## Environment

- Platform: Windows 11
- Started: 2026-07-18 21:09:28 +08:00
- Ended: 2026-07-18 21:42:30 +08:00
- Elapsed time: 33.04 minutes
- Probe samples: 900
- Target interval: 2 seconds

## Aggregate Results

| Metric | Result |
| --- | ---: |
| App crashes | 0 |
| Probe failures | 0 |
| Source health downgrades | 0 |
| Minimum visible task count | 6 |
| Maximum visible task count | 6 |
| Samples with at least two running tasks | 266 |
| Maximum probe duration | 293 ms |

The running-task count changed naturally during the run while the overall task
count remained stable. No SQLite busy state or unavailable rollout was observed.

## Result

The concurrent read-only soak criteria passed: ThreadBeacon remained responsive,
continued observing status changes, and did not report source contention while
multiple Codex tasks were active.

The final visual parity check for renamed titles and Token values remains part of
the Token detail feature's runtime verification. This record intentionally does
not include those sensitive values.
