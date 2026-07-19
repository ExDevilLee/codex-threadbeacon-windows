using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public interface ILogEventRepository
{
    ServiceLogLoadResult LoadLatestIncidents(
        IReadOnlySet<string> threadIds);
}
