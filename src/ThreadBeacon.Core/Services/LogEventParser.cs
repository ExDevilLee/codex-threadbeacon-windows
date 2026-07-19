using System.Globalization;
using System.Text.RegularExpressions;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public sealed partial class LogEventParser
{
    public static IReadOnlySet<string> AllowedTargets { get; } = new HashSet<string>(
        [
            "codex_http_client::default_client",
            "codex_core::responses_retry",
            "codex_core::session::turn",
        ],
        StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ServiceIncident> LatestIncidents(
        IReadOnlyList<LogEventRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var episodes = new Dictionary<EpisodeKey, Episode>();
        foreach (LogEventRecord record in records
            .OrderBy(record => record.OccurredAt)
            .ThenBy(record => record.Target, StringComparer.Ordinal))
        {
            if (!AllowedTargets.Contains(record.Target)
                || TryCapture(TurnIdRegex(), record.Body) is not string episodeId)
            {
                continue;
            }

            var key = new EpisodeKey(record.ThreadId, episodeId);
            if (!episodes.TryGetValue(key, out Episode? episode))
            {
                episode = new Episode();
                episodes.Add(key, episode);
            }

            switch (record.Target)
            {
                case "codex_http_client::default_client":
                    if (TryStatusCode(record.Body) is int statusCode)
                    {
                        if (statusCode == 200)
                        {
                            episode.LatestSuccessAt = Later(episode.LatestSuccessAt, record.OccurredAt);
                        }
                        else if (statusCode is 429 or 503)
                        {
                            episode.HttpStatusCode = statusCode;
                            episode.LatestErrorAt = Later(episode.LatestErrorAt, record.OccurredAt);
                        }
                    }

                    break;

                case "codex_core::responses_retry":
                    Match progress = RetryProgressRegex().Match(record.Body);
                    if (progress.Success
                        && TryPositiveInt(progress.Groups[1].Value) is int attempt
                        && TryPositiveInt(progress.Groups[2].Value) is int limit
                        && attempt <= limit)
                    {
                        episode.RetryAttempt = attempt;
                        episode.RetryLimit = limit;
                        episode.LatestRetryAt = Later(episode.LatestRetryAt, record.OccurredAt);
                    }

                    break;

                case "codex_core::session::turn":
                    if (record.Body.Contains("Turn error:", StringComparison.Ordinal)
                        && TryStatusCode(record.Body) is 429 or 503)
                    {
                        episode.HttpStatusCode = TryStatusCode(record.Body);
                        episode.FailedAt = Later(episode.FailedAt, record.OccurredAt);
                    }

                    break;
            }
        }

        var latestByThread = new Dictionary<string, ServiceIncident>(StringComparer.Ordinal);
        foreach ((EpisodeKey key, Episode episode) in episodes)
        {
            ServiceIncident? incident = episode.CreateIncident(key.EpisodeId);
            if (incident is null
                || latestByThread.TryGetValue(key.ThreadId, out ServiceIncident? current)
                    && current.OccurredAt >= incident.OccurredAt)
            {
                continue;
            }

            latestByThread[key.ThreadId] = incident;
        }

        return latestByThread;
    }

    private static int? TryStatusCode(string body) =>
        TryCapture(StatusCodeRegex(), body) is string value
            && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result)
                ? result
                : null;

    private static int? TryPositiveInt(string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result)
            && result > 0
                ? result
                : null;

    private static string? TryCapture(Regex expression, string value)
    {
        Match match = expression.Match(value);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static DateTimeOffset Later(DateTimeOffset? current, DateTimeOffset candidate) =>
        current is null || candidate > current ? candidate : current.Value;

    [GeneratedRegex(@"turn(?:\.id|_id)=([A-Za-z0-9-]+)", RegexOptions.CultureInvariant)]
    private static partial Regex TurnIdRegex();

    [GeneratedRegex(@"status[= ]+(\d{3})\b", RegexOptions.CultureInvariant)]
    private static partial Regex StatusCodeRegex();

    [GeneratedRegex(@"\((\d+)/(\d+) in ", RegexOptions.CultureInvariant)]
    private static partial Regex RetryProgressRegex();

    private sealed record EpisodeKey(string ThreadId, string EpisodeId);

    private sealed class Episode
    {
        public int? HttpStatusCode { get; set; }
        public int? RetryAttempt { get; set; }
        public int? RetryLimit { get; set; }
        public DateTimeOffset? LatestErrorAt { get; set; }
        public DateTimeOffset? LatestRetryAt { get; set; }
        public DateTimeOffset? LatestSuccessAt { get; set; }
        public DateTimeOffset? FailedAt { get; set; }

        public ServiceIncident? CreateIncident(string episodeId)
        {
            if (FailedAt is DateTimeOffset failedAt)
            {
                return new ServiceIncident(
                    episodeId,
                    ServiceIncidentPhase.Failed,
                    HttpStatusCode,
                    RetryAttempt,
                    RetryLimit,
                    failedAt);
            }

            DateTimeOffset? warningAt = new[] { LatestErrorAt, LatestRetryAt }.Max();
            return warningAt is DateTimeOffset occurredAt
                && occurredAt > (LatestSuccessAt ?? DateTimeOffset.MinValue)
                    ? new ServiceIncident(
                        episodeId,
                        ServiceIncidentPhase.Retrying,
                        HttpStatusCode,
                        RetryAttempt,
                        RetryLimit,
                        occurredAt)
                    : null;
        }
    }
}
