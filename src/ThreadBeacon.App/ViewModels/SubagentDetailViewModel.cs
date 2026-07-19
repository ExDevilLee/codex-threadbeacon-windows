using ThreadBeacon.App.Formatting;
using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class SubagentDetailViewModel
{
    public SubagentDetailViewModel(
        SubagentSnapshot snapshot,
        AppLanguage language = AppLanguage.SimplifiedChinese)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Title = string.IsNullOrWhiteSpace(snapshot.Title)
            ? language is AppLanguage.SimplifiedChinese ? "未命名 Subagent" : "Unnamed Subagent"
            : snapshot.Title;
        TokenUsage? cumulative = snapshot.TokenUsage?.Cumulative;
        DateTimeOffset activityAt = snapshot.LatestEventAt ?? snapshot.UpdatedAt;
        Rows =
        [
            new(AppLanguageText.SubagentDetailLabel(language, 0), StatusLabel(snapshot.Status, language)),
            new(AppLanguageText.SubagentDetailLabel(language, 1), Value(snapshot.AgentNickname)),
            new(AppLanguageText.SubagentDetailLabel(language, 2), Value(snapshot.AgentRole)),
            new(AppLanguageText.SubagentDetailLabel(language, 3), Value(snapshot.Model)),
            new(AppLanguageText.SubagentDetailLabel(language, 4), Value(snapshot.ReasoningEffort)),
            new(AppLanguageText.SubagentDetailLabel(language, 5), TokenUsageFormatter.FormatCount(snapshot.TokenUsage?.TotalTokens)),
            new(AppLanguageText.SubagentDetailLabel(language, 6), TokenUsageFormatter.FormatCount(cumulative?.InputTokens)),
            new(AppLanguageText.SubagentDetailLabel(language, 7), TokenUsageFormatter.FormatCount(cumulative?.CachedInputTokens)),
            new(AppLanguageText.SubagentDetailLabel(language, 8), TokenUsageFormatter.FormatCount(cumulative?.OutputTokens)),
            new(AppLanguageText.SubagentDetailLabel(language, 9), TokenUsageFormatter.FormatCount(cumulative?.ReasoningOutputTokens)),
            new(AppLanguageText.SubagentDetailLabel(language, 10), TokenUsageFormatter.FormatCurrentTurn(snapshot.TokenUsage?.CurrentTurn?.TotalTokens)),
            new(AppLanguageText.SubagentDetailLabel(language, 11), activityAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
        ];
    }

    public string Title { get; }

    public IReadOnlyList<TokenDetailRow> Rows { get; }

    private static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    internal static string StatusLabel(
        ThreadStatus status,
        AppLanguage language = AppLanguage.SimplifiedChinese) => AppLanguageText.Status(language, status);
}
