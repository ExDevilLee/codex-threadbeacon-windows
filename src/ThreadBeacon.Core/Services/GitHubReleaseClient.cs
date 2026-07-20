using System.Net.Http.Headers;
using System.Text.Json;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.Core.Services;

public interface IReleaseClient
{
    Task<AvailableUpdate?> GetLatestAsync(CancellationToken cancellationToken);
}

public sealed class GitHubReleaseClient : IReleaseClient
{
    private static readonly Uri ReleasesUri = new(
        "https://api.github.com/repos/ExDevilLee/codex-threadbeacon-windows/releases?per_page=20");
    private readonly HttpClient httpClient;

    public GitHubReleaseClient(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<AvailableUpdate?> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("ThreadBeacon/1.0");
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

        AvailableUpdate? latest = null;
        foreach (JsonElement release in document.RootElement.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean()
                || !SemanticVersion.TryParse(release.GetProperty("tag_name").GetString(), out SemanticVersion version)
                || !Uri.TryCreate(release.GetProperty("html_url").GetString(), UriKind.Absolute, out Uri? uri))
            {
                continue;
            }

            var candidate = new AvailableUpdate(
                version,
                uri,
                release.GetProperty("prerelease").GetBoolean());
            if (latest is null || candidate.Version > latest.Value.Version)
            {
                latest = candidate;
            }
        }

        return latest;
    }
}
