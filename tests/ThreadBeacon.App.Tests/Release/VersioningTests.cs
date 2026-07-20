using System.Text.RegularExpressions;

namespace ThreadBeacon.App.Tests.Release;

public sealed class VersioningTests
{
    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));

    [Fact]
    public void RepositoryVersion_IsStableSemVerAndMatchesBuildConfiguration()
    {
        string version = File.ReadAllText(Path.Combine(RepositoryRoot, "VERSION")).Trim();
        Assert.Matches(new Regex(@"^0\.[0-9]+\.[0-9]+$", RegexOptions.CultureInvariant), version);

        string buildProps = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        Assert.Contains("VERSION", buildProps, StringComparison.Ordinal);
        Assert.Contains("VersionPrefix", buildProps, StringComparison.Ordinal);
        Assert.Contains(
            "<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>",
            buildProps,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseTag_UsesVPrefix()
    {
        string releaseScript = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "script",
            "publish_release.ps1"));

        Assert.Contains("$tag = \"v$version\"", releaseScript, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseScript_BuildsStandaloneExeAndCompleteZipPackage()
    {
        string releaseScript = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "script",
            "publish_release.ps1"));

        Assert.Contains("PublishSingleFile=true", releaseScript, StringComparison.Ordinal);
        Assert.Contains("IncludeAllContentForSelfExtract=true", releaseScript, StringComparison.Ordinal);
        Assert.Contains("Compress-Archive", releaseScript, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseScript_RejectsTagThatDoesNotMatchVersionFile()
    {
        string releaseScript = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "script",
            "publish_release.ps1"));

        Assert.Contains("GITHUB_REF_NAME", releaseScript, StringComparison.Ordinal);
        Assert.Contains("does not match VERSION tag", releaseScript, StringComparison.Ordinal);
    }
}
