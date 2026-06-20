using AiReliabilityEngineering.Core.Artifacts;

namespace AiReliabilityEngineering.Core.Steps;

public enum WorkflowStep
{
    Requirements = 0,
    Documentation = 1,
    Planning = 2,
    Code = 3,
    Testing = 4,
    Review = 5,
    Finalize = 6
}

public enum WorkflowStepStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4
}

public sealed record WorkflowStepResult(
    WorkflowStep Step,
    string AgentName,
    WorkflowStepStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string Message,
    IReadOnlyList<ArtifactRef> Artifacts);
