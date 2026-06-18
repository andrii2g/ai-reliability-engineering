using AiReliabilityEngineering.Core.Artifacts;
using AiReliabilityEngineering.Core.Runs;

namespace AiReliabilityEngineering.Core.Agents;

public interface IAgent
{
    string Name { get; }

    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken);
}

public sealed record AgentContext(
    RunContext Run,
    IReadOnlyDictionary<string, string> Properties);

public sealed record AgentResult(
    AgentStatus Status,
    string Message,
    IReadOnlyList<ArtifactRef> Artifacts)
{
    public bool IsSuccess => Status == AgentStatus.Succeeded;

    public static AgentResult Success(string message, IReadOnlyList<ArtifactRef>? artifacts = null)
        => new(AgentStatus.Succeeded, message, artifacts ?? Array.Empty<ArtifactRef>());

    public static AgentResult Failure(string message, IReadOnlyList<ArtifactRef>? artifacts = null)
        => new(AgentStatus.Failed, message, artifacts ?? Array.Empty<ArtifactRef>());
}

public enum AgentStatus
{
    Succeeded = 0,
    Failed = 1,
    Skipped = 2
}
