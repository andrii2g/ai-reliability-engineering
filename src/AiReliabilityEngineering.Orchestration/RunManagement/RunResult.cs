namespace AiReliabilityEngineering.Orchestration.RunManagement;

public sealed record RunResult(
    bool Succeeded,
    string? RunId,
    string? RunDirectory,
    string Message);
