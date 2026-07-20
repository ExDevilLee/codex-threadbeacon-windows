using System.Net;
using ThreadBeacon.Core.Models;
using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class GitHubReleaseClientTests
{
    [Fact]
    public async Task GetLatestAsync_SelectsHighestValidNonDraftReleaseIncludingPrerelease()
    {
        const string response = """
            [
              {"tag_name":"v0.7.0-beta.1","html_url":"https://example.test/beta","draft":false,"prerelease":true},
              {"tag_name":"v0.6.0","html_url":"https://example.test/stable","draft":false,"prerelease":false},
              {"tag_name":"v9.0.0","html_url":"https://example.test/draft","draft":true,"prerelease":false},
              {"tag_name":"not-a-version","html_url":"https://example.test/invalid","draft":false,"prerelease":false}
            ]
            """;
        var handler = new StubHandler(response);
        var client = new GitHubReleaseClient(new HttpClient(handler));

        AvailableUpdate? update = await client.GetLatestAsync(CancellationToken.None);

        Assert.NotNull(update);
        Assert.Equal("0.7.0-beta.1", update.Value.Version.ToString());
        Assert.Equal("https://example.test/beta", update.Value.ReleaseUrl.AbsoluteUri);
        Assert.True(update.Value.IsPrerelease);
        Assert.Equal("application/vnd.github+json", handler.Accept);
        Assert.StartsWith("ThreadBeacon/", handler.UserAgent, StringComparison.Ordinal);
    }

    private sealed class StubHandler(string response) : HttpMessageHandler
    {
        public string? Accept { get; private set; }
        public string? UserAgent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Accept = request.Headers.Accept.Single().MediaType;
            UserAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response),
            });
        }
    }
}
