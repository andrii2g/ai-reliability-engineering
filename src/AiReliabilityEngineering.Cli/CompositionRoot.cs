using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Core.CodeExecution;
using AiReliabilityEngineering.Infrastructure.Ai;
using AiReliabilityEngineering.Infrastructure.CodeExecution;
using AiReliabilityEngineering.Infrastructure.Logging;
using AiReliabilityEngineering.Infrastructure.Serialization;
using AiReliabilityEngineering.Infrastructure.Tools;
using AiReliabilityEngineering.Orchestration;
using AiReliabilityEngineering.Orchestration.Agents;
using AiReliabilityEngineering.Orchestration.Logging;
using AiReliabilityEngineering.Orchestration.Pipeline;
using AiReliabilityEngineering.Orchestration.RunManagement;

namespace AiReliabilityEngineering.Cli;

public static class CompositionRoot
{
    public static AireOrchestrator CreateOrchestrator()
        => new(
            runContext =>
            {
                var logFilePath = Path.Combine(runContext.Paths.LogsDirectory, "orchestrator.log");
                return new CompositeRunLogger(
                    [
                        new ConsoleRunLogger(),
                        new FileRunLogger(logFilePath)
                    ]);
            },
            runContext => new JsonRunStateStore(runContext.Paths.StateFilePath),
            CreateAgentPipelineFactory());

    public static RunCleanupService CreateRunCleanupService() => new();

    public static IAiProvider CreateAiProvider(AiProviderSelection? selection = null)
    {
        var factory = new AiProviderFactory();
        return factory.Create(new AiProviderFactoryOptions(selection ?? AiProviderSelection.DefaultFake));
    }

    public static AiRequirementsAgent CreateAiRequirementsAgent(IRunLogger logger)
        => new(CreateAiProvider(AiProviderSelection.DefaultFake), logger);

    public static AgentPipelineFactory CreateAgentPipelineFactory()
        => new(CreateAiProvider, CreateCodeExecutor, () => new ShellToolExecutor());

    public static ICodeExecutor CreateCodeExecutor(CodeExecutorSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return selection.Kind switch
        {
            CodeExecutorKind.Fake => new FakeCodeExecutor(),
            CodeExecutorKind.OpenCode => new OpenCodeExecutor(new ShellToolExecutor()),
            CodeExecutorKind.Codex => new CodexExecutor(new ShellToolExecutor()),
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection.Kind, null)
        };
    }
}
