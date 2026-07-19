using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public interface ILogEventRepository
{
    IReadOnlyDictionary<string, ServiceIncident> LoadLatestIncidents(
        IReadOnlySet<string> threadIds);
}
