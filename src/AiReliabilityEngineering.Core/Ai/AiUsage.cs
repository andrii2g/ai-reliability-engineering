namespace AiReliabilityEngineering.Core.Ai;

public sealed record AiUsage(
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens);
