using AiReliabilityEngineering.Core.Ai;
using AiReliabilityEngineering.Infrastructure.Ai;
using AiReliabilityEngineering.Infrastructure.Logging;
using AiReliabilityEngineering.Infrastructure.Serialization;
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

    public static IAiProvider CreateAiProvider()
    {
        var factory = new AiProviderFactory();
        return factory.Create(AiProviderFactoryOptions.Default);
    }

    public static AiRequirementsAgent CreateAiRequirementsAgent(IRunLogger logger)
        => new(CreateAiProvider(), logger);

    public static AgentPipelineFactory CreateAgentPipelineFactory()
        => new(_ => CreateAiProvider());
}
