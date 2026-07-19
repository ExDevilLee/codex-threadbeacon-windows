using ThreadBeacon.App.Formatting;
using ThreadBeacon.Core.Models;

namespace ThreadBeacon.App.ViewModels;

public sealed class SubagentDetailViewModel
{
    public SubagentDetailViewModel(SubagentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Title = string.IsNullOrWhiteSpace(snapshot.Title) ? "未命名 Subagent" : snapshot.Title;
        TokenUsage? cumulative = snapshot.TokenUsage?.Cumulative;
        DateTimeOffset activityAt = snapshot.LatestEventAt ?? snapshot.UpdatedAt;
        Rows =
        [
            new("状态", StatusLabel(snapshot.Status)),
            new("Agent", Value(snapshot.AgentNickname)),
            new("角色", Value(snapshot.AgentRole)),
            new("模型", Value(snapshot.Model)),
            new("Reasoning", Value(snapshot.ReasoningEffort)),
            new("累计 Token", TokenUsageFormatter.FormatCount(snapshot.TokenUsage?.TotalTokens)),
            new("输入", TokenUsageFormatter.FormatCount(cumulative?.InputTokens)),
            new("缓存输入", TokenUsageFormatter.FormatCount(cumulative?.CachedInputTokens)),
            new("输出", TokenUsageFormatter.FormatCount(cumulative?.OutputTokens)),
            new("Reasoning Token", TokenUsageFormatter.FormatCount(cumulative?.ReasoningOutputTokens)),
            new("当前 turn", TokenUsageFormatter.FormatCurrentTurn(snapshot.TokenUsage?.CurrentTurn?.TotalTokens)),
            new("最近活动", activityAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
        ];
    }

    public string Title { get; }

    public IReadOnlyList<TokenDetailRow> Rows { get; }

    private static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    internal static string StatusLabel(ThreadStatus status) => status switch
    {
        ThreadStatus.Error => "错误",
        ThreadStatus.NeedsAction => "待处理",
        ThreadStatus.Warning => "重试中",
        ThreadStatus.Running => "运行中",
        ThreadStatus.JustCompleted => "刚完成",
        ThreadStatus.Idle => "空闲",
        _ => "未知",
    };
}
