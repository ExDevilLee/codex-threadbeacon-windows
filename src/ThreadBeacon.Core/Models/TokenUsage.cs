namespace ThreadBeacon.Core.Models;

public sealed record TokenUsage(
    long InputTokens,
    long CachedInputTokens,
    long OutputTokens,
    long ReasoningOutputTokens,
    long TotalTokens)
{
    public long UncachedInputTokens => InputTokens - CachedInputTokens;

    public double? CacheRatio => InputTokens > 0
        ? (double)CachedInputTokens / InputTokens
        : null;

    public TokenUsage? Subtract(TokenUsage baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        long input = InputTokens - baseline.InputTokens;
        long cachedInput = CachedInputTokens - baseline.CachedInputTokens;
        long output = OutputTokens - baseline.OutputTokens;
        long reasoningOutput = ReasoningOutputTokens - baseline.ReasoningOutputTokens;
        long total = TotalTokens - baseline.TotalTokens;

        return input >= 0
            && cachedInput >= 0
            && output >= 0
            && reasoningOutput >= 0
            && total >= 0
            ? new TokenUsage(input, cachedInput, output, reasoningOutput, total)
            : null;
    }
}
