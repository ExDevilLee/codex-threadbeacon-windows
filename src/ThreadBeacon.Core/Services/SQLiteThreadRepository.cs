using Microsoft.Data.Sqlite;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public sealed class SQLiteThreadRepository : IThreadRepository
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;
    private const int SqliteSchemaError = 1;

    private readonly string databasePath;

    public SQLiteThreadRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        this.databasePath = Path.GetFullPath(databasePath);
    }

    public ThreadLoadResult LoadRecent(int limit = 8)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        if (!File.Exists(databasePath))
        {
            return Result(ThreadRepositoryStatus.Missing);
        }

        try
        {
            using SqliteConnection connection = OpenReadOnlyConnection();
            bool hasSpawnEdges = HasTable(connection, "thread_spawn_edges");
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = hasSpawnEdges ? RelationshipAwareSql : LegacySql;
            command.Parameters.AddWithValue("$limit", limit);

            using SqliteDataReader reader = command.ExecuteReader();
            var records = new List<ThreadRecord>(Math.Min(limit, 256));
            while (reader.Read())
            {
                records.Add(ReadRecord(reader));
            }

            return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records.ToArray());
        }
        catch (SqliteException exception) when (
            exception.SqliteErrorCode is SqliteBusy or SqliteLocked)
        {
            return Result(ThreadRepositoryStatus.Busy);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is SqliteSchemaError)
        {
            return Result(ThreadRepositoryStatus.Incompatible);
        }
        catch (Exception exception) when (
            exception is SqliteException
                or IOException
                or UnauthorizedAccessException)
        {
            return Result(ThreadRepositoryStatus.Unavailable);
        }
        catch (Exception exception) when (
            exception is InvalidCastException
                or OverflowException
                or ArgumentOutOfRangeException)
        {
            return Result(ThreadRepositoryStatus.Incompatible);
        }
    }

    private SqliteConnection OpenReadOnlyConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            DefaultTimeout = 1,
        };
        var connection = new SqliteConnection(builder.ConnectionString);
        try
        {
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA query_only = ON;";
            command.ExecuteNonQuery();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static bool HasTable(SqliteConnection connection, string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table' AND name = $tableName
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$tableName", tableName);
        return command.ExecuteScalar() is not null;
    }

    private static ThreadRecord ReadRecord(SqliteDataReader reader)
    {
        long updatedAtMilliseconds = reader.GetInt64(3);
        long subagentCountValue = reader.GetInt64(5);
        int subagentCount = checked((int)subagentCountValue);
        if (subagentCount < 0)
        {
            throw new InvalidCastException("Subagent count cannot be negative.");
        }

        return new ThreadRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            DateTimeOffset.FromUnixTimeMilliseconds(updatedAtMilliseconds),
            reader.GetInt64(4),
            subagentCount);
    }

    private static ThreadLoadResult Result(ThreadRepositoryStatus status) =>
        new(status, Array.Empty<ThreadRecord>());

    private const string RelationshipAwareSql = """
        SELECT t.id,
               t.title,
               t.rollout_path,
               COALESCE(t.updated_at_ms, t.updated_at * 1000),
               COALESCE(t.tokens_used, 0),
               COALESCE(children.child_count, 0)
        FROM threads AS t
        LEFT JOIN (
            SELECT parent_thread_id, COUNT(*) AS child_count
            FROM thread_spawn_edges
            GROUP BY parent_thread_id
        ) AS children ON children.parent_thread_id = t.id
        WHERE t.archived = 0
          AND COALESCE(t.thread_source, '') <> 'subagent'
          AND NOT EXISTS (
              SELECT 1
              FROM thread_spawn_edges AS edge
              WHERE edge.child_thread_id = t.id
          )
        ORDER BY t.recency_at_ms DESC, t.id DESC
        LIMIT $limit;
        """;

    private const string LegacySql = """
        SELECT id,
               title,
               rollout_path,
               COALESCE(updated_at_ms, updated_at * 1000),
               COALESCE(tokens_used, 0),
               0
        FROM threads
        WHERE archived = 0
          AND COALESCE(thread_source, '') <> 'subagent'
        ORDER BY recency_at_ms DESC, id DESC
        LIMIT $limit;
        """;
}
