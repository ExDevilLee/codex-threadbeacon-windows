using System.Reflection;
using System.Text;
using System.Text.Json;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class RolloutTailParserTests
{
    private readonly RolloutTailParser parser = new();

    [Fact]
    public void ParseLines_LatestTurnAfterFinalIsRunning()
    {
        string[] lines =
        [
            """{"timestamp":"2026-07-16T01:00:00Z","type":"response_item","payload":{"type":"message","role":"assistant","phase":"final"}}""",
            """{"timestamp":"2026-07-16T01:01:00Z","type":"turn_context","payload":{}}""",
            """{"timestamp":"2026-07-16T01:02:00Z","type":"response_item","payload":{"type":"reasoning","summary":[{"text":"private"}]}}""",
        ];

        RolloutObservation result = parser.ParseLines(lines);

        Assert.Equal(ThreadStatus.Running, result.Status);
        Assert.Equal(ParseDate("2026-07-16T01:02:00Z"), result.LatestEventAt);
    }

    [Fact]
    public void ParseLines_FirstTurnWithoutHistoricalFinalIsRunning()
    {
        string[] lines =
        [
            """{"timestamp":"2026-07-16T01:01:00Z","type":"turn_context","payload":{}}""",
        ];

        RolloutObservation result = parser.ParseLines(lines);

        Assert.Equal(ThreadStatus.Running, result.Status);
        Assert.Equal(ParseDate("2026-07-16T01:01:00Z"), result.StatusChangedAt);
    }

    [Fact]
    public void ParseLines_InterruptedTurnIsInterrupted()
    {
        RolloutObservation result = parser.ParseLines(
        [
            TaskStarted("2026-07-16T01:00:00Z"),
            TurnAborted("2026-07-16T01:01:00Z", "interrupted"),
        ]);

        Assert.Equal(ThreadStatus.Interrupted, result.Status);
        Assert.Equal(ParseDate("2026-07-16T01:01:00Z"), result.StatusChangedAt);
    }

    [Fact]
    public void ParseLines_LaterTaskStartSupersedesInterruption()
    {
        RolloutObservation result = parser.ParseLines(
        [
            TurnAborted("2026-07-16T01:00:00Z", "interrupted"),
            TaskStarted("2026-07-16T01:01:00Z"),
        ]);

        Assert.Equal(ThreadStatus.Running, result.Status);
    }

    [Theory]
    [InlineData("task_complete")]
    [InlineData("final")]
    public void ParseLines_CompletionAtSameTimeSupersedesInterruption(string completionKind)
    {
        const string timestamp = "2026-07-16T01:01:00Z";
        string completion = completionKind is "task_complete"
            ? TaskComplete(timestamp)
            : FinalMessage(timestamp, completionKind);

        RolloutObservation result = parser.ParseLines(
        [
            TaskStarted("2026-07-16T01:00:00Z"),
            TurnAborted(timestamp, "interrupted"),
            completion,
        ]);

        Assert.Equal(ThreadStatus.JustCompleted, result.Status);
        Assert.Equal(ParseDate(timestamp), result.StatusChangedAt);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("not-a-timestamp")]
    public void ParseLines_InvalidCompletedAtFallsBackToEnvelopeTimestamp(string completedAt)
    {
        string aborted = JsonSerializer.Serialize(new
        {
            timestamp = "2026-07-16T01:01:00Z",
            type = "event_msg",
            payload = new { type = "turn_aborted", reason = "interrupted", completed_at = completedAt },
        });

        RolloutObservation result = parser.ParseLines([TaskStarted("2026-07-16T01:00:00Z"), aborted]);

        Assert.Equal(ThreadStatus.Interrupted, result.Status);
        Assert.Equal(ParseDate("2026-07-16T01:01:00Z"), result.StatusChangedAt);
    }

    [Fact]
    public void ParseLines_ParseableCompletedAtCanAdvanceInterruptionTimestamp()
    {
        string aborted = JsonSerializer.Serialize(new
        {
            timestamp = "2026-07-16T01:01:00Z",
            type = "event_msg",
            payload = new
            {
                type = "turn_aborted",
                reason = "interrupted",
                completed_at = "2026-07-16T01:02:00Z",
            },
        });

        RolloutObservation result = parser.ParseLines([TaskStarted("2026-07-16T01:00:00Z"), aborted]);

        Assert.Equal(ThreadStatus.Interrupted, result.Status);
        Assert.Equal(ParseDate("2026-07-16T01:02:00Z"), result.StatusChangedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("cancelled")]
    public void ParseLines_MissingOrDifferentAbortReasonIsIgnored(string? reason)
    {
        RolloutObservation result = parser.ParseLines(
        [
            TaskStarted("2026-07-16T01:00:00Z"),
            TurnAborted("2026-07-16T01:01:00Z", reason),
        ]);

        Assert.Equal(ThreadStatus.Running, result.Status);
    }

    [Fact]
    public void ParseLines_RetainsLatestNonEmptyTaskMetadata()
    {
        string[] lines =
        [
            """{"timestamp":"2026-07-16T01:00:00Z","type":"turn_context","payload":{"model":"gpt-old","effort":"low"}}""",
            """{"timestamp":"2026-07-16T01:01:00Z","type":"turn_context","payload":{"model":"gpt-current","effort":"high"}}""",
            """{"timestamp":"2026-07-16T01:02:00Z","type":"turn_context","payload":{"model":"  ","effort":""}}""",
        ];

        RolloutObservation result = parser.ParseLines(lines);

        Assert.Equal("gpt-current", result.Model);
        Assert.Equal("high", result.ReasoningEffort);
    }

    [Theory]
    [InlineData("final")]
    [InlineData("final_answer")]
    public void ParseLines_FinalAssistantMessageIsJustCompleted(string phase)
    {
        string[] lines =
        [
            """{"timestamp":"2026-07-16T01:00:00Z","type":"turn_context","payload":{}}""",
            FinalMessage("2026-07-16T01:03:00Z", phase),
        ];

        RolloutObservation result = parser.ParseLines(lines);

        Assert.Equal(ThreadStatus.JustCompleted, result.Status);
        Assert.Equal(ParseDate("2026-07-16T01:03:00Z"), result.StatusChangedAt);
    }

    [Fact]
    public void ParseLines_TracksTaskBoundariesWithoutMessageText()
    {
        string[] lines =
        [
            """{"timestamp":"2026-07-16T01:01:00Z","type":"event_msg","payload":{"type":"task_started"}}""",
            """{"timestamp":"2026-07-16T01:02:00Z","type":"event_msg","payload":{"type":"task_complete","last_agent_message":"private"}}""",
            """{"timestamp":"2026-07-16T01:04:00Z","type":"event_msg","payload":{"type":"task_complete","last_agent_message":"new private"}}""",
        ];

        RolloutObservation result = parser.ParseLines(lines);
        string[] retainedProperties = typeof(RolloutObservation)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(ParseDate("2026-07-16T01:01:00Z"), result.LatestTaskStartedAt);
        Assert.Equal(ParseDate("2026-07-16T01:04:00Z"), result.CompletionEventAt);
        Assert.DoesNotContain("LastAgentMessage", retainedProperties);
        Assert.DoesNotContain("Summary", retainedProperties);
    }

    [Fact]
    public void ParseLines_IgnoresMalformedAndReasoningOnlyLines()
    {
        string[] lines =
        [
            "not-json",
            """{"timestamp":"invalid","type":"turn_context"}""",
            """{"timestamp":"2026-07-16T01:00:00Z","type":"response_item","payload":{"type":"reasoning","summary":[{"text":"private"}]}}""",
        ];

        RolloutObservation result = parser.ParseLines(lines);

        Assert.Equal(ThreadStatus.Unknown, result.Status);
        Assert.Equal(ParseDate("2026-07-16T01:00:00Z"), result.LatestEventAt);
    }

    [Fact]
    public void ParseLines_ComputesCurrentTurnFromCumulativeBaseline()
    {
        string[] lines =
        [
            TokenEvent("2026-07-16T01:00:00Z", 900, 400, 100, 30, 1_000),
            """{"timestamp":"2026-07-16T01:01:00Z","type":"event_msg","payload":{"type":"task_started"}}""",
            TokenEvent("2026-07-16T01:02:00Z", 1_350, 650, 150, 40, 1_500),
            TokenEvent("2026-07-16T01:03:00Z", 1_350, 650, 150, 40, 1_500),
        ];

        RolloutObservation result = parser.ParseLines(lines);

        Assert.Equal(1_500, result.TokenUsage?.TotalTokens);
        Assert.Equal(new TokenUsage(450, 250, 50, 10, 500), result.TokenUsage?.CurrentTurn);
        Assert.Equal(ParseDate("2026-07-16T01:03:00Z"), result.TokenUsage?.UpdatedAt);
    }

    [Fact]
    public void ParseLines_DoesNotInventDeltaWithoutReliableBaseline()
    {
        string[] lines =
        [
            """{"timestamp":"2026-07-16T01:01:00Z","type":"event_msg","payload":{"type":"task_started"}}""",
            TokenEvent("2026-07-16T01:02:00Z", 1_350, 650, 150, 40, 1_500),
        ];

        RolloutObservation result = parser.ParseLines(lines);

        Assert.Equal(1_500, result.TokenUsage?.TotalTokens);
        Assert.Null(result.TokenUsage?.CurrentTurn);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(100, 101, 0, 0, 100)]
    [InlineData(100, 50, 10, 11, 110)]
    public void ParseLines_RejectsInvalidTokenCounters(
        long input,
        long cached,
        long output,
        long reasoning,
        long total)
    {
        RolloutObservation result = parser.ParseLines(
            [TokenEvent("2026-07-16T01:00:00Z", input, cached, output, reasoning, total)]);

        Assert.Null(result.TokenUsage);
    }

    [Fact]
    public void Parse_DiscardsTruncatedFirstLineAndSharesWithWriter()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jsonl");
        try
        {
            string prefix = new('x', RolloutTailParser.MaximumBytes + 32);
            string final = """{"timestamp":"2026-07-16T01:03:00Z","type":"response_item","payload":{"type":"message","role":"assistant","phase":"final"}}""";
            File.WriteAllText(path, $"{prefix}\n{final}\n", Encoding.UTF8);
            using var writer = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete);

            RolloutLoadResult result = parser.Parse(path);

            Assert.Equal(RolloutSourceStatus.Healthy, result.Status);
            Assert.Equal(ThreadStatus.JustCompleted, result.Observation.Status);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_ReturnsMissingForAbsentFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jsonl");

        RolloutLoadResult result = parser.Parse(path);

        Assert.Equal(RolloutSourceStatus.Missing, result.Status);
        Assert.Equal(RolloutObservation.Empty, result.Observation);
    }

    private static DateTimeOffset ParseDate(string value) =>
        DateTimeOffset.Parse(value, null, System.Globalization.DateTimeStyles.AssumeUniversal);

    private static string TokenEvent(
        string timestamp,
        long input,
        long cached,
        long output,
        long reasoning,
        long total) =>
        JsonSerializer.Serialize(new
        {
            timestamp,
            type = "event_msg",
            payload = new
            {
                type = "token_count",
                info = new
                {
                    total_token_usage = new
                    {
                        input_tokens = input,
                        cached_input_tokens = cached,
                        output_tokens = output,
                        reasoning_output_tokens = reasoning,
                        total_tokens = total,
                    },
                },
            },
        });

    private static string FinalMessage(string timestamp, string phase) =>
        JsonSerializer.Serialize(new
        {
            timestamp,
            type = "response_item",
            payload = new
            {
                type = "message",
                role = "assistant",
                phase,
            },
        });

    private static string TaskStarted(string timestamp) =>
        JsonSerializer.Serialize(new
        {
            timestamp,
            type = "event_msg",
            payload = new { type = "task_started" },
        });

    private static string TaskComplete(string timestamp) =>
        JsonSerializer.Serialize(new
        {
            timestamp,
            type = "event_msg",
            payload = new { type = "task_complete" },
        });

    private static string TurnAborted(string timestamp, string? reason) =>
        JsonSerializer.Serialize(new
        {
            timestamp,
            type = "event_msg",
            payload = new { type = "turn_aborted", reason },
        });
}
