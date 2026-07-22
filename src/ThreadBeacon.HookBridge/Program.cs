using ThreadBeacon.Core.Services;

try
{
    string payload = Console.In.ReadToEnd();
    _ = new CompactionHookEventHandler().TryHandle(payload);
}
catch
{
    // Hooks must never block Codex because ThreadBeacon telemetry is unavailable.
}

return 0;
