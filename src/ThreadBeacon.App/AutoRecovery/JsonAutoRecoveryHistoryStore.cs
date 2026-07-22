using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThreadBeacon.App.AutoRecovery;

public sealed class JsonAutoRecoveryHistoryStore : IAutoRecoveryHistoryStore
{
    public const int MaximumEntries = 100;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string historyPath;
    private readonly object sync = new();

    public JsonAutoRecoveryHistoryStore(string historyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(historyPath);
        this.historyPath = Path.GetFullPath(historyPath);
    }

    public static JsonAutoRecoveryHistoryStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new JsonAutoRecoveryHistoryStore(
            Path.Combine(root, "ThreadBeacon", "auto-recovery-history.json"));
    }

    public IReadOnlyList<AutoRecoveryHistoryEntry> Load()
    {
        lock (sync)
        {
            try
            {
                AutoRecoveryHistoryEntry[] entries = JsonSerializer.Deserialize<AutoRecoveryHistoryEntry[]>(
                    File.ReadAllText(historyPath),
                    SerializerOptions) ?? [];
                return entries
                    .Where(IsValid)
                    .OrderByDescending(entry => entry.UpdatedAt)
                    .Take(MaximumEntries)
                    .ToArray();
            }
            catch (Exception exception) when (IsStorageException(exception))
            {
                return [];
            }
        }
    }

    public bool Upsert(AutoRecoveryHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!IsValid(entry))
        {
            return false;
        }

        lock (sync)
        {
            try
            {
                List<AutoRecoveryHistoryEntry> entries = Load().ToList();
                entries.RemoveAll(existing => StringComparer.Ordinal.Equals(
                    existing.AttemptId,
                    entry.AttemptId));
                entries.Add(entry);
                AutoRecoveryHistoryEntry[] bounded = entries
                    .OrderByDescending(existing => existing.UpdatedAt)
                    .Take(MaximumEntries)
                    .ToArray();
                Write(bounded);
                return true;
            }
            catch (Exception exception) when (IsStorageException(exception))
            {
                return false;
            }
        }
    }

    private void Write(IReadOnlyList<AutoRecoveryHistoryEntry> entries)
    {
        string? directory = Path.GetDirectoryName(historyPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(entries, SerializerOptions);
        string temporaryPath = $"{historyPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, historyPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static bool IsValid(AutoRecoveryHistoryEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.AttemptId)
        && !string.IsNullOrWhiteSpace(entry.ThreadId)
        && !string.IsNullOrWhiteSpace(entry.EpisodeId)
        && (entry.DiagnosticCode is null
            || AutoRecoveryDiagnosticCodes.IsAllowed(entry.DiagnosticCode));

    private static bool IsStorageException(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException
            or ArgumentException;
}
