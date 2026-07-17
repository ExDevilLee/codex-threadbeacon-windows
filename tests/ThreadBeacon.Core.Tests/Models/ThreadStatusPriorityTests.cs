using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Tests.Models;

public sealed class ThreadStatusPriorityTests
{
    [Fact]
    public void Get_PreservesProductStatusOrder()
    {
        ThreadStatus[] statuses =
        [
            ThreadStatus.Error,
            ThreadStatus.NeedsAction,
            ThreadStatus.Warning,
            ThreadStatus.Running,
            ThreadStatus.JustCompleted,
            ThreadStatus.Idle,
            ThreadStatus.Unknown,
        ];

        int[] priorities = statuses.Select(ThreadStatusPriority.Get).ToArray();

        Assert.Equal(Enumerable.Range(0, statuses.Length), priorities);
    }
}

