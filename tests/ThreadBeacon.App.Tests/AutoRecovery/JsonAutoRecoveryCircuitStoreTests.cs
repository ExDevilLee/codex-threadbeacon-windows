using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class JsonAutoRecoveryCircuitStoreTests
{
    [Fact]
    public void RecordAttempt_CountsDistinctEpisodesAndPersistsPrivacySafeState()
    {
        string path = TempPath();
        try
        {
            var store = new JsonAutoRecoveryCircuitStore(path);
            AutoRecoveryCandidate first = Candidate("episode-1");
            AutoRecoveryCandidate second = Candidate("episode-2");

            store.RecordAttempt(first, DateTimeOffset.FromUnixTimeSeconds(10));
            store.RecordAttempt(first, DateTimeOffset.FromUnixTimeSeconds(11));
            store.RecordAttempt(second, DateTimeOffset.FromUnixTimeSeconds(12));

            AutoRecoveryCircuitState state = Assert.Single(
                new JsonAutoRecoveryCircuitStore(path).Load());
            Assert.Equal(2, state.AttemptCount);
            Assert.Equal("episode-2", state.LastEpisodeId);
            string json = File.ReadAllText(path);
            Assert.DoesNotContain("Title", json, StringComparison.Ordinal);
            Assert.DoesNotContain("rollout", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ObserveCompletion_ResetsOnlyWhenCompletionIsNewerThanLastAttempt()
    {
        string path = TempPath();
        try
        {
            var store = new JsonAutoRecoveryCircuitStore(path);
            store.RecordAttempt(Candidate("episode-1"), DateTimeOffset.FromUnixTimeSeconds(10));

            store.ObserveCompletion("thread-1", DateTimeOffset.FromUnixTimeSeconds(9));
            Assert.Single(store.Load());

            store.ObserveCompletion("thread-1", DateTimeOffset.FromUnixTimeSeconds(11));
            Assert.Empty(store.Load());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Reset_RemovesOnlySelectedTaskAndIncident()
    {
        string path = TempPath();
        try
        {
            var store = new JsonAutoRecoveryCircuitStore(path);
            store.RecordAttempt(Candidate("episode-1"), DateTimeOffset.UnixEpoch);
            store.RecordAttempt(
                Candidate("episode-2") with { IncidentType = AutoRecoveryIncidentType.Http429 },
                DateTimeOffset.UnixEpoch);

            store.Reset("thread-1", AutoRecoveryIncidentType.Http400);

            AutoRecoveryCircuitState remaining = Assert.Single(store.Load());
            Assert.Equal(AutoRecoveryIncidentType.Http429, remaining.IncidentType);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_DeduplicatesStatesAndBoundsAttemptCount()
    {
        string path = TempPath();
        try
        {
            File.WriteAllText(
                path,
                """
                [
                  { "threadId": "thread-1", "incidentType": "Http400", "attemptCount": 2, "lastEpisodeId": "new", "lastAttemptAt": "2026-07-23T10:00:00Z" },
                  { "threadId": "thread-1", "incidentType": "Http400", "attemptCount": 1, "lastEpisodeId": "old", "lastAttemptAt": "2026-07-23T09:00:00Z" },
                  { "threadId": "thread-2", "incidentType": "Http429", "attemptCount": 999999, "lastEpisodeId": "bounded", "lastAttemptAt": "2026-07-23T11:00:00Z" }
                ]
                """);

            IReadOnlyList<AutoRecoveryCircuitState> states =
                new JsonAutoRecoveryCircuitStore(path).Load();

            Assert.Equal(2, states.Count);
            Assert.Equal("new", states.Single(state => state.ThreadId == "thread-1").LastEpisodeId);
            Assert.Equal(20, states.Single(state => state.ThreadId == "thread-2").AttemptCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static AutoRecoveryCandidate Candidate(string episode) => new(
        "thread-1",
        episode,
        AutoRecoveryIncidentType.Http400,
        "Private title",
        @"C:\private\rollout.jsonl",
        DateTimeOffset.UnixEpoch);

    private static string TempPath() => Path.Combine(
        Path.GetTempPath(),
        $"threadbeacon-circuit-{Guid.NewGuid():N}.json");
}
