using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public interface IThreadRepository
{
    ThreadLoadResult LoadRecent(int limit = 8);

    ThreadLoadResult LoadDetachedSubagentCandidates(int limit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, []);
    }

    ThreadLoadResult LoadByIds(IReadOnlySet<string> threadIds)
    {
        ArgumentNullException.ThrowIfNull(threadIds);
        return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, []);
    }

    ThreadLoadResult LoadByIdsIncludingArchived(IReadOnlySet<string> threadIds)
    {
        ArgumentNullException.ThrowIfNull(threadIds);
        return LoadByIds(threadIds);
    }

    SubagentLoadResult LoadDirectSubagents(IReadOnlySet<string> parentIds)
    {
        ArgumentNullException.ThrowIfNull(parentIds);
        return new SubagentLoadResult(
            ThreadRepositoryStatus.Healthy,
            new Dictionary<string, IReadOnlyList<SubagentRecord>>(StringComparer.Ordinal));
    }
}
