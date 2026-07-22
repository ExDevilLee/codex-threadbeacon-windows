using ThreadBeacon.App.Formatting;
using ThreadBeacon.App.Localization;
using ThreadBeacon.Core.Models;
using System.Windows;

namespace ThreadBeacon.App.ViewModels;

public sealed record TokenDetailRow(string Label, string Value);

public sealed class TokenDetailViewModel
{
    public TokenDetailViewModel(
        TokenUsageSnapshot snapshot,
        AppLanguage language = AppLanguage.SimplifiedChinese)
        : this(null, null, snapshot, language)
    {
    }

    public TokenDetailViewModel(
        ThreadSnapshot snapshot,
        AppLanguage language = AppLanguage.SimplifiedChinese)
        : this(snapshot.Model, snapshot.ReasoningEffort, snapshot.TokenUsage, language)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
    }

    private TokenDetailViewModel(
        string? model,
        string? reasoningEffort,
        TokenUsageSnapshot? snapshot,
        AppLanguage language)
    {
        HasMetadata = !string.IsNullOrWhiteSpace(model)
            || !string.IsNullOrWhiteSpace(reasoningEffort);
        MetadataRows =
        [
            new(AppLanguageText.TaskMetadataLabel(language, 0),
                string.IsNullOrWhiteSpace(model) ? "—" : model.Trim()),
            new(AppLanguageText.TaskMetadataLabel(language, 1),
                AppLanguageText.ReasoningEffort(reasoningEffort)),
        ];

        HasTokenUsage = snapshot is not null;
        TokenSectionVisibility = HasTokenUsage ? Visibility.Visible : Visibility.Collapsed;
        TokenDividerVisibility = HasMetadata && HasTokenUsage
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (snapshot is null)
        {
            TokenRows = [];
            Rows = TokenRows;
            Note = AppLanguageText.TokenNote(language);
            return;
        }

        TokenUsage? cumulative = snapshot.Cumulative;
        TokenRows =
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
        Rows = TokenRows;
        Note = AppLanguageText.TokenNote(language);
    }

    public IReadOnlyList<TokenDetailRow> MetadataRows { get; }

    public IReadOnlyList<TokenDetailRow> TokenRows { get; }

    public IReadOnlyList<TokenDetailRow> Rows { get; }

    public bool HasMetadata { get; }

    public bool HasTokenUsage { get; }

    public Visibility TokenSectionVisibility { get; }

    public Visibility TokenDividerVisibility { get; }

    public string Note { get; }
}
