using ThreadBeacon.App.Formatting;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed record TokenDetailRow(string Label, string Value);

public sealed class TokenDetailViewModel
{
    public TokenDetailViewModel(TokenUsageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        TokenUsage? cumulative = snapshot.Cumulative;
        Rows =
        [
            new("会话总量", TokenUsageFormatter.FormatCount(snapshot.TotalTokens)),
            new("输入", TokenUsageFormatter.FormatCount(cumulative?.InputTokens)),
            new("缓存输入", TokenUsageFormatter.FormatCount(cumulative?.CachedInputTokens)),
            new("非缓存输入", TokenUsageFormatter.FormatCount(cumulative?.UncachedInputTokens)),
            new("输出", TokenUsageFormatter.FormatCount(cumulative?.OutputTokens)),
            new("Reasoning", TokenUsageFormatter.FormatCount(cumulative?.ReasoningOutputTokens)),
            new("当前 turn", TokenUsageFormatter.FormatCurrentTurn(snapshot.CurrentTurn?.TotalTokens)),
            new("缓存率", TokenUsageFormatter.FormatPercent(cumulative?.CacheRatio)),
            new("更新时间", TokenUsageFormatter.FormatTime(snapshot.UpdatedAt)),
        ];
    }

    public IReadOnlyList<TokenDetailRow> Rows { get; }

    public string Note => "缓存输入已包含在输入中；Reasoning 已包含在输出中。";
}
