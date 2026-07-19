using Microsoft.Data.Sqlite;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class SQLiteLogEventRepositoryTests
{
    [Fact]
    public void LoadLatestIncidents_ReadsOnlyRequestedThreadsAndAllowedEvents()
    {
        using TemporaryLogDatabase database = TemporaryLogDatabase.Create();
        var repository = new SQLiteLogEventRepository(database.Path);

        IReadOnlyDictionary<string, ServiceIncident> incidents = repository.LoadLatestIncidents(
            new HashSet<string>(StringComparer.Ordinal) { "thread-a", "thread-b" });

        Assert.Equal(2, incidents.Count);
        Assert.Equal(ServiceIncidentPhase.Failed, incidents["thread-a"].Phase);
        Assert.Equal(503, incidents["thread-a"].HttpStatusCode);
        Assert.Equal(ServiceIncidentPhase.Retrying, incidents["thread-b"].Phase);
        Assert.Equal(429, incidents["thread-b"].HttpStatusCode);
        Assert.DoesNotContain("thread-c", incidents.Keys);
    }

    [Fact]
    public void LoadLatestIncidents_ReturnsEmptyWithoutOpeningDatabaseForEmptyIds()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
        var repository = new SQLiteLogEventRepository(missingPath);

        IReadOnlyDictionary<string, ServiceIncident> incidents = repository.LoadLatestIncidents(
            new HashSet<string>(StringComparer.Ordinal));

        Assert.Empty(incidents);
        Assert.False(File.Exists(missingPath));
    }

    [Fact]
    public void LoadLatestIncidents_ReturnsEmptyForMissingDatabase()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
        var repository = new SQLiteLogEventRepository(missingPath);

        IReadOnlyDictionary<string, ServiceIncident> incidents = repository.LoadLatestIncidents(
            new HashSet<string>(StringComparer.Ordinal) { "thread-a" });

        Assert.Empty(incidents);
        Assert.False(File.Exists(missingPath));
    }

    private sealed class TemporaryLogDatabase : IDisposable
    {
        private TemporaryLogDatabase(string path) => Path = path;

        public string Path { get; }

        public static TemporaryLogDatabase Create()
        {
            var database = new TemporaryLogDatabase(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite"));
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = database.Path,
                Pooling = false,
            }.ConnectionString);
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = SchemaAndRows;
            command.ExecuteNonQuery();
            return database;
        }

        public void Dispose()
        {
            File.Delete(Path);
            File.Delete($"{Path}-shm");
            File.Delete($"{Path}-wal");
        }

        private const string SchemaAndRows = """
            CREATE TABLE logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                ts INTEGER NOT NULL,
                ts_nanos INTEGER NOT NULL,
                level TEXT NOT NULL,
                target TEXT NOT NULL,
                feedback_log_body TEXT,
                module_path TEXT,
                file TEXT,
                line INTEGER,
                thread_id TEXT,
                process_uuid TEXT,
                estimated_bytes INTEGER NOT NULL DEFAULT 0
            );
            INSERT INTO logs (ts, ts_nanos, level, target, feedback_log_body, thread_id) VALUES
                (100, 0, 'DEBUG', 'codex_http_client::default_client',
                 'turn{turn.id=turn-a}: Request completed status=503 Service Unavailable', 'thread-a'),
                (101, 0, 'INFO', 'codex_core::responses_retry',
                 'turn{turn.id=turn-a}: retrying sampling request (5/5 in 3s)...', 'thread-a'),
                (102, 0, 'INFO', 'codex_core::session::turn',
                 'turn{turn.id=turn-a}: Turn error: unexpected status 503 Service Unavailable', 'thread-a'),
                (103, 0, 'TRACE', 'codex_http_client::transport',
                 'turn{turn.id=turn-a}: status=429 Too Many Requests private body', 'thread-a'),
                (104, 0, 'DEBUG', 'codex_http_client::default_client',
                 'turn{turn.id=ignored}: Request completed status=418 Teapot', 'thread-a'),
                (200, 250000000, 'DEBUG', 'codex_http_client::default_client',
                 'turn{turn.id=turn-b}: Request completed status=429 Too Many Requests', 'thread-b'),
                (201, 500000000, 'INFO', 'codex_core::responses_retry',
                 'turn{turn.id=turn-b}: retrying sampling request (2/5 in 500ms)...', 'thread-b'),
                (300, 0, 'INFO', 'codex_core::session::turn',
                 'turn{turn.id=turn-c}: Turn error: unexpected status 503 Service Unavailable', 'thread-c');
            """;
    }
}
