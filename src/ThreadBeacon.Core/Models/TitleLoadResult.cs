namespace ThreadBeacon.Core.Models;

public sealed record TitleLoadResult(
    SessionIndexStatus Status,
    IReadOnlyDictionary<string, string> Titles)
{
    public bool IsHealthy => Status is SessionIndexStatus.Healthy;
}
