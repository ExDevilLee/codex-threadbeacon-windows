namespace ThreadBeacon.Core.Models;

public sealed record SubagentRecord(
    string Id,
    string ParentId,
    string Title,
    string RolloutPath,
    DateTimeOffset UpdatedAt,
    long TokensUsed,
    string? AgentNickname,
    string? AgentRole,
    string? Model,
    string? ReasoningEffort,
    string? AgentPath = null);
