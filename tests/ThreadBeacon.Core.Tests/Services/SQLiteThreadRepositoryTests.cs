using Microsoft.Data.Sqlite;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class SQLiteThreadRepositoryTests
{
    [Fact]
    public void LoadRecent_ReturnsActivePrimaryThreadsInRecencyOrder()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();

        ThreadLoadResult result = new SQLiteThreadRepository(database.Path).LoadRecent();

        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.Collection(
            result.Threads,
            record =>
            {
                Assert.Equal("new-thread", record.Id);
                Assert.Equal("New", record.Title);
                Assert.Equal(Path.Combine("D:\\rollouts", "new.jsonl"), record.RolloutPath);
                Assert.Equal(70_808_875, record.TokensUsed);
                Assert.Equal(3, record.SubagentCount);
            },
            record => Assert.Equal("older-thread", record.Id));
    }

    [Fact]
    public void LoadRecent_RespectsRequestedLimit()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();

        ThreadLoadResult result = new SQLiteThreadRepository(database.Path).LoadRecent(1);

        ThreadRecord record = Assert.Single(result.Threads);
        Assert.Equal("new-thread", record.Id);
    }

    [Fact]
    public void LoadRecent_FallsBackWhenSpawnEdgesTableIsMissing()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create(includeSpawnEdges: false);

        ThreadLoadResult result = new SQLiteThreadRepository(database.Path).LoadRecent();

        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.All(result.Threads, record => Assert.Equal(0, record.SubagentCount));
        Assert.DoesNotContain(result.Threads, record => record.Id == "subagent-thread");
    }

    [Fact]
    public void LoadRecent_ReturnsMissingForAbsentDatabase()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");

        ThreadLoadResult result = new SQLiteThreadRepository(path).LoadRecent();

        Assert.Equal(ThreadRepositoryStatus.Missing, result.Status);
        Assert.Empty(result.Threads);
    }

    [Fact]
    public void LoadRecent_ReturnsIncompatibleForUnknownSchema()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.CreateWithUnknownSchema();

        ThreadLoadResult result = new SQLiteThreadRepository(database.Path).LoadRecent();

        Assert.Equal(ThreadRepositoryStatus.Incompatible, result.Status);
        Assert.Empty(result.Threads);
    }

    [Fact]
    public void LoadRecent_ReturnsBusyWhenDatabaseIsExclusivelyLocked()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();
        using SqliteConnection lockConnection = TemporaryThreadDatabase.OpenConnection(database.Path);
        lockConnection.Open();
        using SqliteCommand lockCommand = lockConnection.CreateCommand();
        lockCommand.CommandText = "BEGIN EXCLUSIVE;";
        lockCommand.ExecuteNonQuery();

        ThreadLoadResult result = new SQLiteThreadRepository(database.Path).LoadRecent();

        Assert.Equal(ThreadRepositoryStatus.Busy, result.Status);
        Assert.Empty(result.Threads);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LoadRecent_RejectsInvalidLimit(int limit)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
        var repository = new SQLiteThreadRepository(path);

        Assert.Throws<ArgumentOutOfRangeException>(() => repository.LoadRecent(limit));
    }

    private sealed class TemporaryThreadDatabase : IDisposable
    {
        private TemporaryThreadDatabase(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryThreadDatabase Create(bool includeSpawnEdges = true)
        {
            TemporaryThreadDatabase database = CreateEmpty();
            using var connection = OpenConnection(database.Path);
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = SchemaSql + (includeSpawnEdges ? SpawnEdgesSql : string.Empty);
            command.ExecuteNonQuery();
            return database;
        }

        public static TemporaryThreadDatabase CreateWithUnknownSchema()
        {
            TemporaryThreadDatabase database = CreateEmpty();
            using var connection = OpenConnection(database.Path);
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE something_else (id TEXT PRIMARY KEY);";
            command.ExecuteNonQuery();
            return database;
        }

        public void Dispose()
        {
            File.Delete(Path);
            File.Delete($"{Path}-shm");
            File.Delete($"{Path}-wal");
        }

        private static TemporaryThreadDatabase CreateEmpty() =>
            new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite"));

        public static SqliteConnection OpenConnection(string path)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Pooling = false,
            };
            return new SqliteConnection(builder.ConnectionString);
        }

        private const string SchemaSql = """
            CREATE TABLE threads (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                rollout_path TEXT NOT NULL,
                updated_at INTEGER NOT NULL,
                updated_at_ms INTEGER,
                recency_at_ms INTEGER NOT NULL,
                archived INTEGER NOT NULL DEFAULT 0,
                thread_source TEXT,
                tokens_used INTEGER NOT NULL DEFAULT 0
            );
            INSERT INTO threads VALUES
                ('older-thread', 'Older', 'D:\rollouts\older.jsonl', 100, 100000, 100000, 0, 'user', 1),
                ('new-thread', 'New', 'D:\rollouts\new.jsonl', 200, 200000, 300000, 0, 'user', 70808875),
                ('subagent-thread', 'Child', 'D:\rollouts\child.jsonl', 300, 300000, 500000, 0, 'subagent', 2),
                ('legacy-child', 'Legacy Child', 'D:\rollouts\legacy.jsonl', 310, 310000, 510000, 0, NULL, 4),
                ('archived-child', 'Archived Child', 'D:\rollouts\archived-child.jsonl', 320, 320000, 520000, 1, NULL, 5),
                ('archived-thread', 'Archived', 'D:\rollouts\archived.jsonl', 400, 400000, 400000, 1, 'user', 3);
            """;

        private const string SpawnEdgesSql = """
            CREATE TABLE thread_spawn_edges (
                parent_thread_id TEXT NOT NULL,
                child_thread_id TEXT NOT NULL PRIMARY KEY,
                status TEXT NOT NULL
            );
            INSERT INTO thread_spawn_edges VALUES
                ('new-thread', 'subagent-thread', 'open'),
                ('new-thread', 'legacy-child', 'closed'),
                ('new-thread', 'archived-child', 'closed');
            """;
    }
}
