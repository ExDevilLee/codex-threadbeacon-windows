using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public sealed class SessionIndexTitleRepository : ISessionIndexTitleRepository
{
    private static readonly IReadOnlyDictionary<string, string> EmptyTitles =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    private readonly string indexPath;

    public SessionIndexTitleRepository(string indexPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexPath);
        this.indexPath = Path.GetFullPath(indexPath);
    }

    public TitleLoadResult LoadLatestTitles()
    {
        if (!File.Exists(indexPath))
        {
            return Result(SessionIndexStatus.Missing);
        }

        try
        {
            using var stream = new FileStream(
                indexPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.SequentialScan);
            using var reader = new StreamReader(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: true);
            var titles = new Dictionary<string, string>(StringComparer.Ordinal);

            while (reader.ReadLine() is { } line)
            {
                if (!TryReadEntry(line, out SessionIndexEntry? entry) || entry is null)
                {
                    continue;
                }

                string id = entry.Id?.Trim() ?? string.Empty;
                string title = entry.ThreadName?.Trim() ?? string.Empty;
                if (id.Length > 0 && title.Length > 0)
                {
                    titles[id] = title;
                }
            }

            return new TitleLoadResult(
                SessionIndexStatus.Healthy,
                new ReadOnlyDictionary<string, string>(titles));
        }
        catch (DecoderFallbackException)
        {
            return Result(SessionIndexStatus.Incompatible);
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException)
        {
            return Result(SessionIndexStatus.Unavailable);
        }
    }

    private static bool TryReadEntry(string line, out SessionIndexEntry? entry)
    {
        try
        {
            entry = JsonSerializer.Deserialize<SessionIndexEntry>(line);
            return entry is not null;
        }
        catch (JsonException)
        {
            entry = null;
            return false;
        }
    }

    private static TitleLoadResult Result(SessionIndexStatus status) =>
        new(status, EmptyTitles);

    private sealed record SessionIndexEntry(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("thread_name")] string? ThreadName);
}
