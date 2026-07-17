using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

CodexDataPaths paths = CodexDataPaths.Resolve();
var repository = new SQLiteThreadRepository(paths.StateDatabase);
ThreadLoadResult result = repository.LoadRecent();

Console.WriteLine($"state database: {(File.Exists(paths.StateDatabase) ? "available" : "missing")}");
Console.WriteLine($"returned threads: {result.Threads.Count}");
Console.WriteLine($"source status: {result.Status.ToString().ToLowerInvariant()}");

return result.IsHealthy ? 0 : 1;
