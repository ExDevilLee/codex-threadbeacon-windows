using System.Text.Json;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public sealed class CompactionHookEventHandler
{
    private readonly CompactionActivityRepository repository;
    private readonly TimeProvider timeProvider;

    public CompactionHookEventHandler(
        CompactionActivityRepository? repository = null,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? new CompactionActivityRepository();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool TryHandle(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload.TrimStart('\uFEFF'));
            JsonElement root = document.RootElement;
            if (root.ValueKind is not JsonValueKind.Object
                || !TryReadString(root, "hook_event_name", out string eventName)
                || !TryReadIdentifier(root, "session_id", out string sessionId)
                || !TryReadIdentifier(root, "turn_id", out string turnId)
                || !TryReadString(root, "trigger", out string trigger)
                || trigger is not ("manual" or "auto"))
            {
                return false;
            }

            if (eventName is "PreCompact")
            {
                repository.WritePreCompact(new CompactionActivity(
                    sessionId,
                    turnId,
                    trigger,
                    timeProvider.GetUtcNow()));
                return true;
            }

            if (eventName is "PostCompact")
            {
                repository.ClearPostCompact(sessionId, turnId);
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryReadIdentifier(
        JsonElement root,
        string propertyName,
        out string value)
    {
        if (!TryReadString(root, propertyName, out string raw)
            || !Guid.TryParseExact(raw, "D", out Guid identifier))
        {
            value = string.Empty;
            return false;
        }

        value = identifier.ToString("D");
        return true;
    }

    private static bool TryReadString(
        JsonElement root,
        string propertyName,
        out string value)
    {
        if (root.TryGetProperty(propertyName, out JsonElement element)
            && element.ValueKind is JsonValueKind.String
            && !string.IsNullOrWhiteSpace(element.GetString()))
        {
            value = element.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
