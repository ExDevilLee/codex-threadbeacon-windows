using System.Text;
using System.Text.Json;

namespace ThreadBeacon.Core.AutoRecovery;

public sealed record RolloutRecoveryCheckpoint(long Length);

public sealed record RolloutRecoveryEvidence(
    bool HasExpectedUserMessage,
    bool HasTaskStarted)
{
    public bool IsVerified => HasExpectedUserMessage && HasTaskStarted;
}

public static class RolloutRecoveryVerifier
{
    public const int MaximumEvidenceBytes = 2 * 1024 * 1024;

    public static RolloutRecoveryCheckpoint Capture(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using FileStream stream = OpenRead(path);
        return new RolloutRecoveryCheckpoint(stream.Length);
    }

    public static RolloutRecoveryEvidence ReadEvidence(
        string path,
        RolloutRecoveryCheckpoint checkpoint,
        string expectedMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(checkpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedMessage);
        using FileStream stream = OpenRead(path);
        if (stream.Length < checkpoint.Length
            || stream.Length - checkpoint.Length > MaximumEvidenceBytes)
        {
            return new RolloutRecoveryEvidence(false, false);
        }

        stream.Seek(checkpoint.Length, SeekOrigin.Begin);
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(false, true),
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: false);
        bool hasMessage = false;
        bool hasTaskStarted = false;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(line);
                if (!document.RootElement.TryGetProperty("payload", out JsonElement payload)
                    || !payload.TryGetProperty("type", out JsonElement typeElement))
                {
                    continue;
                }

                string? type = typeElement.GetString();
                if (type == "user_message"
                    && payload.TryGetProperty("message", out JsonElement messageElement)
                    && IsExpectedMessage(messageElement.GetString(), expectedMessage))
                {
                    hasMessage = true;
                }
                else if (type == "task_started")
                {
                    hasTaskStarted = true;
                }
            }
            catch (JsonException)
            {
                return new RolloutRecoveryEvidence(false, false);
            }
        }

        return new RolloutRecoveryEvidence(hasMessage, hasTaskStarted);
    }

    private static bool IsExpectedMessage(string? actual, string expected)
    {
        if (actual is null || !actual.StartsWith(expected, StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> suffix = actual.AsSpan(expected.Length);
        return suffix.Length <= 2 && suffix.IndexOfAnyExcept('\r', '\n') < 0;
    }

    private static FileStream OpenRead(string path) => new(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite | FileShare.Delete);
}
