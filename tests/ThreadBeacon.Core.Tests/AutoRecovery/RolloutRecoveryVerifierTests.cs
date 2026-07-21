using System.Text;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.Core.Tests.AutoRecovery;

public sealed class RolloutRecoveryVerifierTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jsonl");

    [Fact]
    public void ReadEvidence_AcceptsCodexTrailingNewlineAndTaskStarted()
    {
        File.WriteAllText(path, Line("event_msg", "task_complete"), Encoding.UTF8);
        RolloutRecoveryCheckpoint checkpoint = RolloutRecoveryVerifier.Capture(path);
        File.AppendAllText(
            path,
            Line("event_msg", "user_message", "Continue safely.\n") +
            Line("event_msg", "task_started"),
            Encoding.UTF8);

        RolloutRecoveryEvidence evidence = RolloutRecoveryVerifier.ReadEvidence(
            path,
            checkpoint,
            "Continue safely.");

        Assert.True(evidence.HasExpectedUserMessage);
        Assert.True(evidence.HasTaskStarted);
        Assert.True(evidence.IsVerified);
    }

    [Fact]
    public void ReadEvidence_RejectsDifferentMessageEvenWhenTaskStarted()
    {
        File.WriteAllText(path, string.Empty, Encoding.UTF8);
        RolloutRecoveryCheckpoint checkpoint = RolloutRecoveryVerifier.Capture(path);
        File.AppendAllText(
            path,
            Line("event_msg", "user_message", "Different text") +
            Line("event_msg", "task_started"),
            Encoding.UTF8);

        RolloutRecoveryEvidence evidence = RolloutRecoveryVerifier.ReadEvidence(
            path,
            checkpoint,
            "Continue safely.");

        Assert.False(evidence.HasExpectedUserMessage);
        Assert.True(evidence.HasTaskStarted);
        Assert.False(evidence.IsVerified);
    }

    [Fact]
    public void ReadEvidence_DoesNotCountEventsBeforeCheckpoint()
    {
        File.WriteAllText(
            path,
            Line("event_msg", "user_message", "Continue safely.") +
            Line("event_msg", "task_started"),
            Encoding.UTF8);
        RolloutRecoveryCheckpoint checkpoint = RolloutRecoveryVerifier.Capture(path);

        RolloutRecoveryEvidence evidence = RolloutRecoveryVerifier.ReadEvidence(
            path,
            checkpoint,
            "Continue safely.");

        Assert.False(evidence.IsVerified);
    }

    public void Dispose()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string Line(string topType, string payloadType, string? message = null)
    {
        string messageProperty = message is null
            ? string.Empty
            : $",\"message\":{System.Text.Json.JsonSerializer.Serialize(message)}";
        return $"{{\"type\":\"{topType}\",\"payload\":{{\"type\":\"{payloadType}\"{messageProperty}}}}}\n";
    }
}
