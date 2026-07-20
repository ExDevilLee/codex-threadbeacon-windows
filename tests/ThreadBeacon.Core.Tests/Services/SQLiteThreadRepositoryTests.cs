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

    [Fact]
    public void LoadByIds_ReturnsOnlyRequestedActivePrimaryThreads()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();
        var repository = new SQLiteThreadRepository(database.Path);

        ThreadLoadResult result = repository.LoadByIds(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "new-thread",
                "archived-thread",
                "subagent-thread",
                "new-thread' OR 1=1 --",
            });

        ThreadRecord record = Assert.Single(result.Threads);
        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.Equal("new-thread", record.Id);
        Assert.Equal(3, record.SubagentCount);
        Assert.False(record.IsArchived);
    }

    [Fact]
    public void LoadByIdsIncludingArchived_ReturnsRequestedArchivedPrimaryThread()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();
        var repository = new SQLiteThreadRepository(database.Path);

        ThreadLoadResult result = repository.LoadByIdsIncludingArchived(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "archived-thread",
                "subagent-thread",
                "archived-thread' OR 1=1 --",
            });

        ThreadRecord record = Assert.Single(result.Threads);
        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.Equal("archived-thread", record.Id);
        Assert.True(record.IsArchived);
    }

    [Fact]
    public void LoadByIds_EmptySetReturnsHealthyEmptyResult()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();

        ThreadLoadResult result = new SQLiteThreadRepository(database.Path)
            .LoadByIds(new HashSet<string>(StringComparer.Ordinal));

        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.Empty(result.Threads);
    }

    [Fact]
    public void LoadDetachedSubagentCandidates_ReturnsOnlyActiveUnlinkedSubagents()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();

        ThreadLoadResult result = new SQLiteThreadRepository(database.Path)
            .LoadDetachedSubagentCandidates(8);

        ThreadRecord record = Assert.Single(result.Threads);
        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.Equal("orphan-subagent", record.Id);
        Assert.False(record.IsArchived);
        Assert.Equal(0, record.SubagentCount);
    }

    [Fact]
    public void LoadDetachedSubagentCandidates_ReturnsEmptyWhenSpawnEdgesTableIsMissing()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create(
            includeSpawnEdges: false);

        ThreadLoadResult result = new SQLiteThreadRepository(database.Path)
            .LoadDetachedSubagentCandidates(8);

        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.Empty(result.Threads);
    }

    [Fact]
    public void LoadDirectSubagents_ReturnsOnlyRequestedParentsInRecencyOrder()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();
        var repository = new SQLiteThreadRepository(database.Path);

        SubagentLoadResult result = repository.LoadDirectSubagents(
            new HashSet<string>(StringComparer.Ordinal) { "new-thread" });

        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        IReadOnlyList<SubagentRecord> records = result.SubagentsByParent["new-thread"];
        Assert.Collection(
            records,
            record =>
            {
                Assert.Equal("archived-child", record.Id);
                Assert.Equal("new-thread", record.ParentId);
                Assert.Equal("reviewer", record.AgentNickname);
                Assert.Equal("explorer", record.AgentRole);
                Assert.Equal("gpt-test", record.Model);
                Assert.Equal("high", record.ReasoningEffort);
            },
            record => Assert.Equal("legacy-child", record.Id),
            record => Assert.Equal("subagent-thread", record.Id));
    }

    [Fact]
    public void LoadDirectSubagents_ReturnsEmptyForEmptyParentSet()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create();

        SubagentLoadResult result = new SQLiteThreadRepository(database.Path)
            .LoadDirectSubagents(new HashSet<string>(StringComparer.Ordinal));

        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.Empty(result.SubagentsByParent);
    }

    [Fact]
    public void LoadDirectSubagents_FallsBackWhenSpawnEdgesTableIsMissing()
    {
        using TemporaryThreadDatabase database = TemporaryThreadDatabase.Create(includeSpawnEdges: false);

        SubagentLoadResult result = new SQLiteThreadRepository(database.Path)
            .LoadDirectSubagents(new HashSet<string>(StringComparer.Ordinal) { "new-thread" });

        Assert.Equal(ThreadRepositoryStatus.Healthy, result.Status);
        Assert.Empty(result.SubagentsByParent);
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
                tokens_used INTEGER NOT NULL DEFAULT 0,
                agent_nickname TEXT,
                agent_role TEXT,
                model TEXT,
                reasoning_effort TEXT
            );
            INSERT INTO threads VALUES
                ('older-thread', 'Older', 'D:\rollouts\older.jsonl', 100, 100000, 100000, 0, 'user', 1, NULL, NULL, NULL, NULL),
                ('new-thread', 'New', 'D:\rollouts\new.jsonl', 200, 200000, 300000, 0, 'user', 70808875, NULL, NULL, NULL, NULL),
                ('subagent-thread', 'Child', 'D:\rollouts\child.jsonl', 300, 300000, 500000, 0, 'subagent', 2, 'worker', 'reviewer', 'gpt-test', 'medium'),
                ('orphan-subagent', 'Detached', 'D:\rollouts\detached.jsonl', 330, 330000, 530000, 0, 'subagent', 6, NULL, NULL, NULL, NULL),
                ('archived-orphan-subagent', 'Archived Detached', 'D:\rollouts\archived-detached.jsonl', 340, 340000, 540000, 1, 'subagent', 7, NULL, NULL, NULL, NULL),
                ('legacy-child', 'Legacy Child', 'D:\rollouts\legacy.jsonl', 310, 310000, 510000, 0, NULL, 4, NULL, NULL, NULL, NULL),
                ('archived-child', 'Archived Child', 'D:\rollouts\archived-child.jsonl', 320, 320000, 520000, 1, NULL, 5, 'reviewer', 'explorer', 'gpt-test', 'high'),
                ('archived-thread', 'Archived', 'D:\rollouts\archived.jsonl', 400, 400000, 400000, 1, 'user', 3, NULL, NULL, NULL, NULL);
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
