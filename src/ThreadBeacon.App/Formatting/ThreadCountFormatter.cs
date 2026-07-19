using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.Formatting;

public sealed record ThreadCountLabel(
    string DisplayText,
    string AccessibilityLabel);

public static class ThreadCountFormatter
{
    public static ThreadCountLabel Format(IEnumerable<ThreadStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        int runningCount = 0;
        int visibleCount = 0;
        foreach (ThreadStatus status in statuses)
        {
            visibleCount++;
            if (status is ThreadStatus.Running)
            {
                runningCount++;
            }
        }

        return new ThreadCountLabel(
            $"{runningCount}/{visibleCount}",
            $"{runningCount} 个任务正在运行，共显示 {visibleCount} 个任务");
    }
}
