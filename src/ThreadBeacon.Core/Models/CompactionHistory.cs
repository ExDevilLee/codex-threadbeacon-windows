namespace ThreadBeacon.Core.Models;

public sealed record CompactionHistory
{
    public CompactionHistory(int completionCount = 0, DateTimeOffset? lastCompletedAt = null)
    {
        CompletionCount = Math.Max(0, completionCount);
        LastCompletedAt = lastCompletedAt;
    }

    public int CompletionCount { get; }

    public DateTimeOffset? LastCompletedAt { get; }
}
