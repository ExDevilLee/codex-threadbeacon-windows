using System.Globalization;

namespace ThreadBeacon.App.Formatting;

public static class TokenUsageFormatter
{
    public static string FormatCount(long? value)
    {
        if (value is null or < 0)
        {
            return "—";
        }

        return value.Value switch
        {
            < 1_000 => value.Value.ToString(CultureInfo.InvariantCulture),
            < 1_000_000 => $"{value.Value / 1_000d:0.#}K",
            < 1_000_000_000 => $"{value.Value / 1_000_000d:0.#}M",
            _ => $"{value.Value / 1_000_000_000d:0.#}B",
        };
    }

    public static string FormatCurrentTurn(long? value) =>
        value is null or < 0 ? "—" : $"+{FormatCount(value)}";

    public static string FormatPercent(double? ratio) =>
        ratio is null or < 0
            ? "—"
            : $"{Math.Round(ratio.Value * 100):0}%";

    public static string FormatTime(DateTimeOffset? value) =>
        value?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "—";
}
