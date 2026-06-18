using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Steps;

namespace AiReliabilityEngineering.Orchestration.Pipeline;

public sealed record AgentPipelineStep(
    WorkflowStep Step,
    IAgent Agent);
