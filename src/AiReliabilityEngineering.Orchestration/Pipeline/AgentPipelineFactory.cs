using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Steps;
using AiReliabilityEngineering.Core.Workflow;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;
using AiReliabilityEngineering.Orchestration.State;

namespace AiReliabilityEngineering.Orchestration.Pipeline;

public sealed class AgentPipelineFactory
{
    private readonly Func<IRunLogger, IAiProvider> _aiProviderFactory;
    private readonly TimeProvider _timeProvider;

    public AgentPipelineFactory(
        Func<IRunLogger, IAiProvider> aiProviderFactory,
        TimeProvider? timeProvider = null)
    {
        _aiProviderFactory = aiProviderFactory ?? throw new ArgumentNullException(nameof(aiProviderFactory));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public AgentPipeline Create(
        WorkflowProfile profile,
        IRunLogger logger,
        IRunStateStore stateStore)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(stateStore);

        var steps = profile switch
        {
            WorkflowProfile.Fake => CreateFakeSteps(logger),
            WorkflowProfile.AiRequirements => CreateAiRequirementsSteps(logger),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };

        return new AgentPipeline(steps, stateStore, logger, _timeProvider);
    }

    private static IReadOnlyList<AgentPipelineStep> CreateFakeSteps(IRunLogger logger)
        =>
        [
            new(WorkflowStep.Requirements, new FakeRequirementsAgent(logger)),
            new(WorkflowStep.Documentation, new FakeDocumentationAgent(logger)),
            new(WorkflowStep.Planning, new FakePlannerAgent(logger)),
            new(WorkflowStep.Code, new FakeCodeAgent(logger)),
            new(WorkflowStep.Testing, new FakeTestAgent(logger)),
            new(WorkflowStep.Review, new FakeReviewerAgent(logger))
        ];

    private IReadOnlyList<AgentPipelineStep> CreateAiRequirementsSteps(IRunLogger logger)
        =>
        [
            new(WorkflowStep.Requirements, new AiRequirementsAgent(_aiProviderFactory(logger), logger)),
            new(WorkflowStep.Documentation, new FakeDocumentationAgent(logger)),
            new(WorkflowStep.Planning, new FakePlannerAgent(logger)),
            new(WorkflowStep.Code, new FakeCodeAgent(logger)),
            new(WorkflowStep.Testing, new FakeTestAgent(logger)),
            new(WorkflowStep.Review, new FakeReviewerAgent(logger))
        ];
}
