using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Steps;
using AiReliabilityEngineering.Core.Tools;
using AiReliabilityEngineering.Core.Workflow;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;
using AiReliabilityEngineering.Orchestration.State;

namespace AiReliabilityEngineering.Orchestration.Pipeline;

public sealed class AgentPipelineFactory
{
    private readonly Func<AiProviderSelection, IAiProvider> _aiProviderFactory;
    private readonly Func<IToolExecutor> _toolExecutorFactory;
    private readonly TimeProvider _timeProvider;

    public AgentPipelineFactory(
        Func<AiProviderSelection, IAiProvider> aiProviderFactory,
        Func<IToolExecutor>? toolExecutorFactory = null,
        TimeProvider? timeProvider = null)
    {
        _aiProviderFactory = aiProviderFactory ?? throw new ArgumentNullException(nameof(aiProviderFactory));
        _toolExecutorFactory = toolExecutorFactory ?? (() => new MissingToolExecutor());
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public AgentPipeline Create(
        WorkflowProfile profile,
        AiProviderSelection aiProviderSelection,
        IRunLogger logger,
        IRunStateStore stateStore)
    {
        ArgumentNullException.ThrowIfNull(aiProviderSelection);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(stateStore);

        var steps = profile switch
        {
            WorkflowProfile.Fake => CreateFakeSteps(logger),
            WorkflowProfile.AiRequirements => CreateAiRequirementsSteps(aiProviderSelection, logger),
            WorkflowProfile.AiDemo => CreateAiDemoSteps(aiProviderSelection, logger),
            WorkflowProfile.AiDemoDotnet => CreateAiDemoDotnetSteps(aiProviderSelection, logger),
            WorkflowProfile.AiDemoDotnetReview => CreateAiDemoDotnetReviewSteps(aiProviderSelection, logger),
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

    private IReadOnlyList<AgentPipelineStep> CreateAiRequirementsSteps(
        AiProviderSelection aiProviderSelection,
        IRunLogger logger)
        =>
        [
            new(WorkflowStep.Requirements, new AiRequirementsAgent(_aiProviderFactory(aiProviderSelection), logger)),
            new(WorkflowStep.Documentation, new FakeDocumentationAgent(logger)),
            new(WorkflowStep.Planning, new FakePlannerAgent(logger)),
            new(WorkflowStep.Code, new FakeCodeAgent(logger)),
            new(WorkflowStep.Testing, new FakeTestAgent(logger)),
            new(WorkflowStep.Review, new FakeReviewerAgent(logger))
        ];

    private IReadOnlyList<AgentPipelineStep> CreateAiDemoSteps(
        AiProviderSelection aiProviderSelection,
        IRunLogger logger)
    {
        var aiProvider = _aiProviderFactory(aiProviderSelection);
        return
        [
            new(WorkflowStep.Requirements, new AiRequirementsAgent(aiProvider, logger)),
            new(WorkflowStep.Documentation, new AiDocumentationAgent(aiProvider, logger)),
            new(WorkflowStep.Planning, new AiPlannerAgent(aiProvider, logger)),
            new(WorkflowStep.Code, new FakeCodeAgent(logger)),
            new(WorkflowStep.Testing, new FakeTestAgent(logger)),
            new(WorkflowStep.Review, new FakeReviewerAgent(logger))
        ];
    }

    private IReadOnlyList<AgentPipelineStep> CreateAiDemoDotnetReviewSteps(
        AiProviderSelection aiProviderSelection,
        IRunLogger logger)
    {
        var aiProvider = _aiProviderFactory(aiProviderSelection);
        return
        [
            new(WorkflowStep.Requirements, new AiRequirementsAgent(aiProvider, logger)),
            new(WorkflowStep.Documentation, new AiDocumentationAgent(aiProvider, logger)),
            new(WorkflowStep.Planning, new AiPlannerAgent(aiProvider, logger)),
            new(WorkflowStep.Code, new TemplateCodeAgent(logger)),
            new(WorkflowStep.Testing, new BuildTestAgent(_toolExecutorFactory(), logger)),
            new(WorkflowStep.Review, new ArtifactReviewAgent(logger))
        ];
    }

    private IReadOnlyList<AgentPipelineStep> CreateAiDemoDotnetSteps(
        AiProviderSelection aiProviderSelection,
        IRunLogger logger)
    {
        var aiProvider = _aiProviderFactory(aiProviderSelection);
        return
        [
            new(WorkflowStep.Requirements, new AiRequirementsAgent(aiProvider, logger)),
            new(WorkflowStep.Documentation, new AiDocumentationAgent(aiProvider, logger)),
            new(WorkflowStep.Planning, new AiPlannerAgent(aiProvider, logger)),
            new(WorkflowStep.Code, new TemplateCodeAgent(logger)),
            new(WorkflowStep.Testing, new BuildTestAgent(_toolExecutorFactory(), logger)),
            new(WorkflowStep.Review, new FakeReviewerAgent(logger))
        ];
    }

    private sealed class MissingToolExecutor : IToolExecutor
    {
        public Task<ToolExecutionResult> ExecuteAsync(
            ToolExecutionRequest request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("A tool executor must be configured for build/test workflow profiles.");
        }
    }
}
