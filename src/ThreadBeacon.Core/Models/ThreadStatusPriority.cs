namespace ThreadBeacon.Core.Models;

public static class ThreadStatusPriority
{
    public static int Get(ThreadStatus status) => status switch
    {
        ThreadStatus.Error => 0,
        ThreadStatus.NeedsAction => 1,
        ThreadStatus.Warning => 2,
        ThreadStatus.Running => 3,
        ThreadStatus.Interrupted => 4,
        ThreadStatus.JustCompleted => 5,
        ThreadStatus.Idle => 6,
        ThreadStatus.Unknown => 7,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}

