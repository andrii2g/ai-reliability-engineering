using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Runs;
using AiReliabilityEngineering.Core.Steps;
using AiReliabilityEngineering.Orchestration.Logging;
using AiReliabilityEngineering.Orchestration.Pipeline;
using AiReliabilityEngineering.Orchestration.State;

namespace AiReliabilityEngineering.Orchestration.Tests;

public sealed class AgentPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAgentFails_StopsPipelineAndReturnsFailure()
    {
        using var workspace = PipelineWorkspace.Create();
        var stateStore = new InMemoryRunStateStore();
        var logger = new InMemoryRunLogger();
        var pipeline = new AgentPipeline(
            [
                new AgentPipelineStep(WorkflowStep.Requirements, new SuccessfulAgent("FirstAgent")),
                new AgentPipelineStep(WorkflowStep.Documentation, new FailingAgent("FailingAgent")),
                new AgentPipelineStep(WorkflowStep.Planning, new SuccessfulAgent("NeverRunsAgent"))
            ],
            stateStore,
            logger);
        var initialState = new RunState(workspace.RunContext.Id.Value, RunStatus.Running, workspace.RunContext.CreatedAtUtc, workspace.RunContext.CreatedAtUtc, [], null);

        var result = await pipeline.ExecuteAsync(workspace.RunContext, initialState, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(RunStatus.Failed, result.FinalState.Status);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal("FailingAgent", result.Steps[^1].AgentName);
        Assert.Equal(WorkflowStepStatus.Failed, result.Steps[^1].Status);
    }

    [Fact]
    public async Task ExecuteAsync_FinalizeStepUsesFinalizationCompletedBeforeCompleted()
    {
        using var workspace = PipelineWorkspace.Create();
        var stateStore = new InMemoryRunStateStore();
        var pipeline = new AgentPipeline(
            [
                new AgentPipelineStep(WorkflowStep.Review, new SuccessfulAgent("ReviewAgent")),
                new AgentPipelineStep(WorkflowStep.Finalize, new SuccessfulAgent("FinalizeAgent"))
            ],
            stateStore,
            new InMemoryRunLogger());
        var initialState = new RunState(workspace.RunContext.Id.Value, RunStatus.TestingCompleted, workspace.RunContext.CreatedAtUtc, workspace.RunContext.CreatedAtUtc, [], null);

        var result = await pipeline.ExecuteAsync(workspace.RunContext, initialState, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(RunStatus.Completed, result.FinalState.Status);
        Assert.Contains(stateStore.SavedStates, state => state.Status == RunStatus.FinalizationCompleted);
    }

    private sealed class SuccessfulAgent(string name) : IAgent
    {
        public string Name { get; } = name;

        public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
            => Task.FromResult(AgentResult.Success("ok"));
    }

    private sealed class FailingAgent(string name) : IAgent
    {
        public string Name { get; } = name;

        public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
            => Task.FromResult(AgentResult.Failure("failed"));
    }

    private sealed class InMemoryRunStateStore : IRunStateStore
    {
        public List<RunState> SavedStates { get; } = [];

        public Task SaveAsync(RunState state, CancellationToken cancellationToken)
        {
            SavedStates.Add(state);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRunLogger : IRunLogger
    {
        public Task InfoAsync(string message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ErrorAsync(string message, Exception? exception, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class PipelineWorkspace : IDisposable
    {
        private PipelineWorkspace(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            var runPaths = new RunPaths(
                rootDirectory,
                Path.Combine(rootDirectory, "input"),
                Path.Combine(rootDirectory, "workspace"),
                Path.Combine(rootDirectory, "artifacts"),
                Path.Combine(rootDirectory, "reports"),
                Path.Combine(rootDirectory, "logs"),
                Path.Combine(rootDirectory, "run-state.json"));
            Directory.CreateDirectory(runPaths.InputDirectory);
            Directory.CreateDirectory(runPaths.WorkspaceDirectory);
            Directory.CreateDirectory(runPaths.ArtifactsDirectory);
            Directory.CreateDirectory(runPaths.ReportsDirectory);
            Directory.CreateDirectory(runPaths.LogsDirectory);
            RunContext = new RunContext(
                new RunId("test-run"),
                Path.Combine(rootDirectory, "idea.md"),
                Path.Combine(runPaths.InputDirectory, "idea.md"),
                runPaths,
                DateTimeOffset.UtcNow);
        }

        public string RootDirectory { get; }

        public RunContext RunContext { get; }

        public static PipelineWorkspace Create()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "aire-pipeline-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return new PipelineWorkspace(rootDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
