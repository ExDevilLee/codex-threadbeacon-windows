using System.Globalization;
using System.Text.Json;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public sealed class CompactionActivityRepository
{
    public static readonly TimeSpan MaximumAge = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string activeDirectory;

    public CompactionActivityRepository(string? activeDirectory = null)
    {
        this.activeDirectory = Path.GetFullPath(
            activeDirectory
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ThreadBeacon",
                    "compaction",
                    "v1",
                    "active"));
    }

    public void WritePreCompact(CompactionActivity activity)
    {
        if (!IsValid(activity))
        {
            return;
        }

        Directory.CreateDirectory(activeDirectory);
        string path = MarkerPath(activity.SessionId);
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        var marker = new Marker(1, activity.SessionId, activity.TurnId, activity.Trigger, activity.StartedAt);
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(marker, JsonOptions));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public void ClearPostCompact(CompactionActivity activity)
    {
        if (!IsValid(activity))
        {
            return;
        }

        ClearPostCompact(activity.SessionId, activity.TurnId);
    }

    public void ClearPostCompact(string sessionId, string turnId)
    {
        if (!IsValidId(sessionId) || !IsValidId(turnId))
        {
            return;
        }

        string path = MarkerPath(sessionId);
        try
        {
            Marker? marker = ReadMarker(path);
            if (marker is not null
                && StringComparer.Ordinal.Equals(marker.TurnId, turnId))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public CompactionActivity? Read(
        string sessionId,
        DateTimeOffset? completionEvidenceAt,
        DateTimeOffset? interruptionEvidenceAt,
        DateTimeOffset now)
    {
        if (!IsValidId(sessionId))
        {
            return null;
        }

        string path = MarkerPath(sessionId);
        try
        {
            Marker? marker = ReadMarker(path);
            if (marker is null)
            {
                return null;
            }

            DateTimeOffset startedAt = marker.StartedAt;
            bool expired = now - startedAt > MaximumAge || startedAt > now;
            bool superseded = (completionEvidenceAt is { } completed && completed >= startedAt)
                || (interruptionEvidenceAt is { } interrupted && interrupted >= startedAt);
            if (expired || superseded)
            {
                TryDelete(path);
                return null;
            }

            return new CompactionActivity(marker.SessionId, marker.TurnId, marker.Trigger, startedAt);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private string MarkerPath(string sessionId) => Path.Combine(activeDirectory, $"{sessionId}.json");

    private static bool IsValid(CompactionActivity activity) =>
        IsValidId(activity.SessionId)
        && IsValidId(activity.TurnId)
        && (activity.Trigger is "manual" or "auto")
        && activity.StartedAt != default;

    private static bool IsValidId(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.IndexOfAny(['/', '\\', ':', '*', '?', '"', '<', '>', '|']) < 0;

    private static Marker? ReadMarker(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = document.RootElement;
            if (root.ValueKind is not JsonValueKind.Object
                || !root.TryGetProperty("schemaVersion", out JsonElement schemaVersion)
                || schemaVersion.GetInt32() != 1)
            {
                return null;
            }

            string? sessionId = root.TryGetProperty("sessionId", out JsonElement sessionElement)
                ? sessionElement.GetString()
                : null;
            string? turnId = root.TryGetProperty("turnId", out JsonElement turnElement)
                ? turnElement.GetString()
                : null;
            string? trigger = root.TryGetProperty("trigger", out JsonElement triggerElement)
                ? triggerElement.GetString()
                : null;
            string? startedAtText = root.TryGetProperty("startedAt", out JsonElement startedElement)
                ? startedElement.GetString()
                : null;
            if (!IsValidId(sessionId ?? string.Empty)
                || !IsValidId(turnId ?? string.Empty)
                || trigger is not ("manual" or "auto")
                || !DateTimeOffset.TryParse(
                    startedAtText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset startedAt))
            {
                return null;
            }

            return new Marker(1, sessionId!, turnId!, trigger, startedAt);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record Marker(
        int SchemaVersion,
        string SessionId,
        string TurnId,
        string Trigger,
        DateTimeOffset StartedAt);
}
