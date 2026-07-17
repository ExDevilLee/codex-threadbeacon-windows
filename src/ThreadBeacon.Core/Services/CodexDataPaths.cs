namespace ThreadBeacon.Core.Services;

public sealed record CodexDataPaths(
    string CodexHome,
    string SqliteHome,
    string StateDatabase,
    string LogDatabase,
    string SessionIndex,
    string SessionsDirectory)
{
    public static CodexDataPaths Resolve(
        string? userProfile = null,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        string profile = userProfile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new InvalidOperationException("The current user profile directory could not be resolved.");
        }

        string codexHome = ResolvePath(
            getEnvironmentVariable("CODEX_HOME"),
            Path.Combine(profile, ".codex"));
        string sqliteHome = ResolvePath(
            getEnvironmentVariable("CODEX_SQLITE_HOME"),
            codexHome);

        return new CodexDataPaths(
            codexHome,
            sqliteHome,
            Path.Combine(sqliteHome, "state_5.sqlite"),
            Path.Combine(sqliteHome, "logs_2.sqlite"),
            Path.Combine(codexHome, "session_index.jsonl"),
            Path.Combine(codexHome, "sessions"));
    }

    private static string ResolvePath(string? configuredPath, string fallback) =>
        Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath);
}

