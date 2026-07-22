namespace ThreadBeacon.App.AutoRecovery;

public static class AutoRecoveryDiagnosticCodes
{
    public const string UnexpectedError = "unexpected_error";

    private static readonly IReadOnlySet<string> FixedCodes = new HashSet<string>(
        [
            "invalid_thread_id",
            "codex_not_running",
            "codex_frontmost",
            "source_composer_value_unavailable",
            "source_composer_not_empty",
            "deep_link_failed",
            "composer_value_unavailable",
            "composer_not_empty",
            "target_not_changed",
            "selection_timed_out",
            "selection_failed",
            "composer_focus_failed",
            "composer_readback_mismatch",
            "send_button_unavailable",
            "composer_changed_before_send",
            "rollout_evidence_missing",
            UnexpectedError,
        ],
        StringComparer.Ordinal);

    private static readonly string[] CountPrefixes =
    [
        "codex_window_count_",
        "source_composer_count_",
        "target_header_count_",
        "composer_count_",
    ];

    public static bool IsAllowed(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (FixedCodes.Contains(value))
        {
            return true;
        }

        return CountPrefixes.Any(prefix =>
            value.StartsWith(prefix, StringComparison.Ordinal)
            && value[prefix.Length..] is "0" or "1" or "many");
    }

    public static string Normalize(string? value) =>
        IsAllowed(value) ? value! : UnexpectedError;
}
