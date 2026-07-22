using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class LogEventParserTests
{
    [Fact]
    public void LatestIncidents_ExposesActive429Retry()
    {
        LogEventRecord[] records =
        [
            Record(200, "codex_http_client::default_client",
                "turn{turn.id=turn-b}: Request completed status=429 Too Many Requests"),
            Record(201, "codex_core::responses_retry",
                "turn{turn.id=turn-b}: stream disconnected - retrying sampling request (3/5 in 900ms)..."),
        ];

        ServiceIncident incident = Assert.Single(new LogEventParser().LatestIncidents(records)).Value;

        Assert.Equal(ServiceIncidentPhase.Retrying, incident.Phase);
        Assert.Equal(429, incident.HttpStatusCode);
        Assert.Equal(3, incident.RetryAttempt);
        Assert.Equal(5, incident.RetryLimit);
        Assert.Equal("turn-b", incident.EpisodeId);
        Assert.Equal(At(201), incident.OccurredAt);
    }

    [Fact]
    public void LatestIncidents_TurnsExhausted503RetryIntoFailure()
    {
        LogEventRecord[] records =
        [
            Record(100, "codex_http_client::default_client",
                "turn{turn.id=turn-a}: Request completed status=503 Service Unavailable"),
            Record(101, "codex_core::responses_retry",
                "turn{turn.id=turn-a}: retrying sampling request (5/5 in 3.1s)..."),
            Record(102, "codex_core::session::turn",
                "turn{turn.id=turn-a}: Turn error: unexpected status 503 Service Unavailable"),
        ];

        ServiceIncident incident = Assert.Single(new LogEventParser().LatestIncidents(records)).Value;

        Assert.Equal(ServiceIncidentPhase.Failed, incident.Phase);
        Assert.Equal(503, incident.HttpStatusCode);
        Assert.Equal(5, incident.RetryAttempt);
        Assert.Equal(At(102), incident.OccurredAt);
    }

    [Fact]
    public void LatestIncidents_KeepsExhaustedReconnectAsWarningBeforeFinalError()
    {
        LogEventRecord[] records =
        [
            Record(275, "codex_core::responses_retry",
                "turn{turn.id=turn-disconnect-warning}: stream disconnected - retrying sampling request (5/5 in 3s)..."),
        ];

        ServiceIncident incident = Assert.Single(new LogEventParser().LatestIncidents(records)).Value;

        Assert.Equal(ServiceIncidentPhase.Retrying, incident.Phase);
        Assert.Equal(5, incident.RetryAttempt);
        Assert.Equal(5, incident.RetryLimit);
    }

    [Fact]
    public void LatestIncidents_TurnsExhaustedReconnectFollowedByFinalDisconnectIntoFailure()
    {
        LogEventRecord[] records =
        [
            Record(280, "codex_core::responses_retry",
                "turn{turn.id=turn-disconnect}: stream disconnected - retrying sampling request (5/5 in 3s)..."),
            Record(281, "codex_core::session::turn",
                "turn{turn.id=turn-disconnect}: Turn error: stream disconnected before completion: error sending request for url (<redacted>)"),
        ];

        ServiceIncident incident = Assert.Single(new LogEventParser().LatestIncidents(records)).Value;

        Assert.Equal(ServiceIncidentPhase.Failed, incident.Phase);
        Assert.Equal(ServiceIncidentKind.StreamDisconnected, incident.Kind);
        Assert.Null(incident.HttpStatusCode);
        Assert.Equal(5, incident.RetryAttempt);
        Assert.Equal(5, incident.RetryLimit);
        Assert.Equal(At(281), incident.OccurredAt);
    }

    [Fact]
    public void LatestIncidents_IgnoresFinalDisconnectWithoutExhaustedReconnect()
    {
        LogEventRecord[] records =
        [
            Record(290, "codex_core::session::turn",
                "turn{turn.id=turn-disconnect-unmatched}: Turn error: stream disconnected before completion: error sending request for url (<redacted>)"),
        ];

        Assert.Empty(new LogEventParser().LatestIncidents(records));
    }

    [Fact]
    public void LatestIncidents_RecognizesModelCapacityFailure()
    {
        LogEventRecord[] records =
        [
            Record(103, "codex_core::session::turn",
                "turn{turn.id=turn-capacity}: Turn error: Selected model is at capacity. Please try a different model."),
        ];

        ServiceIncident incident = Assert.Single(new LogEventParser().LatestIncidents(records)).Value;

        Assert.Equal(ServiceIncidentPhase.Failed, incident.Phase);
        Assert.Equal(ServiceIncidentKind.ModelCapacity, incident.Kind);
        Assert.Null(incident.HttpStatusCode);
    }

    [Fact]
    public void LatestIncidents_RecognizesBadRequestAsFailure()
    {
        LogEventRecord[] records =
        [
            Record(104, "codex_http_client::default_client",
                "turn{turn.id=turn-bad-request}: Request completed status=400 Bad Request"),
            Record(105, "codex_core::session::turn",
                "turn{turn.id=turn-bad-request}: Turn error: unexpected status 400 Bad Request"),
        ];

        ServiceIncident incident = Assert.Single(new LogEventParser().LatestIncidents(records)).Value;

        Assert.Equal(ServiceIncidentPhase.Failed, incident.Phase);
        Assert.Equal(ServiceIncidentKind.BadRequest, incident.Kind);
        Assert.Equal(400, incident.HttpStatusCode);
    }

    [Fact]
    public void LatestIncidents_RecognizesOtherHttpFailure()
    {
        LogEventRecord[] records =
        [
            Record(106, "codex_http_client::default_client",
                "turn{turn.id=turn-http}: Request completed status=502 Bad Gateway"),
            Record(107, "codex_core::session::turn",
                "turn{turn.id=turn-http}: Turn error: unexpected status 502 Bad Gateway"),
        ];

        ServiceIncident incident = Assert.Single(new LogEventParser().LatestIncidents(records)).Value;

        Assert.Equal(ServiceIncidentPhase.Failed, incident.Phase);
        Assert.Equal(ServiceIncidentKind.OtherHttp, incident.Kind);
        Assert.Equal(502, incident.HttpStatusCode);
    }

    [Fact]
    public void LatestIncidents_ClearsRetryAfterSameTurnRecovers()
    {
        LogEventRecord[] records =
        [
            Record(300, "codex_http_client::default_client",
                "turn{turn.id=turn-c}: Request completed status=503 Service Unavailable"),
            Record(301, "codex_core::responses_retry",
                "turn{turn.id=turn-c}: retrying sampling request (2/5 in 420ms)..."),
            Record(302, "codex_http_client::default_client",
                "turn{turn.id=turn-c}: Request completed status=200 OK"),
        ];

        Assert.Empty(new LogEventParser().LatestIncidents(records));
    }

    [Theory]
    [InlineData("codex_http_client::transport", "turn{turn.id=private}: status=429 Too Many Requests private request body")]
    [InlineData("codex_http_client::default_client", "Request completed status=429 Too Many Requests")]
    [InlineData("codex_core::responses_retry", "turn_id=turn-a retrying sampling request (6/5 in 1s)...")]
    [InlineData("unrelated", "turn_id=turn-a Turn error: status 503 Service Unavailable")]
    public void LatestIncidents_IgnoresDisallowedOrMalformedRecords(string target, string body)
    {
        Assert.Empty(new LogEventParser().LatestIncidents([Record(1, target, body)]));
    }

    [Fact]
    public void LatestIncidents_ReturnsLatestEpisodePerThreadRegardlessOfInputOrder()
    {
        LogEventRecord[] records =
        [
            Record(500, "codex_http_client::default_client",
                "turn_id=new-turn Request completed status=429 Too Many Requests"),
            Record(100, "codex_http_client::default_client",
                "turn.id=old-turn Request completed status=503 Service Unavailable"),
        ];

        ServiceIncident incident = Assert.Single(new LogEventParser().LatestIncidents(records)).Value;

        Assert.Equal("new-turn", incident.EpisodeId);
        Assert.Equal(429, incident.HttpStatusCode);
    }

    private static LogEventRecord Record(long second, string target, string body) =>
        new("thread-a", At(second), target, body);

    private static DateTimeOffset At(long second) => DateTimeOffset.FromUnixTimeSeconds(second);
}
