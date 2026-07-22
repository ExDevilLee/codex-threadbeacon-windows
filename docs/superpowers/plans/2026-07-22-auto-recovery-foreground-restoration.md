# Auto-recovery foreground restoration implementation plan

1. Add failing pure-policy tests for all restoration and skip decisions.
2. Add failing sender tests proving restoration runs after success, failure, and cancellation.
3. Implement a fail-closed Win32 foreground-session adapter using window handle, PID, and process start time.
4. Wire the adapter only into unattended auto recovery.
5. Run all tests and a Release build, publish and install the app, then perform an OS-level focus POC with a dedicated recovery task.
6. Scan staged changes for sensitive information, commit, and push independently.
