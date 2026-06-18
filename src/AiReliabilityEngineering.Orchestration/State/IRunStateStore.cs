using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Orchestration.State;

public interface IRunStateStore
{
    Task SaveAsync(RunState state, CancellationToken cancellationToken);
}
