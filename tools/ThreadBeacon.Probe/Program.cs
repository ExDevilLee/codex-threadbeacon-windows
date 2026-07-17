using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

CodexDataPaths paths = CodexDataPaths.Resolve();
var repository = new SQLiteThreadRepository(paths.StateDatabase);
ThreadLoadResult result = repository.LoadRecent();
var titleRepository = new SessionIndexTitleRepository(paths.SessionIndex);
TitleLoadResult titleResult = titleRepository.LoadLatestTitles();
IReadOnlyList<ThreadRecord> resolvedThreads = ThreadTitleResolver.Resolve(
    result.Threads,
    titleResult.Titles);
int renameMatches = result.Threads.Count(record => titleResult.Titles.ContainsKey(record.Id));
var rolloutParser = new RolloutTailParser();
var rolloutSources = new Dictionary<RolloutSourceStatus, int>();
var displayStatuses = new Dictionary<ThreadStatus, int>();
DateTimeOffset now = DateTimeOffset.UtcNow;
foreach (ThreadRecord thread in resolvedThreads)
{
    RolloutLoadResult rollout = rolloutParser.Parse(thread.RolloutPath);
    rolloutSources[rollout.Status] = rolloutSources.GetValueOrDefault(rollout.Status) + 1;
    ThreadDisplayState display = ThreadStatusPolicy.Evaluate(
        rollout.Observation,
        thread.UpdatedAt,
        now);
    displayStatuses[display.Status] = displayStatuses.GetValueOrDefault(display.Status) + 1;
}

Console.WriteLine($"state database: {(File.Exists(paths.StateDatabase) ? "available" : "missing")}");
Console.WriteLine($"returned threads: {resolvedThreads.Count}");
Console.WriteLine($"source status: {result.Status.ToString().ToLowerInvariant()}");
Console.WriteLine($"session index: {(File.Exists(paths.SessionIndex) ? "available" : "missing")}");
Console.WriteLine($"title source status: {titleResult.Status.ToString().ToLowerInvariant()}");
Console.WriteLine($"rename matches: {renameMatches}");
foreach (RolloutSourceStatus status in Enum.GetValues<RolloutSourceStatus>())
{
    Console.WriteLine($"rollout {status.ToString().ToLowerInvariant()}: {rolloutSources.GetValueOrDefault(status)}");
}

foreach (ThreadStatus status in Enum.GetValues<ThreadStatus>())
{
    Console.WriteLine($"status {status.ToString().ToLowerInvariant()}: {displayStatuses.GetValueOrDefault(status)}");
}

return result.IsHealthy ? 0 : 1;
