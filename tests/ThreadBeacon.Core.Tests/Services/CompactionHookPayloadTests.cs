using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class CompactionHookPayloadTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 23, 9, 30, 0, TimeSpan.Zero);
    private const string SessionId = "11111111-2222-4333-8444-555555555555";
    private const string TurnId = "66666666-7777-4888-8999-aaaaaaaaaaaa";

    [Fact]
    public void Handle_PreCompactWritesOnlyMinimalActivityFields()
    {
        using var fixture = new HookFixture();
        var handler = new CompactionHookEventHandler(fixture.Repository, new FixedTimeProvider(Now));
        string payload = $$"""
            {
              "hook_event_name": "PreCompact",
              "session_id": "{{SessionId}}",
              "turn_id": "{{TurnId}}",
              "trigger": "manual",
              "cwd": "C:\\private\\project",
              "model": "private-model",
              "transcript_path": "C:\\private\\rollout.jsonl"
            }
            """;

        bool handled = handler.TryHandle(payload);

        Assert.True(handled);
        var activity = fixture.Repository.Read(SessionId, null, null, Now);
        Assert.NotNull(activity);
        Assert.Equal(TurnId, activity.TurnId);
        Assert.Equal("manual", activity.Trigger);
        Assert.Equal(Now, activity.StartedAt);
        string marker = File.ReadAllText(fixture.MarkerPath(SessionId));
        Assert.DoesNotContain("private", marker, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("transcript", marker, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("model", marker, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Handle_AcceptsWindowsStandardInputBom()
    {
        using var fixture = new HookFixture();
        var handler = new CompactionHookEventHandler(fixture.Repository, new FixedTimeProvider(Now));

        bool handled = handler.TryHandle(
            "\uFEFF" + Payload("PreCompact", SessionId, TurnId, "auto"));

        Assert.True(handled);
        Assert.NotNull(fixture.Repository.Read(SessionId, null, null, Now));
    }

    [Fact]
    public void Handle_PostCompactClearsOnlyMatchingSessionAndTurn()
    {
        using var fixture = new HookFixture();
        var handler = new CompactionHookEventHandler(fixture.Repository, new FixedTimeProvider(Now));
        Assert.True(handler.TryHandle(Payload("PreCompact", SessionId, TurnId, "auto")));

        Assert.True(handler.TryHandle(Payload(
            "PostCompact",
            SessionId,
            "bbbbbbbb-cccc-4ddd-8eee-ffffffffffff",
            "auto")));
        Assert.NotNull(fixture.Repository.Read(SessionId, null, null, Now));

        Assert.True(handler.TryHandle(Payload("PostCompact", SessionId, TurnId, "auto")));
        Assert.Null(fixture.Repository.Read(SessionId, null, null, Now));
    }

    [Theory]
    [InlineData("Stop", SessionId, TurnId, "manual")]
    [InlineData("PreCompact", "not-a-uuid", TurnId, "manual")]
    [InlineData("PreCompact", SessionId, "not-a-uuid", "manual")]
    [InlineData("PreCompact", SessionId, TurnId, "unknown")]
    public void Handle_RejectsUnsupportedOrInvalidPayload(
        string eventName,
        string sessionId,
        string turnId,
        string trigger)
    {
        using var fixture = new HookFixture();
        var handler = new CompactionHookEventHandler(fixture.Repository, new FixedTimeProvider(Now));

        bool handled = handler.TryHandle(Payload(eventName, sessionId, turnId, trigger));

        Assert.False(handled);
        Assert.Empty(Directory.Exists(fixture.Directory)
            ? Directory.GetFiles(fixture.Directory)
            : []);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    public void Handle_RejectsMalformedOrIncompletePayload(string payload)
    {
        using var fixture = new HookFixture();
        var handler = new CompactionHookEventHandler(fixture.Repository, new FixedTimeProvider(Now));

        Assert.False(handler.TryHandle(payload));
    }

    private static string Payload(string eventName, string sessionId, string turnId, string trigger) => $$"""
        {
          "hook_event_name": "{{eventName}}",
          "session_id": "{{sessionId}}",
          "turn_id": "{{turnId}}",
          "trigger": "{{trigger}}"
        }
        """;

    private sealed class HookFixture : IDisposable
    {
        public HookFixture()
        {
            Directory = Path.Combine(Path.GetTempPath(), "ThreadBeaconHook", Guid.NewGuid().ToString("N"));
            Repository = new CompactionActivityRepository(Directory);
        }

        public string Directory { get; }
        public CompactionActivityRepository Repository { get; }
        public string MarkerPath(string sessionId) => Path.Combine(Directory, $"{sessionId}.json");

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
