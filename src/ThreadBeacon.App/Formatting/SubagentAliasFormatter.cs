namespace ThreadBeacon.App.Formatting;

public static class SubagentAliasFormatter
{
    public static string? Format(string? nickname, string title)
    {
        string? trimmed = nickname?.Trim();
        return string.IsNullOrEmpty(trimmed)
            || StringComparer.Ordinal.Equals(trimmed, title)
                ? null
                : trimmed;
    }
}
