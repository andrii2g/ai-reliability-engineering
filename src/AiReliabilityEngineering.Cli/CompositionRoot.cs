using AiReliabilityEngineering.Infrastructure.Logging;
using AiReliabilityEngineering.Infrastructure.Serialization;
using AiReliabilityEngineering.Orchestration;
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
            runContext => new JsonRunStateStore(runContext.Paths.StateFilePath));

    public static RunCleanupService CreateRunCleanupService() => new();
}
