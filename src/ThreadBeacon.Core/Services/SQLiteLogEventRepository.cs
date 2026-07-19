using Microsoft.Data.Sqlite;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public sealed class SQLiteLogEventRepository : ILogEventRepository
{
    private readonly string databasePath;
    private readonly LogEventParser parser;

    public SQLiteLogEventRepository(string databasePath, LogEventParser? parser = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        this.databasePath = Path.GetFullPath(databasePath);
        this.parser = parser ?? new LogEventParser();
    }

    public IReadOnlyDictionary<string, ServiceIncident> LoadLatestIncidents(
        IReadOnlySet<string> threadIds)
    {
        ArgumentNullException.ThrowIfNull(threadIds);

        string[] requestedIds = threadIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (requestedIds.Length == 0 || !File.Exists(databasePath))
        {
            return EmptyResult();
        }

        using SqliteConnection connection = OpenReadOnlyConnection();
        using SqliteCommand command = connection.CreateCommand();
        string[] parameterNames = new string[requestedIds.Length];
        for (int index = 0; index < requestedIds.Length; index++)
        {
            parameterNames[index] = $"$thread{index}";
            command.Parameters.AddWithValue(parameterNames[index], requestedIds[index]);
        }

        command.CommandText = $"""
            SELECT ts, ts_nanos, target, thread_id, feedback_log_body
            FROM logs
            WHERE thread_id IN ({string.Join(", ", parameterNames)})
              AND feedback_log_body IS NOT NULL
              AND (
                (
                  target = 'codex_http_client::default_client'
                  AND feedback_log_body LIKE '%Request completed%'
                  AND (
                    feedback_log_body LIKE '%status=200 OK%'
                    OR feedback_log_body LIKE '%status=429 Too Many Requests%'
                    OR feedback_log_body LIKE '%status=503 Service Unavailable%'
                  )
                )
                OR (
                  target = 'codex_core::responses_retry'
                  AND feedback_log_body LIKE '%retrying sampling request (%/%'
                )
                OR (
                  target = 'codex_core::session::turn'
                  AND feedback_log_body LIKE '%Turn error:%'
                  AND (
                    feedback_log_body LIKE '%status 429 Too Many Requests%'
                    OR feedback_log_body LIKE '%status 503 Service Unavailable%'
                  )
                )
              )
            ORDER BY ts, ts_nanos, id;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        var records = new List<LogEventRecord>();
        while (reader.Read())
        {
            long seconds = reader.GetInt64(0);
            long nanoseconds = reader.GetInt64(1);
            DateTimeOffset occurredAt = DateTimeOffset
                .FromUnixTimeSeconds(seconds)
                .AddTicks(nanoseconds / 100);
            records.Add(new LogEventRecord(
                reader.GetString(3),
                occurredAt,
                reader.GetString(2),
                reader.GetString(4)));
        }

        return parser.LatestIncidents(records);
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

    private static IReadOnlyDictionary<string, ServiceIncident> EmptyResult() =>
        new Dictionary<string, ServiceIncident>(StringComparer.Ordinal);
}
