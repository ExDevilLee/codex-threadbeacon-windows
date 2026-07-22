namespace ThreadBeacon.App.Formatting;

public static class SubagentAliasFormatter
{
    public static string? Format(string? agentPath, string? nickname, string title)
    {
        string? candidate = SemanticTaskName(agentPath) ?? Normalize(nickname);
        return candidate is null || StringComparer.Ordinal.Equals(candidate, title)
            ? null
            : candidate;
    }

    public static string? Format(string? nickname, string title)
        => Format(null, nickname, title);

    private static string? SemanticTaskName(string? agentPath)
    {
        string? path = Normalize(agentPath);
        if (path is null)
        {
            return null;
        }

        string? component = path.Split(
                ['/', '\\'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
        if (string.IsNullOrEmpty(component))
        {
            return null;
        }

        string words = string.Join(
            ' ',
            component.Split(
                ['_', '-'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return string.IsNullOrEmpty(words)
            ? null
            : char.ToUpperInvariant(words[0]) + words[1..];
    }

    private static string? Normalize(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
