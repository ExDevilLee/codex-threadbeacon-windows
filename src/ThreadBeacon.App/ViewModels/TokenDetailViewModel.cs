using ThreadBeacon.App.Formatting;
using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed record TokenDetailRow(string Label, string Value);

public sealed class TokenDetailViewModel
{
    public TokenDetailViewModel(
        TokenUsageSnapshot snapshot,
        AppLanguage language = AppLanguage.SimplifiedChinese)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        TokenUsage? cumulative = snapshot.Cumulative;
        Rows =
        [
            new(AppLanguageText.TokenLabel(language, 0), TokenUsageFormatter.FormatCount(snapshot.TotalTokens)),
            new(AppLanguageText.TokenLabel(language, 1), TokenUsageFormatter.FormatCount(cumulative?.InputTokens)),
            new(AppLanguageText.TokenLabel(language, 2), TokenUsageFormatter.FormatCount(cumulative?.CachedInputTokens)),
            new(AppLanguageText.TokenLabel(language, 3), TokenUsageFormatter.FormatCount(cumulative?.UncachedInputTokens)),
            new(AppLanguageText.TokenLabel(language, 4), TokenUsageFormatter.FormatCount(cumulative?.OutputTokens)),
            new(AppLanguageText.TokenLabel(language, 5), TokenUsageFormatter.FormatCount(cumulative?.ReasoningOutputTokens)),
            new(AppLanguageText.TokenLabel(language, 6), TokenUsageFormatter.FormatCurrentTurn(snapshot.CurrentTurn?.TotalTokens)),
            new(AppLanguageText.TokenLabel(language, 7), TokenUsageFormatter.FormatPercent(cumulative?.CacheRatio)),
            new(AppLanguageText.TokenLabel(language, 8), TokenUsageFormatter.FormatTime(snapshot.UpdatedAt)),
        ];
        Note = AppLanguageText.TokenNote(language);
    }

    public IReadOnlyList<TokenDetailRow> Rows { get; }

    public string Note { get; }
}
