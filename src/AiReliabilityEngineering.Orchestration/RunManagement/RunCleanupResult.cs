namespace AiReliabilityEngineering.Orchestration.RunManagement;

public sealed record RunCleanupResult(
    bool Succeeded,
    string RunsDirectory,
    int DeletedEntries,
    string Message);
