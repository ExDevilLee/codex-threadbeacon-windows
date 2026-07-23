using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public sealed class JsonAutoRecoveryCircuitStore : IAutoRecoveryCircuitStore
{
    private const int MaximumStoredAttemptCount = 20;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string statePath;
    private readonly object sync = new();
    private readonly Dictionary<string, AutoRecoveryCircuitState> states;

    public JsonAutoRecoveryCircuitStore(string statePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statePath);
        this.statePath = Path.GetFullPath(statePath);
        states = Read()
            .Where(IsValid)
            .Select(Normalize)
            .GroupBy(state => state.Id, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(state => state.LastAttemptAt)
                .First())
            .ToDictionary(state => state.Id, StringComparer.Ordinal);
    }

    public static JsonAutoRecoveryCircuitStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new JsonAutoRecoveryCircuitStore(
            Path.Combine(root, "ThreadBeacon", "auto-recovery-circuits.json"));
    }

    public IReadOnlyList<AutoRecoveryCircuitState> Load()
    {
        lock (sync)
        {
            return states.Values
                .OrderByDescending(state => state.LastAttemptAt)
                .ThenBy(state => state.Id, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public AutoRecoveryCircuitState? StateFor(
        string threadId,
        AutoRecoveryIncidentType incidentType)
    {
        lock (sync)
        {
            return states.GetValueOrDefault(
                AutoRecoveryCircuitState.IdFor(threadId, incidentType));
        }
    }

    public AutoRecoveryCircuitState RecordAttempt(
        AutoRecoveryCandidate candidate,
        DateTimeOffset attemptedAt)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        lock (sync)
        {
            string id = AutoRecoveryCircuitState.IdFor(
                candidate.ThreadId,
                candidate.IncidentType);
            if (states.TryGetValue(id, out AutoRecoveryCircuitState? current)
                && StringComparer.Ordinal.Equals(current.LastEpisodeId, candidate.EpisodeId))
            {
                return current;
            }

            var updated = new AutoRecoveryCircuitState(
                candidate.ThreadId,
                candidate.IncidentType,
                current is null
                    ? 1
                    : Math.Min(current.AttemptCount + 1, MaximumStoredAttemptCount),
                candidate.EpisodeId,
                attemptedAt);
            states[id] = updated;
            Write();
            return updated;
        }
    }

    public void ObserveCompletion(string threadId, DateTimeOffset completedAt)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return;
        }

        lock (sync)
        {
            string[] matchingIds = states.Values
                .Where(state => StringComparer.Ordinal.Equals(state.ThreadId, threadId)
                    && completedAt > state.LastAttemptAt)
                .Select(state => state.Id)
                .ToArray();
            if (matchingIds.Length == 0)
            {
                return;
            }

            foreach (string id in matchingIds)
            {
                states.Remove(id);
            }

            Write();
        }
    }

    public void Reset(string threadId, AutoRecoveryIncidentType incidentType)
    {
        lock (sync)
        {
            if (states.Remove(AutoRecoveryCircuitState.IdFor(threadId, incidentType)))
            {
                Write();
            }
        }
    }

    private AutoRecoveryCircuitState[] Read()
    {
        try
        {
            return JsonSerializer.Deserialize<AutoRecoveryCircuitState[]>(
                File.ReadAllText(statePath),
                SerializerOptions) ?? [];
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            return [];
        }
    }

    private void Write()
    {
        try
        {
            string? directory = Path.GetDirectoryName(statePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = $"{statePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(Load(), SerializerOptions));
                File.Move(temporaryPath, statePath, overwrite: true);
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            // Circuit persistence is best effort and never breaks monitoring.
        }
    }

    private static bool IsValid(AutoRecoveryCircuitState state) =>
        !string.IsNullOrWhiteSpace(state.ThreadId)
        && Enum.IsDefined(state.IncidentType)
        && state.AttemptCount > 0
        && !string.IsNullOrWhiteSpace(state.LastEpisodeId);

    private static AutoRecoveryCircuitState Normalize(AutoRecoveryCircuitState state) =>
        state with
        {
            AttemptCount = Math.Min(state.AttemptCount, MaximumStoredAttemptCount),
        };

    private static bool IsStorageException(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException
            or ArgumentException;
}
