using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class TemplateCodeAgent : IAgent
{
    private readonly IRunLogger _logger;
    private readonly WorkspaceArtifactReader _artifactReader;
    private readonly DotnetTemplateProjectWriter _projectWriter;

    public TemplateCodeAgent(
        IRunLogger logger,
        WorkspaceArtifactReader? artifactReader = null,
        DotnetTemplateProjectWriter? projectWriter = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artifactReader = artifactReader ?? new WorkspaceArtifactReader();
        _projectWriter = projectWriter ?? new DotnetTemplateProjectWriter();
    }

    public string Name => "TemplateCodeAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _logger.InfoAsync("TemplateCodeAgentStarted", cancellationToken);
            var specificationJson = await _artifactReader.ReadSpecificationJsonAsync(context.Run, cancellationToken);
            var tasksJson = await _artifactReader.TryReadTasksJsonAsync(context.Run, cancellationToken);
            var artifacts = await _projectWriter.WriteAsync(context.Run, specificationJson, tasksJson, cancellationToken);
            await _logger.InfoAsync("TemplateCodeAgentCompleted", cancellationToken);

            return AgentResult.Success("Generated deterministic .NET workspace project.", artifacts);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync("TemplateCodeAgentFailed", exception, cancellationToken);
            return AgentResult.Failure(exception.Message);
        }
    }
}
