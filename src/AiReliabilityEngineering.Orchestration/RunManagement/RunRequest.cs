namespace AiReliabilityEngineering.Orchestration.RunManagement;

public sealed record RunRequest(
    string IdeaFilePath,
    string RunsDirectory);
