namespace ThreadBeacon.Core.Models;

public static class ThreadStatusPriority
{
    public static int Get(ThreadStatus status) => status switch
    {
        ThreadStatus.Error => 0,
        ThreadStatus.NeedsAction => 1,
        ThreadStatus.Warning => 2,
        ThreadStatus.Running => 3,
        ThreadStatus.JustCompleted => 4,
        ThreadStatus.Idle => 5,
        ThreadStatus.Unknown => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}

