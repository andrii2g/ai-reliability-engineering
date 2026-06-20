using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Core.Steps;
using AiReliabilityEngineering.Orchestration.Logging;
using AiReliabilityEngineering.Orchestration.State;

namespace AiReliabilityEngineering.Orchestration.Pipeline;

public sealed class AgentPipeline
{
    private readonly IReadOnlyList<AgentPipelineStep> _steps;
    private readonly IRunStateStore _stateStore;
    private readonly IRunLogger _logger;
    private readonly TimeProvider _timeProvider;

    public AgentPipeline(
        IReadOnlyList<AgentPipelineStep> steps,
        IRunStateStore stateStore,
        IRunLogger logger,
        TimeProvider? timeProvider = null)
    {
        _steps = steps.Count == 0 ? throw new ArgumentException("Pipeline requires at least one step.", nameof(steps)) : steps;
        _stateStore = stateStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IReadOnlyList<AgentPipelineStep> Steps => _steps;

    public async Task<AgentPipelineResult> ExecuteAsync(
        RunContext runContext,
        RunState initialState,
        CancellationToken cancellationToken)
    {
        var results = new List<WorkflowStepResult>(initialState.Steps);
        var state = initialState;

        foreach (var step in _steps)
        {
            var startedAt = _timeProvider.GetUtcNow();
            var runningResult = new WorkflowStepResult(
                step.Step,
                step.Agent.Name,
                WorkflowStepStatus.Running,
                startedAt,
                null,
                "Running",
                []);

            results.Add(runningResult);
            state = state with
            {
                Status = StepStartedStatus(step.Step),
                UpdatedAtUtc = startedAt,
                Steps = results.ToArray()
            };
            await _stateStore.SaveAsync(state, cancellationToken);
            await _logger.InfoAsync($"StepStarted {step.Step}", cancellationToken);

            WorkflowStepResult completedResult;

            try
            {
                var agentResult = await step.Agent.ExecuteAsync(
                    new AgentContext(runContext, new Dictionary<string, string>()),
                    cancellationToken);
                var finishedAt = _timeProvider.GetUtcNow();

                completedResult = runningResult with
                {
                    Status = agentResult.IsSuccess ? WorkflowStepStatus.Succeeded : WorkflowStepStatus.Failed,
                    FinishedAtUtc = finishedAt,
                    Message = agentResult.Message,
                    Artifacts = agentResult.Artifacts
                };

                results[^1] = completedResult;

                if (!agentResult.IsSuccess)
                {
                    state = state with
                    {
                        Status = RunStatus.Failed,
                        UpdatedAtUtc = finishedAt,
                        Steps = results.ToArray(),
                        FailureMessage = agentResult.Message
                    };
                    await _stateStore.SaveAsync(state, cancellationToken);
                    await _logger.InfoAsync($"StepFailed {step.Step}", cancellationToken);
                    return new AgentPipelineResult(false, state, results, agentResult.Message);
                }

                state = state with
                {
                    Status = StepCompletedStatus(step.Step),
                    UpdatedAtUtc = finishedAt,
                    Steps = results.ToArray()
                };
                await _stateStore.SaveAsync(state, cancellationToken);
                await _logger.InfoAsync($"StepCompleted {step.Step}", cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var finishedAt = _timeProvider.GetUtcNow();
                completedResult = runningResult with
                {
                    Status = WorkflowStepStatus.Failed,
                    FinishedAtUtc = finishedAt,
                    Message = exception.Message
                };
                results[^1] = completedResult;
                state = state with
                {
                    Status = RunStatus.Failed,
                    UpdatedAtUtc = finishedAt,
                    Steps = results.ToArray(),
                    FailureMessage = exception.Message
                };
                await _logger.ErrorAsync($"StepFailed {step.Step}", exception, cancellationToken);
                await _stateStore.SaveAsync(state, cancellationToken);
                return new AgentPipelineResult(false, state, results, exception.Message);
            }
        }

        var completedAt = _timeProvider.GetUtcNow();
        state = state with
        {
            Status = RunStatus.Completed,
            UpdatedAtUtc = completedAt,
            Steps = results.ToArray(),
            FailureMessage = null
        };
        await _stateStore.SaveAsync(state, cancellationToken);
        return new AgentPipelineResult(true, state, results, "Run completed.");
    }

    private static RunStatus StepStartedStatus(WorkflowStep step) => step switch
    {
        WorkflowStep.Requirements => RunStatus.Running,
        WorkflowStep.Documentation => RunStatus.RequirementsCompleted,
        WorkflowStep.Planning => RunStatus.DocumentationCompleted,
        WorkflowStep.Code => RunStatus.PlanningCompleted,
        WorkflowStep.Testing => RunStatus.CodeCompleted,
        WorkflowStep.Review => RunStatus.TestingCompleted,
        _ => RunStatus.Running
    };

    private static RunStatus StepCompletedStatus(WorkflowStep step) => step switch
    {
        WorkflowStep.Requirements => RunStatus.RequirementsCompleted,
        WorkflowStep.Documentation => RunStatus.DocumentationCompleted,
        WorkflowStep.Planning => RunStatus.PlanningCompleted,
        WorkflowStep.Code => RunStatus.CodeCompleted,
        WorkflowStep.Testing => RunStatus.TestingCompleted,
        WorkflowStep.Review => RunStatus.ReviewCompleted,
        _ => RunStatus.Running
    };
}
