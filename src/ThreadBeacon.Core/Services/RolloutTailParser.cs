using System.Globalization;
using System.Text;
using System.Text.Json;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public sealed class RolloutTailParser : IRolloutTailParser
{
    public const int MaximumBytes = 2 * 1024 * 1024;

    public RolloutLoadResult Parse(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            return Result(RolloutSourceStatus.Missing);
        }

        try
        {
            using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.SequentialScan);
            long length = stream.Length;
            long start = Math.Max(0, length - MaximumBytes);
            stream.Seek(start, SeekOrigin.Begin);
            var buffer = new byte[checked((int)(length - start))];
            stream.ReadExactly(buffer);

            ReadOnlySpan<byte> content = buffer;
            if (start > 0)
            {
                int newline = content.IndexOf((byte)'\n');
                if (newline < 0)
                {
                    return new RolloutLoadResult(
                        RolloutSourceStatus.Healthy,
                        RolloutObservation.Empty);
                }

                content = content[(newline + 1)..];
            }

            return new RolloutLoadResult(
                RolloutSourceStatus.Healthy,
                ParseLines(ReadLines(content)));
        }
        catch (Exception exception) when (
            exception is FileNotFoundException
                or DirectoryNotFoundException)
        {
            return Result(RolloutSourceStatus.Missing);
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or OverflowException)
        {
            return Result(RolloutSourceStatus.Unavailable);
        }
    }

    public RolloutObservation ParseLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        DateTimeOffset? latestTurn = null;
        DateTimeOffset? latestFinal = null;
        DateTimeOffset? latestEvent = null;
        DateTimeOffset? latestCompletion = null;
        DateTimeOffset? latestTaskStarted = null;
        DateTimeOffset? latestInterruption = null;
        TokenUsage? latestTokenUsage = null;
        DateTimeOffset? latestTokenEvent = null;
        TokenUsage? currentTurnBaseline = null;
        string? latestModel = null;
        string? latestReasoningEffort = null;
        var compactionEvents = new List<DateTimeOffset>();

        foreach (string line in lines)
        {
            if (!TryReadEnvelope(line, out JsonDocument? document, out DateTimeOffset timestamp)
                || document is null)
            {
                continue;
            }

            using (document)
            {
                JsonElement root = document.RootElement;
                latestEvent = Latest(latestEvent, timestamp);
                string? envelopeType = GetString(root, "type");

                if (envelopeType is "compacted")
                {
                    compactionEvents.Add(timestamp);
                }

                if (envelopeType is "turn_context")
                {
                    latestTurn = Latest(latestTurn, timestamp);
                }

                if (!root.TryGetProperty("payload", out JsonElement payload)
                    || payload.ValueKind is not JsonValueKind.Object)
                {
                    continue;
                }

                if (envelopeType is "turn_context")
                {
                    latestModel = NonEmpty(GetString(payload, "model")) ?? latestModel;
                    latestReasoningEffort = NonEmpty(GetString(payload, "effort"))
                        ?? latestReasoningEffort;
                }

                string? payloadType = GetString(payload, "type");
                if (envelopeType is "event_msg")
                {
                    switch (payloadType)
                    {
                        case "task_started":
                            currentTurnBaseline = latestTokenUsage;
                            latestTaskStarted = Latest(latestTaskStarted, timestamp);
                            break;
                        case "task_complete":
                            latestCompletion = Latest(latestCompletion, timestamp);
                            break;
                        case "turn_aborted" when GetString(payload, "reason") is "interrupted":
                            DateTimeOffset interruptionAt = timestamp;
                            if (GetString(payload, "completed_at") is { } completedAtText
                                && TryParseTimestamp(completedAtText, out DateTimeOffset completedAt))
                            {
                                interruptionAt = completedAt > interruptionAt
                                    ? completedAt
                                    : interruptionAt;
                            }

                            latestInterruption = Latest(latestInterruption, interruptionAt);
                            break;
                        case "context_compacted":
                            compactionEvents.Add(timestamp);
                            break;
                        case "token_count" when TryReadTokenUsage(payload, out TokenUsage? usage):
                            latestTokenUsage = usage;
                            latestTokenEvent = timestamp;
                            break;
                    }
                }

                if (envelopeType is "response_item"
                    && payloadType is "message"
                    && GetString(payload, "role") is "assistant"
                    && GetString(payload, "phase") is "final" or "final_answer")
                {
                    latestFinal = Latest(latestFinal, timestamp);
                }
            }
        }

        DateTimeOffset? latestRunning = LatestNullable(latestTurn, latestTaskStarted);
        DateTimeOffset? latestCompleted = LatestNullable(latestFinal, latestCompletion);

        ThreadStatus status;
        DateTimeOffset? statusChangedAt;
        if (latestCompleted is { } completed
            && IsAtLeast(completed, latestRunning)
            && IsAtLeast(completed, latestInterruption))
        {
            status = ThreadStatus.JustCompleted;
            statusChangedAt = completed;
        }
        else if (latestInterruption is { } interrupted
            && IsAtLeast(interrupted, latestRunning))
        {
            status = ThreadStatus.Interrupted;
            statusChangedAt = interrupted;
        }
        else if (latestRunning is { } running)
        {
            status = ThreadStatus.Running;
            statusChangedAt = running;
        }
        else
        {
            status = ThreadStatus.Unknown;
            statusChangedAt = null;
        }

        TokenUsageSnapshot? tokenSnapshot = latestTokenUsage is null
            ? null
            : new TokenUsageSnapshot(
                latestTokenUsage.TotalTokens,
                latestTokenUsage,
                currentTurnBaseline is null ? null : latestTokenUsage.Subtract(currentTurnBaseline),
                latestTokenEvent);

        DateTimeOffset[] distinctCompactions = compactionEvents
            .OrderBy(timestamp => timestamp)
            .Aggregate(
                new List<DateTimeOffset>(),
                (distinct, timestamp) =>
                {
                    if (distinct.Count == 0
                        || timestamp - distinct[^1] > TimeSpan.FromSeconds(1))
                    {
                        distinct.Add(timestamp);
                    }

                    return distinct;
                })
            .ToArray();

        return new RolloutObservation(
            status,
            statusChangedAt,
            latestEvent,
            latestCompletion,
            latestTaskStarted,
            tokenSnapshot,
            latestModel,
            latestReasoningEffort,
            new CompactionHistory(
                distinctCompactions.Length,
                distinctCompactions.Length == 0 ? null : distinctCompactions[^1]),
            latestInterruption);
    }

    private static IReadOnlyList<string> ReadLines(ReadOnlySpan<byte> content)
    {
        string text = Encoding.UTF8.GetString(content);
        using var reader = new StringReader(text);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }

    private static bool TryReadEnvelope(
        string line,
        out JsonDocument? document,
        out DateTimeOffset timestamp)
    {
        document = null;
        timestamp = default;
        try
        {
            document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;
            if (root.ValueKind is JsonValueKind.Object
                && GetString(root, "timestamp") is { } timestampText
                && TryParseTimestamp(timestampText, out timestamp))
            {
                return true;
            }

            document.Dispose();
            document = null;
            return false;
        }
        catch (JsonException)
        {
            document?.Dispose();
            document = null;
            return false;
        }
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset timestamp) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out timestamp);

    private static bool IsAtLeast(DateTimeOffset value, DateTimeOffset? other) =>
        other is null || value >= other.Value;

    private static bool TryReadTokenUsage(JsonElement payload, out TokenUsage? usage)
    {
        usage = null;
        if (!payload.TryGetProperty("info", out JsonElement info)
            || info.ValueKind is not JsonValueKind.Object
            || !info.TryGetProperty("total_token_usage", out JsonElement totals)
            || totals.ValueKind is not JsonValueKind.Object
            || !TryGetNonnegativeInt64(totals, "input_tokens", out long input)
            || !TryGetNonnegativeInt64(totals, "cached_input_tokens", out long cachedInput)
            || !TryGetNonnegativeInt64(totals, "output_tokens", out long output)
            || !TryGetNonnegativeInt64(totals, "reasoning_output_tokens", out long reasoningOutput)
            || !TryGetNonnegativeInt64(totals, "total_tokens", out long total)
            || cachedInput > input
            || reasoningOutput > output)
        {
            return false;
        }

        usage = new TokenUsage(input, cachedInput, output, reasoningOutput, total);
        return true;
    }

    private static bool TryGetNonnegativeInt64(
        JsonElement parent,
        string propertyName,
        out long value)
    {
        value = default;
        return parent.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind is JsonValueKind.Number
            && property.TryGetInt64(out value)
            && value >= 0;
    }

    private static string? GetString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind is JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? NonEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset Latest(DateTimeOffset? current, DateTimeOffset candidate) =>
        current is null || candidate > current ? candidate : current.Value;

    private static DateTimeOffset? LatestNullable(
        DateTimeOffset? first,
        DateTimeOffset? second) => second is null
            ? first
            : Latest(first, second.Value);

    private static RolloutLoadResult Result(RolloutSourceStatus status) =>
        new(status, RolloutObservation.Empty);
}
