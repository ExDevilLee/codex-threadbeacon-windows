using ThreadBeacon.App.AutoRecovery;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.Tests.AutoRecovery;

public sealed class JsonAutoRecoveryHistoryStoreTests
{
    [Fact]
    public void Upsert_UpdatesExistingAttemptAndPersistsOnlyBoundedMetadata()
    {
        string path = TempPath();
        try
        {
            var store = new JsonAutoRecoveryHistoryStore(path);
            var sending = new AutoRecoveryHistoryEntry(
                "attempt-1",
                "thread-1",
                "episode-1",
                AutoRecoveryIncidentType.Http429,
                AutoRecoveryHistoryStatus.Sending,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch);

            store.Upsert(sending);
            store.Upsert(sending with
            {
                Status = AutoRecoveryHistoryStatus.Failed,
                DiagnosticCode = "source_composer_not_empty",
                UpdatedAt = DateTimeOffset.UnixEpoch.AddSeconds(2),
            });

            AutoRecoveryHistoryEntry entry = Assert.Single(store.Load());
            Assert.Equal(AutoRecoveryHistoryStatus.Failed, entry.Status);
            Assert.Equal("source_composer_not_empty", entry.DiagnosticCode);
            string json = File.ReadAllText(path);
            Assert.DoesNotContain("rollout", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("prompt", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Upsert_RejectsArbitraryDiagnosticText()
    {
        string path = TempPath();
        try
        {
            var store = new JsonAutoRecoveryHistoryStore(path);
            var entry = new AutoRecoveryHistoryEntry(
                "attempt-private",
                "thread-private",
                "episode-private",
                AutoRecoveryIncidentType.Http400,
                AutoRecoveryHistoryStatus.Failed,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch,
                "draft text must not be stored");

            Assert.False(store.Upsert(entry));
            Assert.False(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Upsert_KeepsOnlyLatestOneHundredEntries()
    {
        string path = TempPath();
        try
        {
            var store = new JsonAutoRecoveryHistoryStore(path);
            for (int index = 0; index < 105; index++)
            {
                store.Upsert(new AutoRecoveryHistoryEntry(
                    $"attempt-{index}",
                    $"thread-{index}",
                    $"episode-{index}",
                    AutoRecoveryIncidentType.Http400,
                    AutoRecoveryHistoryStatus.NotSent,
                    DateTimeOffset.UnixEpoch.AddSeconds(index),
                    DateTimeOffset.UnixEpoch.AddSeconds(index)));
            }

            IReadOnlyList<AutoRecoveryHistoryEntry> entries = store.Load();
            Assert.Equal(100, entries.Count);
            Assert.Equal("attempt-104", entries[0].AttemptId);
            Assert.DoesNotContain(entries, entry => entry.AttemptId == "attempt-0");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempPath() => Path.Combine(
        Path.GetTempPath(),
        $"threadbeacon-auto-recovery-history-{Guid.NewGuid():N}.json");
}
