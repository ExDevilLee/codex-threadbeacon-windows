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

Console.WriteLine($"state database: {(File.Exists(paths.StateDatabase) ? "available" : "missing")}");
Console.WriteLine($"returned threads: {resolvedThreads.Count}");
Console.WriteLine($"source status: {result.Status.ToString().ToLowerInvariant()}");
Console.WriteLine($"session index: {(File.Exists(paths.SessionIndex) ? "available" : "missing")}");
Console.WriteLine($"title source status: {titleResult.Status.ToString().ToLowerInvariant()}");
Console.WriteLine($"rename matches: {renameMatches}");

return result.IsHealthy ? 0 : 1;
