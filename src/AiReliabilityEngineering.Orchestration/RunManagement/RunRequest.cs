using AiReliabilityEngineering.Core.Workflow;

namespace AiReliabilityEngineering.Orchestration.RunManagement;

public sealed record RunRequest(
    string IdeaFilePath,
    string RunsDirectory,
    WorkflowProfile Profile = WorkflowProfile.Fake);
