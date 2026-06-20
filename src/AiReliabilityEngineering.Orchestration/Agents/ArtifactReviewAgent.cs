using AiReliabilityEngineering.Core.Agents;
using AiReliabilityEngineering.Core.Review;
using AiReliabilityEngineering.Orchestration.Logging;

namespace AiReliabilityEngineering.Orchestration.Agents;

public sealed class ArtifactReviewAgent : IAgent
{
    private readonly IRunLogger _logger;
    private readonly RequiredArtifactChecker _artifactChecker;
    private readonly WorkspaceSummaryBuilder _workspaceSummaryBuilder;
    private readonly ReviewReportWriter _reportWriter;

    public ArtifactReviewAgent(
        IRunLogger logger,
        RequiredArtifactChecker? artifactChecker = null,
        WorkspaceSummaryBuilder? workspaceSummaryBuilder = null,
        ReviewReportWriter? reportWriter = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artifactChecker = artifactChecker ?? new RequiredArtifactChecker();
        _workspaceSummaryBuilder = workspaceSummaryBuilder ?? new WorkspaceSummaryBuilder();
        _reportWriter = reportWriter ?? new ReviewReportWriter();
    }

    public string Name => "ArtifactReviewAgent";

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _logger.InfoAsync("ArtifactReviewAgentStarted", cancellationToken);
            var checks = _artifactChecker.Check(context.Run);
            var workspaceSummary = _workspaceSummaryBuilder.Build(context.Run);
            var warnings = checks
                .Where(check => !check.Exists)
                .Select(check => $"Missing required {check.Category} file: {check.RelativePath}")
                .ToList();

            if (workspaceSummary.Files.Count == 0)
            {
                warnings.Add("Workspace contains no generated files.");
            }

            var result = new ArtifactReviewResult(checks, workspaceSummary, warnings);
            var artifacts = await _reportWriter.WriteAsync(result, context.Run, cancellationToken);
            await _logger.InfoAsync("ArtifactReviewAgentCompleted", cancellationToken);

            return AgentResult.Success("Deterministic artifact review completed.", artifacts);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync("ArtifactReviewAgentFailed", exception, cancellationToken);
            return AgentResult.Failure(exception.Message);
        }
    }
}
