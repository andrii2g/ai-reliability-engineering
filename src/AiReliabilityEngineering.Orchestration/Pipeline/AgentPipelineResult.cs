using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Core.Steps;

namespace AiReliabilityEngineering.Orchestration.Pipeline;

public sealed record AgentPipelineResult(
    bool Succeeded,
    RunState FinalState,
    IReadOnlyList<WorkflowStepResult> Steps,
    string Message);
