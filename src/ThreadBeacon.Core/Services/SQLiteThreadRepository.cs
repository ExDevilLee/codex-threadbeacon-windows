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

    public ThreadLoadResult LoadByIds(IReadOnlySet<string> threadIds) =>
        LoadByIds(threadIds, includeArchived: false);

    public ThreadLoadResult LoadByIdsIncludingArchived(IReadOnlySet<string> threadIds) =>
        LoadByIds(threadIds, includeArchived: true);

    private ThreadLoadResult LoadByIds(
        IReadOnlySet<string> threadIds,
        bool includeArchived)
    {
        ArgumentNullException.ThrowIfNull(threadIds);
        string[] requestedIds = threadIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (requestedIds.Length == 0)
        {
            return Result(ThreadRepositoryStatus.Healthy);
        }

        if (!File.Exists(databasePath))
        {
            return Result(ThreadRepositoryStatus.Missing);
        }

        try
        {
            using SqliteConnection connection = OpenReadOnlyConnection();
            bool hasSpawnEdges = HasTable(connection, "thread_spawn_edges");
            using SqliteCommand command = connection.CreateCommand();
            string[] parameterNames = new string[requestedIds.Length];
            for (int index = 0; index < requestedIds.Length; index++)
            {
                parameterNames[index] = $"$thread{index}";
                command.Parameters.AddWithValue(parameterNames[index], requestedIds[index]);
            }

            string relationshipFilter = hasSpawnEdges
                ? "AND NOT EXISTS (SELECT 1 FROM thread_spawn_edges AS edge WHERE edge.child_thread_id = t.id)"
                : string.Empty;
            string subagentCount = hasSpawnEdges
                ? "(SELECT COUNT(*) FROM thread_spawn_edges AS child_edge WHERE child_edge.parent_thread_id = t.id)"
                : "0";
            string archiveFilter = includeArchived ? string.Empty : "AND t.archived = 0";
            command.CommandText = $"""
                SELECT t.id,
                       t.title,
                       t.rollout_path,
                       COALESCE(t.updated_at_ms, t.updated_at * 1000),
                       COALESCE(t.tokens_used, 0),
                       {subagentCount},
                       t.archived,
                       t.model,
                       t.reasoning_effort
                FROM threads AS t
                WHERE t.id IN ({string.Join(", ", parameterNames)})
                  {archiveFilter}
                  AND COALESCE(t.thread_source, '') <> 'subagent'
                  {relationshipFilter}
                ORDER BY t.recency_at_ms DESC, t.id DESC;
                """;

            using SqliteDataReader reader = command.ExecuteReader();
            var records = new List<ThreadRecord>(requestedIds.Length);
            while (reader.Read())
            {
                records.Add(ReadRecord(reader));
            }

            return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records);
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
            exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            return Result(ThreadRepositoryStatus.Unavailable);
        }
        catch (Exception exception) when (
            exception is InvalidCastException or OverflowException or ArgumentOutOfRangeException)
        {
            return Result(ThreadRepositoryStatus.Incompatible);
        }
    }

    public ThreadLoadResult LoadDetachedSubagentCandidates(int limit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        if (!File.Exists(databasePath))
        {
            return Result(ThreadRepositoryStatus.Missing);
        }

        try
        {
            using SqliteConnection connection = OpenReadOnlyConnection();
            if (!HasTable(connection, "thread_spawn_edges"))
            {
                return Result(ThreadRepositoryStatus.Healthy);
            }

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = DetachedSubagentCandidateSql;
            command.Parameters.AddWithValue("$limit", limit);

            using SqliteDataReader reader = command.ExecuteReader();
            var records = new List<ThreadRecord>(Math.Min(limit, 256));
            while (reader.Read())
            {
                records.Add(ReadRecord(reader));
            }

            return new ThreadLoadResult(ThreadRepositoryStatus.Healthy, records);
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
            exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            return Result(ThreadRepositoryStatus.Unavailable);
        }
        catch (Exception exception) when (
            exception is InvalidCastException or OverflowException or ArgumentOutOfRangeException)
        {
            return Result(ThreadRepositoryStatus.Incompatible);
        }
    }

    public SubagentLoadResult LoadDirectSubagents(IReadOnlySet<string> parentIds)
    {
        ArgumentNullException.ThrowIfNull(parentIds);
        if (parentIds.Count == 0)
        {
            return SubagentResult(ThreadRepositoryStatus.Healthy);
        }

        if (!File.Exists(databasePath))
        {
            return SubagentResult(ThreadRepositoryStatus.Missing);
        }

        try
        {
            using SqliteConnection connection = OpenReadOnlyConnection();
            if (!HasTable(connection, "thread_spawn_edges"))
            {
                return SubagentResult(ThreadRepositoryStatus.Healthy);
            }

            string[] requestedIds = parentIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (requestedIds.Length == 0)
            {
                return SubagentResult(ThreadRepositoryStatus.Healthy);
            }

            using SqliteCommand command = connection.CreateCommand();
            string[] parameterNames = new string[requestedIds.Length];
            for (int index = 0; index < requestedIds.Length; index++)
            {
                parameterNames[index] = $"$parent{index}";
                command.Parameters.AddWithValue(parameterNames[index], requestedIds[index]);
            }

            command.CommandText = $"""
                SELECT edge.parent_thread_id,
                       child.id,
                       child.title,
                       child.rollout_path,
                       COALESCE(child.updated_at_ms, child.updated_at * 1000),
                       COALESCE(child.tokens_used, 0),
                       child.agent_nickname,
                       child.agent_role,
                       child.model,
                       child.reasoning_effort
                FROM thread_spawn_edges AS edge
                JOIN threads AS child ON child.id = edge.child_thread_id
                WHERE edge.parent_thread_id IN ({string.Join(", ", parameterNames)})
                ORDER BY edge.parent_thread_id,
                         child.recency_at_ms DESC,
                         child.id DESC;
                """;

            using SqliteDataReader reader = command.ExecuteReader();
            var mutable = new Dictionary<string, List<SubagentRecord>>(StringComparer.Ordinal);
            while (reader.Read())
            {
                string parentId = reader.GetString(0);
                if (!mutable.TryGetValue(parentId, out List<SubagentRecord>? records))
                {
                    records = [];
                    mutable.Add(parentId, records);
                }

                records.Add(new SubagentRecord(
                    reader.GetString(1),
                    parentId,
                    reader.GetString(2),
                    reader.GetString(3),
                    DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(4)),
                    reader.GetInt64(5),
                    ReadOptionalString(reader, 6),
                    ReadOptionalString(reader, 7),
                    ReadOptionalString(reader, 8),
                    ReadOptionalString(reader, 9)));
            }

            IReadOnlyDictionary<string, IReadOnlyList<SubagentRecord>> result = mutable
                .ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<SubagentRecord>)pair.Value.ToArray(),
                    StringComparer.Ordinal);
            return new SubagentLoadResult(ThreadRepositoryStatus.Healthy, result);
        }
        catch (SqliteException exception) when (
            exception.SqliteErrorCode is SqliteBusy or SqliteLocked)
        {
            return SubagentResult(ThreadRepositoryStatus.Busy);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is SqliteSchemaError)
        {
            return SubagentResult(ThreadRepositoryStatus.Incompatible);
        }
        catch (Exception exception) when (
            exception is SqliteException
                or IOException
                or UnauthorizedAccessException)
        {
            return SubagentResult(ThreadRepositoryStatus.Unavailable);
        }
        catch (Exception exception) when (
            exception is InvalidCastException
                or OverflowException
                or ArgumentOutOfRangeException)
        {
            return SubagentResult(ThreadRepositoryStatus.Incompatible);
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
            subagentCount,
            reader.GetInt64(6) != 0,
            ReadOptionalString(reader, 7),
            ReadOptionalString(reader, 8));
    }

    private static ThreadLoadResult Result(ThreadRepositoryStatus status) =>
        new(status, Array.Empty<ThreadRecord>());

    private static SubagentLoadResult SubagentResult(ThreadRepositoryStatus status) =>
        new(
            status,
            new Dictionary<string, IReadOnlyList<SubagentRecord>>(StringComparer.Ordinal));

    private static string? ReadOptionalString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) || string.IsNullOrWhiteSpace(reader.GetString(ordinal))
            ? null
            : reader.GetString(ordinal);

    private const string RelationshipAwareSql = """
        SELECT t.id,
               t.title,
               t.rollout_path,
               COALESCE(t.updated_at_ms, t.updated_at * 1000),
               COALESCE(t.tokens_used, 0),
               COALESCE(children.child_count, 0),
               t.archived,
               t.model,
               t.reasoning_effort
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
               0,
               archived,
               model,
               reasoning_effort
        FROM threads
        WHERE archived = 0
          AND COALESCE(thread_source, '') <> 'subagent'
        ORDER BY recency_at_ms DESC, id DESC
        LIMIT $limit;
        """;

    private const string DetachedSubagentCandidateSql = """
        SELECT t.id,
               t.title,
               t.rollout_path,
               COALESCE(t.updated_at_ms, t.updated_at * 1000),
               COALESCE(t.tokens_used, 0),
               0,
               t.archived,
               t.model,
               t.reasoning_effort
        FROM threads AS t
        WHERE t.archived = 0
          AND COALESCE(t.thread_source, '') = 'subagent'
          AND NOT EXISTS (
              SELECT 1
              FROM thread_spawn_edges AS edge
              WHERE edge.child_thread_id = t.id
          )
        ORDER BY t.recency_at_ms DESC, t.id DESC
        LIMIT $limit;
        """;
}
