using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Core.Steps;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;
using AiReliabilityEngineering.Orchestration.Pipeline;
using AiReliabilityEngineering.Orchestration.RunManagement;
using AiReliabilityEngineering.Orchestration.State;

namespace AiReliabilityEngineering.Orchestration;

public sealed class AireOrchestrator
{
    private readonly Func<RunContext, IRunLogger> _loggerFactory;
    private readonly Func<RunContext, IRunStateStore> _stateStoreFactory;
    private readonly TimeProvider _timeProvider;

    public AireOrchestrator(
        Func<RunContext, IRunLogger> loggerFactory,
        Func<RunContext, IRunStateStore> stateStoreFactory,
        TimeProvider? timeProvider = null)
    {
        _loggerFactory = loggerFactory;
        _stateStoreFactory = stateStoreFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<RunResult> RunAsync(RunRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdeaFilePath))
        {
            return new RunResult(false, null, null, "Idea file path is required.");
        }

        if (!File.Exists(request.IdeaFilePath))
        {
            return new RunResult(false, null, null, $"Idea file not found: {request.IdeaFilePath}");
        }

        if (string.IsNullOrWhiteSpace(request.RunsDirectory))
        {
            return new RunResult(false, null, null, "Runs directory is required.");
        }

        RunContext runContext;

        try
        {
            var runFactory = new RunDirectoryFactory(request.RunsDirectory, _timeProvider);
            runContext = await runFactory.CreateAsync(request.IdeaFilePath, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new RunResult(false, null, null, $"Status: Failed - {exception.Message}");
        }

        var logger = _loggerFactory(runContext);
        var stateStore = _stateStoreFactory(runContext);

        try
        {
            await logger.InfoAsync("RunStarted", cancellationToken);
            var now = _timeProvider.GetUtcNow();
            var state = new RunState(
                runContext.Id.Value,
                RunStatus.Created,
                runContext.CreatedAtUtc,
                now,
                [],
                null);
            await stateStore.SaveAsync(state, cancellationToken);

            state = state with { Status = RunStatus.Running, UpdatedAtUtc = _timeProvider.GetUtcNow() };
            await stateStore.SaveAsync(state, cancellationToken);

            var pipeline = CreatePipeline(logger, stateStore);
            var result = await pipeline.ExecuteAsync(runContext, state, cancellationToken);
            var message = result.Succeeded ? "Status: Completed" : $"Status: Failed - {result.Message}";
            await logger.InfoAsync(result.Succeeded ? "RunCompleted" : "RunFailed", cancellationToken);
            return new RunResult(result.Succeeded, runContext.Id.Value, runContext.Paths.RootDirectory, message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await logger.ErrorAsync("RunFailed", exception, cancellationToken);
            var failedState = new RunState(
                runContext.Id.Value,
                RunStatus.Failed,
                runContext.CreatedAtUtc,
                _timeProvider.GetUtcNow(),
                [],
                exception.Message);
            await stateStore.SaveAsync(failedState, cancellationToken);
            return new RunResult(false, runContext.Id.Value, runContext.Paths.RootDirectory, $"Status: Failed - {exception.Message}");
        }
    }

    private AgentPipeline CreatePipeline(IRunLogger logger, IRunStateStore stateStore)
    {
        var steps = new AgentPipelineStep[]
        {
            new(WorkflowStep.Requirements, new FakeRequirementsAgent(logger)),
            new(WorkflowStep.Documentation, new FakeDocumentationAgent(logger)),
            new(WorkflowStep.Planning, new FakePlannerAgent(logger)),
            new(WorkflowStep.Code, new FakeCodeAgent(logger)),
            new(WorkflowStep.Testing, new FakeTestAgent(logger)),
            new(WorkflowStep.Review, new FakeReviewerAgent(logger))
        };

        return new AgentPipeline(steps, stateStore, logger, _timeProvider);
    }
}
