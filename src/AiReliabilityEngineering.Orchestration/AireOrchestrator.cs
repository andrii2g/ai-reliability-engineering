using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Orchestration.Logging;
using AiReliabilityEngineering.Orchestration.Pipeline;
using AiReliabilityEngineering.Orchestration.RunManagement;
using AiReliabilityEngineering.Orchestration.State;

namespace AiReliabilityEngineering.Orchestration;

public sealed class AireOrchestrator
{
    private readonly Func<RunContext, IRunLogger> _loggerFactory;
    private readonly Func<RunContext, IRunStateStore> _stateStoreFactory;
    private readonly AgentPipelineFactory _pipelineFactory;
    private readonly TimeProvider _timeProvider;

    public AireOrchestrator(
        Func<RunContext, IRunLogger> loggerFactory,
        Func<RunContext, IRunStateStore> stateStoreFactory,
        AgentPipelineFactory? pipelineFactory = null,
        TimeProvider? timeProvider = null)
    {
        _loggerFactory = loggerFactory;
        _stateStoreFactory = stateStoreFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _pipelineFactory = pipelineFactory ?? new AgentPipelineFactory(_ => new MissingAiProvider(), timeProvider: _timeProvider);
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

            var pipeline = _pipelineFactory.Create(request.Profile, request.EffectiveAiProvider, logger, stateStore);
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

    private sealed class MissingAiProvider : IAiProvider
    {
        public string Name => "missing";

        public Task<AiResponse> GenerateAsync(
            AiRequest request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("An AI provider must be configured for AI workflow profiles.");
        }
    }
}
