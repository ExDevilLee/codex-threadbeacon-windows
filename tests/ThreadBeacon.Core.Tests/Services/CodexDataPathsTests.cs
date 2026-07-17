using ThreadBeacon.Core.Services;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class CodexDataPathsTests
{
    [Fact]
    public void Resolve_UsesDefaultWindowsLayout()
    {
        string profile = Path.Combine(Path.GetTempPath(), "threadbeacon-profile");

        CodexDataPaths paths = CodexDataPaths.Resolve(profile, _ => null);

        Assert.Equal(Path.Combine(profile, ".codex"), paths.CodexHome);
        Assert.Equal(paths.CodexHome, paths.SqliteHome);
        Assert.Equal(Path.Combine(paths.SqliteHome, "state_5.sqlite"), paths.StateDatabase);
        Assert.Equal(Path.Combine(paths.SqliteHome, "logs_2.sqlite"), paths.LogDatabase);
        Assert.Equal(Path.Combine(paths.CodexHome, "session_index.jsonl"), paths.SessionIndex);
        Assert.Equal(Path.Combine(paths.CodexHome, "sessions"), paths.SessionsDirectory);
    }

    [Fact]
    public void Resolve_HonorsIndependentHomeOverrides()
    {
        var variables = new Dictionary<string, string?>
        {
            ["CODEX_HOME"] = "D:\\CodexData",
            ["CODEX_SQLITE_HOME"] = "D:\\CodexDatabases",
        };

        CodexDataPaths paths = CodexDataPaths.Resolve(
            "C:\\Users\\Test",
            name => variables.GetValueOrDefault(name));

        Assert.Equal("D:\\CodexData", paths.CodexHome);
        Assert.Equal("D:\\CodexDatabases", paths.SqliteHome);
        Assert.Equal("D:\\CodexDatabases\\state_5.sqlite", paths.StateDatabase);
        Assert.Equal("D:\\CodexData\\session_index.jsonl", paths.SessionIndex);
    }
}

